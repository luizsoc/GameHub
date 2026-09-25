using GameHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using GameHub.Application.Interfaces;

namespace GameHub.Infrastructure.Data;

public class GameHubDbContext : DbContext, IUnitOfWork
{
    public GameHubDbContext(DbContextOptions<GameHubDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Channel> Channels => Set<Channel>();

    public DbSet<ChannelMember> ChannelMembers => Set<ChannelMember>();

    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Username)
                .IsUnique();

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.Property(x => x.Username)
                .IsRequired()
                .HasMaxLength(User.UsernameMaxLength);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(User.EmailMaxLength);

            entity.Property(x => x.PasswordHash)
                .IsRequired();
        });

        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(Channel.NameMaxLength);

            entity.Property(x => x.Description)
                .HasMaxLength(Channel.DescriptionMaxLength);
        });

        modelBuilder.Entity<ChannelMember>(entity =>
        {
            entity.HasKey(x => new
            {
                x.UserId,
                x.ChannelId
            });

            entity.HasOne(x => x.User)
                .WithMany(x => x.ChannelMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Channel)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Content)
                .IsRequired()
                .HasMaxLength(Message.ContentMaxLength);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Channel)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new
            {
                x.ChannelId,
                x.CreatedAt
            });
        });
    }
}