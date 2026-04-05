# 🐸 Ếch Thắt Vòng - Website Bán Hàng Trực Tuyến (ASP.NET Core MVC)

![Banner](https://img.shields.io/badge/ASP.NET%20Core-8.0-blue)
![Entity Framework](https://img.shields.io/badge/EF%20Core-9.0-green)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-red)
![AI Gemini](https://img.shields.io/badge/AI-Gemini-orange)

Ếch Thắt Vòng là một dự án website thương mại điện tử chuyên cung cấp các sản phẩm thời trang. Website được xây dựng trên nền tảng **ASP.NET Core MVC**, tích hợp **AI (Google Gemini)** để hỗ trợ khách hàng và hệ thống quản trị mạnh mẽ.

---

## 📸 Demo

🔗 Website: http://echthatvong.somee.com/

---

## 🚀 Công Nghệ Sử Dụng

* **Backend:** ASP.NET Core 8.0 (MVC)
* **Database:** SQL Server
* **ORM:** Entity Framework Core 9.0
* **AI:** Google Gemini API (Chatbot)
* **Authentication:** Microsoft Identity + Google OAuth 2.0
* **Frontend:** HTML5, CSS3, JavaScript, Bootstrap 5

### 🔧 Thư viện & Công cụ

* Newtonsoft.Json
* RestSharp
* SendGrid / SMTP (Email)
* SignalR (Realtime)

---

## ✨ Tính Năng Nổi Bật

### 👤 Khách Hàng

* Duyệt & lọc sản phẩm theo danh mục, thương hiệu
* Tìm kiếm sản phẩm
* Giỏ hàng linh hoạt
* Thanh toán:

  * Tính phí ship theo địa chỉ
  * Áp dụng mã giảm giá
  * Quản lý nhiều địa chỉ
* Đăng nhập (Email / Google)
* Xem lịch sử đơn hàng
* Chatbot AI tư vấn 24/7

### 🛡️ Quản Trị Viên

* Dashboard thống kê
* Quản lý sản phẩm, danh mục, thương hiệu
* Quản lý đơn hàng
* Phân quyền người dùng
* Quản lý mã giảm giá
* Cấu hình phí vận chuyển
* Quản lý chatbot (Quick Replies)

---

## ⚡ Quick Start

```bash
git clone https://github.com/Letri88/EchThatVong.git
cd EchThatVong
```

---

## 🗄️ Database Setup

File database nằm tại:

```
/Database/EchThatVongDb.sql
```

### 🚀 Cách import:

1. Mở **SQL Server Management Studio (SSMS)**
2. Tạo database mới:

```
EchThatVongDb
```

3. Mở file:

```
Database/EchThatVongDb.sql
```

4. Nhấn **Execute**

👉 Sau khi chạy xong, database sẽ có đầy đủ bảng + dữ liệu mẫu.

---

## ⚙️ Cấu Hình Dự Án

### 1. Connection String

Mở `appsettings.json`:

```json
"ConnectionStrings": {
  "ConnectedDb": "Server=YOUR_SERVER;Database=EchThatVongDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

---

### 2. API Keys (Tùy chọn)

* Google Login:

  * `GoogleKeys:ClientId`
  * `GoogleKeys:ClientSecret`

* Gemini AI:

  * API Key cho `GeminiChatService`

---

### 3. Migration (nếu cần)

```powershell
Update-Database
```

---

### 4. Chạy ứng dụng

```bash
dotnet run
```

Hoặc nhấn **F5** trong Visual Studio

---

## 📂 Cấu Trúc Thư Mục

* `Areas/Admin` → Giao diện & logic quản trị
* `Controllers` → Xử lý request
* `Models` → Entity & ViewModel
* `Repository` → Data access
* `Services` → Email, AI
* `wwwroot` → CSS, JS, Images
* `Database` → File SQL

---

## 📝 Giấy Phép

Dự án phục vụ mục đích học tập và tham khảo.

---

## 📧 Liên Hệ

* 👤 **Lê Văn Trí**
* 📩 Email: [tri962005@gmail.com](mailto:tri962005@gmail.com)
* 💻 GitHub: https://github.com/Letri88

---

⭐ Nếu thấy hữu ích, hãy cho repo một star nhé!
