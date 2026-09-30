using DimDim.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DimDim.Web.Data;

/// <summary>
/// Contexto do Entity Framework Core. Mapeia as duas tabelas do projeto:
/// Clientes (1) ---- (N) Transacoes
/// O DDL equivalente está em scripts/ddl-tabelas.sql.
/// </summary>
public class DimDimContext : DbContext
{
    public DimDimContext(DbContextOptions<DimDimContext> opcoes) : base(opcoes)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Transacao> Transacoes => Set<Transacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("Clientes");
            e.HasIndex(c => c.Email).IsUnique();
        });

        modelBuilder.Entity<Transacao>(e =>
        {
            e.ToTable("Transacoes");

            e.Property(t => t.Valor).HasColumnType("decimal(18,2)");

            // Enums gravados como texto (ex.: "Pagamento"), o que facilita a leitura no banco.
            e.Property(t => t.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

            // Relacionamento 1:N. Não permite excluir um cliente que tenha transações.
            e.HasOne(t => t.Cliente)
                .WithMany(c => c.Transacoes)
                .HasForeignKey(t => t.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
