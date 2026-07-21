using AddiScan.Core.Entities;
using AddiScan.Core.History;
using Microsoft.EntityFrameworkCore;

namespace AddiScan.Infrastructure.Persistence;

public class AddiScanDbContext(DbContextOptions<AddiScanDbContext> options) : DbContext(options)
{
    public DbSet<Additive> Additives => Set<Additive>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ScanRecord> ScanRecords => Set<ScanRecord>();

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

        modelBuilder.Entity<ScanRecord>(scan =>
        {
            scan.Property(s => s.ExtractedText).IsRequired();

            scan.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            scan.OwnsMany(s => s.Matches, match =>
            {
                match.ToTable("ScanRecordMatches");
                match.HasKey(m => m.Id);
                match.Property(m => m.Id).ValueGeneratedNever();
                match.Property(m => m.Kind).HasConversion<int>();
            });
        });
    }
}
