using AiLearning.Console.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiLearning.Console.Data;

public class AiLearningDbContext : DbContext
{
    public AiLearningDbContext(
        DbContextOptions<AiLearningDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products =>
        Set<Product>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(
            entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.PartNumber)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(x => x.PartNumber)
                    .IsUnique();

                entity.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Price)
                    .HasPrecision(18, 2);
            });
    }
}