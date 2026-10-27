CREATE PROCEDURE sp_SearchSaleableProducts
    @SearchTerm NVARCHAR(100) = NULL,
    @SearchFilter NVARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SearchPattern NVARCHAR(102) = '%' + @SearchTerm + '%';

    SELECT
        PV.VariantID,
        PV.ProductID,
        P.ProductName,
        PV.SKU,
        PV.Size,
        PV.Color,
        PV.SellingPrice,
        PV.StockQuantity,
        C.CategoryName AS Category
    FROM
        ProductVariants PV
    INNER JOIN
        Products P ON PV.ProductID = P.ProductID
    INNER JOIN
        Categories C ON P.CategoryID = C.CategoryID
    WHERE
        PV.StockQuantity > 0
        AND PV.Status = 'Active'
        AND P.Status = 'Active'
        AND (@SearchPattern IS NULL 
            OR 
            (
                @SearchFilter = 'Name' AND P.ProductName LIKE @SearchPattern
            )
            OR
            (
                @SearchFilter = 'Category' AND C.CategoryName LIKE @SearchPattern
            )
            OR (@SearchFilter IS NULL AND @SearchPattern IS NOT NULL AND (
                P.ProductName LIKE @SearchPattern
                OR C.CategoryName LIKE @SearchPattern
            ))
        )
    ORDER BY
        P.ProductName ASC,
        PV.Size DESC;
END
GO