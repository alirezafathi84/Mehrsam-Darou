using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Mehrsam_Darou.Models;

public partial class DarouAppContext : DbContext
{
    public DarouAppContext()
    {
    }

    public DarouAppContext(DbContextOptions<DarouAppContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<Setting> Settings { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserEnterLog> UserEnterLogs { get; set; }

    public virtual DbSet<UserStatus> UserStatuses { get; set; }

    public virtual DbSet<VwOnlineUser> VwOnlineUsers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(LocalDB)\\MSSQLLocalDB;Database=DarouApp;Trusted_Connection=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatMess__3214EC074A92845F");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Receiver).WithMany(p => p.ChatMessageReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChatMessages_Receiver");

            entity.HasOne(d => d.Sender).WithMany(p => p.ChatMessageSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChatMessages_Sender");
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.DefaultColor).HasDefaultValue(false);
            entity.Property(e => e.IsMenuDark).HasDefaultValue(false);
            entity.Property(e => e.IsNavDark).HasDefaultValue(false);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.BuyCommercial).HasDefaultValue(false);
            entity.Property(e => e.DefaultPageForTeam).HasMaxLength(100);
            entity.Property(e => e.Financial).HasDefaultValue(false);
            entity.Property(e => e.ManagmentDashboard).HasDefaultValue(false);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Pmo)
                .HasDefaultValue(false)
                .HasColumnName("PMO");
            entity.Property(e => e.Product).HasDefaultValue(false);
            entity.Property(e => e.Qa)
                .HasDefaultValue(false)
                .HasColumnName("QA");
            entity.Property(e => e.Qc)
                .HasDefaultValue(false)
                .HasColumnName("QC");
            entity.Property(e => e.RandD)
                .HasDefaultValue(false)
                .HasColumnName("RAndD");
            entity.Property(e => e.Setting).HasDefaultValue(false);
            entity.Property(e => e.SystemUsers).HasDefaultValue(false);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.AvatarImg).HasMaxLength(150);
            entity.Property(e => e.DateCreated).HasColumnType("datetime");
            entity.Property(e => e.DateModified).HasColumnType("datetime");
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.Password)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(150)
                .IsUnicode(false);

            entity.HasOne(d => d.Team).WithMany(p => p.Users)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("FK_Users_Teams");
        });

        modelBuilder.Entity<UserEnterLog>(entity =>
        {
            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.User).WithMany(p => p.UserEnterLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserEnterLogs_Users");
        });

        modelBuilder.Entity<UserStatus>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__UserStat__1788CC4C525C7BB0");

            entity.ToTable("UserStatus");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.LastSeen)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("offline");

            entity.HasOne(d => d.User).WithOne(p => p.UserStatus)
                .HasForeignKey<UserStatus>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserStatus_User");
        });

        modelBuilder.Entity<VwOnlineUser>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_OnlineUsers");

            entity.Property(e => e.AvatarImg).HasMaxLength(150);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.LastSeen).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Username)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
