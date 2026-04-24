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
    UpdatedAt DATETIME DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1,
    Role NVARCHAR(50) DEFAULT 'customer'
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