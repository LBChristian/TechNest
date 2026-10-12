using TechNest.WinForms.Models;

namespace TechNest.WinForms.Forms;

public class ProductForm : Form
{
    private readonly TextBox _name = new();
    private readonly TextBox _code = new();
    private readonly TextBox _brand = new();
    private readonly TextBox _category = new();
    private readonly NumericUpDown _price = new() { DecimalPlaces = 2, Maximum = 100000000, Minimum = 0, ThousandsSeparator = true };
    private readonly NumericUpDown _quantity = new() { Maximum = 1000000, Minimum = 0 };
    private readonly NumericUpDown _reorder = new() { Maximum = 1000000, Minimum = 0, Value = 5 };

    public Product ProductValue { get; private set; } = new();

    public ProductForm(Product? existing = null)
    {
        Text = existing == null ? "Add Product" : "Edit Product";
        Width = 430; Height = 390; StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        Font = new Font("Segoe UI", 9F); BackColor = Color.White;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2, RowCount = 8 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(layout, 0, "Name", _name); AddRow(layout, 1, "Code", _code); AddRow(layout, 2, "Brand", _brand);
        AddRow(layout, 3, "Category", _category); AddRow(layout, 4, "Unit Price", _price); AddRow(layout, 5, "Quantity", _quantity); AddRow(layout, 6, "Reorder Level", _reorder);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        var save = new Button { Text = "Save", Width = 85, DialogResult = DialogResult.None };
        var cancel = new Button { Text = "Cancel", Width = 85, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => SaveProduct(); buttons.Controls.Add(save); buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, 7); layout.SetColumnSpan(buttons, 2); Controls.Add(layout);
        AcceptButton = save; CancelButton = cancel;
        if (existing != null)
        {
            _name.Text = existing.Name; _code.Text = existing.Code; _brand.Text = existing.Brand; _category.Text = existing.Category;
            _price.Value = Math.Min(_price.Maximum, existing.UnitPrice); _quantity.Value = existing.Quantity; _reorder.Value = existing.ReorderLevel;
        }
    }
    private static void AddRow(TableLayoutPanel layout, int row, string label, Control input)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 0, 0) }, 0, row);
        input.Dock = DockStyle.Fill; input.Margin = new Padding(0, 3, 0, 3); layout.Controls.Add(input, 1, row);
    }
    private void SaveProduct()
    {
        if (string.IsNullOrWhiteSpace(_name.Text) || string.IsNullOrWhiteSpace(_code.Text) ||
            string.IsNullOrWhiteSpace(_brand.Text) || string.IsNullOrWhiteSpace(_category.Text))
        { MessageBox.Show("Complete all required fields.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        ProductValue = new Product { Name = _name.Text.Trim(), Code = _code.Text.Trim(), Brand = _brand.Text.Trim(), Category = _category.Text.Trim(), UnitPrice = _price.Value, Quantity = (int)_quantity.Value, ReorderLevel = (int)_reorder.Value };
        DialogResult = DialogResult.OK; Close();
    }
}
