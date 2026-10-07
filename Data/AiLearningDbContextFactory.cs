using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AiLearning.Console.Data;

public class AiLearningDbContextFactory
    : IDesignTimeDbContextFactory<AiLearningDbContext>
{
    public AiLearningDbContext CreateDbContext(
        string[] args)
    {
        var configuration =
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(
                    "appsettings.json",
                    optional: false)
                .Build();

        string connectionString =
            configuration.GetConnectionString(
                "AiLearningDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'AiLearningDatabase' not found.");

        var optionsBuilder =
            new DbContextOptionsBuilder<AiLearningDbContext>();

        optionsBuilder.UseSqlServer(connectionString);

        return new AiLearningDbContext(
            optionsBuilder.Options);
    }
}