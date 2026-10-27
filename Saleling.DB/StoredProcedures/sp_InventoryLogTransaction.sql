CREATE PROCEDURE sp_InventoryLogTransaction
(
    @VariantID INT,
    @QuantityChange INT,
    @Reason VARCHAR(50),
    @ProcessedByUserID INT,
    @Notes NVARCHAR(MAX) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @NewStockQuantity INT;
    DECLARE @ActualQuantityChange INT = ABS(@QuantityChange);

    IF @Reason NOT IN (
        'Sale', 'Initial Receipt', 'Found Stock', 'Physical Reconciliation', 
        'Damaged/Scrap', 'Shrinkage/Loss', 'Physical Discrepancy', 'System Error Correction'
    )
    BEGIN
        RAISERROR('Invalid Reason provided. Must be a valid Sale, Receipt, or Adjustment reason.', 16, 1);
        RETURN;
    END

    IF @QuantityChange = 0
    BEGIN
        RAISERROR('QuantityChange must not be zero.', 16, 1);
        RETURN;
    END
    
    IF @Reason IN ('Sale', 'Damaged/Scrap', 'Shrinkage/Loss', 'Physical Discrepancy')
    BEGIN
        SET @ActualQuantityChange = -1 * @ActualQuantityChange;
    END

    BEGIN TRANSACTION;

    BEGIN TRY
        SELECT @NewStockQuantity = StockQuantity + @ActualQuantityChange
        FROM ProductVariants WITH (UPDLOCK, HOLDLOCK)
        WHERE VariantID = @VariantID;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('VariantID not found. Cannot update stock.', 16, 1);
        END

        IF @NewStockQuantity < 0
        BEGIN
            RAISERROR('The transaction would result in a negative stock quantity (%d). Operation aborted.', 16, 1, @NewStockQuantity);
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            RETURN;
        END

        UPDATE ProductVariants
        SET StockQuantity = @NewStockQuantity
        WHERE VariantID = @VariantID;
        
        INSERT INTO Inventory (
            VariantID,
            QuantityChange,
            Reason,
            ProcessedByUserID,
            Notes
        )
        VALUES (
            @VariantID,
            @ActualQuantityChange,
            @Reason,
            @ProcessedByUserID,
            @Notes
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
GO