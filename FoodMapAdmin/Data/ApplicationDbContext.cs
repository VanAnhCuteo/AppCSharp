using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace FoodMapAdmin.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Poi> Pois { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<PoiImage> PoiImages { get; set; }
        public DbSet<PoiGuide> PoiGuides { get; set; }
        public DbSet<PoiVisit> PoiVisits { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<PoiPendingChange> PoiPendingChanges { get; set; }
        public DbSet<PoiImagePendingChange> PoiImagePendingChanges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Poi>().ToTable("pois");
            modelBuilder.Entity<Category>().ToTable("categories");
            modelBuilder.Entity<PoiImage>().ToTable("poi_images");
            modelBuilder.Entity<Review>().ToTable("reviews");
            modelBuilder.Entity<PoiPendingChange>().ToTable("poi_pending_changes");
            modelBuilder.Entity<PoiImagePendingChange>().ToTable("poi_image_pending_changes");
            
            // Handle enum status if needed, or use string directly as defined in model
        }
    }
}
