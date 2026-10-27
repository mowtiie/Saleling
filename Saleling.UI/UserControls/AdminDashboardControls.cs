using Saleling.Controller;
using Saleling.Model.Product;
using Saleling.Model.Sale;
using Saleling.Util;

namespace Saleling.UI
{
    public partial class AdminDashboardControls : UserControl
    {
        private ProductController _productController;
        private CategoryController _categoryController;
        private InventoryController _inventoryController;
        private SalesController _salesController;

        public AdminDashboardControls()
        {
            InitializeComponent();
            _productController = new ProductController();
            _categoryController = new CategoryController();
            _inventoryController = new InventoryController();
            _salesController = new SalesController();
            dataFetchTimer.Start();
        }

        private async void AdminDashboardScreen_Load(object sender, EventArgs e)
        {
            await RefreshDashboardMetrics();
        }

        private async void monitoringTimer_TickAsync(object sender, EventArgs e)
        {
            await RefreshDashboardMetrics();
        }

        private async Task RefreshDashboardMetrics()
        {
            try
            {
                Task<List<ProductStockAlertModel>> recentStockAlertsTask = _productController.GetStockAlertsAsync();
                Task<List<SalesAlertModel>> recentTransactionsTask = _salesController.GetRecentSalesAsync();

                Task<decimal> totalSalesTask = _salesController.GetTotalSalesAsync();
                Task<int> transactionCountTask = _salesController.GetTransactionCountAsync();
                Task<int> productCountTask = _productController.GetTotalCountAsync();
                Task<int> categoryCountTask = _categoryController.GetTotalCountAsync();
                Task<int> lowStockCountTask = _inventoryController.GetLowStockCount();
                Task<int> outOfStockCountTask = _inventoryController.GetOutOfStockCount();

                await Task.WhenAll
                (
                    totalSalesTask, transactionCountTask, productCountTask,
                    categoryCountTask, lowStockCountTask, outOfStockCountTask,
                    recentStockAlertsTask, recentTransactionsTask
                );

                dgvStockReport.DataSource = recentStockAlertsTask.Result;
                dgvStockReport.Columns["ProductName"].HeaderText = "Product Name";

                dgvTransactionReport.DataSource = recentTransactionsTask.Result;
                dgvTransactionReport.Columns["SaleID"].HeaderText = "Transaction ID";
                dgvTransactionReport.Columns["SaleDate"].HeaderText = "Transaction Date";
                dgvTransactionReport.Columns["TotalAmount"].HeaderText = "Total Amount";
                dgvTransactionReport.Columns["TotalAmount"].DefaultCellStyle.Format = "C2";

                lblTodaySales.Text = totalSalesTask.Result.ToString("C2");
                lblTransactionCount.Text = PluralUtil.Count(transactionCountTask.Result, "transaction", "transactions");
                lblProductCount.Text = productCountTask.Result.ToString();
                lblCategoryCount.Text = PluralUtil.Count(categoryCountTask.Result, "category", "categories");
                lblLowStockCount.Text = lowStockCountTask.Result.ToString();
                lblOutOfStockCount.Text = $"{outOfStockCountTask.Result.ToString()} out of stock";
            }
            catch (Exception ex)
            {
                await LoggerUtil.Instance.LogExceptionAsync(ex, "Failed to update admin dashboard metrics.");
                MessageBox.Show($"Failed to retrieve dashboard data. See logs for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvStockReport_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataGridViewRow row = dgvStockReport.Rows[e.RowIndex];

            string? status = string.Empty;

            if (row.Cells["Status"] != null && row.Cells["Status"].Value != null)
            {
                status = row.Cells["Status"].Value.ToString();
            }

            if (status == null)
            {
                return;
            }
            else if (status.Equals("Out of Stock"))
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200);
                row.DefaultCellStyle.ForeColor = Color.DarkRed;
            }
            else
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 192);
                row.DefaultCellStyle.ForeColor = Color.DarkGoldenrod;
            }
        }
    }
}
