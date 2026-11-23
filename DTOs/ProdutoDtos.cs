using System.ComponentModel.DataAnnotations;
using Estoque.Entities;

namespace Estoque.DTOs;

/// <summary>RF12. A quantidade não é editável aqui: só muda por movimentação.</summary>
public record ProdutoRequest(
    [Required, StringLength(150)] string Nome,
    [StringLength(500)] string? Descricao,
    [Range(0.01, double.MaxValue)] decimal Preco);

/// <summary>Formato também consumido pelo serviço CompraVendas (id, nome, preco, quantidadeAtual).</summary>
public record ProdutoResponse(int Id, string Nome, string? Descricao, decimal Preco, int QuantidadeAtual)
{
    public static ProdutoResponse De(Produto p) => new(p.Id, p.Nome, p.Descricao, p.PrecoVenda, p.QuantidadeAtual);
}
