//using Microsoft.EntityFrameworkCore;
//using ContentCartel.Models;

//namespace ContentCartel.Data
//{
//    public class ApplicationDBContext : DbContext
//    {
//        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
//            : base(options)
//        {
//        }

//        public DbSet<Service> Services { get; set; }

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            base.OnModelCreating(modelBuilder);

//            modelBuilder.Entity<Service>(entity =>
//            {
//                entity.HasKey(s => s.ServiceId);
//                entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
//                entity.Property(s => s.Category).IsRequired().HasMaxLength(100);
//                entity.Property(s => s.BasePrice).HasColumnType("decimal(18,2)");
//            });
//        }
//    }
//}