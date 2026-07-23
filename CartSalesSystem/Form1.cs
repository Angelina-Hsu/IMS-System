using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Menu;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Newtonsoft.Json;

namespace CartSalesSystem
{
    public partial class Form1 : Form
    {
        // 付款方式資料
        private List<string> paymentMethods = new List<string>();
        // 商品類別
        public class Product
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public string Category { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public string PhotoFileName { get; set; }
        }

        // 會員類別
        public class Member
        {
            public int MemberID { get; set; }
            public string MemberName { get; set; }
            public string Gender { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public int Points { get; set; }
        }

        // 購物車項目類別
        public class CartItem
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public decimal SubTotal => Price * Quantity;
        }

        // 訂單類別
        public class Order
        {
            public int OrderID { get; set; }
            public DateTime OrderDate { get; set; }
            public int MemberID { get; set; }
            public List<CartItem> Items { get; set; }
            public int TotalQuantity => Items.Sum(i => i.Quantity);
            public decimal TotalAmount => Items.Sum(i => i.SubTotal);
        }

        private List<Product> products = new List<Product>();
        private List<Member> members = new List<Member>();
        private List<CartItem> cart = new List<CartItem>();
        private List<Order> orders = new List<Order>();
        private List<Supplier> suppliers = new List<Supplier>();
        private List<PurchaseOrder> purchaseOrders = new List<PurchaseOrder>();
        private List<PurchaseDetail> purchaseDetails = new List<PurchaseDetail>();
        private int nextOrderID = 1;

        // 照片資料夾路徑
        private string imageFolderPath;
        private string dataFolderPath;

        // 供應商類別
        public class Supplier
        {
            public int SupplierID { get; set; }
            public string SupplierName { get; set; }
            public string ContactPerson { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
            public string TaxNumber { get; set; }  // 統一編號
            public string BankAccount { get; set; } // 銀行帳號
            public string Remark { get; set; }      // 備註
            public DateTime CreateDate { get; set; } // 建立日期
        }


        public Form1()
        {
            InitializeComponent();
            InitializeForm();
            LoadData();
            LoadCategories();
            LoadProducts();
            SetupDataGrids();
            // 手動綁定照片點擊事件
            // 手動綁定事件
            this.dgvProducts.SelectionChanged += dgvProducts_SelectionChanged;
            this.cmbCategory.SelectedIndexChanged += cmbCategory_SelectedIndexChanged;
            this.btnAddToCart.Click += btnAddToCart_Click;
            this.btnRemove.Click += btnRemove_Click;
            this.btnUpdateQty.Click += btnUpdateQty_Click;
            this.btnCheckout.Click += btnCheckout_Click;
            this.btnSortByProductID.Click += new EventHandler(btnSortByProductID_Click);
            this.picProductImage.Click += picProductImage_Click;  // ← 加入這行
            this.btnSearch.Click += new EventHandler(btnSearch_Click);
        }
        private void InitializeForm()
        {
            this.Text = "購物車銷售系統 ";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;

            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.SplitterDistance = (int)(this.Width * 0.4);

            nudQuantity.Minimum = 1;
            nudQuantity.Maximum = 99;
            nudQuantity.Value = 1;

            // 設定產品名稱 Label 位置
            if (lblProductName != null)
            {
                lblProductName.Location = new Point(picProductImage.Location.X,
                                                    picProductImage.Location.Y + picProductImage.Height + 5);
                lblProductName.Width = picProductImage.Width;
                lblProductName.Height = 30;
                lblProductName.TextAlign = ContentAlignment.MiddleCenter;
            }

            // 設定資料路徑
            dataFolderPath = Path.Combine(Application.StartupPath, "Data");
            imageFolderPath = Path.Combine(Application.StartupPath, "Images");

            if (!Directory.Exists(dataFolderPath))
                Directory.CreateDirectory(dataFolderPath);
            if (!Directory.Exists(imageFolderPath))
                Directory.CreateDirectory(imageFolderPath);

            // 設定付款方式
            SetupPaymentMethods();

            SetupMenu();
        }

        private void SetupPaymentMethods()
        {
            // 固定的付款方式選項
            // 固定的付款方式選項
            paymentMethods = new List<string>
    {
        "信用卡",
        "Line Pay",
        "街口支付",
        "Apple Pay",
        "Google Pay"
    };

            clbPayment.Items.Clear();
            foreach (var method in paymentMethods)
            {
                clbPayment.Items.Add(method);
            }
            clbPayment.CheckOnClick = true;
            clbPayment.Height = 100;

            // 設定為單選模式
            clbPayment.SelectionMode = SelectionMode.One;

            // 綁定項目選取變更事件
            clbPayment.ItemCheck += ClbPayment_ItemCheck;
        }


        private void SetupMenu()
        {
            // 清除現有選單
            menuStrip1.Items.Clear();

            // ========== 檔案選單 ==========
            var fileMenu = new ToolStripMenuItem("檔案");
            var exitItem = new ToolStripMenuItem("離開");
            exitItem.Click += (s, e) => Application.Exit();
            fileMenu.DropDownItems.Add(exitItem);

            // ========== 客戶管理選單 ==========
            var customerMenu = new ToolStripMenuItem("客戶管理");

            var customerManageItem = new ToolStripMenuItem("客戶資料維護");
            customerManageItem.Click += (s, e) => OpenCustomerManagement();
            customerMenu.DropDownItems.Add(customerManageItem);

            var customerHistoryItem = new ToolStripMenuItem("客戶訂購記錄查詢");
            customerHistoryItem.Click += (s, e) => ShowCustomerOrderHistory();
            customerMenu.DropDownItems.Add(customerHistoryItem);


            // ========== 供應商選單 ==========
            var supplierMenu = new ToolStripMenuItem("供應商");

            var supplierManageItem = new ToolStripMenuItem("供應商管理");
            supplierManageItem.Click += (s, e) => OpenSupplierManagement();
            supplierMenu.DropDownItems.Add(supplierManageItem);

            // ========== 產品選單 ==========
            var productMenu = new ToolStripMenuItem("產品");

            var productManageItem = new ToolStripMenuItem("產品管理");
            productManageItem.Click += (s, e) => OpenProductManagement();
            productMenu.DropDownItems.Add(productManageItem);

            // ========== 採購管理選單 ==========
            var purchaseMenu = new ToolStripMenuItem("採購管理");

            var purchaseOrderItem = new ToolStripMenuItem("採購進貨");
            purchaseOrderItem.Click += (s, e) => OpenPurchaseOrderForm();
            purchaseMenu.DropDownItems.Add(purchaseOrderItem);

            var purchaseQueryItem = new ToolStripMenuItem("採購記錄查詢");
            purchaseQueryItem.Click += (s, e) => ShowPurchaseHistory();
            purchaseMenu.DropDownItems.Add(purchaseQueryItem);

            // ========== 訂單選單 ==========
            var orderMenu = new ToolStripMenuItem("訂單");

            var orderQueryItem = new ToolStripMenuItem("訂單查詢");
            orderQueryItem.Click += (s, e) => ShowOrderHistory();
            orderMenu.DropDownItems.Add(orderQueryItem);


            // ========== 說明選單 ==========
            var helpMenu = new ToolStripMenuItem("說明");

            var aboutItem = new ToolStripMenuItem("關於");
            aboutItem.Click += (s, e) => MessageBox.Show("購物車銷售系統 v1.0\n© 2024", "關於");
            helpMenu.DropDownItems.Add(aboutItem);

            // ========== 將所有選單加入 menuStrip1 ==========
            menuStrip1.Items.AddRange(new ToolStripMenuItem[] {
        fileMenu, customerMenu, supplierMenu, purchaseMenu, productMenu, orderMenu, helpMenu
    });
        }


        private void LoadData()
        {
            LoadProductsFromJson();
            LoadMembersFromJson();
            LoadOrdersFromJson();
            LoadSuppliersFromJson();
            LoadPurchaseDataFromJson();
            GeneratePurchaseRecords();

        }

        private void LoadProductsFromJson()
        {
            string filePath = Path.Combine(dataFolderPath, "Products.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                products = JsonConvert.DeserializeObject<List<Product>>(json);
            }
            else
            {
                // 使用預設產品資料
                products = GetDefaultProducts();
                SaveProductsToJson();
            }
        }

        private List<Product> GetDefaultProducts()
        {
            return new List<Product>
            {
                      new Product { ProductID = 1, ProductName = "木質調洗髮乳", Category = "頭髮護理", Price = 520, Stock = 30, PhotoFileName = "木質調洗髮乳.jpg" },
        new Product { ProductID = 2, ProductName = "木質調護髮乳", Category = "頭髮護理", Price = 550, Stock = 28, PhotoFileName = "木質調護髮乳.jpg" },
        new Product { ProductID = 3, ProductName = "花香洗髮乳", Category = "頭髮護理", Price = 480, Stock = 35, PhotoFileName = "花香洗髮乳.jpg" },
        new Product { ProductID = 4, ProductName = "花果護髮乳", Category = "頭髮護理", Price = 510, Stock = 22, PhotoFileName = "花果護髮乳.jpg" },
        new Product { ProductID = 5, ProductName = "森林洗髮乳", Category = "頭髮護理", Price = 500, Stock = 40, PhotoFileName = "森林洗髮乳.jpg" },
        new Product { ProductID = 6, ProductName = "森林護髮乳", Category = "頭髮護理", Price = 530, Stock = 25, PhotoFileName = "森林護髮乳.jpg" },
        new Product { ProductID = 7, ProductName = "月光玫瑰沐浴乳", Category = "身體沐洗保養", Price = 380, Stock = 50, PhotoFileName = "月光玫瑰沐浴乳.jpg" },
        new Product { ProductID = 8, ProductName = "牡丹夢境沐浴乳", Category = "身體沐洗保養", Price = 390, Stock = 45, PhotoFileName = "牡丹夢境沐浴乳.jpg" },
        new Product { ProductID = 9, ProductName = "募光花語沐浴乳", Category = "身體沐洗保養", Price = 400, Stock = 42, PhotoFileName = "募光花語沐浴乳.jpg" },
        new Product { ProductID = 10, ProductName = "綠茶身體乳", Category = "身體沐洗保養", Price = 450, Stock = 38, PhotoFileName = "綠茶身體乳.jpg" },
        new Product { ProductID = 11, ProductName = "蜂蜜身體乳", Category = "身體沐洗保養", Price = 460, Stock = 35, PhotoFileName = "蜂蜜身體乳.jpg" },
        new Product { ProductID = 12, ProductName = "薰衣草身體乳", Category = "身體沐洗保養", Price = 440, Stock = 40, PhotoFileName = "薰衣草身體乳.jpg" },
        new Product { ProductID = 13, ProductName = "雪松身體乳", Category = "身體沐洗保養", Price = 470, Stock = 33, PhotoFileName = "雪松身體乳.jpg" },
        new Product { ProductID = 14, ProductName = "柑橘身體乳", Category = "身體沐洗保養", Price = 430, Stock = 37, PhotoFileName = "柑橘身體乳.jpg" },
        new Product { ProductID = 15, ProductName = "雨後甘霖身體乳", Category = "身體沐洗保養", Price = 490, Stock = 29, PhotoFileName = "雨後甘霖身體乳.jpg" },
        new Product { ProductID = 16, ProductName = "亮白洗面乳", Category = "臉部清潔", Price = 350, Stock = 60, PhotoFileName = "亮白洗面乳.jpg" },
        new Product { ProductID = 17, ProductName = "玻尿酸保濕洗面乳", Category = "臉部清潔", Price = 370, Stock = 55, PhotoFileName = "玻尿酸保濕洗面乳.jpg" },
        new Product { ProductID = 18, ProductName = "膠原蛋白洗面乳", Category = "臉部清潔", Price = 390, Stock = 50, PhotoFileName = "膠原蛋白洗面乳.jpg" },
        new Product { ProductID = 19, ProductName = "積雪草舒緩洗面乳", Category = "臉部清潔", Price = 410, Stock = 48, PhotoFileName = "積雪草舒緩洗面乳.jpg" },
        new Product { ProductID = 20, ProductName = "玫瑰花水", Category = "臉部清潔", Price = 280, Stock = 70, PhotoFileName = "玫瑰花水.jpg" },
        new Product { ProductID = 21, ProductName = "薰衣草花水", Category = "臉部清潔", Price = 280, Stock = 68, PhotoFileName = "薰衣草花水.jpg" },
        new Product { ProductID = 22, ProductName = "薄荷花水", Category = "臉部清潔", Price = 290, Stock = 65, PhotoFileName = "薄荷花水.jpg" },
        new Product { ProductID = 23, ProductName = "美白精華", Category = "臉部清潔", Price = 880, Stock = 25, PhotoFileName = "美白精華.jpg" },
        new Product { ProductID = 24, ProductName = "燕窩胜肽精華", Category = "臉部清潔", Price = 1200, Stock = 20, PhotoFileName = "燕窩胜肽精華.jpg" },
        new Product { ProductID = 25, ProductName = "藍銅修護精華", Category = "臉部清潔", Price = 1350, Stock = 18, PhotoFileName = "藍銅修護精華.jpg" },
        new Product { ProductID = 26, ProductName = "雪絨花緊緻反重力乳液", Category = "臉部清潔", Price = 1680, Stock = 15, PhotoFileName = "雪絨花緊緻反重力乳液.jpg" },
        new Product { ProductID = 27, ProductName = "海罌粟沙棘繃亮霜", Category = "臉部清潔", Price = 1490, Stock = 16, PhotoFileName = "海罌粟沙棘繃亮霜.jpg" },
        new Product { ProductID = 28, ProductName = "松藻舒敏賦活霜", Category = "臉部清潔", Price = 1420, Stock = 14, PhotoFileName = "松藻舒敏賦活霜.jpg" }
    };
        }

        private void SaveProductsToJson()
        {
            string filePath = Path.Combine(dataFolderPath, "Products.json");
            string json = JsonConvert.SerializeObject(products, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private void LoadMembersFromJson()
        {
            string filePath = Path.Combine(dataFolderPath, "Members.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                members = JsonConvert.DeserializeObject<List<Member>>(json);
            }
            else
            {
                members = GetDefaultMembers();
                SaveMembersToJson();
            }
        }

        private List<Member> GetDefaultMembers()
        {
            return new List<Member>
    {
        new Member { MemberID = 1, MemberName = "王小明", Gender = "男", Phone = "0912345678", Email = "xiaoming@example.com", Points = 120 },
        new Member { MemberID = 2, MemberName = "陳美華", Gender = "女", Phone = "0923456789", Email = "meihua@example.com", Points = 350 },
        new Member { MemberID = 3, MemberName = "李志豪", Gender = "男", Phone = "0934567890", Email = "zhihao@example.com", Points = 80 },
        new Member { MemberID = 4, MemberName = "林怡君", Gender = "女", Phone = "0945678901", Email = "yijun@example.com", Points = 500 },
        new Member { MemberID = 5, MemberName = "張家豪", Gender = "男", Phone = "0956789012", Email = "jiahao@example.com", Points = 230 },
        new Member { MemberID = 6, MemberName = "黃淑芬", Gender = "女", Phone = "0967890123", Email = "shufen@example.com", Points = 680 },
        new Member { MemberID = 7, MemberName = "吳承翰", Gender = "男", Phone = "0978901234", Email = "chenghan@example.com", Points = 45 },
        new Member { MemberID = 8, MemberName = "楊雅婷", Gender = "女", Phone = "0989012345", Email = "yating@example.com", Points = 310 },
        new Member { MemberID = 9, MemberName = "劉志明", Gender = "男", Phone = "0990123456", Email = "zhiming@example.com", Points = 175 },
        new Member { MemberID = 10, MemberName = "周欣怡", Gender = "女", Phone = "0911123456", Email = "xinyi@example.com", Points = 420 },
        new Member { MemberID = 11, MemberName = "徐偉翔", Gender = "男", Phone = "0922234567", Email = "weixiang@example.com", Points = 890 },
        new Member { MemberID = 12, MemberName = "孫婉婷", Gender = "女", Phone = "0933345678", Email = "wanting@example.com", Points = 60 },
        new Member { MemberID = 13, MemberName = "陳建宏", Gender = "男", Phone = "0944456789", Email = "jianhong@example.com", Points = 1500 },
        new Member { MemberID = 14, MemberName = "林靜怡", Gender = "女", Phone = "0955567890", Email = "jingyi@example.com", Points = 210 },
        new Member { MemberID = 15, MemberName = "羅志祥", Gender = "男", Phone = "0966678901", Email = "zhixiang@example.com", Points = 0 },
        new Member { MemberID = 16, MemberName = "張韶涵", Gender = "女", Phone = "0977789012", Email = "shaohan@example.com", Points = 750 },
        new Member { MemberID = 17, MemberName = "周杰倫", Gender = "男", Phone = "0988890123", Email = "jielun@example.com", Points = 2000 },
        new Member { MemberID = 18, MemberName = "蔡依林", Gender = "女", Phone = "0999901234", Email = "jolin@example.com", Points = 1800 },
        new Member { MemberID = 19, MemberName = "蕭敬騰", Gender = "男", Phone = "0910012345", Email = "jam@example.com", Points = 620 },
        new Member { MemberID = 20, MemberName = "田馥甄", Gender = "女", Phone = "0921123456", Email = "hebe@example.com", Points = 880 },
        new Member { MemberID = 21, MemberName = "林俊傑", Gender = "男", Phone = "0932234567", Email = "jj@example.com", Points = 950 },
        new Member { MemberID = 22, MemberName = "王心凌", Gender = "女", Phone = "0943345678", Email = "cyndi@example.com", Points = 430 },
        new Member { MemberID = 23, MemberName = "楊丞琳", Gender = "女", Phone = "0954456789", Email = "rainie@example.com", Points = 560 },
        new Member { MemberID = 24, MemberName = "潘瑋柏", Gender = "男", Phone = "0965567890", Email = "wilber@example.com", Points = 340 },
        new Member { MemberID = 25, MemberName = "陳嘉樺", Gender = "女", Phone = "0976678901", Email = "ella@example.com", Points = 1200 },
        new Member { MemberID = 26, MemberName = "任家萱", Gender = "女", Phone = "0987789012", Email = "selina@example.com", Points = 1100 },
        new Member { MemberID = 27, MemberName = "田馥甄", Gender = "女", Phone = "0998890123", Email = "hebe2@example.com", Points = 1300 },
        new Member { MemberID = 28, MemberName = "梁靜茹", Gender = "女", Phone = "0919901234", Email = "fish@example.com", Points = 670 },
        new Member { MemberID = 29, MemberName = "王力宏", Gender = "男", Phone = "0920012345", Email = "leehom@example.com", Points = 780 },
        new Member { MemberID = 30, MemberName = "張惠妹", Gender = "女", Phone = "0931123456", Email = "a-mei@example.com", Points = 3000 },
        new Member { MemberID = 31, MemberName = "無訂單客戶1", Gender = "男", Phone = "0900000001", Email = "noorder1@test.com", Points = 0 },
new Member { MemberID = 32, MemberName = "無訂單客戶2", Gender = "女", Phone = "0900000002", Email = "noorder2@test.com", Points = 0 },
new Member { MemberID = 33, MemberName = "無訂單客戶3", Gender = "男", Phone = "0900000003", Email = "noorder3@test.com", Points = 0 },
new Member { MemberID = 34, MemberName = "無訂單客戶4", Gender = "女", Phone = "0900000004", Email = "noorder4@test.com", Points = 0 },
new Member { MemberID = 35, MemberName = "無訂單客戶5", Gender = "男", Phone = "0900000005", Email = "noorder5@test.com", Points = 0 },
new Member { MemberID = 36, MemberName = "無訂單客戶6", Gender = "女", Phone = "0900000006", Email = "noorder6@test.com", Points = 0 },
new Member { MemberID = 37, MemberName = "無訂單客戶7", Gender = "男", Phone = "0900000007", Email = "noorder7@test.com", Points = 0 },
new Member { MemberID = 38, MemberName = "無訂單客戶8", Gender = "女", Phone = "0900000008", Email = "noorder8@test.com", Points = 0 }
    };
        }

        private void SaveMembersToJson()
        {
            string filePath = Path.Combine(dataFolderPath, "Members.json");
            string json = JsonConvert.SerializeObject(members, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private void LoadOrdersFromJson()
        {
            string filePath = Path.Combine(dataFolderPath, "Orders.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                orders = JsonConvert.DeserializeObject<List<Order>>(json);
                if (orders.Count > 0)
                    nextOrderID = orders.Max(o => o.OrderID) + 1;
            }
            else
            {
                orders = new List<Order>();
            }
        }

        private void SaveOrdersToJson()
        {
            string filePath = Path.Combine(dataFolderPath, "Orders.json");
            string json = JsonConvert.SerializeObject(orders, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private void LoadCategories()
        {
            var categories = products.Select(p => p.Category).Distinct().ToList();
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add("全部");
            foreach (var cat in categories)
                cmbCategory.Items.Add(cat);
            cmbCategory.SelectedIndex = 0;
        }

        private void LoadProducts(string category = "全部")
        {
            var filtered = category == "全部" ? products : products.Where(p => p.Category == category).ToList();
            dgvProducts.DataSource = null;
            dgvProducts.DataSource = filtered.Select(p => new
            {
                p.ProductID,
                p.ProductName,
                p.Price,
                p.Stock,
                p.Category
            }).ToList();
        }

        private void SetupDataGrids()
        {
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.ReadOnly = true;
            // 隱藏 Stock 欄位
            if (dgvProducts.Columns.Contains("Stock"))
            {
                dgvProducts.Columns["Stock"].Visible = false;
            }
            dgvCart.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCart.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCart.ReadOnly = true;
        }

        private void cmbCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 清除查詢關鍵字
            txtSearch.Text = "";

            // 載入產品
            LoadProducts(cmbCategory.SelectedItem?.ToString());
        }

        private void dgvProducts_SelectionChanged(object sender, EventArgs e)
        {

            if (dgvProducts.CurrentRow != null && picProductImage != null)
            {

                int productId = (int)dgvProducts.CurrentRow.Cells["ProductID"].Value;
                var product = products.FirstOrDefault(p => p.ProductID == productId);

                // 顯示產品名稱
                if (lblProductName != null)
                {
                    lblProductName.Text = product?.ProductName ?? "";
                    lblProductName.TextAlign = ContentAlignment.MiddleCenter;
                }

                if (product != null && !string.IsNullOrEmpty(product.PhotoFileName))
                {
                    string imagePath = Path.Combine(imageFolderPath, product.PhotoFileName);
                    if (File.Exists(imagePath))
                    {
                        try
                        {
                            picProductImage.Image = Image.FromFile(imagePath);
                            picProductImage.SizeMode = PictureBoxSizeMode.Zoom;
                            return;
                        }
                        catch { }
                    }
                }
                picProductImage.Image = null;
            }
        }
        private void btnAddToCart_Click(object sender, EventArgs e)
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

            if (quantity > product.Stock)
            {
                MessageBox.Show($"庫存不足！只剩 {product.Stock} 件", "提示");
                return;
            }

            var existing = cart.FirstOrDefault(c => c.ProductID == productId);
            if (existing != null)
            {
                if (existing.Quantity + quantity > product.Stock)
                {
                    MessageBox.Show($"加入後數量超過庫存 {product.Stock} 件", "提示");
                    return;
                }
                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    Quantity = quantity
                });
            }

            RefreshCart();
            MessageBox.Show($"已加入 {quantity} 件 {product.ProductName}", "成功");
        }

        private void RefreshCart()
        {
            // 按照產品編號排序後顯示
            var sortedCart = cart.OrderBy(c => c.ProductID).ToList();

            dgvCart.DataSource = null;
            dgvCart.DataSource = sortedCart;

            int totalQty = cart.Sum(c => c.Quantity);
            decimal totalAmt = cart.Sum(c => c.SubTotal);

            lblTotalItems.Text = $"總數量: {totalQty}";
            lblTotalAmount.Text = $"總金額: NT${totalAmt:N0}";
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (dgvCart.CurrentRow != null)
            {
                var item = (CartItem)dgvCart.CurrentRow.DataBoundItem;
                cart.Remove(item);
                RefreshCart();
                MessageBox.Show($"已移除 {item.ProductName}", "成功");
            }
        }



        private string ShowInputDialog(string text, string caption, string defaultValue)
        {
            Form inputForm = new Form();
            inputForm.Text = caption;
            inputForm.Width = 400;
            inputForm.Height = 150;
            inputForm.StartPosition = FormStartPosition.CenterParent;
            inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;

            Label label = new Label() { Text = text, Left = 10, Top = 20, Width = 360 };
            TextBox textBox = new TextBox() { Text = defaultValue, Left = 10, Top = 55, Width = 300 };
            Button okButton = new Button() { Text = "確定", Left = 210, Top = 85, Width = 80, DialogResult = DialogResult.OK };
            Button cancelButton = new Button() { Text = "取消", Left = 295, Top = 85, Width = 80, DialogResult = DialogResult.Cancel };

            inputForm.Controls.Add(label);
            inputForm.Controls.Add(textBox);
            inputForm.Controls.Add(okButton);
            inputForm.Controls.Add(cancelButton);
            inputForm.AcceptButton = okButton;
            inputForm.CancelButton = cancelButton;

            return inputForm.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }

        private void btnCheckout_Click(object sender, EventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("購物車是空的", "提示");
                return;
            }

            if (members.Count == 0)
            {
                MessageBox.Show("請先建立會員資料", "提示");
                return;
            }

            // 取得選中的付款方式
            List<string> selectedPayments = new List<string>();
            for (int i = 0; i < clbPayment.CheckedItems.Count; i++)
            {
                selectedPayments.Add(clbPayment.CheckedItems[i].ToString());
            }

            if (selectedPayments.Count == 0)
            {
                MessageBox.Show("請至少選擇一種付款方式", "提示");
                return;
            }

            string paymentText = string.Join(", ", selectedPayments);

            // 選擇會員（簡化版，使用第一個會員）
            var member = members.First();

            DialogResult result = MessageBox.Show(
                $"會員: {member.MemberName}\n付款方式: {paymentText}\n總金額: NT${cart.Sum(c => c.SubTotal):N0}\n總數量: {cart.Sum(c => c.Quantity)}\n\n確認結帳？",
                "結帳確認",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            // 建立訂單
            var order = new Order
            {
                OrderID = nextOrderID++,
                OrderDate = DateTime.Now,
                MemberID = member.MemberID,
                Items = new List<CartItem>(cart)
            };

            orders.Add(order);
            SaveOrdersToJson();

            // 更新庫存
            foreach (var item in cart)
            {
                var product = products.FirstOrDefault(p => p.ProductID == item.ProductID);
                if (product != null)
                    product.Stock -= item.Quantity;
            }
            SaveProductsToJson();

            // 清空購物車
            cart.Clear();
            RefreshCart();
            LoadProducts(cmbCategory.SelectedItem?.ToString());

            MessageBox.Show($"訂單 #{order.OrderID} 完成！\n總金額: NT${order.TotalAmount:N0}", "結帳成功");
        }

        private void ShowMemberManagement()
        {
            MessageBox.Show("會員管理功能開發中...\n目前有 " + members.Count + " 位會員", "會員管理");
            // 可以擴充會員管理表單
        }

        private void SearchMember()
        {
            MessageBox.Show("會員查詢功能開發中...", "會員查詢");
        }

        private void OpenProductManagement()
        {
            var productForm = new ProductManagementForm(products, dataFolderPath, imageFolderPath);
            productForm.ShowDialog();

            // 關閉後重新載入產品列表
            LoadCategories();
            LoadProducts(cmbCategory.SelectedItem?.ToString());
            SaveProductsToJson();
        }

        private void OpenCustomerManagement()
        {

            var customerForm = new CustomerManagementForm(members, orders, dataFolderPath);
            customerForm.ShowDialog();

            // 關閉後重新整理客戶資料
            LoadMembersFromJson();
        }
        private void ShowOrderHistory()
        {
            if (orders.Count == 0)
            {
                MessageBox.Show("尚無訂單記錄", "訂單查詢");
                return;
            }

            // 建立表格格式的字串
            string orderInfo = "╔══════════╦══════════════════╦══════════╦════════════╦════════════════════════════════════════════╗\n";
            orderInfo += "║ 訂單編號  ║    訂單日期       ║  總數量   ║   總金額    ║                 明細                       ║\n";
            orderInfo += "╠══════════╬══════════════════╬══════════╬════════════╬════════════════════════════════════════════╣\n";

            foreach (var order in orders)
            {
                // 訂單日期格式化
                string orderDate = order.OrderDate.ToString("yyyy/MM/dd HH:mm");

                // 總金額格式化
                string totalAmount = $"NT${order.TotalAmount:N0}";

                // 明細格式化（限制長度）
                string details = string.Join(", ", order.Items.Select(i => $"{i.ProductName} x{i.Quantity}"));
                if (details.Length > 50) details = details.Substring(0, 47) + "...";

                // 寫入表格列
                orderInfo += $"║ {order.OrderID,-8} ║ {orderDate,-16} ║ {order.TotalQuantity,-8} ║ {totalAmount,-10} ║ {details,-50} ║\n";
            }

            orderInfo += "╚══════════╩══════════════════╩══════════════╩════════════╩════════════════════════════════════════════╝\n";

            MessageBox.Show(orderInfo, "訂單記錄查詢", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void ShowCustomerOrderHistory()
        {
            // 選擇客戶後顯示訂單記錄
            if (members.Count == 0)
            {
                MessageBox.Show("尚無客戶資料", "提示");
                return;
            }

            // 建立表格格式的字串
            string result = "╔════════╦══════════════════╦════════════╦════════════════════╗\n";
            result += "║ 客戶ID  ║    客戶姓名       ║   總訂單數  ║     總消費金額      ║\n";
            result += "╠════════╬══════════════════╬════════════╬════════════════════╣\n";

            // 只顯示前30筆，避免訊息過長
            int displayCount = Math.Min(members.Count, 30);
            for (int i = 0; i < displayCount; i++)
            {
                var member = members[i];
                var memberOrders = orders.Where(o => o.MemberID == member.MemberID).ToList();
                int orderCount = memberOrders.Count;
                decimal totalAmount = memberOrders.Sum(o => o.TotalAmount);

                result += $"║ {member.MemberID,-6} ║ {member.MemberName,-16} ║ {orderCount,-10} ║ NT${totalAmount,-14:N0} ║\n";
            }

            if (members.Count > 30)
            {
                result += $"║ ... 還有 {members.Count - 30} 筆客戶，請使用客戶管理查詢 ║\n";
            }

            result += "╚════════╩══════════════════╩════════════╩════════════════════╝\n";

            MessageBox.Show(result, "客戶訂購記錄", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GenerateHistoricalOrders()
        {
            Random random = new Random();
            List<Product> availableProducts = products.Where(p => p.Stock > 0).ToList();

            if (availableProducts.Count == 0)
            {
                MessageBox.Show("沒有可用的商品來產生訂單", "提示");
                return;
            }

            int orderId = nextOrderID;
            int ordersGenerated = 0;

            foreach (var member in members)
            {
                // 每個客戶隨機產生 0-5 筆訂單
                int orderCount = random.Next(1, 6);

                for (int i = 0; i < orderCount; i++)
                {
                    // 隨機決定訂單商品數量（1-3項商品）
                    int itemCount = random.Next(1, 4);
                    var cartItems = new List<CartItem>();

                    // 隨機選取商品
                    var selectedProducts = availableProducts.OrderBy(x => random.Next()).Take(itemCount).ToList();

                    foreach (var product in selectedProducts)
                    {
                        int quantity = random.Next(1, 4);
                        cartItems.Add(new CartItem
                        {
                            ProductID = product.ProductID,
                            ProductName = product.ProductName,
                            Price = product.Price,
                            Quantity = quantity
                        });
                    }

                    // 隨機產生過去180天內的日期
                    DateTime orderDate = DateTime.Now.AddDays(-random.Next(0, 180));

                    var order = new Order
                    {
                        OrderID = orderId++,
                        OrderDate = orderDate,
                        MemberID = member.MemberID,
                        Items = cartItems
                    };

                    orders.Add(order);
                    ordersGenerated++;
                }
            }

            nextOrderID = orderId;
            SaveOrdersToJson();

            MessageBox.Show($"已產生 {ordersGenerated} 筆歷史訂單！\n訂單編號範圍：{nextOrderID - ordersGenerated} ~ {nextOrderID - 1}", "完成");
        }

        private void btnUpdateQty_Click(object sender, EventArgs e)
        {
            if (dgvCart.CurrentRow == null) return;

            var item = (CartItem)dgvCart.CurrentRow.DataBoundItem;
            var product = products.FirstOrDefault(p => p.ProductID == item.ProductID);

            string input = ShowInputDialog($"請輸入 {item.ProductName} 的新數量", "更新數量", item.Quantity.ToString());

            if (int.TryParse(input, out int newQty) && newQty > 0)
            {
                if (newQty > product.Stock)
                {
                    MessageBox.Show($"庫存不足！只剩 {product.Stock} 件", "提示");
                    return;
                }
                item.Quantity = newQty;
                RefreshCart();
            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void LoadButtonIcons()
        {
            // 設定按鈕圖示的目標尺寸
            int iconWidth = 24;
            int iconHeight = 24;

            // 加入購物車按鈕圖示
            string addIconPath = Path.Combine(imageFolderPath, "add.png");
            if (File.Exists(addIconPath))
            {
                using (Image original = Image.FromFile(addIconPath))
                {
                    Bitmap resized = new Bitmap(original, new Size(iconWidth, iconHeight));
                    btnAddToCart.Image = resized;
                }
                btnAddToCart.ImageAlign = ContentAlignment.MiddleLeft;
                btnAddToCart.TextImageRelation = TextImageRelation.ImageBeforeText;
            }
        }

        private void picProductImage_Click(object sender, EventArgs e)
        {
            // 檢查是否有選擇商品
            if (dgvProducts.CurrentRow == null)
            {
                MessageBox.Show("請先選擇商品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 取得當前選擇的商品
            int productId = (int)dgvProducts.CurrentRow.Cells["ProductID"].Value;
            var product = products.FirstOrDefault(p => p.ProductID == productId);

            if (product == null)
            {
                MessageBox.Show("無法取得商品資訊", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 取得數量
            int quantity = (int)nudQuantity.Value;

            // 檢查庫存
            if (quantity > product.Stock)
            {
                MessageBox.Show($"庫存不足！只剩 {product.Stock} 件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 檢查購物車是否已有相同商品
            var existing = cart.FirstOrDefault(c => c.ProductID == productId);
            if (existing != null)
            {
                if (existing.Quantity + quantity > product.Stock)
                {
                    MessageBox.Show($"加入後數量 ({existing.Quantity + quantity}) 超過庫存 {product.Stock} 件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    Quantity = quantity
                });
            }

            // 更新購物車顯示
            RefreshCart();

            // 顯示成功訊息
            MessageBox.Show($"已將 {quantity} 件 {product.ProductName} 加入購物車！\n(點擊照片快速加入)", "加入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSortByProductID_Click(object sender, EventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("購物車是空的，無需排序", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 按照產品編號排序
            var sortedCart = cart.OrderBy(c => c.ProductID).ToList();

            // 更新購物車資料來源
            dgvCart.DataSource = null;
            dgvCart.DataSource = sortedCart;

            MessageBox.Show("已按照產品編號排序完成！", "排序完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void ClbPayment_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // 如果要取消勾選
            if (e.NewValue == CheckState.Unchecked)
            {
                return;
            }

            // 如果要勾選，先清除所有其他勾選
            for (int i = 0; i < clbPayment.Items.Count; i++)
            {
                if (i != e.Index)
                {
                    clbPayment.SetItemChecked(i, false);
                }
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text.Trim();

            if (string.IsNullOrEmpty(keyword))
            {
                // 如果關鍵字為空，顯示所有產品
                LoadProducts(cmbCategory.SelectedItem?.ToString());
            }
            else
            {
                // 根據關鍵字篩選產品（產品名稱或分類）
                var filtered = products.Where(p =>
                    p.ProductName.Contains(keyword) ||
                    p.Category.Contains(keyword)).ToList();

                dgvProducts.DataSource = null;
                dgvProducts.DataSource = filtered.Select(p => new
                {
                    p.ProductID,
                    p.ProductName,
                    p.Price,
                    p.Stock,
                    p.Category
                }).ToList();

                if (filtered.Count == 0)
                {
                    MessageBox.Show("找不到符合條件的商品", "查詢結果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        private void LoadSuppliersFromJson()
        {
            string filePath = Path.Combine(dataFolderPath, "Suppliers.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                suppliers = JsonConvert.DeserializeObject<List<Supplier>>(json);
            }
            else
            {
                suppliers = GetDefaultSuppliers();
                SaveSuppliersToJson();
            }
        }

        private List<Supplier> GetDefaultSuppliers()
        {
            return new List<Supplier>
    {
        // === 有進貨記錄的供應商 (ID 1-10) ===
        new Supplier { SupplierID = 1, SupplierName = "美妝國際有限公司", ContactPerson = "周世明", Phone = "02-12345678", Email = "contact@beauty.com", Address = "台北市信義區信義路五段100號", TaxNumber = "12345678", BankAccount = "123-456-789", Remark = "有進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 2, SupplierName = "天然本草企業社", ContactPerson = "葉容真", Phone = "03-87654321", Email = "sales@herbal.com", Address = "桃園市桃園區中正路200號", TaxNumber = "87654321", BankAccount = "987-654-321", Remark = "有進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 3, SupplierName = "歐洲精品貿易公司", ContactPerson = "林雅婷", Phone = "04-11223344", Email = "order@europe.com", Address = "台中市西屯區台灣大道三段300號", TaxNumber = "11223344", BankAccount = "111-222-333", Remark = "有進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 4, SupplierName = "日本美妝直輸入", ContactPerson = "盧厭煩", Phone = "07-55667788", Email = "japan@beauty.com", Address = "高雄市左營區博愛二路400號", TaxNumber = "55667788", BankAccount = "444-555-666", Remark = "有進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 5, SupplierName = "韓流美妝企業", ContactPerson = "李惠宣", Phone = "02-99887766", Email = "korea@kbeauty.com", Address = "新北市板橋區文化路一段150號", TaxNumber = "99887766", BankAccount = "555-666-777", Remark = "有進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 6, SupplierName = "有機生活用品有限公司", ContactPerson = "林子厭", Phone = "03-33445566", Email = "organic@green.com", Address = "新竹市東區光復路二段80號", TaxNumber = "33445566", BankAccount = "666-777-888", Remark = "有進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 7, SupplierName = "東南亞香料王國", ContactPerson = "陳又中", Phone = "04-22334455", Email = "spice@asia.com", Address = "彰化縣彰化市中山路二段50號", TaxNumber = "22334455", BankAccount = "777-888-999", Remark = "有進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 8, SupplierName = "美國直送貿易商", ContactPerson = "蔡要興", Phone = "02-44556677", Email = "usa@import.com", Address = "台北市大安區忠孝東路四段200號", TaxNumber = "44556677", BankAccount = "888-999-000", Remark = "有進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 9, SupplierName = "法國香氛工坊", ContactPerson = "林喜聰", Phone = "07-66778899", Email = "france@perfume.com", Address = "高雄市鼓山區美術東二路300號", TaxNumber = "66778899", BankAccount = "999-000-111", Remark = "有進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 10, SupplierName = "義大利精品皮件", ContactPerson = "杜紅中", Phone = "04-77889900", Email = "italy@luxury.com", Address = "台中市西區公益路250號", TaxNumber = "77889900", BankAccount = "000-111-222", Remark = "有進貨記錄", CreateDate = DateTime.Now },

        // === 無進貨記錄的供應商 (ID 11-16) ===
        new Supplier { SupplierID = 11, SupplierName = "泰國天然保養品", ContactPerson = "林丞宏", Phone = "02-88990011", Email = "thai@herbal.com", Address = "台北市中山區南京東路三段120號", TaxNumber = "88990011", BankAccount = "111-222-333", Remark = "無進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 12, SupplierName = "澳洲有機農場", ContactPerson = "黃伯凱", Phone = "03-99001122", Email = "australia@organic.com", Address = "桃園市蘆竹區南崁路一段88號", TaxNumber = "99001122", BankAccount = "222-333-444", Remark = "無進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 13, SupplierName = "德國科技化工", ContactPerson = "莊喬雲", Phone = "04-00112233", Email = "germany@chemical.com", Address = "台中市北區中清路一段180號", TaxNumber = "00112233", BankAccount = "333-444-555", Remark = "無進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 14, SupplierName = "瑞士生物科技", ContactPerson = "陳家盛", Phone = "02-22334455", Email = "swiss@biotech.com", Address = "台北市內湖區瑞光路500號", TaxNumber = "22334455", BankAccount = "555-666-777", Remark = "無進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 15, SupplierName = "加拿大冰河礦泉水", ContactPerson = "黃唯義", Phone = "03-33445566", Email = "canada@water.com", Address = "新竹縣竹北市光明六路200號", TaxNumber = "33445566", BankAccount = "666-777-888", Remark = "無進貨記錄", CreateDate = DateTime.Now },
        new Supplier { SupplierID = 16, SupplierName = "以色列死海保養品", ContactPerson = "林基俊", Phone = "02-66778899", Email = "israel@deadsea.com", Address = "台北市松山區敦化北路300號", TaxNumber = "66778899", BankAccount = "999-000-111", Remark = "無進貨記錄", CreateDate = DateTime.Now },
    };
        }

        private void SaveSuppliersToJson()
        {
            string filePath = Path.Combine(dataFolderPath, "Suppliers.json");
            string json = JsonConvert.SerializeObject(suppliers, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        private void OpenSupplierManagement()
        {
            var supplierForm = new SupplierManagementForm(suppliers, purchaseOrders, dataFolderPath);
            supplierForm.ShowDialog();
        }
        // 採購主檔類別
        public class PurchaseOrder
        {
            public int PurchaseID { get; set; }      // 採購單號
            public DateTime PurchaseDate { get; set; } // 採購日期
            public int SupplierID { get; set; }       // 供應商ID
            public string SupplierName { get; set; }  // 供應商名稱
            public int TotalQuantity { get; set; }    // 總數量
            public decimal TotalAmount { get; set; }  // 總金額
            public string Status { get; set; }        // 狀態(已進貨/未進貨)
        }

        // 採購明細檔類別
        public class PurchaseDetail
        {
            public int DetailID { get; set; }
            public int PurchaseID { get; set; }
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal SubTotal => Quantity * UnitPrice;
        }
  

        private void GeneratePurchaseRecords()
        {
            // 如果已經有採購記錄，不重複產生
            if (purchaseOrders.Count > 0) return;

            Random random = new Random();
            int purchaseId = 1;

            // 只為前 10 個供應商產生進貨記錄
            var suppliersWithPurchase = suppliers.Take(10).ToList();

            foreach (var supplier in suppliersWithPurchase)
            {
                // 每個供應商產生 2-5 筆採購單
                int orderCount = random.Next(2, 6);

                for (int i = 0; i < orderCount; i++)
                {
                    // 隨機選取 1-4 項商品
                    int itemCount = random.Next(1, 5);
                    var selectedProducts = products.OrderBy(x => random.Next()).Take(itemCount).ToList();

                    var items = new List<PurchaseDetail>();
                    int totalQty = 0;
                    decimal totalAmt = 0;

                    foreach (var product in selectedProducts)
                    {
                        int qty = random.Next(10, 101); // 進貨數量 10-100
                        decimal price = product.Price * (decimal)(0.7 + random.NextDouble() * 0.3); // 成本價 7-10折

                        items.Add(new PurchaseDetail
                        {
                            DetailID = purchaseDetails.Count + items.Count + 1,
                            PurchaseID = purchaseId,
                            ProductID = product.ProductID,
                            ProductName = product.ProductName,
                            Quantity = qty,
                            UnitPrice = Math.Round(price, 0)
                        });

                        totalQty += qty;
                        totalAmt += qty * price;
                    }

                    purchaseOrders.Add(new PurchaseOrder
                    {
                        PurchaseID = purchaseId,
                        PurchaseDate = DateTime.Now.AddDays(-random.Next(0, 365)),
                        SupplierID = supplier.SupplierID,
                        SupplierName = supplier.SupplierName,
                        TotalQuantity = totalQty,
                        TotalAmount = Math.Round(totalAmt, 0),
                        Status = "已進貨"
                    });

                    purchaseDetails.AddRange(items);
                    purchaseId++;
                }
            }

            SavePurchaseDataToJson();
        }

        private void SavePurchaseDataToJson()
        {
            string purchaseOrderPath = Path.Combine(dataFolderPath, "PurchaseOrders.json");
            string purchaseDetailPath = Path.Combine(dataFolderPath, "PurchaseDetails.json");

            string orderJson = JsonConvert.SerializeObject(purchaseOrders, Formatting.Indented);
            string detailJson = JsonConvert.SerializeObject(purchaseDetails, Formatting.Indented);

            File.WriteAllText(purchaseOrderPath, orderJson);
            File.WriteAllText(purchaseDetailPath, detailJson);
        }

        private void OpenPurchaseOrderForm()
        {
            var purchaseForm = new PurchaseOrderForm(suppliers, products, purchaseOrders, purchaseDetails, dataFolderPath);
            purchaseForm.ShowDialog();

            // 關閉後重新整理資料
            LoadProductsFromJson();
            LoadPurchaseDataFromJson();
        }

        private void ShowPurchaseHistory()
        {
            if (purchaseOrders.Count == 0)
            {
                MessageBox.Show("尚無採購記錄", "採購記錄查詢");
                return;
            }

            string result = "╔══════════╦══════════════════╦════════════════╦════════════╦══════════╗\n";
            result += "║ 採購單號  ║    採購日期       ║    供應商       ║   總數量    ║   總金額   ║\n";
            result += "╠══════════╬══════════════════╬════════════════╬════════════╬══════════╣\n";

            foreach (var order in purchaseOrders)
            {
                result += $"║ {order.PurchaseID,-8} ║ {order.PurchaseDate.ToString("yyyy/MM/dd"),-16} ║ {order.SupplierName,-14} ║ {order.TotalQuantity,-10} ║ NT${order.TotalAmount,-8:N0} ║\n";
            }
            result += "╚══════════╩══════════════════╩════════════════╩════════════╩══════════╝\n";

            MessageBox.Show(result, "採購記錄查詢", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadPurchaseDataFromJson()
        {
            string purchaseOrderPath = Path.Combine(dataFolderPath, "PurchaseOrders.json");
            string purchaseDetailPath = Path.Combine(dataFolderPath, "PurchaseDetails.json");

            if (File.Exists(purchaseOrderPath))
            {
                string json = File.ReadAllText(purchaseOrderPath);
                purchaseOrders = JsonConvert.DeserializeObject<List<PurchaseOrder>>(json);
            }

            if (File.Exists(purchaseDetailPath))
            {
                string json = File.ReadAllText(purchaseDetailPath);
                purchaseDetails = JsonConvert.DeserializeObject<List<PurchaseDetail>>(json);
            }
        }

    }
}


 
            
        


    
    

