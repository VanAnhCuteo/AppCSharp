using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace DbTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");
            
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            using (var context = new ApplicationDbContext(optionsBuilder.Options))
            {
                var users = context.Users.ToList();
                Console.WriteLine("--- Users Roles ---");
                foreach (var u in users)
                {
                    Console.WriteLine($"User: {u.Username}, Role: [{u.Role}]");
                }
                
                var hangingAudio = context.PoiGuides.Where(g => g.Title.Contains("CHỜ DUYỆT")).ToList();
                Console.WriteLine("\n--- Guides with 'CHỜ DUYỆT' in Title ---");
                foreach (var g in hangingAudio)
                {
                    Console.WriteLine($"GuideID: {g.GuideId}, Title: [{g.Title}]");
                }
            }
        }
    }
}
