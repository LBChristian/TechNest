using TechNest.WinForms.Models;
using TechNest.WinForms.Services;

namespace TechNest.WinForms.Forms;

public class MainForm : Form
{
    private readonly ApiClient _api = new();
    private readonly DataGridView _productsGrid = Grid();
    private readonly DataGridView _salesGrid = Grid();
    private readonly TextBox _search = new();
    private readonly Label _status = new() { AutoSize = true, Text = "Ready." };
    private readonly Label _summary = new() { AutoSize = true, Text = "Summary not loaded." };

    public MainForm()
    {
        Text = "TechNest Inventory Management";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1120; Height = 720; MinimumSize = new Size(900, 600);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var title = new Label { Text = "TechNest Inventory Management", AutoSize = true, Font = new Font("Segoe UI", 15F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 10) };
        root.Controls.Add(title, 0, 0);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildProductsTab());
        tabs.TabPages.Add(BuildSalesTab());
        tabs.TabPages.Add(BuildReportsTab());
        tabs.SelectedIndexChanged += async (_, _) => await RefreshCurrentTab(tabs.SelectedIndex);
        root.Controls.Add(tabs, 0, 1);
        var statusPanel = new Panel { Dock = DockStyle.Fill, Height = 28, Padding = new Padding(0, 7, 0, 0) };
        statusPanel.Controls.Add(_status);
        root.Controls.Add(statusPanel, 0, 2);
        Controls.Add(root);
        Shown += async (_, _) => await LoadProducts();
    }

    private TabPage BuildProductsTab()
    {
        var page = new TabPage("Products") { BackColor = Color.White, Padding = new Padding(8) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var tools = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(0, 0, 0, 8) };
        tools.Controls.Add(new Label { Text = "Search", AutoSize = true, Margin = new Padding(0, 8, 6, 0) });
        _search.Width = 230; _search.Margin = new Padding(0, 2, 8, 0); tools.Controls.Add(_search);
        tools.Controls.Add(Button("Search", async () => await LoadProducts()));
        tools.Controls.Add(Button("Show All", async () => { _search.Clear(); await LoadProducts(); }));
        tools.Controls.Add(Button("Add Product", async () => await AddProduct()));
        tools.Controls.Add(Button("Edit Selected", async () => await EditProduct()));
        tools.Controls.Add(Button("Delete Selected", async () => await DeleteProduct()));
        tools.Controls.Add(Button("Add Stock", async () => await AdjustStock(1)));
        tools.Controls.Add(Button("Remove Stock", async () => await AdjustStock(-1)));
        tools.Controls.Add(Button("Record Sale", async () => await RecordSale()));
        layout.Controls.Add(tools, 0, 0);
        ConfigureProductGrid();
        layout.Controls.Add(_productsGrid, 0, 1);
        layout.Controls.Add(new Label { Text = "Select a product row before using Edit, Delete, Stock, or Record Sale.", AutoSize = true, Margin = new Padding(0, 8, 0, 0) }, 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildSalesTab()
    {
        var page = new TabPage("Sales History") { BackColor = Color.White, Padding = new Padding(8) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var tools = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 0, 0, 8) };
        tools.Controls.Add(Button("Refresh Sales", async () => await LoadSales()));
        layout.Controls.Add(tools, 0, 0);
        _salesGrid.AutoGenerateColumns = true;
        layout.Controls.Add(_salesGrid, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildReportsTab()
    {
        var page = new TabPage("Reports") { BackColor = Color.White, Padding = new Padding(12) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 3 };
        layout.Controls.Add(new Label { Text = "Inventory Summary", AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 12) }, 0, 0);
        layout.Controls.Add(_summary, 0, 1);
        layout.Controls.Add(Button("Refresh Summary", async () => await LoadSummary()), 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    private void ConfigureProductGrid()
    {
        _productsGrid.AutoGenerateColumns = false;
        _productsGrid.Columns.Clear();
        AddColumn("ProductId", "ID", 55);
        AddColumn("Name", "Name", 180);
        AddColumn("Code", "Code", 100);
        AddColumn("Brand", "Brand", 120);
        AddColumn("Category", "Category", 130);
        AddColumn("UnitPrice", "Unit Price", 100, "N2");
        AddColumn("Quantity", "Quantity", 80);
        AddColumn("ReorderLevel", "Reorder Level", 100);
        _productsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _productsGrid.MultiSelect = false;
    }
    private void AddColumn(string property, string header, int width, string? format = null)
    {
        var column = new DataGridViewTextBoxColumn { DataPropertyName = property, HeaderText = header, Width = width, ReadOnly = true };
        if (format != null) column.DefaultCellStyle.Format = format;
        _productsGrid.Columns.Add(column);
    }

    private static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false
    };
    private static Button Button(string text, Func<Task> action)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 30, FlatStyle = FlatStyle.Standard, Margin = new Padding(0, 0, 6, 4), Padding = new Padding(8, 2, 8, 2) };
        button.Click += async (_, _) => { button.Enabled = false; try { await action(); } catch (Exception ex) { MessageBox.Show(ex.Message, "TechNest", MessageBoxButtons.OK, MessageBoxIcon.Warning); } finally { button.Enabled = true; } };
        return button;
    }

    private async Task RefreshCurrentTab(int index)
    {
        if (index == 0) await LoadProducts(); else if (index == 1) await LoadSales(); else await LoadSummary();
    }
    private async Task LoadProducts()
    {
        var products = await _api.GetProductsAsync(_search.Text.Trim());
        _productsGrid.DataSource = products;
        _status.Text = $"Loaded {products.Count} product(s).";
    }
    private async Task LoadSales()
    {
        _salesGrid.DataSource = await _api.GetSalesAsync();
        _status.Text = "Sales history refreshed.";
    }
    private async Task LoadSummary()
    {
        var report = await _api.GetSummaryAsync();
        _summary.Text = $"Total products: {report.ProductCount}\nProducts at or below reorder level: {report.LowStockCount}\nTotal recorded sales: PHP {report.SalesTotal:N2}";
        _status.Text = "Summary refreshed.";
    }

    private Product? SelectedProduct() => _productsGrid.CurrentRow?.DataBoundItem as Product;
    private async Task AddProduct()
    {
        using var form = new ProductForm();
        if (form.ShowDialog(this) != DialogResult.OK) return;
        await _api.CreateProductAsync(form.ProductValue);
        await LoadProducts();
        _status.Text = "Product added.";
    }
    private async Task EditProduct()
    {
        var selected = SelectedProduct();
        if (selected == null) { MessageBox.Show("Select a product first."); return; }
        using var form = new ProductForm(selected);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        form.ProductValue.ProductId = selected.ProductId;
        await _api.UpdateProductAsync(form.ProductValue);
        await LoadProducts();
        _status.Text = "Product updated.";
    }
    private async Task DeleteProduct()
    {
        var selected = SelectedProduct();
        if (selected == null) { MessageBox.Show("Select a product first."); return; }
        if (MessageBox.Show($"Delete {selected.Name}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        await _api.DeleteProductAsync(selected.ProductId);
        await LoadProducts();
        _status.Text = "Product deleted.";
    }
    private async Task AdjustStock(int direction)
    {
        var selected = SelectedProduct();
        if (selected == null) { MessageBox.Show("Select a product first."); return; }
        var label = direction > 0 ? "Quantity to add" : "Quantity to remove";
        var quantity = AskForInteger(label, 1);
        if (quantity == null) return;
        await _api.AdjustStockAsync(selected.ProductId, direction * quantity.Value);
        await LoadProducts();
        _status.Text = "Stock updated.";
    }
    private async Task RecordSale()
    {
        var selected = SelectedProduct();
        if (selected == null) { MessageBox.Show("Select a product first."); return; }
        var quantity = AskForInteger($"Quantity to sell (available: {selected.Quantity})", 1);
        if (quantity == null) return;
        await _api.RecordSaleAsync(selected.ProductId, quantity.Value);
        await LoadProducts();
        _status.Text = "Sale recorded and stock deducted.";
    }
    private static int? AskForInteger(string prompt, int initial)
    {
        using var form = new Form { Text = prompt, Width = 340, Height = 150, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var label = new Label { Text = prompt, Left = 12, Top = 12, AutoSize = true };
        var input = new NumericUpDown { Left = 12, Top = 38, Width = 290, Minimum = 1, Maximum = 100000, Value = initial };
        var ok = new Button { Text = "OK", Left = 146, Top = 76, Width = 75, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Left = 227, Top = 76, Width = 75, DialogResult = DialogResult.Cancel };
        form.Controls.AddRange(new Control[] { label, input, ok, cancel }); form.AcceptButton = ok; form.CancelButton = cancel;
        return form.ShowDialog() == DialogResult.OK ? (int)input.Value : null;
    }
}
