using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TechNest.API.Data;
using TechNest.API.Models;

namespace TechNest.API.Controllers;

[ApiController]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly SqlDataAccess _db;
    public SalesController(SqlDataAccess db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SaleRecord>>> GetAll()
    {
        var sales = new List<SaleRecord>();
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT SaleId, SaleDate, TotalAmount FROM dbo.Sales ORDER BY SaleDate DESC;";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) sales.Add(new SaleRecord
        {
            SaleId = reader.GetInt32(0), SaleDate = reader.GetDateTime(1), TotalAmount = reader.GetDecimal(2)
        });
        return Ok(sales);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSaleRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { message = "Add at least one item to the sale." });
        if (request.Items.Any(x => x.ProductId <= 0 || x.Quantity <= 0))
            return BadRequest(new { message = "Each sale item needs a valid product and positive quantity." });

        // Combine repeated product rows before checking and deducting stock.
        var lines = request.Items.GroupBy(x => x.ProductId)
            .Select(g => new CreateSaleLine { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) }).ToList();

        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        try
        {
            decimal total = 0;
            var resolved = new List<(int ProductId, string Name, decimal Price, int Quantity)>();
            foreach (var line in lines)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "SELECT Name, UnitPrice, Quantity FROM dbo.Products WITH (UPDLOCK, ROWLOCK) WHERE ProductId=@id;";
                command.Parameters.AddWithValue("@id", line.ProductId);
                await using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) throw new InvalidOperationException($"Product ID {line.ProductId} was not found.");
                var name = reader.GetString(0); var price = reader.GetDecimal(1); var stock = reader.GetInt32(2);
                await reader.CloseAsync();
                if (stock < line.Quantity) throw new InvalidOperationException($"Insufficient stock for {name}. Available: {stock}.");
                total += price * line.Quantity;
                resolved.Add((line.ProductId, name, price, line.Quantity));
            }

            int saleId;
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO dbo.Sales(TotalAmount) OUTPUT INSERTED.SaleId VALUES(@total);";
                command.Parameters.AddWithValue("@total", total);
                saleId = Convert.ToInt32(await command.ExecuteScalarAsync());
            }
            foreach (var line in resolved)
            {
                await using var update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = "UPDATE dbo.Products SET Quantity=Quantity-@qty WHERE ProductId=@id AND Quantity>=@qty;";
                update.Parameters.AddWithValue("@qty", line.Quantity);
                update.Parameters.AddWithValue("@id", line.ProductId);
                if (await update.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Stock changed before the sale could be completed.");

                await using var detail = connection.CreateCommand();
                detail.Transaction = transaction;
                detail.CommandText = @"INSERT INTO dbo.SaleDetails(SaleId, ProductId, ProductName, Quantity, UnitPrice)
                    VALUES(@sale,@product,@name,@qty,@price);";
                detail.Parameters.AddWithValue("@sale", saleId); detail.Parameters.AddWithValue("@product", line.ProductId);
                detail.Parameters.AddWithValue("@name", line.Name); detail.Parameters.AddWithValue("@qty", line.Quantity);
                detail.Parameters.AddWithValue("@price", line.Price);
                await detail.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
            return CreatedAtAction(nameof(GetAll), new { id = saleId }, new { saleId, totalAmount = total, message = "Sale recorded." });
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { message = ex.Message });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
