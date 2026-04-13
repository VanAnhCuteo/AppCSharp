using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodMapAdmin.Services
{
    public interface IPoiGuidePendingChangeService
    {
        Task<List<PoiGuidePendingChange>> GetAllPendingChangesAsync();
        Task<bool> SubmitChangeAsync(PoiGuidePendingChange change);
        Task<bool> ApproveChangeAsync(int changeId);
        Task<bool> RejectChangeAsync(int changeId);
        Task<List<PoiGuidePendingChange>> GetChangesByOwnerAsync(int ownerId);
        Task<bool> CancelAsync(int id);
    }

    public class PoiGuidePendingChangeService : IPoiGuidePendingChangeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPoiGuideService _guideService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        private readonly INotificationService _notificationService;

        public PoiGuidePendingChangeService(
            ApplicationDbContext context, 
            IPoiGuideService guideService,
            ITranslationService translationService,
            ILanguageService languageService,
            INotificationService notificationService)
        {
            _context = context;
            _guideService = guideService;
            _translationService = translationService;
            _languageService = languageService;
            _notificationService = notificationService;
        }

        public async Task<List<PoiGuidePendingChange>> GetAllPendingChangesAsync()
        {
            return await _context.PoiGuidePendingChanges
                .Include(c => c.Poi)
                .Include(c => c.Requester)
                .Where(c => c.Status == "pending")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> SubmitChangeAsync(PoiGuidePendingChange change)
        {
            change.Status = "pending";
            change.CreatedAt = DateTime.Now;
            _context.PoiGuidePendingChanges.Add(change);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ApproveChangeAsync(int changeId)
        {
            var request = await _context.PoiGuidePendingChanges
                .Include(c => c.Poi)
                .FirstOrDefaultAsync(c => c.ChangeId == changeId);

            if (request == null || request.Status != "pending") return false;

            try 
            {
                // 1. Process based on ChangeType
                if (request.ChangeType == "delete")
                {
                    // Delete all guides for this POI
                    var guides = await _context.PoiGuides.Where(g => g.PoiId == request.PoiId).ToListAsync();
                    _context.PoiGuides.RemoveRange(guides);
                }
                else
                {
                    // CREATE or UPDATE
                    // 2. Save the primary (Vietnamese) guide
                    PoiGuide? existingGuide = null;
                    if (request.GuideId.HasValue)
                    {
                        existingGuide = await _context.PoiGuides.FindAsync(request.GuideId.Value);
                    }
                    else 
                    {
                        // Check if a Vietnamese guide already exists for this POI
                        existingGuide = await _context.PoiGuides.FirstOrDefaultAsync(g => g.PoiId == request.PoiId && g.Language == "vi");
                    }

                    if (existingGuide != null)
                    {
                        existingGuide.Title = request.Title;
                        existingGuide.Description = request.Description;
                        _context.PoiGuides.Update(existingGuide);
                    }
                    else
                    {
                        existingGuide = new PoiGuide
                        {
                            PoiId = request.PoiId,
                            Title = request.Title,
                            Description = request.Description,
                            Language = "vi"
                        };
                        _context.PoiGuides.Add(existingGuide);
                    }
                    
                    await _context.SaveChangesAsync();

                    // 3. TRIGGER AUTOMATIC TRANSLATION
                    // We do this AFTER saving the Vietnamese one so we have a source of truth
                    var activeLangs = await _languageService.GetAllLanguagesAsync();
                    var otherLangs = activeLangs.Where(l => l.LanguageCode != "vi").Select(l => l.LanguageCode).ToList();

                    foreach (var lang in otherLangs)
                    {
                        try 
                        {
                            var sourceTitle = request.Title ?? "";
                            var sourceDesc = request.Description ?? "";
                            var poiName = request.Poi?.Name ?? "";
                            var placeholder = "[[POI_NAME]]";

                            // Inject placeholder
                            if (!string.IsNullOrEmpty(poiName))
                            {
                                sourceTitle = sourceTitle.Replace(poiName, placeholder);
                                sourceDesc = sourceDesc.Replace(poiName, placeholder);
                            }

                            var tTitle = await _translationService.TranslateAsync(sourceTitle, lang);
                            var tDesc = await _translationService.TranslateAsync(sourceDesc, lang);

                            // Restore placeholder
                            if (!string.IsNullOrEmpty(poiName))
                            {
                                tTitle = tTitle?.Replace(placeholder, poiName)
                                               ?.Replace("[[ POI_NAME ]]", poiName)
                                               ?.Replace("[[ poi_name ]]", poiName) ?? "";
                                tDesc = tDesc?.Replace(placeholder, poiName)
                                              ?.Replace("[[ POI_NAME ]]", poiName)
                                              ?.Replace("[[ poi_name ]]", poiName) ?? "";
                            }

                            // Save or update translation
                            var langGuide = await _context.PoiGuides.FirstOrDefaultAsync(g => g.PoiId == request.PoiId && g.Language == lang);
                            if (langGuide != null)
                            {
                                langGuide.Title = tTitle;
                                langGuide.Description = tDesc;
                                _context.PoiGuides.Update(langGuide);
                            }
                            else
                            {
                                langGuide = new PoiGuide
                                {
                                    PoiId = request.PoiId,
                                    Title = tTitle,
                                    Description = tDesc,
                                    Language = lang
                                };
                                _context.PoiGuides.Add(langGuide);
                            }
                            await _context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Mod Approval] Translation Error for {lang}: {ex.Message}");
                        }
                    }
                }

                // 4. Update request status
                request.Status = "approved";

                // Notifications
                string actionVn = request.ChangeType == "create" ? "THÊM MỚI" : (request.ChangeType == "delete" ? "XÓA BỎ" : "CẬP NHẬT");
                await _notificationService.SendNotificationAsync(
                    request.UserId ?? 0,
                    "Audio Guide được phê duyệt",
                    $"Yêu cầu {actionVn} thuyết minh cho quán '{request.Poi?.Name}' đã được duyệt và dịch tự động.",
                    "success",
                    "audio"
                );

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Mod Approval] Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RejectChangeAsync(int changeId)
        {
            var request = await _context.PoiGuidePendingChanges.FindAsync(changeId);
            if (request == null || request.Status != "pending") return false;

            request.Status = "rejected";

            // Notifications
            string actionVn = request.ChangeType == "create" ? "THÊM MỚI" : (request.ChangeType == "delete" ? "XÓA BỎ" : "CẬP NHẬT");
            await _notificationService.SendNotificationAsync(
                request.UserId ?? 0,
                "Audio Guide bị từ chối",
                $"Rất tiếc, yêu cầu {actionVn} thuyết minh cho quán '{request.Poi?.Name}' đã bị từ chối.",
                "alert",
                "audio"
            );

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<PoiGuidePendingChange>> GetChangesByOwnerAsync(int ownerId)
        {
            return await _context.PoiGuidePendingChanges
                .Include(c => c.Poi)
                .Where(c => c.UserId == ownerId && c.Status == "pending")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CancelAsync(int id)
        {
            var p = await _context.PoiGuidePendingChanges.FindAsync(id);
            if (p != null)
            {
                _context.PoiGuidePendingChanges.Remove(p);
                return await _context.SaveChangesAsync() > 0;
            }
            return false;
        }
    }
}
