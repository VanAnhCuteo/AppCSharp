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
        public DbSet<Tour> Tours { get; set; }
        public DbSet<TourPoi> TourPois { get; set; }

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

            modelBuilder.Entity<Tour>(entity =>
            {
                entity.ToTable("tours");
                entity.Property(e => e.TourId).HasColumnName("tour_id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
                entity.Property(e => e.Price).HasColumnName("price");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<TourPoi>(entity =>
            {
                entity.ToTable("tour_pois");
                entity.Property(e => e.TourPoiId).HasColumnName("tour_poi_id");
                entity.Property(e => e.TourId).HasColumnName("tour_id");
                entity.Property(e => e.PoiId).HasColumnName("poi_id");
                entity.Property(e => e.SequenceOrder).HasColumnName("sequence_order");
                entity.Property(e => e.StayDuration).HasColumnName("stay_duration");
                entity.Property(e => e.AveragePrice).HasColumnName("average_price");
            });
            
            // Handle enum status if needed, or use string directly as defined in model
        }
    }
}
