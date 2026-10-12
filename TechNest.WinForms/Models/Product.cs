namespace TechNest.WinForms.Models;

public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
    public DateTime? CreatedAt { get; set; }
}
public class SaleRecord
{
    public int SaleId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
}
public class ReportSummary
{
    public int ProductCount { get; set; }
    public int LowStockCount { get; set; }
    public decimal SalesTotal { get; set; }
}
