# Running Junimo v3 on macOS

This project was developed on Windows using SQL Server LocalDB, which is not available on macOS.
To run this project on macOS, you need running a SQL Server via Docker.

## Prerequisites
1.  [Docker Desktop for Mac](https://www.docker.com/products/docker-desktop/) installed and running.
2.  [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed.

## Step 1: Start the Database
From the project root (where `docker-compose.yml` is), run:
```bash
docker-compose up -d
```
This starts a SQL Server container listening on port 1433.

## Step 2: Configure Connection String
Copy the provided `appsettings.Mac.json` into `appsettings.Development.json` (or just use its contents):

```bash
cp appsettings.Mac.json appsettings.Development.json
```

Or manually update `appsettings.Development.json` to:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=junimo;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```
*Note: The password must match the `SA_PASSWORD` in `docker-compose.yml`.*

## Step 3: Apply Migrations
Apply the database schema to the new container:
```bash
dotnet tool install --global dotnet-ef
dotnet ef database update
```

## Step 4: Run the Application
```bash
dotnet run
```
The application should now be accessible at https://localhost:7196 (or the port specified in launchSettings.json).

## Database Data
By default, the database will be empty (except for Roles seeded in Program.cs).
If you need specific data from your Windows machine:
1.  Export your data as a SQL script from SSMS on Windows.
2.  Run that script against the Docker database on Mac (using Azure Data Studio or `sqlcmd`).
