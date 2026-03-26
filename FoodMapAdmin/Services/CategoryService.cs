using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

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
        private readonly IActivityLogger _logger;
        private readonly AuthenticationStateProvider _authStateProvider;

        public CategoryService(ApplicationDbContext context, IActivityLogger logger, AuthenticationStateProvider authStateProvider)
        {
            _context = context;
            _logger = logger;
            _authStateProvider = authStateProvider;
        }

        private async Task<int?> GetCurrentUserIdAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userIdStr = user.FindFirst(c => c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out var id) ? id : null;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<bool> CreateCategoryAsync(Category category)
        {
            _context.Categories.Add(category);
            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                var userId = await GetCurrentUserIdAsync();
                await _logger.LogAsync(userId, "Thêm danh mục", category.CategoryName, $"Đã tạo danh mục {category.CategoryName}");
            }
            return result;
        }

        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            _context.Categories.Update(category);
            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                var userId = await GetCurrentUserIdAsync();
                await _logger.LogAsync(userId, "Cập nhật danh mục", category.CategoryName, $"Đã sửa thông tin danh mục {category.CategoryName}");
            }
            return result;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;
            
            var name = category.CategoryName;
            _context.Categories.Remove(category);
            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                var userId = await GetCurrentUserIdAsync();
                await _logger.LogAsync(userId, "Xóa danh mục", name, $"Đã xóa danh mục {name}");
            }
            return result;
        }
    }
}
