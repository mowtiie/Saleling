using Saleling.Controller;
using Saleling.Model;
using Saleling.Model.Product;
using Saleling.Model.Sale;
using Saleling.Util;

namespace Saleling.UI
{
    public partial class CashierDashboardControls : UserControl
    {
        private ProductController _productController;
        private SalesController _salesController;
        private InventoryController _inventoryController;

        private UserModel _currentUser;

        public CashierDashboardControls()
        {
            InitializeComponent();
            _productController = new ProductController();
            _salesController = new SalesController();
            _inventoryController = new InventoryController();
            _currentUser = SessionUtil.Instance.CurrentUser;
            dataFetchTimer.Start();
        }

        private async Task RefreshDashboardMetrics()
        {
            try
            {
                Task<List<ProductStockAlertModel>> recentStockAlertsTask = _productController.GetStockAlertsAsync();
                Task<List<SalesAlertModel>> recentTransactionsTask = _salesController.GetRecentSalesAsync(_currentUser.UserID);

                Task<decimal> salesTask = _salesController.GetTotalSalesTodayByUserIDAsync(_currentUser.UserID);
                Task<decimal> avgPerTransactionTask = _salesController.GetAvgItemsPerTransactionTodayByUserIDAsync(_currentUser.UserID);
                Task<int> transactionCountTask = _salesController.GetTransactionCountTodayByUserIDAsync(_currentUser.UserID);
                Task<int> itemsSoldCountTask = _salesController.GetTotalItemsSoldTodayByUserIDAsync(_currentUser.UserID);
                Task<int> lowStockCountTask = _inventoryController.GetLowStockCount();

                await Task.WhenAll
                (
                    salesTask, avgPerTransactionTask, transactionCountTask,
                    itemsSoldCountTask, lowStockCountTask,
                    recentStockAlertsTask, recentTransactionsTask
                );

                dgvStockReport.DataSource = recentStockAlertsTask.Result;
                dgvStockReport.Columns["ProductName"].HeaderText = "Product Name";

                dgvTransactionReport.DataSource = recentTransactionsTask.Result;
                dgvTransactionReport.Columns["SaleID"].HeaderText = "Transaction ID";
                dgvTransactionReport.Columns["SaleDate"].HeaderText = "Transaction Date";
                dgvTransactionReport.Columns["TotalAmount"].HeaderText = "Total Amount";
                dgvTransactionReport.Columns["TotalAmount"].DefaultCellStyle.Format = "C2";
                dgvTransactionReport.Columns["Cashier"].Visible = false;

                lblSales.Text = salesTask.Result.ToString("C2");
                lblTransactions.Text = PluralUtil.Count(transactionCountTask.Result, "transaction", "transactions");
                lblItemsSold.Text = itemsSoldCountTask.Result.ToString();
                lblAveragePerTransaction.Text = $"Average: {avgPerTransactionTask.Result:F2} per transaction";
                lblLowStock.Text = lowStockCountTask.Result.ToString();
            }
            catch (Exception ex)
            {
                await LoggerUtil.Instance.LogExceptionAsync(ex, "Failed to update cashier dashboard metrics.");
                MessageBox.Show($"Failed to retrieve dashboard data. See logs for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void dataFetchTimer_Tick(object sender, EventArgs e)
        {
            await RefreshDashboardMetrics();
        }

        private async void CashierDashboardControls_Load(object sender, EventArgs e)
        {
            await RefreshDashboardMetrics();
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
