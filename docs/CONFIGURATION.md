# Configuration

ASP.NET Core configuration is read from `appsettings.json`, `appsettings.{Environment}.json`, and environment variables.

For production and shared repos, do **not** commit live API keys or DB passwords into `appsettings.json`.

## Required keys

### Database

- `ConnectionStrings:MITCContext`

Example (replace values):

```json
{
  "ConnectionStrings": {
    "MITCContext": "Server=localhost;Database=MitcRegPortal;User ID=root;Password=CHANGEME;"
  }
}
```

### Paystack

Used by:

- `Implementation/Services/PayStackService.cs`
- `Controllers/WebhookController.cs`

Keys:

- `Paystack:SecretKey`
- `Paystack:CallbackUrl` (Paystack redirects to `Payment/CallBack`)

Example:

```json
{
  "Paystack": {
    "SecretKey": "sk_test_xxx",
    "CallbackUrl": "https://localhost:7188/Payment/CallBack"
  }
}
```

### Brevo (email)

Used by:

- `Messaging/MailSender.cs`

Keys:

- `MITCAPIs:BrevoApiKey`

Example:

```json
{
  "MITCAPIs": {
    "BrevoApiKey": "xkeysib-xxxx"
  }
}
```

### Email “from” identity

Used by:

- `Implementation/MailingService/MailService.cs`
- `Configuration/EmailConfiguration.cs`

Keys:

- `EmailConfiguration:FromEmail`
- `EmailConfiguration:FromName`

Example:

```json
{
  "EmailConfiguration": {
    "FromEmail": "noreply@example.com",
    "FromName": "MITC Registration"
  }
}
```

## Setting values via environment variables

On Windows PowerShell (for the current session):

```powershell
$env:ConnectionStrings__MITCContext = "Server=localhost;Database=MitcRegPortal;User ID=root;Password=CHANGEME;"
$env:Paystack__SecretKey = "sk_test_xxx"
$env:Paystack__CallbackUrl = "https://localhost:7188/Payment/CallBack"
$env:MITCAPIs__BrevoApiKey = "xkeysib-xxxx"
$env:EmailConfiguration__FromEmail = "noreply@example.com"
$env:EmailConfiguration__FromName = "MITC Registration"
dotnet run
```

## Notes

- `Program.cs` currently sets the MySQL server version to `9.0.0` for the EF Core provider. If your MySQL server version differs, update that version to match (or use the provider’s auto-detect pattern if you prefer).
- Session is used to store registration progress; for multi-instance deployments, use a distributed session store (e.g., Redis) to avoid cross-node issues.

