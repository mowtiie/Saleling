CREATE TABLE ProductVariants (
    VariantID INT PRIMARY KEY IDENTITY(1,1),
    ProductID INT NOT NULL,
    SKU VARCHAR(100) UNIQUE NOT NULL,
    Size VARCHAR(20) NOT NULL,
    Color VARCHAR(50) NOT NULL,
    CostPrice DECIMAL(12, 2) NOT NULL,
    SellingPrice DECIMAL(12, 2) NOT NULL,
    StockQuantity INT NOT NULL DEFAULT 0 CONSTRAINT CK_ProductVariants_StockQuantity CHECK (StockQuantity >= 0),
    ReorderLevel INT NOT NULL DEFAULT 5,
    Status VARCHAR(20) NOT NULL CHECK (Status IN ('Active', 'Discontinued')) DEFAULT 'Active',

    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);
