using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static CartSalesSystem.Form1;

namespace CartSalesSystem
{
    public partial class SupplierManagementForm : Form
    {
        private List<Supplier> suppliers;
        private List<PurchaseOrder> purchaseOrders;
        private string dataFolderPath;
        private Supplier selectedSupplier;
       
           public SupplierManagementForm(List<Supplier> supplierList, List<PurchaseOrder> purchaseOrderList, string dataPath)
        {
            InitializeComponent();
            suppliers = supplierList;
            purchaseOrders = purchaseOrderList;
            dataFolderPath = dataPath;

            txtSupplierID.ReadOnly = true;

            LoadSuppliersToGrid();
            SetupDataGridView();
        }

        private void LoadSuppliersToGrid()
        {
            dgvSuppliers.DataSource = null;
            dgvSuppliers.DataSource = suppliers.Select(s => new
            {
                s.SupplierID,
                s.SupplierName,
                s.ContactPerson,
                s.Phone,
                s.Email,
                s.Address
            }).ToList();
        }

        private void SetupDataGridView()
        {
            dgvSuppliers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSuppliers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSuppliers.ReadOnly = true;
            dgvSuppliers.SelectionChanged += DgvSuppliers_SelectionChanged;
        }

        private void DgvSuppliers_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvSuppliers.CurrentRow != null)
            {
                int supplierId = (int)dgvSuppliers.CurrentRow.Cells["SupplierID"].Value;
                selectedSupplier = suppliers.FirstOrDefault(s => s.SupplierID == supplierId);

                if (selectedSupplier != null)
                {
                    txtSupplierID.Text = selectedSupplier.SupplierID.ToString();
                    txtSupplierName.Text = selectedSupplier.SupplierName;
                    txtContactPerson.Text = selectedSupplier.ContactPerson;
                    txtPhone.Text = selectedSupplier.Phone;
                    txtEmail.Text = selectedSupplier.Email;
                    txtAddress.Text = selectedSupplier.Address;
                    txtTaxNumber.Text = selectedSupplier.TaxNumber;
                    txtBankAccount.Text = selectedSupplier.BankAccount;
                    txtRemark.Text = selectedSupplier.Remark;

                    // 載入該供應商的進貨記錄
                    LoadPurchaseHistory(supplierId);
                }
            }
        }
        private void LoadPurchaseHistory(int supplierId)
        {
            var supplierOrders = purchaseOrders.Where(o => o.SupplierID == supplierId).OrderByDescending(o => o.PurchaseDate).ToList();

            dgvPurchaseHistory.DataSource = null;
            if (supplierOrders.Count > 0)
            {
                dgvPurchaseHistory.DataSource = supplierOrders.Select(o => new
                {
                    o.PurchaseID,
                    採購日期 = o.PurchaseDate.ToString("yyyy/MM/dd"),
                    o.TotalQuantity,
                    總金額 = o.TotalAmount,
                    o.Status
                }).ToList();
            }
            else
            {
                var emptyList = new[] { new { PurchaseID = 0, 採購日期 = "", TotalQuantity = 0, 總金額 = 0m, Status = "無進貨記錄" } }.ToList();
                dgvPurchaseHistory.DataSource = emptyList;
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSupplierName.Text))
            {
                MessageBox.Show("請輸入供應商名稱", "提示");
                return;
            }

            int newId = suppliers.Count > 0 ? suppliers.Max(s => s.SupplierID) + 1 : 1;

            var newSupplier = new Supplier
            {
                SupplierID = newId,
                SupplierName = txtSupplierName.Text,
                ContactPerson = txtContactPerson.Text,
                Phone = txtPhone.Text,
                Email = txtEmail.Text,
                Address = txtAddress.Text,
                TaxNumber = txtTaxNumber.Text,
                BankAccount = txtBankAccount.Text,
                Remark = txtRemark.Text,
                CreateDate = DateTime.Now
            };

            suppliers.Add(newSupplier);
            SaveSuppliers();
            LoadSuppliersToGrid();
            ClearForm();
            MessageBox.Show("供應商新增成功！", "成功");
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedSupplier == null)
            {
                MessageBox.Show("請先選擇要修改的供應商", "提示");
                return;
            }

            selectedSupplier.SupplierName = txtSupplierName.Text;
            selectedSupplier.ContactPerson = txtContactPerson.Text;
            selectedSupplier.Phone = txtPhone.Text;
            selectedSupplier.Email = txtEmail.Text;
            selectedSupplier.Address = txtAddress.Text;
            selectedSupplier.TaxNumber = txtTaxNumber.Text;
            selectedSupplier.BankAccount = txtBankAccount.Text;
            selectedSupplier.Remark = txtRemark.Text;

            SaveSuppliers();
            LoadSuppliersToGrid();
            MessageBox.Show("供應商修改成功！", "成功");
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedSupplier == null)
            {
                MessageBox.Show("請先選擇要刪除的供應商", "提示");
                return;
            }

            DialogResult result = MessageBox.Show($"確定要刪除「{selectedSupplier.SupplierName}」嗎？", "確認刪除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                suppliers.Remove(selectedSupplier);
                SaveSuppliers();
                LoadSuppliersToGrid();
                ClearForm();
                MessageBox.Show("供應商刪除成功！", "成功");
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
            dgvSuppliers.ClearSelection();
            selectedSupplier = null;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                LoadSuppliersToGrid();
            }
            else
            {
                var filtered = suppliers.Where(s =>
                    s.SupplierName.Contains(keyword) ||
                    s.ContactPerson.Contains(keyword) ||
                    s.Phone.Contains(keyword)).ToList();

                dgvSuppliers.DataSource = null;
                dgvSuppliers.DataSource = filtered.Select(s => new
                {
                    s.SupplierID,
                    s.SupplierName,
                    s.ContactPerson,
                    s.Phone,
                    s.Email,
                    s.Address
                }).ToList();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ClearForm()
        {
            txtSupplierID.Clear();
            txtSupplierName.Clear();
            txtContactPerson.Clear();
            txtPhone.Clear();
            txtEmail.Clear();
            txtAddress.Clear();
            txtTaxNumber.Clear();
            txtBankAccount.Clear();
            txtRemark.Clear();
            selectedSupplier = null;
        }

        private void SaveSuppliers()
        {
            string filePath = Path.Combine(dataFolderPath, "Suppliers.json");
            string json = JsonConvert.SerializeObject(suppliers, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
 }
