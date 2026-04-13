using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodMapAdmin.Services
{
    public interface ILanguageService
    {
        Task<List<Language>> GetAllLanguagesAsync();
        Task<bool> AddLanguageAsync(Language language);
        Task<bool> DeleteLanguageAsync(string code);
    }

    public class LanguageService : ILanguageService
    {
        private readonly ApplicationDbContext _context;

        public LanguageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Language>> GetAllLanguagesAsync()
        {
            return await _context.Languages.OrderBy(l => l.Name).ToListAsync();
        }

        public async Task<bool> AddLanguageAsync(Language language)
        {
            if (await _context.Languages.AnyAsync(l => l.LanguageCode == language.LanguageCode))
                return false;

            _context.Languages.Add(language);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteLanguageAsync(string code)
        {
            var lang = await _context.Languages.FindAsync(code);
            if (lang == null || code == "vi") return false;

            _context.Languages.Remove(lang);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
