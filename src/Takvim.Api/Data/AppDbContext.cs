using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Takvim.Api.Models;

namespace Takvim.Api.Data;

public class AppDbContext
    : IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    
    public DbSet<Calendar> Calendars => Set<Calendar>();
    public DbSet<CalendarEvent> Events => Set<CalendarEvent>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Reminder> Reminders => Set<Reminder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // USERS
        
        modelBuilder.Entity<AppUser>(entity =>
        {
        entity.ToTable("Users");

        entity.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();
        });


        // CALENDARS
        modelBuilder.Entity<Calendar>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.ColorHex)
                .HasMaxLength(7)
                .IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Calendars)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CATEGORIES
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(x => x.ColorHex)
                .HasMaxLength(7)
                .IsRequired();
        });

        // EVENTS
        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.Property(x => x.Location)
                .HasMaxLength(250);

            entity.HasOne(x => x.Calendar)
                .WithMany(x => x.Events)
                .HasForeignKey(x => x.CalendarId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Events)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x =>
                new { x.CalendarId, x.StartAtUtc });
        });

        // REMINDERS
        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Event)
                .WithMany(x => x.Reminders)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }}