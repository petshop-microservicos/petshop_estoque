using Estoque.Data;
using Estoque.DTOs;
using Estoque.Entities;
using Estoque.Infra;
using Microsoft.EntityFrameworkCore;

namespace Estoque.Services;

/// <summary>RF12 (gerenciar produtos) e parte do RF14 (quantidades disponíveis).</summary>
public class ProdutoService(EstoqueDbContext db)
{
    public async Task<List<ProdutoResponse>> ListarAsync(string? busca, bool somenteDisponiveis, CancellationToken ct)
    {
        var q = db.Produtos.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(busca)) q = q.Where(p => p.Nome.Contains(busca));
        if (somenteDisponiveis) q = q.Where(p => p.QuantidadeAtual > 0);
        var lista = await q.OrderBy(p => p.Nome).ToListAsync(ct);
        return lista.Select(ProdutoResponse.De).ToList();
    }

    public async Task<ProdutoResponse> ObterAsync(int id, CancellationToken ct) =>
        ProdutoResponse.De(await BuscarAsync(id, ct));

    /// <summary>Produto nasce com quantidade 0; o saldo inicial entra como movimentação manual (fica no histórico).</summary>
    public async Task<ProdutoResponse> CriarAsync(ProdutoRequest req, CancellationToken ct)
    {
        var p = new Produto { QuantidadeAtual = 0 };
        Aplicar(p, req);
        db.Produtos.Add(p);
        await db.SaveChangesAsync(ct);
        return ProdutoResponse.De(p);
    }

    public async Task<ProdutoResponse> AtualizarAsync(int id, ProdutoRequest req, CancellationToken ct)
    {
        var p = await BuscarAsync(id, ct);
        Aplicar(p, req);   // não toca em QuantidadeAtual
        await db.SaveChangesAsync(ct);
        return ProdutoResponse.De(p);
    }

    public async Task ExcluirAsync(int id, CancellationToken ct)
    {
        var p = await BuscarAsync(id, ct);
        if (await db.Movimentacoes.AnyAsync(m => m.ProdutoId == id, ct))
            throw new ConflitoException("Produto possui histórico de movimentações e não pode ser excluído.");
        db.Produtos.Remove(p);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Produto> BuscarAsync(int id, CancellationToken ct) =>
        await db.Produtos.FindAsync([id], ct)
        ?? throw new NaoEncontradoException($"Produto {id} não encontrado.");

    private static void Aplicar(Produto p, ProdutoRequest r)
    {
        p.Nome = r.Nome.Trim();
        p.Descricao = r.Descricao;
        p.PrecoVenda = r.Preco;
    }
}
