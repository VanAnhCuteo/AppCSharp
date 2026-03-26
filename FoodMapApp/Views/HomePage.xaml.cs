using FoodMapApp.Services;
using System.Net.Http.Json;
using System.Linq;
using System.Text;
using System.Globalization;

namespace FoodMapApp.Views
{
    public partial class HomePage : ContentPage
    {
        private static string BackendUrl => AppConfig.FoodApiUrl;
        private List<FoodItem> _allRestaurants = new();
        private List<CategoryItem> _allCategories = new();
        private int? _selectedCategoryId = null;

        public HomePage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Set Vietnamese text from code-behind
            var username = AuthService.CurrentUsername;
            UsernameLabel.Text = string.IsNullOrEmpty(username) ? "Khách" : username;

            var hour = DateTime.Now.Hour;
            GreetingLabel.Text = hour < 12 ? "Chào buổi sáng," :
                                 hour < 18 ? "Chào buổi chiều," : "Chào buổi tối,";

            SearchEntry.Placeholder = "Tìm món ăn, quán...";

            CategoryTitleLabel.Text = "Khám Phá";
            await LoadCategories();
            await LoadRestaurants();
        }

        private async Task LoadCategories()
        {
            try
            {
                var categories = await HttpService.GetAsync<List<CategoryItem>>($"{BackendUrl}/categories");
                if (categories != null)
                {
                    _allCategories.Clear();
                    foreach (var c in categories) _allCategories.Add(c);

                    // Assign elegant pastel colors & beautiful real food images
                    var styleMap = new Dictionary<string, (string bg, string text, string img)>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Hải sản",  ("#F0F8FF", "#5D9CEC", "https://images.unsplash.com/photo-1615141982883-c7ad0e69fd62?auto=format&fit=crop&w=300&q=80") },
                        { "vặt",      ("#FFFBF0", "#F6BB42", "https://images.unsplash.com/photo-1563729784474-d77dbb933a9e?auto=format&fit=crop&w=300&q=80") },
                        { "Ăn vặt",   ("#FFFBF0", "#F6BB42", "https://images.unsplash.com/photo-1563729784474-d77dbb933a9e?auto=format&fit=crop&w=300&q=80") },
                        { "nướng",    ("#FFF0F3", "#ED5565", "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=300&q=80") },
                    };

                    foreach (var cat in _allCategories)
                    {
                        var normalizedCat = RemoveDiacritics(cat.category_name?.ToLower() ?? "");
                        var matched = styleMap.FirstOrDefault(kv => 
                            normalizedCat.Contains(RemoveDiacritics(kv.Key.ToLower())));

                        cat.BackgroundColor = matched.Key != null ? matched.Value.bg : "#F5F5F7";
                        cat.TextColor       = matched.Key != null ? matched.Value.text : "#A1A1B5";
                        cat.ImageUrl        = matched.Key != null ? matched.Value.img : "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?auto=format&fit=crop&w=300&q=80"; // Default restaurant img
                    }
                    CategoriesCollection.ItemsSource = _allCategories;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        private async Task LoadRestaurants(int? categoryId = null)
        {
            try
            {
                string url = $"{BackendUrl}?lang=vi";
                if (categoryId.HasValue) url += $"&category_id={categoryId.Value}";

                var foods = await HttpService.GetAsync<List<FoodItem>>(url);
                if (foods != null)
                {
                    _allRestaurants = foods;
                    RestaurantsCollection.ItemsSource = _allRestaurants;

                    // Update visibility if searching
                    if (!string.IsNullOrWhiteSpace(SearchEntry.Text))
                        OnSearchTextChanged(null, new TextChangedEventArgs(null, SearchEntry.Text));
                    else
                    {
                        RestaurantsCollection.IsVisible = _allRestaurants.Any();
                        NoResultsLabel.IsVisible = !_allRestaurants.Any();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading restaurants: {ex.Message}");
            }
        }

        private async void OnCategoryTapped(object sender, TappedEventArgs e)
        {
            var category = (CategoryItem)e.Parameter;
            if (category == null) return;

            // Simple toggle logic or just select
            if (_selectedCategoryId == category.category_id)
            {
                _selectedCategoryId = null; // Unselect
            }
            else
            {
                _selectedCategoryId = category.category_id;
            }

            await LoadRestaurants(_selectedCategoryId);
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? "";
            var searchNormalized = RemoveDiacritics(searchText);

            if (string.IsNullOrWhiteSpace(searchText))
            {
                RestaurantsCollection.ItemsSource = _allRestaurants;
                RestaurantsCollection.IsVisible = true;
                NoResultsLabel.IsVisible = false;
                return;
            }

            var filtered = _allRestaurants.Where(f =>
            {
                var nameNormalized = RemoveDiacritics(f.name?.ToLower() ?? "");
                var descNormalized = RemoveDiacritics(f.description?.ToLower() ?? "");
                var addrNormalized = RemoveDiacritics(f.address?.ToLower() ?? "");

                return nameNormalized.Contains(searchNormalized) ||
                       descNormalized.Contains(searchNormalized) ||
                       addrNormalized.Contains(searchNormalized);
            }).ToList();

            if (filtered.Any())
            {
                RestaurantsCollection.ItemsSource = filtered;
                RestaurantsCollection.IsVisible = true;
                NoResultsLabel.IsVisible = false;
            }
            else
            {
                RestaurantsCollection.ItemsSource = null;
                RestaurantsCollection.IsVisible = false;
                NoResultsLabel.IsVisible = true;
            }
        }

        private void OnBackgroundTapped(object sender, EventArgs e)
        {
            SearchEntry.Unfocus();
        }

        private async void OnExploreTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not FoodItem food) 
            {
                Console.WriteLine("DEBUG: Explore tapped but parameter is not FoodItem");
                return;
            }

            Console.WriteLine($"DEBUG: Explore tapped for food ID: {food.id}");
            MainPage.PendingOpenFoodId = food.id;

            try 
            {
                // Native global URI routing to bypass explicit implicit Tab gaps
                await Shell.Current.GoToAsync("//MainPage");
                
                // Force immediate check if already on MainPage or navigation logic needs a kick
                if (MainPage.Instance != null)
                {
                    _ = MainPage.Instance.TryOpenPendingDetail();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Navigation error: {ex.Message}");
                // Fallback to absolute route
                await Shell.Current.GoToAsync("///MainPage");
                if (MainPage.Instance != null)
                {
                    _ = MainPage.Instance.TryOpenPendingDetail();
                }
            }
        }

        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLower();
        }
    }

    public class FoodItem
    {
        public int id { get; set; }
        public int category_id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public string? address { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string? open_time { get; set; }
        public string? image_url { get; set; }

        public string? first_image
        {
            get
            {
                if (string.IsNullOrWhiteSpace(image_url)) return null;
                
                string? rawMatch = null;
                try
                {
                    // Handle JSON array format
                    if (image_url.TrimStart().StartsWith("["))
                    {
                        var arr = System.Text.Json.JsonSerializer.Deserialize<string[]>(image_url);
                        if (arr != null && arr.Length > 0) rawMatch = arr[0];
                    }
                }
                catch { }

                // Handle comma/semicolon separated list
                if (rawMatch == null) 
                {
                    var parts = image_url.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        rawMatch = parts[0].Trim().Trim('"', '\'', '[', ']');
                    }
                }
                
                if (string.IsNullOrWhiteSpace(rawMatch))
                    rawMatch = image_url;

                // Ensure it's an absolute URL targeting the backend static files
                if (!string.IsNullOrWhiteSpace(rawMatch) && !rawMatch.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    // Use the central BaseURL and remove the /api suffix to get the host root
                    string hostUrl = AppConfig.BaseUrl.Replace("/api", "").TrimEnd('/');
                    
                    // Safely get the filename only, regardless of existing path segments or separators
                    string fileName = System.IO.Path.GetFileName(rawMatch);
                    
                    // All restaurant images are served from the /images/ folder on the backend
                    return $"{hostUrl}/images/{fileName}";
                }
                
                return rawMatch;
            }
        }
    }

    public class CategoryItem
    {
        public int category_id { get; set; }
        public string? category_name { get; set; }
        public string? description { get; set; }
        public string? BackgroundColor { get; set; }
        public string? TextColor { get; set; }
        public string? ImageUrl { get; set; }
    }
}
