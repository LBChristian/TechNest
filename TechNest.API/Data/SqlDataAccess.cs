using Microsoft.Data.SqlClient;
using System.Data;

namespace TechNest.API.Data;

public class SqlDataAccess
{
    private readonly string _connectionString;
    public SqlDataAccess(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TechNest")
            ?? throw new InvalidOperationException("Missing TechNest connection string.");
    }
    public SqlConnection CreateConnection() => new(_connectionString);

    public static void AddParameter(SqlCommand command, string name, object value)
        => command.Parameters.AddWithValue(name, value ?? DBNull.Value);
}
