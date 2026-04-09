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

        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<PoiPendingChange> PoiPendingChanges { get; set; }
        public DbSet<PoiImagePendingChange> PoiImagePendingChanges { get; set; }
        public DbSet<PoiAudioLog> PoiAudioLogs { get; set; }
        public DbSet<PoiQr> PoiQrs { get; set; }
        public DbSet<UserLocation> UserLocations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Poi>().ToTable("pois");
            modelBuilder.Entity<Category>().ToTable("categories");
            modelBuilder.Entity<PoiImage>().ToTable("poi_images");

            modelBuilder.Entity<PoiPendingChange>().ToTable("poi_pending_changes");
            modelBuilder.Entity<PoiImagePendingChange>().ToTable("poi_image_pending_changes");
            modelBuilder.Entity<PoiAudioLog>().ToTable("poi_audio_logs");

            modelBuilder.Entity<PoiQr>().ToTable("poi_qrs");
            modelBuilder.Entity<UserLocation>().ToTable("user_locations");
            
            // Handle enum status if needed, or use string directly as defined in model
        }
    }
}
