using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HackerNewsAPI.Infrastructure.Entities;

namespace HackerNewsAPI.Infrastructure.Configurations;

public class StoryEntityConfiguration : IEntityTypeConfiguration<StoryEntity>
{
    public void Configure(EntityTypeBuilder<StoryEntity> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd();
        
        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(s => s.Uri)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(s => s.PostedBy)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(s => s.Time)
            .IsRequired();
        
        builder.Property(s => s.Score)
            .IsRequired();
        
        builder.Property(s => s.CommentCount)
            .IsRequired();
        
        builder.Property(s => s.CreatedAt)
            .IsRequired();
        
        builder.Property(s => s.UpdatedAt)
            .IsRequired();
        
        // Indexes for performance
        builder.HasIndex(s => s.Score);
        builder.HasIndex(s => s.Time);
        builder.HasIndex(s => s.PostedBy);
        
        // Seed data for testing purposes
        builder.HasData(
            new StoryEntity
            {
                Id = 1,
                Title = "Sample Story 1",
                Uri = "https://example.com/story1",
                PostedBy = "admin",
                Time = DateTime.UtcNow.AddHours(-1),
                Score = 150,
                CommentCount = 25,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new StoryEntity
            {
                Id = 2,
                Title = "Sample Story 2",
                Uri = "https://example.com/story2",
                PostedBy = "user",
                Time = DateTime.UtcNow.AddHours(-2),
                Score = 120,
                CommentCount = 18,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new StoryEntity
            {
                Id = 3,
                Title = "Sample Story 3",
                Uri = "https://example.com/story3",
                PostedBy = "moderator",
                Time = DateTime.UtcNow.AddHours(-3),
                Score = 200,
                CommentCount = 42,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
    }
}
