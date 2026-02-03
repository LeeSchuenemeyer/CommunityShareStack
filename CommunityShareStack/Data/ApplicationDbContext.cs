using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CommunityShareStack.Models;

namespace CommunityShareStack.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Item> Items { get; set; }
        public DbSet<ItemImage> ItemImages { get; set; }
        public DbSet<LoanRequest> LoanRequests { get; set; }
        public DbSet<HoldRequest> HoldRequests { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<RenewalRequest> RenewalRequests { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ScanSession> ScanSessions { get; set; }
        public DbSet<ScanImage> ScanImages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Item>()
                .HasMany(i => i.Images)
                .WithOne(ii => ii.Item)
                .HasForeignKey(ii => ii.ItemId);

            builder.Entity<Loan>()
                .HasOne(l => l.Item)
                .WithMany()
                .HasForeignKey(l => l.ItemId);

            builder.Entity<Loan>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId);

            builder.Entity<LoanRequest>()
                .HasOne(lr => lr.Item)
                .WithMany()
                .HasForeignKey(lr => lr.ItemId);

            builder.Entity<LoanRequest>()
                .HasOne(lr => lr.User)
                .WithMany()
                .HasForeignKey(lr => lr.UserId);

            builder.Entity<HoldRequest>()
                .HasOne(hr => hr.Item)
                .WithMany()
                .HasForeignKey(hr => hr.ItemId);

            builder.Entity<HoldRequest>()
                .HasOne(hr => hr.User)
                .WithMany()
                .HasForeignKey(hr => hr.UserId);

            builder.Entity<RenewalRequest>()
                .HasOne(rr => rr.Loan)
                .WithMany()
                .HasForeignKey(rr => rr.LoanId);

            builder.Entity<RenewalRequest>()
                .HasOne(rr => rr.User)
                .WithMany()
                .HasForeignKey(rr => rr.UserId);

            builder.Entity<Review>()
                .HasOne(r => r.Item)
                .WithMany()
                .HasForeignKey(r => r.ItemId);

            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId);

            builder.Entity<ScanSession>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId);

            builder.Entity<ScanSession>()
                .HasMany(s => s.Images)
                .WithOne(i => i.ScanSession)
                .HasForeignKey(i => i.ScanSessionId);
        }
    }
}
