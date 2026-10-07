using AiLearning.Console.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiLearning.Console.Data;

public class DatabaseInitializer
{
    private readonly AiLearningDbContext _dbContext;

    public DatabaseInitializer(
        AiLearningDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Products.AnyAsync(
            cancellationToken))
        {
            return;
        }

        var products = new List<Product>
        {
            new()
            {
                PartNumber = "ABC-100",
                Name = "24 Position Connector",
                Price = 125.50m,
                Stock = 15,
                IsActive = true
            },
            new()
            {
                PartNumber = "ABC-200",
                Name = "12 Position Connector",
                Price = 89.99m,
                Stock = 0,
                IsActive = true
            },
            new()
            {
                PartNumber = "TERM-300",
                Name = "Female Terminal",
                Price = 12.75m,
                Stock = 250,
                IsActive = true
            },
            new()
            {
                PartNumber = "SEAL-400",
                Name = "Wire Seal",
                Price = 4.25m,
                Stock = 0,
                IsActive = false
            }
        };

        await _dbContext.Products.AddRangeAsync(
            products,
            cancellationToken);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}