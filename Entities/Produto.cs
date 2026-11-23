namespace Estoque.Entities;

public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string? Descricao { get; set; }
    public decimal PrecoVenda { get; set; }
    /// <summary>Só é alterada por movimentações (nunca por PUT de produto). RN16: nunca negativa.</summary>
    public int QuantidadeAtual { get; set; }
}
