using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AddiScan.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef` build the context at design time without running AddiScan.Api.</summary>
public class AddiScanDbContextFactory : IDesignTimeDbContextFactory<AddiScanDbContext>
{
    public AddiScanDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AddiScanDbContext>()
            .UseSqlite("Data Source=addiscan.db");

        return new AddiScanDbContext(optionsBuilder.Options);
    }
}
