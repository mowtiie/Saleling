CREATE PROCEDURE sp_ProcessSaleTransaction
(
    @CustomerID INT,
    @ProcessedByUserID INT,
    @TotalAmount DECIMAL(10, 2),
    @SaleItems udt_SaleItemType READONLY
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Prices, line totals and the sale total are recomputed from ProductVariants.
    -- The client-supplied UnitPrice and SubTotal are ignored. @TotalAmount is the total the
    -- cashier was shown; the sale is rejected if current prices no longer add up to it.
    IF NOT EXISTS (SELECT 1 FROM @SaleItems)
    BEGIN
        RAISERROR('Sale failed: No items were provided.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM @SaleItems WHERE Quantity <= 0 OR ISNULL(Discount, 0) < 0)
    BEGIN
        RAISERROR('Sale failed: Quantity must be positive and discount cannot be negative.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT VariantID FROM @SaleItems GROUP BY VariantID HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR('Sale failed: Duplicate variants in sale items.', 16, 1);
        RETURN;
    END

    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @Priced TABLE
        (
            VariantID INT PRIMARY KEY,
            Quantity INT NOT NULL,
            UnitPrice DECIMAL(10, 2) NOT NULL,
            Discount DECIMAL(10, 2) NOT NULL,
            SubTotal DECIMAL(10, 2) NOT NULL
        );

        -- Atomically take the stock; the row is locked by the UPDATE, so concurrent sales cannot oversell.
        UPDATE PV
        SET PV.StockQuantity = PV.StockQuantity - SI.Quantity
        OUTPUT
            inserted.VariantID,
            SI.Quantity,
            inserted.SellingPrice,
            CASE WHEN ISNULL(SI.Discount, 0) > SI.Quantity * inserted.SellingPrice
                 THEN SI.Quantity * inserted.SellingPrice
                 ELSE ISNULL(SI.Discount, 0) END,
            SI.Quantity * inserted.SellingPrice
                - CASE WHEN ISNULL(SI.Discount, 0) > SI.Quantity * inserted.SellingPrice
                       THEN SI.Quantity * inserted.SellingPrice
                       ELSE ISNULL(SI.Discount, 0) END
        INTO @Priced (VariantID, Quantity, UnitPrice, Discount, SubTotal)
        FROM ProductVariants PV
        INNER JOIN @SaleItems SI ON PV.VariantID = SI.VariantID
        WHERE PV.StockQuantity >= SI.Quantity
          AND PV.Status = 'Active';

        IF (SELECT COUNT(*) FROM @Priced) <> (SELECT COUNT(*) FROM @SaleItems)
        BEGIN
            RAISERROR('Sale failed: Insufficient stock or unavailable product for one or more items.', 16, 1);
        END

        DECLARE @ComputedTotal DECIMAL(10, 2) = (SELECT SUM(SubTotal) FROM @Priced);

        IF @ComputedTotal <> @TotalAmount
        BEGIN
            RAISERROR('Sale failed: The total no longer matches current prices. Please refresh the product list and try again.', 16, 1);
        END
        DECLARE @NewSaleID INT;

        INSERT INTO Sales (CustomerID, ProcessedByUserID, TotalAmount)
        VALUES (@CustomerID, @ProcessedByUserID, @ComputedTotal);

        SET @NewSaleID = SCOPE_IDENTITY();

        INSERT INTO SalesItems (SaleID, VariantID, Quantity, UnitPrice, Discount, SubTotal)
        SELECT @NewSaleID, VariantID, Quantity, UnitPrice, Discount, SubTotal
        FROM @Priced;

        INSERT INTO Inventory (VariantID, QuantityChange, Reason, ProcessedByUserID, TransactionReferenceID, Notes)
        SELECT VariantID, -Quantity, 'Sale', @ProcessedByUserID, @NewSaleID, 'Customer sale transaction.'
        FROM @Priced;

        COMMIT TRANSACTION;

        SELECT @NewSaleID AS SaleID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
GO
