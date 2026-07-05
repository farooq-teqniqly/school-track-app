# Trakmark

## Developer Setup

All commands run from the **repo root** unless noted otherwise.

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://docs.docker.com/get-docker/) or [Podman](https://podman.io/getting-started/installation)
- A Google account with access to [Google Developer Console](https://console.developers.google.com/)

---

### 1. SQL Server via Docker

Create a `.env` file in the repo root (gitignored — never commit it):

```
MSSQL_SA_PASSWORD=<your-strong-password>
```

Start SQL Server:

```bash
# Docker
docker compose up -d

# Podman
podman compose up -d
```

This runs SQL Server 2025 on `127.0.0.1,1433` with a persistent volume (`sqlserver_data`).

---

### 2. User Secrets

All secrets go in user secrets — never in `appsettings.json`.

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1,1433;Database=Trakmark;User Id=sa;Password=<your-password>;TrustServerCertificate=True;" --project Trakmark
dotnet user-secrets set "Authentication:Google:ClientId" "<your-client-id>" --project Trakmark
dotnet user-secrets set "Authentication:Google:ClientSecret" "<your-client-secret>" --project Trakmark
```

---

### 3. Google OAuth

1. Open [Google Developer Console](https://console.developers.google.com/) and create a project.
2. Go to **APIs & Services → Credentials → Create Credentials → OAuth 2.0 Client ID**.
3. Application type: **Web application**.
4. Add an authorized redirect URI: `https://localhost:7214/signin-google`
5. Copy the **Client ID** and **Client Secret** into user secrets (step 2).

---

### 4. Database Migration

Restore the local `dotnet-ef` tool from the manifest:

```powershell
dotnet tool restore
```

Set the design-time connection string environment variable, then run migrations:

```powershell
$env:TRAKMARK_DESIGN_TIME_CONNSTR = "Server=127.0.0.1,1433;Database=Trakmark;User Id=sa;Password=<your-password>;TrustServerCertificate=True;"
dotnet ef database update --project Trakmark --startup-project Trakmark
```

To scaffold a new migration:

```powershell
dotnet ef migrations add <MigrationName> --project Trakmark --startup-project Trakmark
```

---

### 5. Run the App

```powershell
dotnet run --project Trakmark --launch-profile https
```

App is available at `https://localhost:7214`.

---

### 6. Grant Admin Role

After logging in for the first time, assign your account the Admin role directly in the database:

```sql
DECLARE @UserId  NVARCHAR(450) = (SELECT Id FROM AspNetUsers WHERE Email = '<your-email>');
DECLARE @RoleId  NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name  = 'Admin');

IF @UserId IS NULL  THROW 50000, 'User not found — check the email address.', 1;
IF @RoleId IS NULL  THROW 50001, 'Admin role not found — ensure the app has been run at least once to seed roles.', 1;

IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
    UPDATE AspNetUserRoles SET RoleId = @RoleId WHERE UserId = @UserId;
```

Run this against the `Trakmark` database on `127.0.0.1,1433` using any SQL client (SSMS, Azure Data Studio, etc.).

---

### 7. OpenTelemetry — Aspire Dashboard (optional)

Run the Aspire standalone dashboard to view traces and logs locally:

```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Open `http://localhost:18888` to view telemetry. See [Aspire standalone dashboard docs](https://aspire.dev/dashboard/standalone/) for configuration options.
