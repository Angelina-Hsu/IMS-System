using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static CartSalesSystem.Form1;

namespace CartSalesSystem
{
    public partial class CustomerManagementForm : Form
    {
        private List<Member> members;
        private List<Order> orders;
        private string dataFolderPath;
        private Member selectedMember;

        public CustomerManagementForm(List<Member> memberList, List<Order> orderList, string dataPath)
        {
            InitializeComponent();
            members = memberList;
            orders = orderList;
            dataFolderPath = dataPath;

            // 設定性別下拉選單
            cmbGender.Items.Clear();
            cmbGender.Items.Add("男");
            cmbGender.Items.Add("女");
            cmbGender.DropDownStyle = ComboBoxStyle.DropDownList;

            // 設定客戶編號唯讀
            txtCustomerID.ReadOnly = true;

            LoadCustomersToGrid();
            SetupDataGrids();
        }

        private void LoadCustomersToGrid()
        {
            dgvCustomers.DataSource = null;
            dgvCustomers.DataSource = members.Select(m => new
            {
                m.MemberID,
                m.MemberName,
                m.Gender,
                m.Phone,
                m.Email,
                m.Points
            }).ToList();
        }

        private void SetupDataGrids()
        {
            dgvCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCustomers.ReadOnly = true;
            dgvCustomers.SelectionChanged += DgvCustomers_SelectionChanged;

            dgvCustomerOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCustomerOrders.ReadOnly = true;
        }

        private void DgvCustomers_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvCustomers.CurrentRow != null)
            {
                int memberId = (int)dgvCustomers.CurrentRow.Cells["MemberID"].Value;
                selectedMember = members.FirstOrDefault(m => m.MemberID == memberId);

                if (selectedMember != null)
                {
                    txtCustomerID.Text = selectedMember.MemberID.ToString();
                    txtCustomerName.Text = selectedMember.MemberName;
                    cmbGender.Text = selectedMember.Gender;
                    txtPhone.Text = selectedMember.Phone;
                    txtEmail.Text = selectedMember.Email;
                    txtPoints.Text = selectedMember.Points.ToString();

                    // 載入該客戶的歷史訂單
                    LoadCustomerOrders(selectedMember.MemberID);
                }
            }
        }

        private void LoadCustomerOrders(int memberId)
        {
            var customerOrders = orders.Where(o => o.MemberID == memberId).OrderByDescending(o => o.OrderDate).ToList();

            dgvCustomerOrders.DataSource = null;
            if (customerOrders.Count > 0)
            {
                var orderData = customerOrders.Select(o => new
                {
                    o.OrderID,
                    訂單日期 = o.OrderDate.ToString("yyyy/MM/dd HH:mm"),
                    總數量 = o.TotalQuantity,
                    總金額 = o.TotalAmount,
                    明細 = string.Join(", ", o.Items.Select(i => $"{i.ProductName} x{i.Quantity}"))
                }).ToList();
                dgvCustomerOrders.DataSource = orderData;

                // 調整欄位寬度
                if (dgvCustomerOrders.Columns.Contains("明細"))
                    dgvCustomerOrders.Columns["明細"].Width = 300;
            }
            else
            {
                var emptyList = new[] { new { OrderID = 0, 訂單日期 = "", 總數量 = 0, 總金額 = 0m, 明細 = "無訂單記錄" } }.ToList();
                dgvCustomerOrders.DataSource = emptyList;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                MessageBox.Show("請輸入客戶姓名", "提示");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("請輸入Email", "提示");
                return;
            }

            // 檢查Email是否重複
            if (members.Any(m => m.Email == txtEmail.Text))
            {
                MessageBox.Show("此Email已存在！", "提示");
                return;
            }

            int newId = members.Count > 0 ? members.Max(m => m.MemberID) + 1 : 1;

            var newMember = new Member
            {
                MemberID = newId,
                MemberName = txtCustomerName.Text,
                Gender = cmbGender.SelectedItem?.ToString(),
                Phone = txtPhone.Text,
                Email = txtEmail.Text,
                Points = int.TryParse(txtPoints.Text, out int points) ? points : 0
            };

            members.Add(newMember);
            SaveMembers();
            LoadCustomersToGrid();
            ClearForm();
            MessageBox.Show("客戶新增成功！", "成功");
        }



        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedMember == null)
            {
                MessageBox.Show("請先選擇要刪除的客戶", "提示");
                return;
            }

            // 檢查是否有訂單記錄
            if (orders.Any(o => o.MemberID == selectedMember.MemberID))
            {
                MessageBox.Show("該客戶有訂單記錄，無法刪除！", "提示");
                return;
            }

            DialogResult result = MessageBox.Show($"確定要刪除「{selectedMember.MemberName}」嗎？", "確認刪除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                members.Remove(selectedMember);
                SaveMembers();
                LoadCustomersToGrid();
                ClearForm();
                MessageBox.Show("客戶刪除成功！", "成功");
            }
        }


        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedMember == null)
            {
                MessageBox.Show("請先選擇要修改的客戶", "提示");
                return;
            }

            selectedMember.MemberName = txtCustomerName.Text;
            selectedMember.Gender = cmbGender.SelectedItem?.ToString();
            selectedMember.Phone = txtPhone.Text;
            selectedMember.Email = txtEmail.Text;
            selectedMember.Points = int.TryParse(txtPoints.Text, out int points) ? points : 0;

            SaveMembers();
            LoadCustomersToGrid();
            MessageBox.Show("客戶修改成功！", "成功");
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
            dgvCustomers.ClearSelection();
            selectedMember = null;
            dgvCustomerOrders.DataSource = null;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                LoadCustomersToGrid();
            }
            else
            {
                var filtered = members.Where(m =>
                    m.MemberName.Contains(keyword) ||
                    m.Email.Contains(keyword) ||
                    m.Phone.Contains(keyword)).ToList();

                dgvCustomers.DataSource = null;
                dgvCustomers.DataSource = filtered.Select(m => new
                {
                    m.MemberID,
                    m.MemberName,
                    m.Gender,
                    m.Phone,
                    m.Email,
                    m.Points
                }).ToList();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ClearForm()
        {
            txtCustomerID.Clear();
            txtCustomerName.Clear();
            cmbGender.SelectedIndex = -1;
            txtPhone.Clear();
            txtEmail.Clear();
            txtPoints.Clear();
            selectedMember = null;
        }

        private void SaveMembers()
        {
            string filePath = Path.Combine(dataFolderPath, "Members.json");
            string json = JsonConvert.SerializeObject(members, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}
