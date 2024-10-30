using Microsoft.EntityFrameworkCore;
using web_api.Entities;

namespace web_api.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Folder>().HasKey(t => t.Id);
        modelBuilder.Entity<Category>().HasKey(c => c.Id);
        modelBuilder.Entity<Period>().HasKey(p => p.Id);

        modelBuilder.Entity<Category>()
              .Property(c => c.BudgetAmount)
              .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Category>()
            .Property(c => c.AmountSpent)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Category>()
            .HasOne(c => c.Folder)
            .WithMany(f => f.Categories)
            .HasForeignKey(c => c.FolderId);
        
        modelBuilder.Entity<Period>()
            .Property(c => c.BudgetAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Period>()
            .Property(c => c.AmountSpent)
            .HasColumnType("decimal(18,2)");
    }

    public DbSet<Folder> Folders { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Period> Periods { get; set; }
}