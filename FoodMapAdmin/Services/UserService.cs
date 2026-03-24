using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodMapAdmin.Services
{
    public interface IUserService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            // Note: In real app, use password hashing! 
            // The provided SQL dump shows '123456' as password for all.
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }
    }
}
