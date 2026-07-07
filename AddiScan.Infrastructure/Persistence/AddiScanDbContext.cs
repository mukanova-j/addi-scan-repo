using AddiScan.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AddiScan.Infrastructure.Persistence;

public class AddiScanDbContext(DbContextOptions<AddiScanDbContext> options) : DbContext(options)
{
    public DbSet<Additive> Additives => Set<Additive>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Additive>(additive =>
        {
            additive.OwnsOne(a => a.Grading);
            additive.PrimitiveCollection(a => a.CommonNamesAndSynonyms);
            additive.PrimitiveCollection(a => a.EvidenceSources);
        });

        modelBuilder.Entity<User>(user =>
        {
            user.HasIndex(u => u.Email).IsUnique();
        });
    }
}
