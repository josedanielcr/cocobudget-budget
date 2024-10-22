using Microsoft.EntityFrameworkCore;
using web_api.Entities;

namespace web_api.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Folder>().HasKey(t => t.Id);
    }
    
    public DbSet<Folder> Folders { get; set; }
}