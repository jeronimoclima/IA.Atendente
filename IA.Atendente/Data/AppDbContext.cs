using IA.Atendente.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace IA.Atendente.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AiConfiguration> AiConfigurations => Set<AiConfiguration>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<SessaoAtendimento> SessoesAtendimento => Set<SessaoAtendimento>();
    public DbSet<Mensagem> Mensagens => Set<Mensagem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.NomeClinica).IsRequired().HasMaxLength(150);
            entity.Property(t => t.Documento).HasMaxLength(20);

            // Mapeamento das novas propriedades de localização e horário
            entity.Property(t => t.Endereco).HasMaxLength(250);
            entity.Property(t => t.HorarioFuncionamento).HasMaxLength(150);
        });

        modelBuilder.Entity<AiConfiguration>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasOne(a => a.Tenant)
                  .WithOne(t => t.AiConfiguration)
                  .HasForeignKey<AiConfiguration>(a => a.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(a => a.SystemPrompt).IsRequired();
            entity.Property(a => a.ModeloIa).HasMaxLength(50);
            entity.Property(a => a.Temperatura).HasColumnType("decimal(3,2)");
        });

        modelBuilder.Entity<Servico>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Descricao).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Preco).HasColumnType("decimal(18,2)");

            entity.HasOne(s => s.Tenant)
                  .WithMany(t => t.Servicos)
                  .HasForeignKey(s => s.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessaoAtendimento>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.IdentificadorCliente).IsRequired().HasMaxLength(50);

            entity.HasOne(s => s.Tenant)
                  .WithMany()
                  .HasForeignKey(s => s.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Mensagem>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Role).IsRequired().HasMaxLength(20);
            entity.Property(m => m.Conteudo).IsRequired();

            entity.HasOne(m => m.SessaoAtendimento)
                  .WithMany(s => s.Mensagens)
                  .HasForeignKey(m => m.SessaoAtendimentoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}