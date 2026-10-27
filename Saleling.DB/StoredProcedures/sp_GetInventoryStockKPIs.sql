CREATE PROCEDURE sp_GetInventoryStockKPIs
AS
BEGIN
    DECLARE @TotalInventoryValue DECIMAL(18, 2);
    SELECT
        @TotalInventoryValue = SUM(PV.StockQuantity * PV.CostPrice)
    FROM
        ProductVariants PV
    INNER JOIN
        Products P ON PV.ProductID = P.ProductID
    WHERE
        PV.Status = 'Active'
        AND P.Status = 'Active'
        AND PV.StockQuantity > 0;

    DECLARE @TotalSKUsInStock INT;
    SELECT
        @TotalSKUsInStock = COUNT(PV.VariantID)
    FROM
        ProductVariants PV
    INNER JOIN
        Products P ON PV.ProductID = P.ProductID
    WHERE
        PV.Status = 'Active'
        AND P.Status = 'Active'
        AND PV.StockQuantity > 0;

    DECLARE @TotalSKUsBelowReorderLevel INT;
    SELECT
        @TotalSKUsBelowReorderLevel = COUNT(PV.VariantID)
    FROM
        ProductVariants PV
    INNER JOIN
        Products P ON PV.ProductID = P.ProductID
    WHERE
        PV.Status = 'Active'
        AND P.Status = 'Active'
        AND PV.StockQuantity <= PV.ReorderLevel;

    -- Out-of-stock SKUs count as below their reorder level, so the rate has to be taken
    -- against every active SKU, not just the ones in stock, or it can pass 100%.
    DECLARE @TotalActiveSKUs INT;

    SELECT
        @TotalActiveSKUs = COUNT(PV.VariantID)
    FROM
        ProductVariants PV
    INNER JOIN
        Products P ON PV.ProductID = P.ProductID
    WHERE
        PV.Status = 'Active'
        AND P.Status = 'Active';

    DECLARE @LowStockRate DECIMAL(5, 2);
    SET @LowStockRate =
        CASE
            WHEN @TotalActiveSKUs = 0 THEN 0.00
            ELSE CAST(@TotalSKUsBelowReorderLevel * 100.0 / @TotalActiveSKUs AS DECIMAL(5, 2))
        END;

    SELECT
        ISNULL(@TotalInventoryValue, 0.00) AS TotalInventoryValue,
        ISNULL(@TotalSKUsInStock, 0) AS TotalSKUsInStock,
        ISNULL(@TotalSKUsBelowReorderLevel, 0) AS TotalSKUsBelowReorderLevel,
        ISNULL(@LowStockRate, 0.00) AS LowStockRate;

END
GO