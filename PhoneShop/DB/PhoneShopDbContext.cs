using Microsoft.EntityFrameworkCore;
using PhoneShop.Models;

namespace PhoneShop.DB
{
    public class PhoneShopDbContext: DbContext
    {
        public PhoneShopDbContext(DbContextOptions<PhoneShopDbContext> options) : base(options)
        {
        }

        protected PhoneShopDbContext()
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<OrdersDetails> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Orders>()
                .HasIndex(order => order.PayPalOrderId)
                .IsUnique();

            modelBuilder.Entity<OrdersDetails>()
                .HasOne(detail => detail.Orders)
                .WithMany(order => order.OrderDetails)
                .HasForeignKey(detail => detail.OrdersId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrdersDetails>()
                .HasOne(detail => detail.Product)
                .WithMany()
                .HasForeignKey(detail => detail.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
