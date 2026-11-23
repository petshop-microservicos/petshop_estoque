using System.ComponentModel.DataAnnotations;
using Estoque.Entities;

namespace Estoque.DTOs;

/// <summary>RF13: entrada/saída manual (origem sempre Manual).</summary>
public record MovimentacaoManualRequest(
    [Range(1, int.MaxValue)] int ProdutoId,
    TipoMovimento Tipo,
    [Range(1, int.MaxValue)] int Quantidade,
    [StringLength(500)] string? Observacao);

public record ItemLoteRequest(
    [Range(1, int.MaxValue)] int ProdutoId,
    [Range(1, int.MaxValue)] int Quantidade);

/// <summary>Usado pelo CompraVendas (RN17: origem Compra ou Venda). Atômico e idempotente.</summary>
public record LoteRequest(
    TipoMovimento Tipo,
    OrigemMovimentacao Origem,
    [Range(1, int.MaxValue)] int OrigemId,
    [Required, MinLength(1)] List<ItemLoteRequest> Itens);

/// <summary>RF14: filtros do histórico.</summary>
public record MovimentacaoFiltro(
    int? ProdutoId,
    TipoMovimento? Tipo,
    OrigemMovimentacao? Origem,
    DateTime? De,
    DateTime? Ate);

public record MovimentacaoResponse(
    int Id, int ProdutoId, string Produto, string Tipo, string Origem, int? OrigemId,
    int Quantidade, int SaldoApos, DateTime Data, string? Observacao)
{
    public static MovimentacaoResponse De(MovimentacaoEstoque m) => new(
        m.Id, m.ProdutoId, m.Produto.Nome, m.Tipo.ToString(), m.Origem.ToString(), m.OrigemId,
        m.Quantidade, m.SaldoApos, m.Data, m.Observacao);
}
