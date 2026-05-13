using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MapAppLoadTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // URL của API đang chạy local
            // Lưu ý: Đảm bảo Port 5000 là đúng với Port API của bạn đang chạy
            string url = "http://localhost:5000/api/food/16/guide?lang=vi&userId=";
            int totalRequests = 1000;
            
            using var client = new HttpClient();
            // Tăng timeout lên 2 phút để tránh bị ngắt kết nối khi server xử lý hàng đợi lớn
            client.Timeout = TimeSpan.FromMinutes(2); 

            Console.WriteLine($"--- DANG KHOI CHAY LOAD TEST: {totalRequests} request ---");
            Console.WriteLine($"URL: {url}[1-{totalRequests}]");
            var sw = Stopwatch.StartNew();

            // Tạo 1000 task cùng lúc (Sử dụng ID âm để giả lập Guest)
            var tasks = Enumerable.Range(1, totalRequests).Select(async i =>
            {
                int virtualUserId = -i; // Giả lập ID khách (âm)
                var perRequest = Stopwatch.StartNew();
                try
                {
                    var response = await client.GetAsync(url + virtualUserId);
                    perRequest.Stop();
                    return new
                    {
                        UserId = virtualUserId,
                        Status = (int)response.StatusCode,
                        Ms = perRequest.ElapsedMilliseconds,
                        Success = response.IsSuccessStatusCode
                    };
                }
                catch (Exception)
                {
                    perRequest.Stop();
                    return new
                    {
                        UserId = virtualUserId,
                        Status = 0, // Lỗi kết nối hoặc Timeout
                        Ms = perRequest.ElapsedMilliseconds,
                        Success = false
                    };
                }
            }).ToArray();

            // Chờ tất cả request hoàn tất
            var results = await Task.WhenAll(tasks);
            sw.Stop();

            // Thống kê kết quả
            var success = results.Count(r => r.Success);
            var fail = results.Count(r => !r.Success);
            var avgMs = results.Any() ? results.Average(r => r.Ms) : 0;
            var maxMs = results.Any() ? results.Max(r => r.Ms) : 0;
            var minMs = results.Any() ? results.Min(r => r.Ms) : 0;

            Console.WriteLine($"\n=== KET QUA LOAD TEST ===");
            Console.WriteLine($"Tong request  : {totalRequests}");
            Console.WriteLine($"Thanh cong    : {success}");
            Console.WriteLine($"That bai      : {fail}");
            Console.WriteLine($"Thoi gian min : {minMs}ms");
            Console.WriteLine($"Thoi gian avg : {avgMs:F1}ms");
            Console.WriteLine($"Thoi gian max : {maxMs}ms");
            Console.WriteLine($"Tong thoi gian thuc thi: {sw.ElapsedMilliseconds}ms");

            // In 5 request chậm nhất để phân tích nghẽn
            Console.WriteLine($"\n--- 5 request cham nhat ---");
            foreach (var r in results.OrderByDescending(r => r.Ms).Take(5))
            {
                string statusText = r.Status == 0 ? "Error/Timeout" : r.Status.ToString();
                Console.WriteLine($"  User {r.UserId}: {r.Ms}ms (status {statusText})");
            }

            Console.WriteLine("\nTest hoan tat. Nhan phim bat ky de thoat...");
            Console.ReadKey();
        }
    }
}
