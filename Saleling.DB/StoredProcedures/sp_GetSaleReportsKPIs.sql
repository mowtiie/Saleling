CREATE PROCEDURE sp_GetSaleReportsKPIs
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL,
    @ProcessedByUserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AdjustedEndDate DATETIME2 = NULL;
    IF @EndDate IS NOT NULL
    BEGIN
        SET @AdjustedEndDate = DATEADD(day, 1, CAST(@EndDate AS DATE));
    END

    SELECT
        COUNT(S.SaleID) AS TransactionCount,
        ISNULL(SUM(S.TotalAmount), 0.00) AS TotalSalesRevenue,
        ISNULL(AVG(S.TotalAmount), 0.00) AS AverageSaleValue
    INTO
        #SalesSummary
    FROM
        Sales S
    WHERE
        (@StartDate IS NULL OR S.SaleDate >= @StartDate)
        AND (@AdjustedEndDate IS NULL OR S.SaleDate < @AdjustedEndDate)
        AND (@ProcessedByUserID IS NULL OR S.ProcessedByUserID = @ProcessedByUserID);

    SELECT
        ISNULL(SUM(SI.Quantity), 0) AS ItemsSoldCount
    INTO
        #ItemsSummary
    FROM
        SalesItems SI
    JOIN
        Sales S ON S.SaleID = SI.SaleID
    WHERE
        (@StartDate IS NULL OR S.SaleDate >= @StartDate)
        AND (@AdjustedEndDate IS NULL OR S.SaleDate < @AdjustedEndDate)
        AND (@ProcessedByUserID IS NULL OR S.ProcessedByUserID = @ProcessedByUserID);

    SELECT
        SS.TotalSalesRevenue,
        SS.TransactionCount,
        ITS.ItemsSoldCount,
        SS.AverageSaleValue
    FROM
        #SalesSummary AS SS
    CROSS JOIN
        #ItemsSummary AS ITS;

    DROP TABLE #SalesSummary;
    DROP TABLE #ItemsSummary;

END
GO