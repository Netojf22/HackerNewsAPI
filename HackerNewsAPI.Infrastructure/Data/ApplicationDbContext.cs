using Microsoft.EntityFrameworkCore;
using HackerNewsAPI.Infrastructure.Entities;
using HackerNewsAPI.Infrastructure.Configurations;

namespace HackerNewsAPI.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users { get; set; }
    public DbSet<StoryEntity> Stories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StoryEntityConfiguration());
    }
}
