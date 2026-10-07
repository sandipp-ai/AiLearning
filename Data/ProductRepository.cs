using AiLearning.Console.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiLearning.Console.Data;

public class ProductRepository
{
    private readonly AiLearningDbContext _dbContext;

    public ProductRepository(
        AiLearningDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .OrderBy(x => x.PartNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByPartNumberAsync(
        string partNumber,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.PartNumber == partNumber,
                cancellationToken);
    }

    public async Task<int> GetActiveCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .CountAsync(
                x => x.IsActive,
                cancellationToken);
    }

    public async Task<List<Product>> GetOutOfStockAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.Stock == 0)
            .OrderBy(x => x.PartNumber)
            .ToListAsync(cancellationToken);
    }
}