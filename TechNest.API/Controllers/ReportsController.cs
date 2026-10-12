using Microsoft.AspNetCore.Mvc;
using TechNest.API.Data;
using TechNest.API.Models;

namespace TechNest.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly SqlDataAccess _db;
    public ReportsController(SqlDataAccess db) => _db = db;

    [HttpGet("summary")]
    public async Task<ActionResult<ReportSummary>> Summary()
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT
            (SELECT COUNT(*) FROM dbo.Products) AS ProductCount,
            (SELECT COUNT(*) FROM dbo.Products WHERE Quantity <= ReorderLevel) AS LowStockCount,
            (SELECT COALESCE(SUM(TotalAmount), 0) FROM dbo.Sales) AS SalesTotal;";
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return Ok(new ReportSummary
        {
            ProductCount = reader.GetInt32(0), LowStockCount = reader.GetInt32(1), SalesTotal = reader.GetDecimal(2)
        });
    }
}
