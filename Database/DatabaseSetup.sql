IF DB_ID(N'TechNestDb') IS NULL
    CREATE DATABASE TechNestDb;
GO
USE TechNestDb;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products (
        ProductId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        Code NVARCHAR(40) NOT NULL UNIQUE,
        Brand NVARCHAR(80) NOT NULL,
        Category NVARCHAR(80) NOT NULL,
        UnitPrice DECIMAL(12,2) NOT NULL CHECK (UnitPrice >= 0),
        Quantity INT NOT NULL CHECK (Quantity >= 0),
        ReorderLevel INT NOT NULL DEFAULT 5 CHECK (ReorderLevel >= 0),
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF OBJECT_ID(N'dbo.Sales', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sales (
        SaleId INT IDENTITY(1,1) PRIMARY KEY,
        SaleDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        TotalAmount DECIMAL(12,2) NOT NULL CHECK (TotalAmount >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.SaleDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SaleDetails (
        SaleDetailId INT IDENTITY(1,1) PRIMARY KEY,
        SaleId INT NOT NULL REFERENCES dbo.Sales(SaleId),
        ProductId INT NOT NULL REFERENCES dbo.Products(ProductId),
        ProductName NVARCHAR(120) NOT NULL,
        Quantity INT NOT NULL CHECK (Quantity > 0),
        UnitPrice DECIMAL(12,2) NOT NULL CHECK (UnitPrice >= 0),
        LineTotal AS (CONVERT(DECIMAL(12,2), Quantity * UnitPrice)) PERSISTED
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    INSERT INTO dbo.Products (Name, Code, Brand, Category, UnitPrice, Quantity, ReorderLevel)
    VALUES
    (N'Wireless Mouse', N'TECH-001', N'Logitech', N'Accessories', 599.00, 25, 5),
    (N'Mechanical Keyboard', N'TECH-002', N'Redragon', N'Accessories', 1899.00, 12, 4),
    (N'USB-C Hub', N'TECH-003', N'Anker', N'Connectivity', 1299.00, 8, 3);
END;
GO
