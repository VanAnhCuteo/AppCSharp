using FoodMapAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tour> Tours { get; set; }
        public DbSet<TourPoi> TourPois { get; set; }
        public DbSet<TourHistory> TourHistories { get; set; }
        public DbSet<Poi> Pois { get; set; }
        public DbSet<PoiImage> PoiImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tour>().ToTable("tours");
            modelBuilder.Entity<TourPoi>().ToTable("tour_pois");
            modelBuilder.Entity<TourHistory>().ToTable("tour_histories");
            modelBuilder.Entity<Poi>().ToTable("pois");
            modelBuilder.Entity<PoiImage>().ToTable("poi_images");
        }
    }
}
