using ItemManagement.Model.DTO;
using Microsoft.EntityFrameworkCore;
using System; // Ensure System is included for Guid and DateTime

namespace ItemManagement.Model
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure precision for decimal properties to ensure correct mapping to SQL Server
            // This prevents potential data loss or unexpected behavior with monetary values.
            modelBuilder.Entity<CartItem>()
                .Property(ci => ci.Price)
                .HasPrecision(18, 2); // 18 total digits, 2 after decimal point

            modelBuilder.Entity<Item>()
                .Property(i => i.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.Price)
                .HasPrecision(18, 2);

            // Define relationships using fluent API for clarity and control

            // OrderItem relationships
          modelBuilder.Entity<OrderItem>()
              .HasOne(oi => oi.Order)
              .WithMany(o => o.OrderItems)
              .HasForeignKey(oi => oi.OrderId)
              .OnDelete(DeleteBehavior.Cascade); // KEEP CASCADE here

         modelBuilder.Entity<OrderItem>()
             .HasOne(oi => oi.Item)
             .WithMany()
             .HasForeignKey(oi => oi.ItemId)
             .OnDelete(DeleteBehavior.NoAction); // ✅ NO CASCADE here

            // Define composite primary keys
            modelBuilder.Entity<CartItem>()
                .HasKey(ci => new { ci.CartId, ci.ItemId }); // Composite key for CartItem

            modelBuilder.Entity<OrderItem>()
                .HasKey(oi => new { oi.OrderId, oi.ItemId }); // Composite key for OrderItem

            // Store relationships with Location entities (Country, State, City)
            modelBuilder.Entity<Store>()
                .HasOne(s => s.Country) // Each Store has one Country
                .WithMany() // Country can be associated with many Stores (no navigation property on Country)
                .HasForeignKey(s => s.CountryId) // Foreign key is CountryId
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Store>()
                .HasOne(s => s.State) // Each Store has one State
                .WithMany() // State can be associated with many Stores
                .HasForeignKey(s => s.StateId) // Foreign key is StateId
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Store>()
                .HasOne(s => s.City) // Each Store has one City
                .WithMany() // City can be associated with many Stores
                .HasForeignKey(s => s.CityId) // Foreign key is CityId
                .OnDelete(DeleteBehavior.Restrict);

            // Configure User entity relationships if needed (e.g., User has one Cart)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Cart)
                .WithOne(c => c.User)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);// If user is deleted, their cart is deleted

            modelBuilder.Entity<DashboardCountsDto>().HasNoKey(); // REQUIRED for SP mapping
            base.OnModelCreating(modelBuilder);
        }
        
        // DbSet properties for all your entities, which EF Core will map to database tables
        public DbSet<User> Users { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Logger> Loggers { get; set; } // Assuming you have a Logger model
        public DbSet<Country> Countrys { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<City> Citys { get; set; }
        public DbSet<DashboardCountsDto> DashboardCounts { get; set; }

      
        // For SP mapping
    }
}
