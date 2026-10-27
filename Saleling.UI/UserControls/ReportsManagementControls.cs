using Saleling.Controller;
using Saleling.Model;
using Saleling.Model.Enums;
using Saleling.Model.Inventory;
using Saleling.Model.Sale;
using Saleling.Util;
using System.Drawing.Printing;
using System.Reflection;
using System.Text;

namespace Saleling.UI
{
    public partial class ReportsManagementControls : UserControl
    {
        private readonly SalesController _saleController;
        private readonly InventoryController _inventoryController;
        private readonly UserController _userController;

        private List<UserModel> _cashiers;

        private object? _currentReportList;
        private object? _currentKPIModel;

        private UserModel _currentUser;
        private ReportType _reportType;

        private int _currentPrintPage;
        private int _currentPageNumber;

        private struct ReportTimeFrame
        {
            public string Period { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }

        public ReportsManagementControls()
        {
            InitializeComponent();

            _saleController = new SalesController();
            _inventoryController = new InventoryController();
            _userController = new UserController();
            _cashiers = new List<UserModel>();
            _currentUser = SessionUtil.Instance.CurrentUser;
            _reportType = ReportType.SalesSummary;

            InitializeControls();
        }

        private void InitializeControls()
        {
            cmbProcessedBy.Enabled = _currentUser.Role.Equals("Admin");
            cmbReportType.Enabled = _currentUser.Role.Equals("Admin");

            cmbReportType.SelectedIndex = 0;
            cmbReportPeriod.SelectedIndex = 0;

            cmbProcessedBy.Items.Add
            (
                _currentUser.Role.Equals("Admin")
                ?   "All Cashier"
                :   _currentUser.FullName
            );
            cmbProcessedBy.SelectedIndex = 0;

            reportDocument.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);
            reportDocument.PrintPage -= reportDocument_PrintPage;
            reportDocument.PrintPage += reportDocument_PrintPage;
            reportPreviewDialog.Document = reportDocument;
        }

        private void SetupReportsUI()
        {
            dgvReport.DataSource = null;

            lblReportHeader.Text = $"Daily {_reportType.GetDescription()} Report";

            lblLeftKPI.Text = 0.ToString("C2");
            lblMiddleKPI.Text = "0";
            lblRightKPI.Text = 0.ToString("C2");
            lblLeftSubKPI.Text = "0 transactions";

            switch (_reportType)
            {
                case ReportType.SalesSummary:
                    dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                    lblLeftKPIHeader.Text = "Total Sales";
                    lblMiddleKPIHeader.Text = "Items Sold";
                    lblRightKPIHeader.Text = "Average Sale";
                    cmbReportPeriod.Enabled = true;
                    break;
                case ReportType.InventoryStock:
                    dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    lblLeftKPIHeader.Text = "Low Stock Items";
                    lblMiddleKPIHeader.Text = "SKUs In Stock";
                    lblRightKPIHeader.Text = "Total Inventory Value";
                    cmbReportPeriod.Enabled = false;
                    break;
                case ReportType.InventoryMovement:
                    dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    lblLeftKPIHeader.Text = "Total Units Sold";
                    lblMiddleKPIHeader.Text = "Total Adjustments";
                    lblRightKPIHeader.Text = "Net Inventory Change";
                    cmbReportPeriod.Enabled = true;
                    break;
            }
        }

        private void cmbReportPeriod_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isCustom = cmbReportPeriod.Text.Equals(ReportPeriod.Custom.GetDescription());
            dtpFromDate.Enabled = isCustom;
            dtpToDate.Enabled = isCustom;
        }

        private void btnClearFilters_Click(object sender, EventArgs e)
        {
            cmbReportPeriod.SelectedIndex = 0;
            cmbProcessedBy.SelectedIndex = 0;
            cmbReportType.SelectedIndex = 0;
            dtpFromDate.Value = DateTime.Today;
            dtpToDate.Value = DateTime.Today;
        }

        private ReportTimeFrame GetReportTimeFrame()
        {
            if (cmbReportPeriod.SelectedItem == null)
            {
                throw new InvalidOperationException("Please select a valid report time period.");
            }

            string periodDescription = cmbReportPeriod.Text.Trim();
            DateTime? customStart = null;
            DateTime? customEnd = null;

            if (periodDescription.Equals(ReportPeriod.Custom.GetDescription()))
            {
                customStart = dtpFromDate.Value.Date;
                customEnd = dtpToDate.Value.Date.AddDays(1).AddTicks(-1);

                if (customStart > customEnd)
                {
                    throw new InvalidOperationException("Invalid date range. The 'From' date cannot be after the 'To' date.");
                }
            }

            ReportPeriod selectedPeriod = Enum.GetValues(typeof(ReportPeriod))
                .Cast<ReportPeriod>().
                FirstOrDefault(p => p.GetDescription().Equals(periodDescription, StringComparison.OrdinalIgnoreCase));

            string periodName = selectedPeriod.ToString();

            if (string.IsNullOrEmpty(periodName))
            {
                periodName = periodDescription;
            }

            return new ReportTimeFrame
            {
                Period = periodName,
                StartDate = customStart,
                EndDate = customEnd
            };
        }

        private async void btnGenerate_Click(object sender, EventArgs e)
        {
            btnGenerate.Enabled = false;
            SetupReportsUI();

            try
            {
                await DisplayReportData();
            }
            catch (InvalidOperationException ioex)
            {
                await LoggerUtil.Instance.LogWarningAsync($"Report generation blocked: {ioex.Message}");
                MessageBox.Show(ioex.Message, "Invalid Operation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                await LoggerUtil.Instance.LogExceptionAsync(ex, $"Critical error during report generation for {_reportType}.");
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGenerate.Enabled = true;
            }
        }

        private async Task DisplayReportData()
        {
            ReportTimeFrame timeFrame = GetReportTimeFrame();
            string headerText = $"{_reportType.GetDescription()} Report";

            if (_reportType == ReportType.InventoryMovement || _reportType == ReportType.SalesSummary)
            {
                if (timeFrame.StartDate.HasValue && timeFrame.EndDate.HasValue)
                {
                    string startDate = timeFrame.StartDate.Value.ToString("MMM dd, yyyy");
                    string endDate = timeFrame.EndDate.Value.ToString("MMM dd, yyyy");
                    headerText = $"{_reportType.GetDescription()} Report from {startDate} to {endDate}";
                }
                else
                {
                    headerText = $"{timeFrame.Period} {_reportType.GetDescription()} Report";
                }
            }
            lblReportHeader.Text = headerText;

            switch (_reportType)
            {
                case ReportType.SalesSummary:
                    int? cashierIdFilter = null;

                    if (_currentUser.Role.Equals("Cashier"))
                    {
                        cashierIdFilter = _currentUser.UserID;
                    }
                    else if (!cmbProcessedBy.Text.Equals("All Cashier"))
                    {
                        cashierIdFilter = _cashiers.FirstOrDefault(c => c.FullName.Equals(cmbProcessedBy.Text))?.UserID;
                    }

                    if (cashierIdFilter == null)
                    {
                        _currentKPIModel = await _saleController.GetKPIsAsync(timeFrame.Period, timeFrame.StartDate, timeFrame.EndDate);
                    }
                    else
                    {
                        _currentKPIModel = await _saleController.GetKPIsAsync(timeFrame.Period, timeFrame.StartDate, timeFrame.EndDate, cashierIdFilter);
                    }

                    _currentReportList = await _saleController.GetSalesReportAsync(
                        timeFrame.Period,
                        timeFrame.StartDate,
                        timeFrame.EndDate,
                        cashierIdFilter
                    );
                    break;

                case ReportType.InventoryStock:
                    _currentKPIModel = await _inventoryController.GetStockKPIsAsync();
                    _currentReportList = await _inventoryController.GetStockReportAsync();
                    break;

                case ReportType.InventoryMovement:
                    _currentKPIModel = await _inventoryController.GetMovementKPIsAsync(timeFrame.Period, timeFrame.StartDate, timeFrame.EndDate);
                    _currentReportList = await _inventoryController.GenerateMovementReportAsync(timeFrame.Period, timeFrame.StartDate, timeFrame.EndDate);
                    break;

                default:
                    throw new InvalidOperationException($"Report type '{_reportType}' is not implemented.");
            }

            UpdateKPIDisplay();
            UpdateDataGridView();
        }

        private void UpdateKPIDisplay()
        {
            if (_currentKPIModel == null) return;

            if (_currentKPIModel is SalesReportKPIModel salesKPI)
            {
                lblLeftKPI.Text = salesKPI.TotalSalesRevenue.ToString("C2");
                lblRightKPI.Text = salesKPI.AverageSaleValue.ToString("C2");
                lblLeftSubKPI.Text = PluralUtil.Count(salesKPI.TransactionCount, "transaction", "transactions");
                lblMiddleKPI.Text = salesKPI.ItemsSoldCount.ToString("N0");
            }
            else if (_currentKPIModel is InventoryStockKPIModel stockKPI)
            {
                lblLeftKPI.Text = stockKPI.TotalSKUsBelowReorderLevel.ToString("N0");
                lblLeftSubKPI.Text = $"Low Stock Rate of {(stockKPI.LowStockRate / 100m):P2}";
                lblMiddleKPI.Text = stockKPI.TotalSKUsInStock.ToString("N0");
                lblRightKPI.Text = stockKPI.TotalInventoryValue.ToString("C2");
            }
            else if (_currentKPIModel is InventoryMovementKPIModel movementKPI)
            {
                lblLeftKPI.Text = movementKPI.TotalUnitsSold.ToString("N0");
                lblLeftSubKPI.Text = $"{movementKPI.TotalUnitsReceived.ToString("N0")} units received";
                lblMiddleKPI.Text = movementKPI.TotalAdjustments.ToString("N0");
                lblRightKPI.Text = movementKPI.NetInventoryChange.ToString("N0");
            }
        }

        private void UpdateDataGridView()
        {
            dgvReport.DataSource = _currentReportList;

            if (_currentReportList is List<SalesReportItemModel>)
            {
                dgvReport.Columns["SaleID"].HeaderText = "Sale ID";
                dgvReport.Columns["SaleDate"].HeaderText = "Date";
                dgvReport.Columns["ProcessedBy"].HeaderText = "Cashier";
                dgvReport.Columns["ProductName"].HeaderText = "Product Name";
                dgvReport.Columns["VariantDetails"].HeaderText = "Variant Details";
                dgvReport.Columns["TotalAmount"].HeaderText = "Total Amount";
                dgvReport.Columns["UnitPrice"].HeaderText = "Unit Price";
                dgvReport.Columns["GrossProfit"].HeaderText = "Gross Profit";

                dgvReport.Columns["TotalAmount"].DefaultCellStyle.Format = "C2";
                dgvReport.Columns["UnitPrice"].DefaultCellStyle.Format = "C2";
                dgvReport.Columns["Discount"].DefaultCellStyle.Format = "C2";
                dgvReport.Columns["Revenue"].DefaultCellStyle.Format = "C2";
                dgvReport.Columns["COGS"].DefaultCellStyle.Format = "C2";
                dgvReport.Columns["GrossProfit"].DefaultCellStyle.Format = "C2";
            }
            else if (_currentReportList is List<InventoryStockReportModel>)
            {
                dgvReport.Columns["ProductName"].HeaderText = "Product Name";
                dgvReport.Columns["CategoryName"].HeaderText = "Category";
                dgvReport.Columns["VariantDetails"].HeaderText = "Variant Details";
                dgvReport.Columns["StockQuantity"].HeaderText = "Stock Quantity";
                dgvReport.Columns["ReorderLevel"].HeaderText = "Reorder Level";
            }
            else
            {
                dgvReport.Columns["ChangeDate"].HeaderText = "Date";
                dgvReport.Columns["QuantityChange"].HeaderText = "Quantity Change";
                dgvReport.Columns["ProcessedBy"].HeaderText = "Processor";
                dgvReport.Columns["TransactionReferenceID"].HeaderText = "Transaction ID";
                dgvReport.Columns["ProductName"].HeaderText = "Product Name";
                dgvReport.Columns["VariantDetails"].HeaderText = "Variant Details";
            }
        }

        private async Task LoadCashierNames()
        {
            if (!_currentUser.Role.Equals("Admin"))
            {
                return;
            }

            _cashiers = await _userController.GetAllCashierAsync();
            foreach (UserModel cashier in _cashiers)
            {
                cmbProcessedBy.Items.Add(cashier.FullName);
            }
        }

        private async void ReportsManagementControls_Load(object sender, EventArgs e)
        {
            try
            {
                SetupReportsUI();
                await LoadCashierNames();
            }
            catch (Exception ex)
            {
                await LoggerUtil.Instance.LogExceptionAsync(ex, "Critical error during initial report data load on startup.");
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cmbReportType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedDescription = cmbReportType.Text.Trim();

            ReportType newReportType = Enum.GetValues(typeof(ReportType))
                .Cast<ReportType>()
                .FirstOrDefault(r => r.GetDescription().Equals(selectedDescription, StringComparison.OrdinalIgnoreCase));

            _reportType = newReportType;
            cmbProcessedBy.Enabled = _reportType.Equals(ReportType.SalesSummary) && _currentUser.Role.Equals("Admin");

            switch (newReportType)
            {
                case ReportType.SalesSummary:
                    if (cmbProcessedBy.Items.Count > 0)
                    {
                        cmbProcessedBy.Items.Remove("N/A");
                        cmbProcessedBy.SelectedIndex = 0;
                    }

                    if (cmbReportPeriod.Items.Count > 0)
                    {
                        cmbReportPeriod.Items.Remove("Snapshot");
                        cmbReportPeriod.SelectedIndex = 0;
                    }
                    cmbReportPeriod.Enabled = true;
                    break;
                case ReportType.InventoryStock:
                    cmbProcessedBy.Items.Add("N/A");
                    cmbReportPeriod.Items.Add("Snapshot");

                    cmbProcessedBy.SelectedIndex = cmbProcessedBy.Items.IndexOf("N/A");
                    cmbReportPeriod.SelectedIndex = cmbReportPeriod.Items.IndexOf("Snapshot");
                    cmbReportPeriod.Enabled = false;
                    break;
                case ReportType.InventoryMovement:
                    if (cmbReportPeriod.Items.Contains("Snapshot"))
                    {
                        cmbReportPeriod.Items.Remove("Snapshot");
                        cmbReportPeriod.SelectedIndex = 0;
                        cmbReportPeriod.Enabled = true;
                    }

                    cmbProcessedBy.Items.Add("N/A");
                    cmbProcessedBy.SelectedIndex = cmbProcessedBy.Items.IndexOf("N/A");
                    break;
            }
        }

        private float DrawReportKPIs(Graphics graphics, float leftMargin, float pageWidth, float yPosition)
        {
            using (Font kpiHeaderFont = new Font("Arial", 8, FontStyle.Bold))
            using (Font kpiValueFont = new Font("Arial", 14, FontStyle.Bold))
            using (Font kpiSubFont = new Font("Arial", 7, FontStyle.Regular))
            using (SolidBrush textBrush = new SolidBrush(Color.Black))
            using (SolidBrush subBrush = new SolidBrush(Color.Gray))
            using (SolidBrush boxBrush = new SolidBrush(Color.FromArgb(245, 245, 245)))
            {
                float columnWidth = (pageWidth - 40) / 3f;
                float boxHeight = 80;
                float padding = 10;
                float currentX = leftMargin;

                List<(string header, string value, string sub)> kpiData = new();

                if (_currentKPIModel is SalesReportKPIModel salesKPI)
                {
                    kpiData.Add(("TOTAL SALES REVENUE", salesKPI.TotalSalesRevenue.ToString("C2"),
                        PluralUtil.Count(salesKPI.TransactionCount, "transaction", "transactions")));
                    kpiData.Add(("TOTAL ITEMS SOLD", salesKPI.ItemsSoldCount.ToString("N0"), ""));
                    kpiData.Add(("AVERAGE SALE VALUE", salesKPI.AverageSaleValue.ToString("C2"), ""));
                }
                else if (_currentKPIModel is InventoryStockKPIModel stockKPI)
                {
                    kpiData.Add(("LOW STOCK ITEMS", stockKPI.TotalSKUsBelowReorderLevel.ToString("N0"),
                        $"Rate: {(stockKPI.LowStockRate / 100m):P2}"));
                    kpiData.Add(("TOTAL SKUs IN STOCK", stockKPI.TotalSKUsInStock.ToString("N0"), ""));
                    kpiData.Add(("TOTAL INVENTORY VALUE", stockKPI.TotalInventoryValue.ToString("C2"), ""));
                }
                else if (_currentKPIModel is InventoryMovementKPIModel movementKPI)
                {
                    kpiData.Add(("TOTAL UNITS SOLD", movementKPI.TotalUnitsSold.ToString("N0"),
                        $"{movementKPI.TotalUnitsReceived:N0} received"));
                    kpiData.Add(("TOTAL ADJUSTMENTS", movementKPI.TotalAdjustments.ToString("N0"), ""));
                    kpiData.Add(("NET INVENTORY CHANGE", movementKPI.NetInventoryChange.ToString("N0"), ""));
                }

                foreach (var (header, value, sub) in kpiData)
                {
                    // Draw box background
                    graphics.FillRectangle(boxBrush, currentX, yPosition, columnWidth, boxHeight);
                    graphics.DrawRectangle(Pens.LightGray, currentX, yPosition, columnWidth, boxHeight);

                    // Draw content
                    float textX = currentX + padding;
                    float textY = yPosition + padding;

                    graphics.DrawString(header, kpiHeaderFont, subBrush, textX, textY);
                    textY += kpiHeaderFont.GetHeight(graphics) + 5;

                    graphics.DrawString(value, kpiValueFont, textBrush, textX, textY);
                    textY += kpiValueFont.GetHeight(graphics) + 2;

                    if (!string.IsNullOrEmpty(sub))
                    {
                        graphics.DrawString(sub, kpiSubFont, subBrush, textX, textY);
                    }

                    currentX += columnWidth + 20; // 20 for spacing
                }

                return yPosition + boxHeight + 10;
            }
        }

        private void reportDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            float xPos = e.MarginBounds.Left;
            float yPos = e.MarginBounds.Top;
            float margin = e.MarginBounds.Left;
            float pageWidth = e.MarginBounds.Width;

            using (Font companyFont = new Font("Arial", 20, FontStyle.Bold))
            using (Font taglineFont = new Font("Arial", 9, FontStyle.Italic))
            {
                string companyName = "SALELING";
                graphics.DrawString(companyName, companyFont, Brushes.Black, xPos, yPos);
                yPos += companyFont.GetHeight(graphics) + 2;

                graphics.DrawString("Point of Sale & Inventory Management System", taglineFont, Brushes.Gray, xPos, yPos);
                yPos += taglineFont.GetHeight(graphics) + 20;
            }

            using (Pen thickPen = new Pen(Color.Black, 2))
            {
                graphics.DrawLine(thickPen, margin, yPos, e.MarginBounds.Right, yPos);
                yPos += 15;
            }

            using (Font titleFont = new Font("Arial", 18, FontStyle.Bold))
            {
                string reportTitle = $"{_reportType.GetDescription()} Report".ToUpper();
                SizeF titleSize = graphics.MeasureString(reportTitle, titleFont);
                float titleX = margin + (pageWidth - titleSize.Width) / 2;
                graphics.DrawString(reportTitle, titleFont, Brushes.Black, titleX, yPos);
                yPos += titleSize.Height + 20;
            }

            using (Font metaFont = new Font("Arial", 9))
            using (Font metaBoldFont = new Font("Arial", 9, FontStyle.Bold))
            {
                float metaColumnWidth = pageWidth / 2;
                float leftColumnX = margin;
                float rightColumnX = margin + metaColumnWidth;
                float metaStartY = yPos;
                float lineSpacing = metaFont.GetHeight(graphics) + 4;

                graphics.DrawString("Report Generated:", metaBoldFont, Brushes.Black, leftColumnX, yPos);
                graphics.DrawString(DateTime.Now.ToString("MMMM dd, yyyy hh:mm tt"), metaFont, Brushes.Black, leftColumnX + 120, yPos);
                yPos += lineSpacing;

                graphics.DrawString("Generated By:", metaBoldFont, Brushes.Black, leftColumnX, yPos);
                graphics.DrawString(_currentUser.FullName, metaFont, Brushes.Black, leftColumnX + 120, yPos);
                yPos += lineSpacing;

                yPos = metaStartY;

                if (_reportType == ReportType.SalesSummary || _reportType == ReportType.InventoryMovement)
                {
                    ReportTimeFrame timeFrame = GetReportTimeFrame();
                    graphics.DrawString("Report Period:", metaBoldFont, Brushes.Black, rightColumnX, yPos);

                    string periodText = timeFrame.Period;
                    if (timeFrame.StartDate.HasValue && timeFrame.EndDate.HasValue)
                    {
                        periodText = $"{timeFrame.StartDate.Value:MMM dd, yyyy} - {timeFrame.EndDate.Value:MMM dd, yyyy}";
                    }

                    graphics.DrawString(periodText, metaFont, Brushes.Black, rightColumnX + 100, yPos);
                    yPos += lineSpacing;
                }

                if (_reportType == ReportType.SalesSummary && !cmbProcessedBy.Text.Equals("All Cashier"))
                {
                    graphics.DrawString("Processed By:", metaBoldFont, Brushes.Black, rightColumnX, yPos);
                    graphics.DrawString(cmbProcessedBy.Text, metaFont, Brushes.Black, rightColumnX + 100, yPos);
                    yPos += lineSpacing;
                }

                yPos = Math.Max(yPos, metaStartY + lineSpacing * 2) + 15;
            }

            graphics.DrawLine(Pens.Gray, margin, yPos, e.MarginBounds.Right, yPos);
            yPos += 20;

            using (Font sectionFont = new Font("Arial", 11, FontStyle.Bold))
            {
                graphics.DrawString("KEY PERFORMANCE INDICATORS", sectionFont, Brushes.Black, margin, yPos);
                yPos += sectionFont.GetHeight(graphics) + 10;
            }

            yPos = DrawReportKPIs(graphics, margin, pageWidth, yPos);
            yPos += 20;

            graphics.DrawLine(Pens.Gray, margin, yPos, e.MarginBounds.Right, yPos);
            yPos += 15;

            using (Font sectionFont = new Font("Arial", 11, FontStyle.Bold))
            {
                graphics.DrawString("DETAILED REPORT DATA", sectionFont, Brushes.Black, margin, yPos);
                yPos += sectionFont.GetHeight(graphics) + 10;
            }

            List<(string PropertyName, string DisplayName, int Width, string Format)> columnConfig = new();
            IEnumerable<object>? dataList = _currentReportList as IEnumerable<object>;

            switch (_reportType)
            {
                case ReportType.SalesSummary:
                    columnConfig.Add(("SaleDate", "Date", 100, "date"));
                    columnConfig.Add(("ProductName", "Product Name", 200, "text"));
                    columnConfig.Add(("VariantDetails", "Variant", 130, "text"));
                    columnConfig.Add(("Quantity", "Qty", 50, "number"));
                    columnConfig.Add(("Revenue", "Revenue", 90, "currency"));
                    columnConfig.Add(("GrossProfit", "Profit", 90, "currency"));
                    break;
                case ReportType.InventoryStock:
                    columnConfig.Add(("SKU", "SKU", 90, "text"));
                    columnConfig.Add(("ProductName", "Product Name", 180, "text"));
                    columnConfig.Add(("VariantDetails", "Variant", 130, "text"));
                    columnConfig.Add(("StockQuantity", "Stock", 70, "number"));
                    columnConfig.Add(("ReorderLevel", "Reorder", 70, "number"));
                    columnConfig.Add(("Status", "Status", 90, "text"));
                    break;
                case ReportType.InventoryMovement:
                    columnConfig.Add(("ChangeDate", "Date", 100, "date"));
                    columnConfig.Add(("SKU", "SKU", 80, "text"));
                    columnConfig.Add(("ProductName", "Product", 160, "text"));
                    columnConfig.Add(("Reason", "Reason", 120, "text"));
                    columnConfig.Add(("QuantityChange", "Change", 70, "number"));
                    columnConfig.Add(("TransactionReferenceID", "Ref ID", 90, "text"));
                    break;
            }

            if (dataList == null || !dataList.Any() || !columnConfig.Any())
            {
                using (Font noDataFont = new Font("Arial", 10, FontStyle.Italic))
                {
                    string noDataText = "No data available for the selected criteria.";
                    SizeF noDataSize = graphics.MeasureString(noDataText, noDataFont);
                    float noDataX = margin + (pageWidth - noDataSize.Width) / 2;
                    graphics.DrawString(noDataText, noDataFont, Brushes.Gray, noDataX, yPos + 20);
                }
                e.HasMorePages = false;
                DrawPageFooter(graphics, e, _currentPageNumber);
                return;
            }

            using (Font headerFont = new Font("Arial", 9, FontStyle.Bold))
            using (SolidBrush headerBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
            {
                float headerHeight = headerFont.GetHeight(graphics) + 10;
                graphics.FillRectangle(headerBrush, margin, yPos, pageWidth, headerHeight);
                graphics.DrawRectangle(Pens.Black, margin, yPos, pageWidth, headerHeight);

                float currentX = margin + 5;
                foreach (var (propName, displayName, width, format) in columnConfig)
                {
                    graphics.DrawString(displayName, headerFont, Brushes.Black, currentX, yPos + 5);
                    currentX += width;
                }

                yPos += headerHeight;
            }

            List<object> dataAsList = dataList.ToList();
            PropertyInfo[] properties = dataAsList.FirstOrDefault()?.GetType().GetProperties() ?? Array.Empty<PropertyInfo>();

            float rowHeight;
            int maxRows;
            int rowsPrinted = 0;
            float tableStartY = yPos;

            using (Font dataFont = new Font("Arial", 8))
            {
                rowHeight = dataFont.GetHeight(graphics) + 8;
                float availableHeight = e.MarginBounds.Bottom - yPos - 50;
                maxRows = Math.Max(1, (int)(availableHeight / rowHeight));
                bool isAlternateRow = false;

                using (StringFormat format = new StringFormat(StringFormat.GenericTypographic)
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap,
                    LineAlignment = StringAlignment.Center
                })
                {
                    while (_currentPrintPage < dataAsList.Count && rowsPrinted < maxRows)
                    {
                        object item = dataAsList[_currentPrintPage];

                        Color rowBackColor = isAlternateRow ? Color.FromArgb(250, 250, 250) : Color.White;
                        Color rowForeColor = Color.Black;

                        if (_reportType == ReportType.InventoryStock)
                        {
                            var stockQtyProp = properties.FirstOrDefault(p => p.Name == "StockQuantity");
                            var reorderLevelProp = properties.FirstOrDefault(p => p.Name == "ReorderLevel");

                            if (stockQtyProp != null && reorderLevelProp != null)
                            {
                                int stockQuantity = Convert.ToInt32(stockQtyProp.GetValue(item, null) ?? 0);
                                int reorderLevel = Convert.ToInt32(reorderLevelProp.GetValue(item, null) ?? 0);

                                if (stockQuantity == 0)
                                {
                                    rowBackColor = Color.FromArgb(255, 200, 200);
                                    rowForeColor = Color.DarkRed;
                                }
                                else if (stockQuantity <= reorderLevel)
                                {
                                    rowBackColor = Color.FromArgb(255, 255, 192);
                                    rowForeColor = Color.DarkGoldenrod;
                                }
                            }
                        }

                        using (SolidBrush rowBrush = new SolidBrush(rowBackColor))
                        using (SolidBrush textBrush = new SolidBrush(rowForeColor))
                        {
                            graphics.FillRectangle(rowBrush, margin, yPos, pageWidth, rowHeight);

                            float currentX = margin + 5;

                            foreach (var (propName, displayName, width, formatType) in columnConfig)
                            {
                                object value = properties.FirstOrDefault(p => p.Name == propName)?.GetValue(item, null) ?? string.Empty;
                                string cellText = FormatCellValue(value, formatType);

                                RectangleF drawRect = new RectangleF(currentX, yPos, width - 10, rowHeight);
                                graphics.DrawString(cellText, dataFont, textBrush, drawRect, format);

                                currentX += width;
                            }
                        }

                        graphics.DrawLine(Pens.LightGray, margin, yPos + rowHeight, e.MarginBounds.Right, yPos + rowHeight);

                        yPos += rowHeight;
                        _currentPrintPage++;
                        rowsPrinted++;
                        isAlternateRow = !isAlternateRow;
                    }
                }
            }

            float tableHeight = yPos - tableStartY;
            graphics.DrawRectangle(Pens.Black, margin, tableStartY, pageWidth, tableHeight);

            if (_currentPrintPage < dataList.Count())
            {
                e.HasMorePages = true;
                DrawPageFooter(graphics, e, _currentPageNumber);
                _currentPageNumber++;
            }
            else
            {
                e.HasMorePages = false;
                DrawPageFooter(graphics, e, _currentPageNumber);
            }
        }

        private string FormatCellValue(object value, string formatType)
        {
            if (value == null) return string.Empty;

            return formatType switch
            {
                "currency" when value is decimal dec => dec.ToString("C2"),
                "number" when value is int num => num.ToString("N0"),
                "date" when value is DateTime date => date.ToString("MMM dd, yyyy"),
                "text" => value.ToString() ?? string.Empty,
                _ => value.ToString() ?? string.Empty
            };
        }

        private void DrawPageFooter(Graphics graphics, PrintPageEventArgs e, int pageNumber)
        {
            using (Font footerFont = new Font("Arial", 8))
            using (SolidBrush footerBrush = new SolidBrush(Color.Gray))
            {
                float footerY = e.MarginBounds.Bottom + 20;

                string confidentialText = "CONFIDENTIAL - For Internal Use Only";
                graphics.DrawString(confidentialText, footerFont, footerBrush, e.MarginBounds.Left, footerY);

                string pageText = $"Page {pageNumber}";
                SizeF pageSize = graphics.MeasureString(pageText, footerFont);
                float pageX = e.MarginBounds.Left + (e.MarginBounds.Width - pageSize.Width) / 2;
                graphics.DrawString(pageText, footerFont, footerBrush, pageX, footerY);

                string dateText = DateTime.Now.ToString("MM/dd/yyyy");
                SizeF dateSize = graphics.MeasureString(dateText, footerFont);
                float dateX = e.MarginBounds.Right - dateSize.Width;
                graphics.DrawString(dateText, footerFont, footerBrush, dateX, footerY);
            }
        }

        private async void btnPrintReport_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvReport.DataSource == null)
                {
                    throw new InvalidOperationException("Please generate a report first before attempting to print.");
                }

                _currentPrintPage = 0;
                _currentPageNumber = 1;
                reportPreviewDialog.ShowDialog();
            }
            catch (InvalidOperationException ioex)
            {
                await LoggerUtil.Instance.LogExceptionAsync(ioex, ioex.Message);
                MessageBox.Show(ioex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                await LoggerUtil.Instance.LogExceptionAsync(ex, "Critical error during printing of reports.");
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnExportExcel_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvReport.DataSource == null)
                {
                    throw new InvalidOperationException("Please generate a report first before attempting to export.");
                }

                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "CSV files (*.csv)|*.csv";
                    saveDialog.Title = "Save DataGrid as CSV File";
                    saveDialog.FileName = "Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        btnExportExcel.Enabled = false;
                        await ExportToCsvAsync(saveDialog.FileName);
                        MessageBox.Show($"Report successfully exported to:\n{saveDialog.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (InvalidOperationException ioex)
            {
                await LoggerUtil.Instance.LogExceptionAsync(ioex, ioex.Message);
                MessageBox.Show(ioex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                await LoggerUtil.Instance.LogExceptionAsync(ex, "Critical error during the process of exporting to a CSV.");
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnExportExcel.Enabled = true;
            }
        }

        private async Task ExportToCsvAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new InvalidOperationException("Invalid file path.");
            }

            StringBuilder csvBuilder = new StringBuilder();

            StringBuilder headerRow = new StringBuilder();
            for (int i = 0; i < dgvReport.Columns.Count; i++)
            {
                if (dgvReport.Columns[i].Visible)
                {
                    headerRow.Append(FormatCsvData(dgvReport.Columns[i].HeaderText));
                    if (i < dgvReport.Columns.Count - 1)
                    {
                        headerRow.Append(",");
                    }
                }
            }
            csvBuilder.AppendLine(headerRow.ToString());

            foreach (DataGridViewRow row in dgvReport.Rows)
            {
                if (!row.IsNewRow)
                {
                    var dataRow = new StringBuilder();
                    for (int i = 0; i < dgvReport.Columns.Count; i++)
                    {
                        if (dgvReport.Columns[i].Visible)
                        {
                            object cellValue = row.Cells[i].Value;
                            string? cellString = cellValue != null ? cellValue.ToString() : string.Empty;

                            dataRow.Append(FormatCsvData(cellString));
                            if (i < dgvReport.Columns.Count - 1)
                            {
                                dataRow.Append(",");
                            }
                        }
                    }
                    csvBuilder.AppendLine(dataRow.ToString());
                }
            }
            await File.WriteAllTextAsync(filePath, csvBuilder.ToString(), Encoding.UTF8);
        }

        private static string FormatCsvData(string data)
        {
            if (data.Contains(",") || data.Contains("\"") || data.Contains("\n"))
            {
                data = data.Replace("\"", "\"\"");
                return $"\"{data}\"";
            }
            return data;
        }

        private void dgvReport_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (_reportType.Equals(ReportType.InventoryStock))
            {
                DataGridViewRow row = dgvReport.Rows[e.RowIndex];

                int stockQuantity = 0;
                int reorderLevel = 0;

                if (row.Cells["StockQuantity"] != null && row.Cells["StockQuantity"].Value != null)
                {
                    int.TryParse(row.Cells["StockQuantity"].Value.ToString(), out stockQuantity);
                }

                if (row.Cells["ReorderLevel"] != null && row.Cells["ReorderLevel"].Value != null)
                {
                    int.TryParse(row.Cells["ReorderLevel"].Value.ToString(), out reorderLevel);
                }


                if (stockQuantity == 0)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200);
                    row.DefaultCellStyle.ForeColor = Color.DarkRed;
                }
                else if (stockQuantity <= reorderLevel)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 192);
                    row.DefaultCellStyle.ForeColor = Color.DarkGoldenrod;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = dgvReport.DefaultCellStyle.BackColor;
                    row.DefaultCellStyle.ForeColor = dgvReport.DefaultCellStyle.ForeColor;
                }
            }
        }
    }
}
