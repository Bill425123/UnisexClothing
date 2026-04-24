# 🎯 HƯỚNG DẪN SỬ DỤNG ADMIN PANEL - UniStyle

## 📋 Mục lục

1. [Đăng nhập Admin](#đăng-nhập-admin)
2. [Chức năng Dashboard](#dashboard)
3. [Quản lý Sản phẩm](#quản-lý-sản-phẩm)
4. [Quản lý Đơn hàng](#quản-lý-đơn-hàng)
5. [Quản lý Người dùng](#quản-lý-người-dùng)
6. [API Endpoints](#api-endpoints)

---

## 🔐 Đăng nhập Admin

### URL Đăng nhập
```
https://localhost:7056/Admin/Login
hoặc
http://localhost:5000/Admin/Login
```

### Thông tin đăng nhập Demo
```
Username: admin
Password: admin123
```

### Đăng xuất
- Nhấn vào avatar góc phải → Đăng xuất
- Hoặc truy cập: `/Admin/Logout`

---

## 📊 Dashboard

### URL
```
/Admin/Dashboard
hoặc
/Admin
```

### Thống kê hiển thị

#### Tổng quan (Statistics Cards)
- **Tổng sản phẩm**: 12 sản phẩm
- **Tổng đơn hàng**: 25 đơn
- **Người dùng**: 150 người
- **Doanh thu**: 15.750.000₫

#### Biểu đồ
1. **Doanh thu theo tháng** (Line Chart)
   - Hiển thị doanh thu 12 tháng
   - Có thể nhìn thấy xu hướng tăng trưởng

2. **Sản phẩm bán chạy** (Doughnut Chart)
   - Phân loại theo danh mục: Áo, Quần, Phụ kiện
   - Tỷ lệ %: 45%, 35%, 20%

#### Đơn hàng gần đây
- 3 đơn hàng mới nhất
- Hiển thị: Mã ĐH, Khách hàng, Tổng tiền, Trạng thái
- Nút "Xem tất cả" → chuyển đến quản lý đơn hàng

---

## 📦 Quản lý Sản phẩm

### URL
```
/Admin/Products
```

### Chức năng

#### 1. Danh sách sản phẩm
- Hiển thị tất cả 12 sản phẩm
- Thông tin: Hình ảnh, Tên, Danh mục, Giá, Giảm giá, Tồn kho, Trạng thái

#### 2. Thêm sản phẩm mới
- Nhấn nút **"Thêm sản phẩm mới"**
- Chuyển đến `/Admin/ProductEdit`

#### 3. Sửa sản phẩm
- Nhấn icon **Sửa** (✏️) trên mỗi sản phẩm
- Chuyển đến `/Admin/ProductEdit/{id}`

#### 4. Xóa sản phẩm
- Nhấn icon **Xóa** (🗑️)
- Hiện popup xác nhận
- Sản phẩm sẽ bị xóa khỏi danh sách

#### 5. Lọc & Tìm kiếm
- **Tìm kiếm**: Nhập tên sản phẩm
- **Lọc theo danh mục**: Áo, Quần, Phụ kiện
- **Lọc theo trạng thái**: Đang bán, Tạm ngừng

#### 6. Phân trang
- Hiển thị 12 sản phẩm/trang
- Nút: Trước, 1, 2, Sau

### Danh sách sản phẩm hiện có

| ID | Tên sản phẩm | Giá | Giảm giá | Danh mục |
|----|--------------|-----|----------|----------|
| 1 | Áo Phông UniStyle Classic | 399.000₫ | 25% | Áo |
| 2 | Áo Polo Premium | 450.000₫ | 20% | Áo |
| 3 | Áo Khoác Bomber | 650.000₫ | 15% | Áo |
| 4 | Quần Short Thể Thao | 299.000₫ | 20% | Quần |
| 5 | Quần Jeans Slim Fit | 550.000₫ | 10% | Quần |
| 6 | Quần Jogger Basic | 420.000₫ | 15% | Quần |
| 7 | Nón Snapback | 199.000₫ | 25% | Phụ kiện |
| 8 | Ba Lô Canvas | 399.000₫ | 20% | Phụ kiện |
| 9 | Giày Sneaker White | 799.000₫ | 30% | Phụ kiện |
| 10 | Áo Hoodie Oversize | 550.000₫ | 15% | Áo |
| 11 | Áo Thun Dài Tay | 350.000₫ | 20% | Áo |
| 12 | Quần Kaki Túi Hộp | 480.000₫ | 15% | Quần |

---

## 🛒 Quản lý Đơn hàng

### URL
```
/Admin/Orders
```

### Chức năng

#### 1. Thống kê trạng thái
- **Chờ xác nhận**: 8 đơn
- **Đang giao**: 5 đơn
- **Hoàn thành**: 10 đơn
- **Đã hủy**: 2 đơn

#### 2. Danh sách đơn hàng
Hiển thị:
- Mã đơn hàng
- Thông tin khách hàng (Tên, SĐT)
- Số lượng sản phẩm
- Tổng tiền
- Phương thức thanh toán
- Trạng thái
- Ngày đặt

#### 3. Cập nhật trạng thái
Chọn dropdown trạng thái:
- ⏳ Chờ xác nhận
- ⚙️ Đang xử lý
- 🚚 Đang giao
- ✅ Hoàn thành
- ❌ Đã hủy

Khi thay đổi → Popup xác nhận → Cập nhật

#### 4. Xem chi tiết đơn hàng
- Nhấn icon **Xem** (👁️)
- Chuyển đến `/Admin/OrderDetail/{id}`
- Hiển thị: Sản phẩm, Địa chỉ giao hàng, Lịch sử trạng thái

#### 5. In hóa đơn
- Nhấn icon **In** (🖨️)
- Xuất hóa đơn dạng PDF (sắp có)

#### 6. Lọc & Tìm kiếm
- **Tìm kiếm**: Mã đơn hàng, Tên khách hàng
- **Lọc theo trạng thái**: Dropdown
- **Lọc theo ngày**: Date picker

---

## 👥 Quản lý Người dùng

### URL
```
/Admin/Users
```

### Chức năng

#### 1. Thống kê
- **Tổng người dùng**: 150
- **Đang hoạt động**: 142
- **Người dùng mới (tháng này)**: 12

#### 2. Danh sách người dùng
Hiển thị:
- ID
- Họ tên + Avatar
- Email
- Số điện thoại
- Số đơn hàng đã mua
- Trạng thái (Hoạt động / Khóa)
- Ngày đăng ký

#### 3. Thêm người dùng mới
Nhấn **"Thêm người dùng"** → Popup form:
- Họ tên
- Email
- Số điện thoại
- Mật khẩu
- Vai trò (Khách hàng / Admin)

#### 4. Thao tác với người dùng
- **Xem chi tiết** (👁️): Xem thông tin đầy đủ
- **Chỉnh sửa** (✏️): Sửa thông tin
- **Khóa tài khoản** (🔒): Vô hiệu hóa tài khoản

#### 5. Lọc & Tìm kiếm
- **Tìm kiếm**: Tên, Email, SĐT
- **Lọc theo vai trò**: Admin, Khách hàng
- **Lọc theo trạng thái**: Hoạt động, Khóa

---

## 🔌 API Endpoints

### 1. Get Dashboard Stats
```javascript
GET /Admin/GetDashboardStats

Response:
{
  "success": true,
  "totalProducts": 12,
  "totalOrders": 25,
  "totalUsers": 150,
  "totalRevenue": 15750000,
  "todayOrders": 5,
  "pendingOrders": 8,
  "recentOrders": [...]
}
```

### 2. Update Order Status
```javascript
POST /Admin/UpdateOrderStatus
Body: {
  "orderId": 1,
  "status": "shipping"
}

Response:
{
  "success": true,
  "message": "Đã cập nhật trạng thái đơn hàng #1"
}
```

### 3. Delete Product
```javascript
POST /Admin/ProductDelete
Body: {
  "id": 5
}

Response:
{
  "success": true,
  "message": "Đã xóa sản phẩm thành công!"
}
```

---

## 🎨 Giao diện

### Màu sắc chủ đạo
- **Primary**: #667eea (Tím xanh)
- **Secondary**: #764ba2 (Tím)
- **Gradient**: Linear gradient từ Primary sang Secondary

### Layout
- **Sidebar**: Bên trái, có thể thu gọn
- **Top Navbar**: Nút toggle sidebar + User menu
- **Content Area**: Nội dung chính

### Responsive
- **Desktop**: Sidebar hiển thị đầy đủ
- **Tablet**: Sidebar có thể toggle
- **Mobile**: Sidebar ẩn mặc định, click để mở

---

## 🔧 Cấu hình

### Session Timeout
- **Thời gian**: 2 giờ (120 phút)
- Sau 2 giờ không hoạt động → Tự động đăng xuất

### Security
- **Authentication**: Session-based
- **Demo mode**: Username/Password đơn giản
- **Production**: Nên dùng ASP.NET Identity + JWT

---

## 📁 Cấu trúc File

```
UnisexClothes/
├── Controllers/
│   └── AdminController.cs         # Controller chính
├── Views/
│   ├── Admin/
│   │   ├── Login.cshtml           # Trang đăng nhập
│   │   ├── Dashboard.cshtml       # Dashboard
│   │   ├── Products.cshtml        # Quản lý sản phẩm
│   │   ├── Orders.cshtml          # Quản lý đơn hàng
│   │   └── Users.cshtml           # Quản lý người dùng
│   └── Shared/
│       └── _AdminLayout.cshtml    # Layout riêng cho Admin
├── wwwroot/
│   ├── css/
│   │   └── admin.css              # CSS cho Admin Panel
│   └── js/
│       └── admin.js               # JavaScript cho Admin
└── ADMIN_GUIDE.md                 # File này
```

---

## 🚀 Quick Start

### 1. Chạy ứng dụng
```powershell
cd UnisexClothes
dotnet run
```

### 2. Truy cập Admin
```
https://localhost:7056/Admin/Login
```

### 3. Đăng nhập
```
Username: admin
Password: admin123
```

### 4. Bắt đầu quản lý!

---

## 📝 Ghi chú

- Đây là **Full version** đã tích hợp với SQL Server database
- **Upload file không giới hạn**: Có thể upload mọi loại file và dung lượng bao nhiêu cũng được (jpg, png, pdf, zip, v.v.)
- Hình ảnh sản phẩm lấy từ `/wwwroot/images/HinhSanPhamUnisex/`
- Charts sử dụng **Chart.js**

---

## ✅ Tính năng đã có

- [x] Upload hình ảnh sản phẩm (không giới hạn loại file và dung lượng)
- [ ] Export Excel/CSV
- [ ] Gửi email thông báo
- [ ] Báo cáo chi tiết
- [ ] Quản lý danh mục
- [ ] Quản lý mã giảm giá
- [ ] Tích hợp thanh toán online

---

**🎉 Chúc bạn quản lý UniStyle hiệu quả!**


