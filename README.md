# TechNest Inventory Management (Module 2)

TechNest is a Windows Forms inventory application for an electronics shop. The desktop client communicates with an ASP.NET Core REST API using HTTP. The API is responsible for database access; the Windows Forms client does not connect directly to SQL Server.

## Features
- Product CRUD and search
- Stock adjustments and low-stock handling
- Sales recording, automatic stock deduction, and sales history
- Basic inventory and sales reports
- SQL Server persistence

## Requirements
- Windows and .NET 6 SDK
- SQL Server Express or LocalDB

## Database setup
1. Connect to SQL Server using SSMS.
2. Execute `Database/DatabaseSetup.sql`.
3. Confirm `TechNestDb` contains `Products`, `Sales`, and `SaleDetails`.

## Configure the API
Edit `TechNest.API/appsettings.json` and set `ConnectionStrings:TechNest` for your SQL Server instance. The current development setting points to `CLB\\SQLEXPRESS`; change this if your instance name differs.

## Run the API
From `TechNest.API`:

```powershell
dotnet restore
dotnet run
```

Default base URL: `http://localhost:5261`. Swagger is available at `/swagger` in Development.

## Run the Windows Forms client
From `TechNest.WinForms`, in a second terminal:

```powershell
dotnet restore
dotnet run
```

The desktop client calls `http://localhost:5261/api/` by default. Start the API before launching the client.

## Structure
- `TechNest.API/` — REST endpoints, SQL Server data access, and API models
- `TechNest.WinForms/` — desktop UI, client models, and HTTP API client
- `Database/DatabaseSetup.sql` — database and table setup with sample products
- `global.json` — .NET SDK version selection

Build and run the project on the target Windows/.NET environment before submission.