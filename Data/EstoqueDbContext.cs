using Estoque.Entities;
using Microsoft.EntityFrameworkCore;

namespace Estoque.Data;

public class EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<MovimentacaoEstoque> Movimentacoes => Set<MovimentacaoEstoque>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Produto>(e =>
        {
            e.Property(p => p.Nome).HasMaxLength(150).IsRequired();
            e.Property(p => p.Descricao).HasMaxLength(500);
            e.Property(p => p.PrecoVenda).HasPrecision(18, 2);
            // RN16 garantida também no banco, como última barreira.
            e.ToTable(t => t.HasCheckConstraint("CK_Produto_QuantidadeAtual", "\"QuantidadeAtual\" >= 0"));
            e.HasIndex(p => p.Nome);
        });

        mb.Entity<MovimentacaoEstoque>(e =>
        {
            e.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(10);
            e.Property(m => m.Origem).HasConversion<string>().HasMaxLength(10);
            e.Property(m => m.Observacao).HasMaxLength(500);
            // Produto com histórico não pode ser excluído.
            e.HasOne(m => m.Produto).WithMany().HasForeignKey(m => m.ProdutoId).OnDelete(DeleteBehavior.Restrict);
            // Idempotência: um lote (tipo, origem, origemId) só é aplicado uma vez por produto.
            e.HasIndex(m => new { m.Tipo, m.Origem, m.OrigemId, m.ProdutoId })
             .IsUnique().HasFilter("\"OrigemId\" IS NOT NULL");
            e.HasIndex(m => new { m.ProdutoId, m.Data });
        });
    }
}
