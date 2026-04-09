namespace FoodMapAdmin.Models
{
    public class DashboardStats
    {
        public int TotalPois { get; set; }

        public int TotalUsers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalOwners { get; set; }
        public int TotalCustomers { get; set; }
        public List<ActivityDto> RecentActivities { get; set; } = new();
    }

    public class ActivityDto
    {
        public string ActionType { get; set; } = ""; // e.g., "Đánh giá mới", "Người dùng mới", "Quán ăn mới"
        public string UserName { get; set; } = "";
        public string TargetName { get; set; } = "";
        public DateTime Time { get; set; }
        public string RelativeTime => GetRelativeTime(Time);

        private static string GetRelativeTime(DateTime time)
        {
            var diff = DateTime.Now - time;
            if (diff.TotalMinutes < 1) return "vừa xong";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
            return time.ToString("dd/MM/yyyy");
        }
    }
}
