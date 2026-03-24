# 📌 (PRD) - BUI VIEN AUDIO GUIDE

## 📑 1. Thông tin tài liệu
* **Sản phẩm:** Bui Vien Audio Guide (Hệ thống hướng dẫn ẩm thực tự động) 
* **Ngày cập nhật:** 25/03/2026
* **Nền tảng phát triển:** 
 **Mobile:** .NET MAUI (C#) 
 *  **Web Admin:** ASP.NET Core Razor Pages (.NET 8.0)


## 🎯 2. Tầm nhìn & Mục tiêu sản phẩm

### 🌟 Tóm tắt sản phẩm :
* **Ứng dụng Mobile:** Trải nghiệm nghe thuyết minh tự động dựa trên vị trí GPS.
* **Web Admin:** Quản trị toàn bộ "linh hồn" của ứng dụng (nội dung, tọa độ, âm thanh).

### 🎯 Mục tiêu cốt lõi :
* **🤖 Tự động hóa:** Tự động kích hoạt thuyết minh khi khách vào vùng Geofencing.
* **🎧 Chuẩn hóa trải nghiệm:** Mang lại cảm giác có hướng dẫn viên riêng cho từng thực khách.
* **📶 Offline First:** Đảm bảo hoạt động mượt mà ngay cả khi mất kết nối mạng.


##  3. Phân tích chức năng hệ thống

### 📱 3.1 Phân hệ Mobile App (Dành cho du khách)
* **🗺️ Interactive Map (Map View):** Hiển thị bản đồ trực quan, vị trí thực và các điểm POI (Point of Interest) xung quanh.
* **🛰️ Real-time GPS Tracking:** Theo dõi tọa độ liên tục ở cả chế độ **Foreground** (đang mở) và **Background** (chạy nền).
* **🔔 Geofence Trigger:** Tự động phát âm thanh khi chạm "vùng xanh" của địa điểm.
* **🎙️ Narration Engine:** Hỗ trợ linh hoạt cả **Text-To-Speech (TTS)** và **Audio thu sẵn**.
* **🔍 QR Code Activation:** Giải pháp dự phòng khi sóng GPS yếu (Quét để nghe ngay).

### 🌐 3.2 Phân hệ Web Admin (Dành cho Quản trị)
* **📝 CMS POI:** Quản lý toàn diện Tên, Mô tả, Hình ảnh và Tọa độ GPS chính xác.
* **⚙️ Cấu hình Geofence:** Tùy chỉnh bán kính (Radius) và mức độ ưu tiên hiển thị.
* **📈 Smart Analytics:** Xem thống kê mức độ quan tâm của du khách và **Heatmap** (Bản đồ nhiệt) mật độ di chuyển.



## ⚙️ 4. Yêu cầu kỹ thuật (Technical Requirements)

| Thành phần | Công nghệ sử dụng | Vai trò |
| :--- | :--- | :--- |
| **Mobile Framework** | .NET 8.0, .NET MAUI | Phát triển ứng dụng đa nền tảng (Android/iOS) |
| **Web Framework** | ASP.NET Core Razor Pages | Xây dựng trang quản trị và API dữ liệu |
| **Data Storage** | SQLite (Mobile) & SQL Server (Web) | Lưu trữ dữ liệu cục bộ và tập trung |
| **Data Format** | JSON (`pois.json`) | Ngôn ngữ chung để đồng bộ giữa Web và App |
| **Map Engine** | Google Maps SDK / Maui.Controls | Hiển thị bản đồ và xử lý địa lý |

## 🔄 5. Luồng hoạt động hệ thống (System Workflows)

### 🖥️ 5.1. Luồng nhập dữ liệu (Admin Workflow)
*Dành cho quản trị viên quản lý nội dung*
* **Bước 1:** Admin truy cập vào Dashboard quản trị tại: `https://localhost:7130/POIs`.
* **Bước 2:** Admin thực hiện các tác vụ **CRUD** (Thêm mới, Chỉnh sửa, Xóa) địa điểm.
* **Bước 3:** Xác định tọa độ bằng cách nhập `Latitude`/`Longitude` và thiết lập `Radius` (Bán kính kích hoạt).
* **Bước 4:** Nhấn **Save**, dữ liệu được lưu xuống **Database SQL** thông qua Entity Framework.



### 🌐 5.2. Luồng cung cấp dữ liệu (Backend/API Workflow)
*Cầu nối giữa Web và App thông qua JSON*
* **Endpoint:** Hệ thống cung cấp API tại `/api/PoiApi`.
* **Xử lý:** Server truy vấn dữ liệu từ MySQL/SQL Server khi nhận được yêu cầu.
* **Định dạng:** Chuyển đổi dữ liệu C# sang định dạng **JSON** chuẩn.
* **Phản hồi:** Gửi gói tin JSON (chứa tọa độ, mô tả, audio link) về cho App Mobile.


### 📱 5.3. Luồng trải nghiệm người dùng (Mobile App Workflow)
*Quy trình tự động hóa trên thiết bị của du khách*
1.  📥 **Tải dữ liệu:** App gọi API để lấy danh sách POI và hiển thị lên bản đồ.
2.  🛰️ **Theo dõi vị trí:** Sử dụng GPS để cập nhật vị trí thực của khách liên tục.
3.  📐 **Tính toán:** App so sánh khoảng cách giữa khách và các tọa độ POI.
4.  🔊 **Kích hoạt:** * Nếu `Khoảng cách < Radius`: App tự động phát bài thuyết minh (Audio/TTS).
    * Nếu `Khoảng cách > Radius`: App tiếp tục ở chế độ chờ.





