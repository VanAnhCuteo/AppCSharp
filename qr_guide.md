# Hướng dẫn tạo mã QR cho FoodMapApp

Bạn có thể tạo mã QR thủ công bằng bất kỳ công cụ tạo QR nào (như [QR Tiger](https://www.qrcode-tiger.com/), [The QR Code Generator](https://www.the-qr-code-generator.com/), v.v.) theo hướng dẫn dưới đây:

### 1. Định dạng đường link
Mỗi quán ăn trong hệ thống đều có một **ID** riêng. Để tạo mã QR cho một quán, bạn hãy sử dụng đường link (URL) theo định dạng sau:

**`foodmap://poi/[ID_CỦA_QUÁN]`**

---

### 2. Ví dụ cụ thể
Nếu quán của bạn có ID là **123**, đường link để tạo mã QR sẽ là:
> `foodmap://poi/123`

Nếu quán có ID là **5**, đường link sẽ là:
> `foodmap://poi/5`

---

### 3. Cách tạo mã QR
1. Mở công cụ tạo mã QR bất kỳ.
2. Chọn loại dữ liệu là **URL** hoặc **Text**.
3. Nhập đường link theo định dạng trên vào ô tương ứng.
4. Tải ảnh mã QR về và sử dụng.

---

### 4. Cách sử dụng (Dành cho người dùng)
1. Người dùng mở ứng dụng **Camera** trên điện thoại hoặc bất kỳ ứng dụng quét mã QR nào.
2. Quét mã QR đã tạo.
3. Điện thoại sẽ hiện thông báo mở ứng dụng **FoodMapApp**.
4. Khi nhấn vào, ứng dụng sẽ tự động mở trang bản đồ, hiển thị chi tiết quán ăn đó và tự động phát âm thanh hướng dẫn (Audio).

---
*Lưu ý: Đảm bảo người dùng đã cài đặt ứng dụng FoodMapApp trên điện thoại để tính năng này hoạt động.*
