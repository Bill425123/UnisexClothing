-- UniStyle E-commerce Database Creation Script
-- =============================================

USE master;
GO

-- Drop database if exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'UniStyleDB')
BEGIN
    ALTER DATABASE UniStyleDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE UniStyleDB;
END
GO

-- Create database
CREATE DATABASE UniStyleDB;
GO

USE UniStyleDB;
GO

-- =============================================
-- 1. Users Table
-- =============================================
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(20),
    Address NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);
GO

-- =============================================
-- 2. Categories Table
-- =============================================
CREATE TABLE Categories (
    CategoryID INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL,
    CategoryImage NVARCHAR(255),
    Description NVARCHAR(500),
    DisplayOrder INT DEFAULT 0,
    IsActive BIT DEFAULT 1
);
GO

-- =============================================
-- 3. Products Table
-- =============================================
CREATE TABLE Products (
    ProductID INT PRIMARY KEY IDENTITY(1,1),
    ProductName NVARCHAR(200) NOT NULL,
    ProductImage NVARCHAR(255),
    Price DECIMAL(18,2) NOT NULL,
    DiscountPercent INT DEFAULT 0,
    Description NTEXT,
    CategoryID INT NOT NULL,
    StockQuantity INT DEFAULT 0,
    Rating DECIMAL(2,1) DEFAULT 0.0,
    ViewCount INT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);
GO

-- =============================================
-- 4. ProductImages Table
-- =============================================
CREATE TABLE ProductImages (
    ImageID INT PRIMARY KEY IDENTITY(1,1),
    ProductID INT NOT NULL,
    ImageUrl NVARCHAR(255) NOT NULL,
    IsPrimary BIT DEFAULT 0,
    DisplayOrder INT DEFAULT 0,
    CONSTRAINT FK_ProductImages_Products FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE
);
GO

-- =============================================
-- 5. ProductVariants Table
-- =============================================
CREATE TABLE ProductVariants (
    VariantID INT PRIMARY KEY IDENTITY(1,1),
    ProductID INT NOT NULL,
    Color NVARCHAR(50),
    Size NVARCHAR(10),
    StockQuantity INT DEFAULT 0,
    AdditionalPrice DECIMAL(18,2) DEFAULT 0,
    CONSTRAINT FK_ProductVariants_Products FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE
);
GO

-- =============================================
-- 6. Cart Table
-- =============================================
CREATE TABLE Cart (
    CartID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    ProductID INT NOT NULL,
    VariantID INT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    AddedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Cart_Users FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    CONSTRAINT FK_Cart_Products FOREIGN KEY (ProductID) REFERENCES Products(ProductID),
    CONSTRAINT FK_Cart_Variants FOREIGN KEY (VariantID) REFERENCES ProductVariants(VariantID)
);
GO

-- =============================================
-- 7. Orders Table
-- =============================================
CREATE TABLE Orders (
    OrderID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    ShippingAddress NVARCHAR(500) NOT NULL,
    SubTotal DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) DEFAULT 0,
    ShippingFee DECIMAL(18,2) DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL,
    OrderStatus NVARCHAR(50) DEFAULT N'Chờ xác nhận',
    OrderDate DATETIME DEFAULT GETDATE(),
    DeliveredDate DATETIME NULL,
    Notes NTEXT,
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

-- =============================================
-- 8. OrderDetails Table
-- =============================================
CREATE TABLE OrderDetails (
    OrderDetailID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT NOT NULL,
    ProductID INT NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    ProductImage NVARCHAR(255),
    Color NVARCHAR(50),
    Size NVARCHAR(10),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);
GO

-- =============================================
-- 9. Favorites Table
-- =============================================
CREATE TABLE Favorites (
    FavoriteID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    ProductID INT NOT NULL,
    AddedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    CONSTRAINT FK_Favorites_Products FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE
);
GO

-- =============================================
-- 10. Reviews Table
-- =============================================
CREATE TABLE Reviews (
    ReviewID INT PRIMARY KEY IDENTITY(1,1),
    ProductID INT NOT NULL,
    UserID INT NOT NULL,
    Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    Comment NTEXT,
    CreatedAt DATETIME DEFAULT GETDATE(),
    IsApproved BIT DEFAULT 0,
    CONSTRAINT FK_Reviews_Products FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_Users FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

-- =============================================
-- Create Indexes for Performance
-- =============================================

-- Users
CREATE INDEX IDX_Users_Email ON Users(Email);

-- Products
CREATE INDEX IDX_Products_CategoryID ON Products(CategoryID);
CREATE INDEX IDX_Products_Price ON Products(Price);
CREATE INDEX IDX_Products_Rating ON Products(Rating);
CREATE INDEX IDX_Products_IsActive ON Products(IsActive);

-- Orders
CREATE INDEX IDX_Orders_UserID ON Orders(UserID);
CREATE INDEX IDX_Orders_OrderDate ON Orders(OrderDate);
CREATE INDEX IDX_Orders_OrderStatus ON Orders(OrderStatus);

-- Cart
CREATE INDEX IDX_Cart_UserID ON Cart(UserID);
CREATE INDEX IDX_Cart_ProductID ON Cart(ProductID);

-- OrderDetails
CREATE INDEX IDX_OrderDetails_OrderID ON OrderDetails(OrderID);
CREATE INDEX IDX_OrderDetails_ProductID ON OrderDetails(ProductID);

-- Favorites
CREATE INDEX IDX_Favorites_UserID ON Favorites(UserID);
CREATE INDEX IDX_Favorites_ProductID ON Favorites(ProductID);

-- Reviews
CREATE INDEX IDX_Reviews_ProductID ON Reviews(ProductID);
CREATE INDEX IDX_Reviews_UserID ON Reviews(UserID);

GO

-- =============================================
-- Insert Sample Data
-- =============================================

-- Categories
INSERT INTO Categories (CategoryName, CategoryImage, Description, DisplayOrder) VALUES
(N'Áo', '/images/HinhDanhMucUnisex/Áo.jpg', N'Áo thời trang unisex', 1),
(N'Quần', '/images/HinhDanhMucUnisex/Quần.png', N'Quần thời trang unisex', 2),
(N'Phụ kiện', '/images/HinhDanhMucUnisex/Phụ kiện.jpg', N'Phụ kiện thời trang', 3);
GO

-- Products
INSERT INTO Products (ProductName, ProductImage, Price, DiscountPercent, Description, CategoryID, StockQuantity, Rating) VALUES
(N'Áo Phông UniStyle Classic', '/images/HinhSanPhamUnisex/Áo phông UniSex 1.png', 399000, 25, N'Áo phông unisex chất liệu cotton cao cấp', 1, 100, 4.5),
(N'Áo Polo Premium', '/images/HinhSanPhamUnisex/Áo polo unisex.png', 450000, 20, N'Áo polo phối màu nam nữ', 1, 80, 4.8),
(N'Áo Khoác Bomber', '/images/HinhSanPhamUnisex/Áo khoác BomBer.png', 650000, 15, N'Áo khoác bomber thời trang', 1, 50, 4.7),
(N'Quần Short Thể Thao', '/images/HinhSanPhamUnisex/Quần short thể thao Unisex.png', 299000, 20, N'Quần short thể thao thoáng mát', 2, 120, 4.6),
(N'Quần Jeans Slim Fit', '/images/HinhSanPhamUnisex/Quần Jeans Unisex.png', 550000, 10, N'Quần jeans unisex co giãn', 2, 90, 4.5),
(N'Quần Jogger Basic', '/images/HinhSanPhamUnisex/Quần Jogger Unisex.png', 420000, 15, N'Quần jogger phong cách streetwear', 2, 100, 4.7),
(N'Nón Snapback', '/images/HinhSanPhamUnisex/Nón Snapback UniSex.png', 199000, 25, N'Nón snapback thời trang', 3, 150, 4.4),
(N'Ba Lô Canvas', '/images/HinhSanPhamUnisex/Ba Lô Unisex.png', 399000, 20, N'Ba lô canvas đa năng', 3, 70, 4.6),
(N'Giày Sneaker White', '/images/HinhSanPhamUnisex/Giày sneaker unisex.png', 799000, 30, N'Giày sneaker trắng basic', 3, 60, 4.8),
(N'Áo Hoodie Oversize', '/images/HinhSanPhamUnisex/Áo hoodie unisex.png', 550000, 15, N'Áo hoodie oversize form rộng', 1, 80, 4.7),
(N'Áo Thun Dài Tay', '/images/HinhSanPhamUnisex/Áo thun dài tay unisex.png', 350000, 20, N'Áo thun dài tay basic', 1, 110, 4.5),
(N'Quần Kaki Túi Hộp', '/images/HinhSanPhamUnisex/Quần Kaki túi hộp Unisex.png', 480000, 15, N'Quần kaki túi hộp phong cách', 2, 85, 4.6);
GO

-- Sample User
INSERT INTO Users (FullName, Email, Password, PhoneNumber, Address) VALUES
(N'Nguyễn Văn A', 'nguyenvana@gmail.com', 'hashed_password_here', '0901234567', N'123 Đường ABC, Quận 1, TP.HCM');
GO

PRINT 'Database created successfully!';
GO
SELECT * FROM Products;
-- 1. Áo Phông UniStyle Classic -> Áo Phông UniStyle.jpg (có file)
UPDATE Products 
SET ProductImage = '/images/HinhSanPhamUnisex/Áo Phông UniStyle.jpg' 
WHERE ProductName = N'Áo Phông UniStyle Classic' OR ProductName LIKE N'%Áo Phông UniStyle%';
GO

-- 2. Áo Polo Premium -> Áo channe.jpg (có file)
UPDATE Products 
SET ProductImage = '/images/HinhSanPhamUnisex/Áo channe.jpg' 
WHERE ProductName = N'Áo Polo Premium';
GO

-- 3. Áo Khoác Bomber -> Áo Khoác UniStyle.jpeg (có file)
UPDATE Products 
SET ProductImage = '/images/HinhSanPhamUnisex/Áo Khoác UniStyle.jpeg' 
WHERE ProductName = N'Áo Khoác Bomber';
GO

-- 4. Quần Short Thể Thao -> Quần Short UniStyle.jpg (có file)
UPDATE Products 
SET ProductImage = '/images/HinhSanPhamUnisex/Quần Short UniStyle.jpg' 
WHERE ProductName = N'Quần Short Thể Thao';
GO

-- 5. Quần Jeans Slim Fit -> Quần Jean UniStyle.jpeg (có file)
UPDATE Products 
SET ProductImage = '/images/HinhSanPhamUnisex/Quần Jean UniStyle.jpeg' 
WHERE ProductName = N'Quần Jeans Slim Fit';
GO

-- 6. Quần Jogger Basic -> Quần Gucchi.jpg (có file)
UPDATE Products 
SET ProductImage = '/images/HinhSanPhamUnisex/Quần Gucchi.jpg' 
WHERE ProductName = N'Quần Jogger Basic';
GO

-- 7. Nón Snapback -> Mũ Snapback UniStyle.jpg (có file)
UPDATE Products 
SET ProductImage = '/images/HinhSanPhamUnisex/Mũ Snapback UniStyle.jpg' 
WHERE ProductName = N'Nón Snapback';
GO

-- 8. Ba Lô Canvas -> Balo UniStyle.jpg (có file)
UPDATE Products 
SET ProductImage = '/images/HinhSanPhamUnisex/Balo UniStyle.jpg' 
WHERE ProductName = N'Ba Lô Canvas';
GO

-- 9. Giày Sneaker White -> Giày Sneaker UniStyle.jpg (có file)
UPDATE Products 
SET ProductImage = '/images/HinhSanPhamUnisex/Giày Sneaker UniStyle.jpg' 
WHERE ProductName = N'Giày Sneaker White';
GO

-- 10. Áo Hoodie Oversize -> Áo Hoodie UniStyle.jpg (có file)
UPDATE Products 
SET ProductImage = '/images/HinhSanPhamUnisex/Áo Hoodie UniStyle.jpg' 
WHERE ProductName = N'Áo Hoodie Oversize';
GO

-- 11. Áo Thun Dài Tay -> Áo Hoodie UniStyle.jpg (fallback, có file)
UPDATE Products 
SET ProductImage = '/images/HinhSanPhamUnisex/Áo Hoodie UniStyle.jpg' 
WHERE ProductName = N'Áo Thun Dài Tay';
GO

-- 12. Quần Kaki Túi Hộp -> Quần Jean UniStyle.jpeg (fallback, có file)
UPDATE Products 
SET ProductImage = '/images/HinhSanPhamUnisex/Quần Jean UniStyle.jpeg' 
WHERE ProductName = N'Quần Kaki Túi Hộp';
GO
SELECT ProductID, ProductName, ProductImage 
FROM Products 
ORDER BY ProductID;
GO
-----
USE UniStyleDB;
GO

-- Kiểm tra xem bảng Users có tồn tại không
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    PRINT 'Đang thêm dữ liệu mẫu cho người dùng...';
    
    -- Xóa dữ liệu cũ nếu muốn (bỏ comment nếu cần)
    -- DELETE FROM [dbo].[Users];
    -- PRINT 'Đã xóa dữ liệu cũ';
    
    -- Kiểm tra và thêm Admin đầu tiên (nếu chưa có)
    IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE Email = 'admin@unistyle.com')
    BEGIN
        INSERT INTO [dbo].[Users] (FullName, Email, Password, PhoneNumber, Address, Role, IsActive, CreatedAt)
        VALUES 
        (N'Admin UniStyle', 'admin@unistyle.com', 'admin123', '0123456789', N'TP. Hồ Chí Minh', 'admin', 1, GETDATE());
        PRINT 'Đã thêm Admin: admin@unistyle.com (Password: admin123)';
    END
    ELSE
    BEGIN
        PRINT 'Admin đã tồn tại, bỏ qua';
    END
    
    -- Thêm các khách hàng mẫu
    IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE Email = 'nguyenvana@gmail.com')
    BEGIN
        INSERT INTO [dbo].[Users] (FullName, Email, Password, PhoneNumber, Address, Role, IsActive, CreatedAt)
        VALUES 
        (N'Nguyễn Văn A', 'nguyenvana@gmail.com', '12345678', '0901234567', N'123 Đường ABC, Quận 1, TP. Hồ Chí Minh', 'customer', 1, DATEADD(day, -30, GETDATE())),
        (N'Trần Thị B', 'tranthib@gmail.com', '12345678', '0987654321', N'456 Đường XYZ, Quận 3, TP. Hồ Chí Minh', 'customer', 1, DATEADD(day, -25, GETDATE())),
        (N'Lê Văn C', 'levanc@gmail.com', '12345678', '0912345678', N'789 Đường DEF, Quận 5, TP. Hồ Chí Minh', 'customer', 1, DATEADD(day, -20, GETDATE())),
        (N'Phạm Thị D', 'phamthid@gmail.com', '12345678', '0923456789', N'321 Đường GHI, Quận 7, TP. Hồ Chí Minh', 'customer', 1, DATEADD(day, -15, GETDATE())),
        (N'Hoàng Văn E', 'hoangvane@gmail.com', '12345678', '0934567890', N'654 Đường JKL, Quận 10, TP. Hồ Chí Minh', 'customer', 1, DATEADD(day, -10, GETDATE())),
        (N'Nguyễn Thị F', 'nguyenthif@gmail.com', '12345678', '0945678901', N'987 Đường MNO, Quận 12, TP. Hồ Chí Minh', 'customer', 1, DATEADD(day, -5, GETDATE())),
        (N'Võ Văn G', 'vovang@gmail.com', '12345678', '0956789012', N'147 Đường PQR, Quận Bình Thạnh, TP. Hồ Chí Minh', 'customer', 1, DATEADD(day, -3, GETDATE())),
        (N'Đặng Thị H', 'dangthih@gmail.com', '12345678', '0967890123', N'258 Đường STU, Quận Tân Bình, TP. Hồ Chí Minh', 'customer', 1, DATEADD(day, -2, GETDATE())),
        (N'Bùi Văn I', 'buivani@gmail.com', '12345678', '0978901234', N'369 Đường VWX, Quận Gò Vấp, TP. Hồ Chí Minh', 'customer', 0, DATEADD(day, -1, GETDATE())),
        (N'Lý Thị K', 'lythik@gmail.com', '12345678', '0989012345', N'741 Đường YZA, Quận Phú Nhuận, TP. Hồ Chí Minh', 'customer', 1, GETDATE());
        
        PRINT 'Đã thêm 10 khách hàng mẫu';
    END
    ELSE
    BEGIN
        PRINT 'Dữ liệu khách hàng đã tồn tại, bỏ qua';
    END
    
    -- Thêm thêm một số admin khác (nếu cần)
    IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE Email = 'manager@unistyle.com')
    BEGIN
        INSERT INTO [dbo].[Users] (FullName, Email, Password, PhoneNumber, Address, Role, IsActive, CreatedAt)
        VALUES 
        (N'Quản lý Store', 'manager@unistyle.com', 'manager123', '0123456788', N'TP. Hồ Chí Minh', 'admin', 1, DATEADD(day, -60, GETDATE()));
        PRINT 'Đã thêm Manager: manager@unistyle.com (Password: manager123)';
    END
    
    PRINT 'Hoàn thành thêm dữ liệu mẫu!';
    PRINT '';
    PRINT '=== THÔNG TIN ĐĂNG NHẬP ===';
    PRINT 'Admin: admin@unistyle.com / admin123';
    PRINT 'Manager: manager@unistyle.com / manager123';
    PRINT 'Khách hàng: nguyenvana@gmail.com / 12345678';
    PRINT 'Tất cả mật khẩu khách hàng: 12345678';
END
ELSE
BEGIN
    PRINT 'LỖI: Không tìm thấy bảng Users trong database UniStyleDB';
    PRINT 'Vui lòng chạy CreateDatabase.sql hoặc UpdateUsersTable.sql trước';
END
GO