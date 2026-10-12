using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TechNest.API.Data;
using TechNest.API.Models;

namespace TechNest.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly SqlDataAccess _db;
    public ProductsController(SqlDataAccess db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll([FromQuery] string? search)
    {
        var results = new List<Product>();
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT ProductId, Name, Code, Brand, Category, UnitPrice, Quantity, ReorderLevel, CreatedAt
            FROM dbo.Products
            WHERE (@search IS NULL OR Name LIKE @pattern OR Code LIKE @pattern OR Brand LIKE @pattern OR Category LIKE @pattern)
            ORDER BY Name;";
        SqlDataAccess.AddParameter(command, "@search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : search);
        SqlDataAccess.AddParameter(command, "@pattern", "%" + (search ?? "") + "%");
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) results.Add(ReadProduct(reader));
        return Ok(results);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT ProductId, Name, Code, Brand, Category, UnitPrice, Quantity, ReorderLevel, CreatedAt
            FROM dbo.Products WHERE ProductId=@id;";
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return NotFound(new { message = "Product not found." });
        return Ok(ReadProduct(reader));
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create(Product product)
    {
        var error = Validate(product);
        if (error != null) return BadRequest(new { message = error });
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO dbo.Products(Name, Code, Brand, Category, UnitPrice, Quantity, ReorderLevel)
            OUTPUT INSERTED.ProductId, INSERTED.Name, INSERTED.Code, INSERTED.Brand, INSERTED.Category,
                   INSERTED.UnitPrice, INSERTED.Quantity, INSERTED.ReorderLevel, INSERTED.CreatedAt
            VALUES(@name,@code,@brand,@category,@price,@quantity,@reorder);";
        AddProductParameters(command, product);
        try
        {
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var created = ReadProduct(reader);
            return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, created);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return Conflict(new { message = "A product with this code already exists." });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Product product)
    {
        var error = Validate(product);
        if (error != null) return BadRequest(new { message = error });
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"UPDATE dbo.Products SET Name=@name, Code=@code, Brand=@brand, Category=@category,
            UnitPrice=@price, Quantity=@quantity, ReorderLevel=@reorder WHERE ProductId=@id;";
        AddProductParameters(command, product);
        command.Parameters.AddWithValue("@id", id);
        try
        {
            if (await command.ExecuteNonQueryAsync() == 0) return NotFound(new { message = "Product not found." });
            return NoContent();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return Conflict(new { message = "A product with this code already exists." });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.Products WHERE ProductId=@id;";
        command.Parameters.AddWithValue("@id", id);
        try
        {
            if (await command.ExecuteNonQueryAsync() == 0) return NotFound(new { message = "Product not found." });
            return NoContent();
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            return Conflict(new { message = "This product has sales history and cannot be deleted." });
        }
    }

    [HttpPost("{id:int}/stock-adjustment")]
    public async Task<IActionResult> AdjustStock(int id, StockAdjustment adjustment)
    {
        if (adjustment.Change == 0) return BadRequest(new { message = "Stock change cannot be zero." });
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"UPDATE dbo.Products SET Quantity=Quantity+@change
            WHERE ProductId=@id AND Quantity+@change >= 0;";
        command.Parameters.AddWithValue("@change", adjustment.Change);
        command.Parameters.AddWithValue("@id", id);
        if (await command.ExecuteNonQueryAsync() == 0)
        {
            await using var check = connection.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM dbo.Products WHERE ProductId=@id;";
            check.Parameters.AddWithValue("@id", id);
            var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;
            return exists ? BadRequest(new { message = "Stock cannot become negative." }) : NotFound(new { message = "Product not found." });
        }
        return NoContent();
    }

    private static string? Validate(Product p)
    {
        if (string.IsNullOrWhiteSpace(p.Name) || string.IsNullOrWhiteSpace(p.Code) ||
            string.IsNullOrWhiteSpace(p.Brand) || string.IsNullOrWhiteSpace(p.Category))
            return "Name, code, brand, and category are required.";
        if (p.UnitPrice < 0) return "Unit price cannot be negative.";
        if (p.Quantity < 0 || p.ReorderLevel < 0) return "Quantity and reorder level cannot be negative.";
        return null;
    }

    private static void AddProductParameters(SqlCommand c, Product p)
    {
        c.Parameters.AddWithValue("@name", p.Name.Trim());
        c.Parameters.AddWithValue("@code", p.Code.Trim());
        c.Parameters.AddWithValue("@brand", p.Brand.Trim());
        c.Parameters.AddWithValue("@category", p.Category.Trim());
        c.Parameters.AddWithValue("@price", p.UnitPrice);
        c.Parameters.AddWithValue("@quantity", p.Quantity);
        c.Parameters.AddWithValue("@reorder", p.ReorderLevel);
    }

    private static Product ReadProduct(SqlDataReader r) => new()
    {
        ProductId = r.GetInt32(0), Name = r.GetString(1), Code = r.GetString(2), Brand = r.GetString(3),
        Category = r.GetString(4), UnitPrice = r.GetDecimal(5), Quantity = r.GetInt32(6),
        ReorderLevel = r.GetInt32(7), CreatedAt = r.GetDateTime(8)
    };
}
