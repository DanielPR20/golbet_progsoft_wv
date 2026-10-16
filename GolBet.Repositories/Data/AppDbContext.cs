using GolBet.Entities;
using GolBet.Entities.Common;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GolBet.Repositories.Data
{
    public class AppDbContext : IdentityDbContext<AppUser> //We changed inheritance from DbContext to IdentityDbContext<AppUser>, the parameter AppUser is the class that we use to reference from the framework.
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        //Section DBSets
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<Bet> Bets => Set<Bet>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) //This method nullyfies/overrides the logic of the parent class, adding our owl logic to the model creation.
        {
            base.OnModelCreating(modelBuilder);   // FIRST: Identity maps its 6 tables here. The modelBuilder must map them before.

            // Team names must be unique (case- and accent-insensitive)
            modelBuilder.Entity<Team>()
                .Property(t => t.Name)
                .UseCollation("SQL_Latin1_General_CP1_CI_AI"); //Esta parte me indica el Sensitive Case

            modelBuilder.Entity<Team>()
                .HasIndex(t => t.Name)
                .IsUnique();

            // Double relationship Match -> Team: convention cannot resolve it
            modelBuilder.Entity<Match>()
                .HasOne(m => m.HomeTeam)
                .WithMany()
                .HasForeignKey(m => m.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.AwayTeam)
                .WithMany()
                .HasForeignKey(m => m.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // A match with bets cannot be deleted
            modelBuilder.Entity<Bet>()
                .HasOne(b => b.Match)
                .WithMany(m => m.Bets)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- Identity additions ----

            // Bet -> User: a user's bets cannot be destroyed by deleting the user
            modelBuilder.Entity<Bet>()  //This is a one to many relationship, between User and Bets.
                .HasOne(b => b.User)
                .WithMany(u => u.Bets)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Business rule #3: same user cannot repeat the same pick on a match. We can only have one bet per user on the match
            modelBuilder.Entity<Bet>()
                .HasIndex(b => new { b.MatchId, b.UserId, b.Pick })
                .IsUnique();

            // FutCoins balance precision (up to 9,999,999,999.99). Balance property allows us to store the amount of FutCoins that the user has with a precision of 12 digits (10 integer, 2 decimal)
            modelBuilder.Entity<AppUser>()
                .Property(u => u.Balance)
                .HasPrecision(12, 2);
        }

        // ---- Automatic audit timestamps ----
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = utcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.ModifiedDate = utcNow;
                        // CreatedDate must never change after creation
                        entry.Property(e => e.CreatedDate).IsModified = false;
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
