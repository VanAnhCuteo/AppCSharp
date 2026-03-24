using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodMapAdmin.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync();
        Task<bool> CreateCategoryAsync(Category category);
        Task<bool> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(int id);
    }

    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<bool> CreateCategoryAsync(Category category)
        {
            _context.Categories.Add(category);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            _context.Categories.Update(category);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;
            _context.Categories.Remove(category);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
