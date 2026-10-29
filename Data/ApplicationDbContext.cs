using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Staybnb.Models;

namespace Staybnb.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ============================================================
        // STAYBNB DATABASE TABLES
        // ============================================================
        public DbSet<Property> Properties { get; set; }
public DbSet<PropertyImage> PropertyImages { get; set; }
public DbSet<HostApplication> HostApplications { get; set; }
public DbSet<HostApplicationImage> HostApplicationImages { get; set; }
public DbSet<Booking> Bookings { get; set; }
public DbSet<GuestDocument> GuestDocuments { get; set; }
public DbSet<CheckInProcess> CheckInProcesses { get; set; }
public DbSet<Message> Messages { get; set; }
public DbSet<Notification> Notifications { get; set; }
public DbSet<ActivityLog> ActivityLogs { get; set; }
public DbSet<Review> Reviews { get; set; }


        // ============================================================
        // DATABASE RELATIONSHIPS
        // ============================================================

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Always call the Identity configuration first.
            base.OnModelCreating(builder);


            // ========================================================
            // PROPERTY -> HOST
            // ========================================================

            builder.Entity<Property>()
                .HasOne(p => p.Host)
                .WithMany()
                .HasForeignKey(p => p.HostId)
                .OnDelete(DeleteBehavior.Restrict);


            // ========================================================
            // PROPERTY IMAGE -> PROPERTY
            // ========================================================

            builder.Entity<PropertyImage>()
                .HasOne(pi => pi.Property)
                .WithMany()
                .HasForeignKey(pi => pi.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);


            // ========================================================
            // HOST APPLICATION -> APPLICANT
            // ========================================================

            builder.Entity<HostApplication>()
                .HasOne(h => h.Applicant)
                .WithMany()
                .HasForeignKey(h => h.ApplicantId)
                .OnDelete(DeleteBehavior.Cascade);


            // ========================================================
            // HOST APPLICATION -> REVIEWER
            // ========================================================

            builder.Entity<HostApplication>()
                .HasOne(h => h.ReviewedBy)
                .WithMany()
                .HasForeignKey(h => h.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);


            // ========================================================
            // BOOKING -> GUEST
            // ========================================================

            builder.Entity<Booking>()
                .HasOne(b => b.Guest)
                .WithMany()
                .HasForeignKey(b => b.GuestId)
                .OnDelete(DeleteBehavior.Restrict);


            // ========================================================
            // BOOKING -> PROPERTY
            // ========================================================

            builder.Entity<Booking>()
                .HasOne(b => b.Property)
                .WithMany()
                .HasForeignKey(b => b.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);


            // ========================================================
            // GUEST DOCUMENT -> BOOKING
            // ========================================================

            builder.Entity<GuestDocument>()
                .HasOne(g => g.Booking)
                .WithMany()
                .HasForeignKey(g => g.BookingId)
                .OnDelete(DeleteBehavior.Cascade);


            // ========================================================
            // GUEST DOCUMENT -> GUEST
            // ========================================================

            builder.Entity<GuestDocument>()
                .HasOne(g => g.Guest)
                .WithMany()
                .HasForeignKey(g => g.GuestId)
                .OnDelete(DeleteBehavior.Restrict);


            // ========================================================
            // CHECK-IN PROCESS -> BOOKING
            // ========================================================

            builder.Entity<CheckInProcess>()
                .HasOne(c => c.Booking)
                .WithMany()
                .HasForeignKey(c => c.BookingId)
                .OnDelete(DeleteBehavior.Cascade);


            // ========================================================
            // MESSAGE -> SENDER
            // ========================================================

            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);


            // ========================================================
            // MESSAGE -> RECEIVER
            // ========================================================

            builder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);


            // ========================================================
            // NOTIFICATION -> USER
            // ========================================================

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // ========================================================
            // ACTIVITY LOG -> USER
            // ========================================================

            builder.Entity<ActivityLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);


            // ========================================================
            // REVIEW -> GUEST
            // ========================================================

            builder.Entity<Review>()
                .HasOne(r => r.Guest)
                .WithMany()
                .HasForeignKey(r => r.GuestId)
                .OnDelete(DeleteBehavior.Restrict);


            // ========================================================
            // REVIEW -> PROPERTY
            // ========================================================

            builder.Entity<Review>()
                .HasOne(r => r.Property)
                .WithMany()
                .HasForeignKey(r => r.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);


            // ========================================================
            // REVIEW -> BOOKING
            // ========================================================

            builder.Entity<Review>()
                .HasOne(r => r.Booking)
                .WithMany()
                .HasForeignKey(r => r.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

               // ============================================================
// DECIMAL PRECISION
// ============================================================

builder.Entity<Property>()
    .Property(p => p.PricePerNight)
    .HasPrecision(18, 2);

builder.Entity<HostApplication>()
    .Property(h => h.PricePerNight)
    .HasPrecision(18, 2);

builder.Entity<Booking>()
    .Property(b => b.PricePerNight)
    .HasPrecision(18, 2);

builder.Entity<Booking>()
    .Property(b => b.CleaningFee)
    .HasPrecision(18, 2);

builder.Entity<Booking>()
    .Property(b => b.ServiceFee)
    .HasPrecision(18, 2);

builder.Entity<Booking>()
    .Property(b => b.TotalPrice)
    .HasPrecision(18, 2);
        }
    }
}