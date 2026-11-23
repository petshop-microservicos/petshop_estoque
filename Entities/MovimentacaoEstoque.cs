namespace Estoque.Entities;

public class MovimentacaoEstoque
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;
    public TipoMovimento Tipo { get; set; }
    public OrigemMovimentacao Origem { get; set; }
    /// <summary>Id da Compra/Venda no serviço CompraVendas. Nulo quando Origem = Manual.</summary>
    public int? OrigemId { get; set; }
    public int Quantidade { get; set; }
    /// <summary>Saldo do produto imediatamente após esta movimentação (histórico auditável).</summary>
    public int SaldoApos { get; set; }
    public DateTime Data { get; set; } = DateTime.UtcNow;
    public string? Observacao { get; set; }
}
