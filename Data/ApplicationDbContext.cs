using ChatApplicationAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApplicationAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();
    public DbSet<UserStatus> UserStatuses => Set<UserStatus>();
    public DbSet<CallLog> CallLogs => Set<CallLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure indexes & unique constraints
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Foreign keys for FriendRequest
        modelBuilder.Entity<FriendRequest>()
            .HasOne(fr => fr.Sender)
            .WithMany()
            .HasForeignKey(fr => fr.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FriendRequest>()
            .HasOne(fr => fr.Receiver)
            .WithMany()
            .HasForeignKey(fr => fr.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure ChatParticipant relationship
        modelBuilder.Entity<ChatParticipant>()
            .HasOne(cp => cp.Chat)
            .WithMany(c => c.Participants)
            .HasForeignKey(cp => cp.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatParticipant>()
            .HasOne(cp => cp.User)
            .WithMany(u => u.ChatParticipants)
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
