using Microsoft.EntityFrameworkCore;
using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculator.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // Füge hier deine Tabellen als DbSet hinzu
    public DbSet<tankinfomodel> TankinfoModel { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Hier kannst du den Tabellenname explizit festlegen
        modelBuilder.Entity<tankinfomodel>()
            .ToTable("tankinfomodel");  // Tabellenname in Kleinbuchstaben
    }
}
