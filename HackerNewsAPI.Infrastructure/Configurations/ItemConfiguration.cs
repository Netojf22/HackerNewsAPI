using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HackerNewsAPI.Infrastructure.Entities;

namespace HackerNewsAPI.Infrastructure.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(i => i.Id);
        
        builder.Property(i => i.Id)
            .ValueGeneratedNever();
        
        builder.Property(i => i.By)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(i => i.Url)
            .HasMaxLength(500);
        
        builder.Property(i => i.Time)
            .IsRequired();
        
        builder.Property(i => i.Score)
            .IsRequired();
        
        builder.Property(i => i.Type)
            .IsRequired();
        
        builder.Property(i => i.CachedAt)
            .IsRequired();
        
        // Indexes for performance
        builder.HasIndex(i => i.Score);
        builder.HasIndex(i => i.Time);
        builder.HasIndex(i => i.Type);
        builder.HasIndex(i => i.CachedAt);
    }
}
