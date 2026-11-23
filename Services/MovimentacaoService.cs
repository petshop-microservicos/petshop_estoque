using Estoque.Data;
using Estoque.DTOs;
using Estoque.Entities;
using Estoque.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace Estoque.Services;

/// <summary>RF13 (entradas/saídas manuais), RF14 (histórico), RN16 e RN17.</summary>
public class MovimentacaoService(EstoqueDbContext db)
{
    public async Task<List<MovimentacaoResponse>> ListarAsync(MovimentacaoFiltro f, CancellationToken ct)
    {
        var q = db.Movimentacoes.AsNoTracking().Include(m => m.Produto).AsQueryable();
        if (f.ProdutoId is { } p) q = q.Where(m => m.ProdutoId == p);
        if (f.Tipo is { } t) q = q.Where(m => m.Tipo == t);
        if (f.Origem is { } o) q = q.Where(m => m.Origem == o);
        if (f.De is { } de) q = q.Where(m => m.Data >= de);
        if (f.Ate is { } ate) q = q.Where(m => m.Data <= ate);
        var lista = await q.OrderByDescending(m => m.Data).ThenByDescending(m => m.Id).ToListAsync(ct);
        return lista.Select(MovimentacaoResponse.De).ToList();
    }

    public async Task<MovimentacaoResponse> ObterAsync(int id, CancellationToken ct)
    {
        var m = await db.Movimentacoes.AsNoTracking().Include(x => x.Produto).FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NaoEncontradoException($"Movimentação {id} não encontrada.");
        return MovimentacaoResponse.De(m);
    }

    /// <summary>RF13: origem sempre Manual (RN17).</summary>
    public async Task<MovimentacaoResponse> RegistrarManualAsync(MovimentacaoManualRequest req, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var id = await AplicarAsync(req.ProdutoId, req.Tipo, req.Quantidade,
            OrigemMovimentacao.Manual, null, req.Observacao, ct);
        await tx.CommitAsync(ct);
        return await ObterAsync(id, ct);
    }

    /// <summary>
    /// Chamado pelo CompraVendas. ATÔMICO (todos os itens ou nenhum; RN16) e IDEMPOTENTE
    /// por (tipo, origem, origemId): reenviar o mesmo lote não altera o estoque de novo.
    /// Estornos usam o tipo oposto com a mesma origem/origemId (chave diferente, sem colisão).
    /// </summary>
    public async Task MovimentarLoteAsync(LoteRequest req, CancellationToken ct)
    {
        if (req.Origem == OrigemMovimentacao.Manual)
            throw new NegocioException("O lote aceita apenas origem Compra ou Venda.");

        if (await JaProcessadoAsync(req, ct)) return;

        var itens = req.Itens
            .GroupBy(i => i.ProdutoId)
            .Select(g => (ProdutoId: g.Key, Quantidade: g.Sum(x => x.Quantidade)))
            .OrderBy(i => i.ProdutoId)
            .ToList();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var (produtoId, quantidade) in itens)
                await AplicarAsync(produtoId, req.Tipo, quantidade, req.Origem, req.OrigemId, null, ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Duas requisições simultâneas com a mesma chave: o índice único barra a segunda.
            await tx.RollbackAsync(CancellationToken.None);
            if (await JaProcessadoAsync(req, CancellationToken.None)) return;
            throw;
        }
        // Qualquer outra exceção (404/409) sai sem commit: o Dispose da transação faz rollback.
    }

    private Task<bool> JaProcessadoAsync(LoteRequest req, CancellationToken ct) =>
        db.Movimentacoes.AnyAsync(m => m.Tipo == req.Tipo && m.Origem == req.Origem && m.OrigemId == req.OrigemId, ct);

    /// <summary>
    /// Aplica UMA movimentação dentro da transação do chamador.
    /// RN16: a saída usa UPDATE condicional (QuantidadeAtual >= quantidade); se não afetar linha,
    /// o saldo não é suficiente. Sem leitura-depois-escrita, logo sem corrida entre requisições.
    /// </summary>
    private async Task<int> AplicarAsync(int produtoId, TipoMovimento tipo, int quantidade,
        OrigemMovimentacao origem, int? origemId, string? observacao, CancellationToken ct)
    {
        int afetadas = tipo == TipoMovimento.Entrada
            ? await db.Produtos.Where(p => p.Id == produtoId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.QuantidadeAtual, p => p.QuantidadeAtual + quantidade), ct)
            : await db.Produtos.Where(p => p.Id == produtoId && p.QuantidadeAtual >= quantidade)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.QuantidadeAtual, p => p.QuantidadeAtual - quantidade), ct);

        var atual = await db.Produtos.AsNoTracking()
            .Where(p => p.Id == produtoId)
            .Select(p => new { p.Nome, p.QuantidadeAtual })
            .FirstOrDefaultAsync(ct);

        if (atual is null)
            throw new NaoEncontradoException($"Produto {produtoId} não encontrado.");
        if (afetadas == 0)
            throw new ConflitoException(
                $"Estoque insuficiente para '{atual.Nome}' (disponível {atual.QuantidadeAtual}, solicitado {quantidade}).");

        var mov = new MovimentacaoEstoque
        {
            ProdutoId = produtoId, Tipo = tipo, Origem = origem, OrigemId = origemId,
            Quantidade = quantidade, SaldoApos = atual.QuantidadeAtual,
            Data = DateTime.UtcNow, Observacao = observacao
        };
        db.Movimentacoes.Add(mov);
        await db.SaveChangesAsync(ct);
        return mov.Id;
    }
}
