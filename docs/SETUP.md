# Setup & local development

## Prerequisites

- .NET 9 SDK
- MySQL server (must match your configured connection string)

## Configure

See `docs/CONFIGURATION.md`.

## Run

```powershell
dotnet restore
dotnet build
dotnet run
```

Open:

- `http://localhost:5210`
- `https://localhost:7188`

## Database initialization

- In `Development`, the app auto-runs `db.Database.Migrate()` on startup (see `Program.cs`).
- In other environments, apply migrations explicitly:

```powershell
dotnet ef database update
```

If `dotnet ef` isn't installed:

```powershell
dotnet tool install --global dotnet-ef
```

## File uploads

Document uploads are written to:

- `wwwroot/uploads/certificates/`
- `wwwroot/uploads/passports/`

For production, consider using object storage (S3/Azure Blob) instead of local disk if you deploy multiple instances.

## Publish (production build)

```powershell
dotnet publish -c Release -o .\\publish
```

Set environment variables for connection strings and API keys on the host (see `docs/CONFIGURATION.md`).

## Paystack setup

- Callback URL should point to `GET /Payment/CallBack`.
- Webhook URL (recommended) should point to `POST /webhooks/paystack`.

## Troubleshooting

- **Migrations fail on startup**: confirm the MySQL connection string is valid and the DB user has schema permissions.
- **Payment init fails**: confirm `Paystack:SecretKey` is set and outbound HTTPS to `api.paystack.co` is allowed.
- **Email fails**: confirm `MITCAPIs:BrevoApiKey` is set and Brevo sender domain/from email are configured.
