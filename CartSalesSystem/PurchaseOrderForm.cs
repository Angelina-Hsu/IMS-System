using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static CartSalesSystem.Form1;

namespace CartSalesSystem
{
    public partial class PurchaseOrderForm : Form
    {
        private List<Supplier> suppliers;
        private List<Product> products;
        private List<PurchaseOrder> purchaseOrders;
        private List<PurchaseDetail> purchaseDetails;
        private string dataFolderPath;

        private List<PurchaseItem> currentItems = new List<PurchaseItem>();
        private int nextPurchaseId;

        public class PurchaseItem
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal SubTotal => Quantity * UnitPrice;
        }

        public PurchaseOrderForm(List<Supplier> supplierList, List<Product> productList,
                                 List<PurchaseOrder> purchaseOrderList, List<PurchaseDetail> purchaseDetailList,
                                 string dataPath)
        {
            InitializeComponent();
            suppliers = supplierList;
            products = productList;
            purchaseOrders = purchaseOrderList;
            purchaseDetails = purchaseDetailList;
            dataFolderPath = dataPath;

            nextPurchaseId = purchaseOrders.Count > 0 ? purchaseOrders.Max(o => o.PurchaseID) + 1 : 1;

            LoadSuppliers();
            LoadProductsToGrid();
            SetupDataGrids();
            UpdateTotalAmount();
            CheckLowStock();
        }

        private void LoadSuppliers()
        {
            cmbSupplier.DataSource = null;
            cmbSupplier.DisplayMember = "SupplierName";
            cmbSupplier.ValueMember = "SupplierID";
            cmbSupplier.DataSource = suppliers.ToList();
            cmbSupplier.SelectedIndex = -1;
        }

        private void LoadProductsToGrid()
        {
            dgvProducts.DataSource = null;
            dgvProducts.DataSource = products.Select(p => new
            {
                p.ProductID,
                p.ProductName,
                單價 = p.Price,
                庫存 = p.Stock,
                狀態 = p.Stock < 20 ? (p.Stock <= 0 ? "❌ 缺貨" : "⚠️ 庫存不足") : "✅ 正常"
            }).ToList();
        }

        private void SetupDataGrids()
        {
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.ReadOnly = true;
            dgvProducts.SelectionChanged += DgvProducts_SelectionChanged;

            dgvPurchaseItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPurchaseItems.ReadOnly = true;
        }

        private void CheckLowStock()
        {
            var lowStockProducts = products.Where(p => p.Stock < 20 && p.Stock > 0).ToList();
            var outOfStockProducts = products.Where(p => p.Stock <= 0).ToList();

            if (lowStockProducts.Count > 0 || outOfStockProducts.Count > 0)
            {
                string warningMessage = "⚠️ 庫存警示 ⚠️\n\n";

                if (outOfStockProducts.Count > 0)
                {
                    warningMessage += $"❌ 缺貨商品 ({outOfStockProducts.Count} 項):\n";
                    foreach (var p in outOfStockProducts.Take(5))
                    {
                        warningMessage += $"   • {p.ProductName} (庫存: {p.Stock})\n";
                    }
                    if (outOfStockProducts.Count > 5) warningMessage += $"   ... 共 {outOfStockProducts.Count} 項\n";
                    warningMessage += "\n";
                }

                if (lowStockProducts.Count > 0)
                {
                    warningMessage += $"⚠️ 庫存低於20的商品 ({lowStockProducts.Count} 項):\n";
                    foreach (var p in lowStockProducts.Take(5))
                    {
                        warningMessage += $"   • {p.ProductName} (庫存: {p.Stock})\n";
                    }
                    if (lowStockProducts.Count > 5) warningMessage += $"   ... 共 {lowStockProducts.Count} 項\n";
                }

                warningMessage += "\n建議立即進貨！";

                if (lblStockWarning != null)
                {
                    lblStockWarning.Text = $"⚠️ 有 {lowStockProducts.Count + outOfStockProducts.Count} 項商品庫存不足！\n請點擊「檢查庫存」查看詳細";
                    lblStockWarning.ForeColor = Color.Red;
                    lblStockWarning.Visible = true;
                }

                MessageBox.Show(warningMessage, "庫存警示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                if (lblStockWarning != null)
                {
                    lblStockWarning.Text = "✅ 所有商品庫存充足";
                    lblStockWarning.ForeColor = Color.Green;
                    lblStockWarning.Visible = true;
                }
            }
        }

        private void DgvProducts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow != null)
            {
                int productId = (int)dgvProducts.CurrentRow.Cells["ProductID"].Value;
                var product = products.FirstOrDefault(p => p.ProductID == productId);

                if (product != null)
                {
                    txtUnitPrice.Text = product.Price.ToString();
                    nudQuantity.Value = 1;
                }
            }
        }

        private void btnAddItem_Click(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow == null)
            {
                MessageBox.Show("請先選擇商品", "提示");
                return;
            }

            int productId = (int)dgvProducts.CurrentRow.Cells["ProductID"].Value;
            var product = products.FirstOrDefault(p => p.ProductID == productId);

            if (product == null) return;

            int quantity = (int)nudQuantity.Value;
            if (quantity <= 0)
            {
                MessageBox.Show("請輸入大於0的數量", "提示");
                return;
            }

            if (!decimal.TryParse(txtUnitPrice.Text, out decimal unitPrice) || unitPrice <= 0)
            {
                MessageBox.Show("請輸入正確的單價", "提示");
                return;
            }

            var existing = currentItems.FirstOrDefault(i => i.ProductID == productId);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                currentItems.Add(new PurchaseItem
                {
                    ProductID = productId,
                    ProductName = product.ProductName,
                    Quantity = quantity,
                    UnitPrice = unitPrice
                });
            }

            RefreshItemsGrid();
            UpdateTotalAmount();
        }

        private void btnRemoveItem_Click(object sender, EventArgs e)
        {
            if (dgvPurchaseItems.CurrentRow != null)
            {
                var item = (PurchaseItem)dgvPurchaseItems.CurrentRow.DataBoundItem;
                currentItems.Remove(item);
                RefreshItemsGrid();
                UpdateTotalAmount();
            }
        }

        private void RefreshItemsGrid()
        {
            dgvPurchaseItems.DataSource = null;
            dgvPurchaseItems.DataSource = currentItems.Select(i => new
            {
                i.ProductID,
                i.ProductName,
                i.Quantity,
                單價 = i.UnitPrice,
                小計 = i.SubTotal
            }).ToList();
        }

        private void UpdateTotalAmount()
        {
            decimal total = currentItems.Sum(i => i.SubTotal);
            lblTotalAmount.Text = $"總金額: NT${total:N0}";
        }

        private void btnSaveOrder_Click(object sender, EventArgs e)
        {
            if (cmbSupplier.SelectedItem == null)
            {
                MessageBox.Show("請選擇供應商", "提示");
                return;
            }

            if (currentItems.Count == 0)
            {
                MessageBox.Show("請至少加入一項商品", "提示");
                return;
            }

            int supplierId = (int)cmbSupplier.SelectedValue;
            var supplier = suppliers.FirstOrDefault(s => s.SupplierID == supplierId);

            var purchaseOrder = new PurchaseOrder
            {
                PurchaseID = nextPurchaseId,
                PurchaseDate = dtpPurchaseDate.Value,
                SupplierID = supplierId,
                SupplierName = supplier.SupplierName,
                TotalQuantity = currentItems.Sum(i => i.Quantity),
                TotalAmount = currentItems.Sum(i => i.SubTotal),
                Status = "已進貨"
            };
            purchaseOrders.Add(purchaseOrder);

            int detailId = purchaseDetails.Count > 0 ? purchaseDetails.Max(d => d.DetailID) + 1 : 1;
            foreach (var item in currentItems)
            {
                purchaseDetails.Add(new PurchaseDetail
                {
                    DetailID = detailId++,
                    PurchaseID = nextPurchaseId,
                    ProductID = item.ProductID,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });

                var product = products.FirstOrDefault(p => p.ProductID == item.ProductID);
                if (product != null)
                {
                    product.Stock += item.Quantity;
                }
            }

            SaveData();
            nextPurchaseId++;

            MessageBox.Show($"採購單 #{purchaseOrder.PurchaseID} 儲存成功！\n總金額: NT${purchaseOrder.TotalAmount:N0}", "成功");

            ClearForm();
            LoadProductsToGrid();
            CheckLowStock();
        }

        private void SaveData()
        {
            string orderPath = Path.Combine(dataFolderPath, "PurchaseOrders.json");
            string orderJson = JsonConvert.SerializeObject(purchaseOrders, Formatting.Indented);
            File.WriteAllText(orderPath, orderJson);

            string detailPath = Path.Combine(dataFolderPath, "PurchaseDetails.json");
            string detailJson = JsonConvert.SerializeObject(purchaseDetails, Formatting.Indented);
            File.WriteAllText(detailPath, detailJson);

            string productPath = Path.Combine(dataFolderPath, "Products.json");
            string productJson = JsonConvert.SerializeObject(products, Formatting.Indented);
            File.WriteAllText(productPath, productJson);
        }

        private void ClearForm()
        {
            cmbSupplier.SelectedIndex = -1;
            dtpPurchaseDate.Value = DateTime.Now;
            currentItems.Clear();
            RefreshItemsGrid();
            UpdateTotalAmount();
            txtUnitPrice.Clear();
            nudQuantity.Value = 1;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnCheckStock_Click(object sender, EventArgs e)
        {
            var lowStockProducts = products.Where(p => p.Stock < 20).OrderBy(p => p.Stock).ToList();

            if (lowStockProducts.Count == 0)
            {
                MessageBox.Show("所有商品庫存充足！", "庫存查詢", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string report = "╔════════════════════════════════════════════════════════════╗\n";
            report += "║                    庫存不足商品報告                          ║\n";
            report += "╠══════════════╦══════════════════════╦════════════════════╣\n";
            report += "║   商品編號    ║      商品名稱         ║      目前庫存       ║\n";
            report += "╠══════════════╬══════════════════════╬════════════════════╣\n";

            foreach (var p in lowStockProducts)
            {
                report += $"║ {p.ProductID,-12} ║ {p.ProductName,-20} ║ {p.Stock,-18} ║\n";
            }
            report += "╚══════════════╩══════════════════════╩════════════════════╝\n";

            MessageBox.Show(report, "庫存警示詳細報告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}