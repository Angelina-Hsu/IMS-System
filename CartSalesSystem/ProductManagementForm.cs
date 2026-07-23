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
    public partial class ProductManagementForm : Form
    {
        private List<Product> products;
        private string dataFolderPath;
        private string imageFolderPath;
        private Product selectedProduct;
        public ProductManagementForm(List<Product> productList, string dataPath, string imagePath)
        {
            InitializeComponent();
            products = productList;
            dataFolderPath = dataPath;
            imageFolderPath = imagePath;

            // 設定分類下拉選單
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add("頭髮護理");
            cmbCategory.Items.Add("身體沐洗保養");
            cmbCategory.Items.Add("臉部清潔");
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;

            // 設定產品編號唯讀
            txtProductID.ReadOnly = true;

            LoadProductsToGrid();
            SetupDataGridView();
        }

        private void LoadProductsToGrid()
        {
            dgvProducts.DataSource = null;
            dgvProducts.DataSource = products.Select(p => new
            {
                p.ProductID,
                p.ProductName,
                p.Category,
                p.Price,
                p.Stock,
                p.PhotoFileName
            }).ToList();
        }

        private void SetupDataGridView()
        {
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.ReadOnly = true;
            dgvProducts.SelectionChanged += DgvProducts_SelectionChanged;
        }

        private void DgvProducts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow != null)
            {
                string productName = dgvProducts.CurrentRow.Cells["ProductName"].Value.ToString();
                selectedProduct = products.FirstOrDefault(p => p.ProductName == productName);

                if (selectedProduct != null)
                {
                    txtProductID.Text = selectedProduct.ProductID.ToString();
                    txtProductName.Text = selectedProduct.ProductName;
                    cmbCategory.Text = selectedProduct.Category;
                    txtPrice.Text = selectedProduct.Price.ToString();
                    txtStock.Text = selectedProduct.Stock.ToString();
                    txtPhotoFileName.Text = selectedProduct.PhotoFileName;

                    // 顯示照片
                    string imagePath = Path.Combine(imageFolderPath, selectedProduct.PhotoFileName);
                    if (File.Exists(imagePath))
                    {
                        try
                        {
                            picProductImage.Image = Image.FromFile(imagePath);
                            picProductImage.SizeMode = PictureBoxSizeMode.Zoom;
                        }
                        catch
                        {
                            picProductImage.Image = null;
                        }
                    }
                    else
                    {
                        picProductImage.Image = null;
                    }
                }
            }
        }

        private void ProductManagementForm_Load(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            // 檢查必填欄位
            if (string.IsNullOrWhiteSpace(txtProductName.Text))
            {
                MessageBox.Show("請輸入產品名稱", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbCategory.SelectedItem == null)
            {
                MessageBox.Show("請選擇分類", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrice.Text, out decimal price))
            {
                MessageBox.Show("請輸入正確的價格", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtStock.Text, out int stock))
            {
                MessageBox.Show("請輸入正確的庫存數量", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 產生新ID
            int newId = products.Count > 0 ? products.Max(p => p.ProductID) + 1 : 1;

            var newProduct = new Product
            {
                ProductID = newId,
                ProductName = txtProductName.Text,
                Category = cmbCategory.SelectedItem.ToString(),
                Price = price,
                Stock = stock,
                PhotoFileName = txtPhotoFileName.Text
            };

            products.Add(newProduct);
            SaveProducts();
            LoadProductsToGrid();
            ClearForm();
            MessageBox.Show("產品新增成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedProduct == null)
            {
                MessageBox.Show("請先從列表中選擇要修改的產品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 檢查必填欄位
            if (string.IsNullOrWhiteSpace(txtProductName.Text))
            {
                MessageBox.Show("請輸入產品名稱", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbCategory.SelectedItem == null)
            {
                MessageBox.Show("請選擇分類", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrice.Text, out decimal price))
            {
                MessageBox.Show("請輸入正確的價格", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtStock.Text, out int stock))
            {
                MessageBox.Show("請輸入正確的庫存數量", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selectedProduct.ProductName = txtProductName.Text;
            selectedProduct.Category = cmbCategory.SelectedItem.ToString();
            selectedProduct.Price = price;
            selectedProduct.Stock = stock;
            selectedProduct.PhotoFileName = txtPhotoFileName.Text;

            SaveProducts();
            LoadProductsToGrid();
            MessageBox.Show("產品修改成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedProduct == null)
            {
                MessageBox.Show("請先從列表中選擇要刪除的產品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show($"確定要刪除「{selectedProduct.ProductName}」嗎？", "確認刪除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                products.Remove(selectedProduct);
                SaveProducts();
                LoadProductsToGrid();
                ClearForm();
                MessageBox.Show("產品刪除成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
            // 清除選取
            dgvProducts.ClearSelection();
            selectedProduct = null;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                LoadProductsToGrid();
            }
            else
            {
                var filtered = products.Where(p =>
                    p.ProductName.Contains(keyword) ||
                    p.Category.Contains(keyword)).ToList();

                dgvProducts.DataSource = null;
                dgvProducts.DataSource = filtered.Select(p => new
                {
                    p.ProductID,
                    p.ProductName,
                    p.Category,
                    p.Price,
                    p.Stock,
                    p.PhotoFileName
                }).ToList();
            }
        }



        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ClearForm()
        {
            txtProductID.Clear();
            txtProductName.Clear();
            cmbCategory.SelectedIndex = -1;
            txtPrice.Clear();
            txtStock.Clear();
            txtPhotoFileName.Clear();
            picProductImage.Image = null;
            selectedProduct = null;
        }

        private void SaveProducts()
        {
            string filePath = Path.Combine(dataFolderPath, "Products.json");
            string json = JsonConvert.SerializeObject(products, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}
