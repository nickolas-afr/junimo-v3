using junimo_v3.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Data
{
    public class RepositoryContext : IdentityDbContext<User>
    {
        public RepositoryContext(DbContextOptions<RepositoryContext> options)
            : base(options)
        {
        }

        public DbSet<Game> Games { get; set; }
        public DbSet<GameGenre> GameGenres { get; set; }
        public DbSet<GameGenreV2> GameGenresV2 { get; set; } // This is the new table
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItems> OrderItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<User> Users { get; set; } // This is already part of IdentityDbContext
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameGenre>()
                .HasKey(gg => new { gg.GameId, gg.GenreId });

            modelBuilder.Entity<GameGenreV2>()
                .HasKey(gg => new { gg.GameId, gg.genre});

            modelBuilder.Entity<OrderItems>()
                .HasKey(oi => new { oi.OrderId, oi.GameId });

            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>()
            .HasKey(l => new { l.LoginProvider, l.ProviderKey });

            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>()
                .HasKey(r => new { r.UserId, r.RoleId });

            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>()
                .HasKey(t => new { t.UserId, t.LoginProvider, t.Name });

            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion(
                v => v.ToString(),
                v => (OrderStatus)Enum.Parse(typeof(OrderStatus), v));

            modelBuilder.Entity<UserFriendship>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.FriendshipsInitiated)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFriendship>()
                .HasOne(uf => uf.Friend)
                .WithMany(u => u.FriendshipsReceived)
                .HasForeignKey(uf => uf.FriendId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFriendship>()
                .Property(uf => uf.Status)
                .HasConversion(
                    v => v.ToString(),
                    v => (FriendshipStatus)Enum.Parse(typeof(FriendshipStatus), v));
        }
    }
}
