# MITC Registration Portal

An ASP.NET Core MVC web app for managing student registration, document uploads, Paystack payment, and admission-status updates via email.

## What this project does

- Student registration in multiple steps (personal details -> document upload -> payment).
- Admin views to list students, view details, delete records, and export student directory to Excel.
- Paystack payment initialization + verification callback, plus an optional Paystack webhook receiver for asynchronous payment events.
- Email notifications (Brevo/Sendinblue API) using Razor views as email templates.

## Tech stack

- Runtime/framework: .NET `net9.0` (ASP.NET Core MVC)
- Database: MySQL via `Pomelo.EntityFrameworkCore.MySql`
- ORM: Entity Framework Core (migrations included)
- Auth: ASP.NET Core Identity (custom `UserStore` / `RoleStore`) + Cookie auth
- Validation: FluentValidation (+ client-side adapters)
- Payments: Paystack REST API (initialize/verify) + webhook signature verification
- Email: Brevo transactional email API (attachments used for inline assets)
- UI: Razor views + Bootstrap (static `wwwroot/lib`)

## Quick start (local development)

### 1) Prerequisites

- .NET 9 SDK installed
- A MySQL server you can connect to (local or remote)

### 2) Configure secrets (recommended)

This repo currently contains sensitive values in `appsettings.json`. For local development and deployments, prefer environment variables or user-secrets instead of committing real keys.

Minimum config values used by the app:

- `ConnectionStrings:MITCContext`
- `Paystack:SecretKey`
- `Paystack:CallbackUrl`
- `MITCAPIs:BrevoApiKey`
- `EmailConfiguration:FromEmail`
- `EmailConfiguration:FromName`

See `docs/CONFIGURATION.md` for exact keys and examples.

### 3) Run the app

```powershell
dotnet restore
dotnet run
```

By default, `Properties/launchSettings.json` defines:

- HTTP: `http://localhost:5210`
- HTTPS: `https://localhost:7188`

In Development, the app auto-applies EF Core migrations on startup (see `Program.cs`).

## Project structure (touches every folder)

High-level layered layout (MVC + services + repositories + EF Core):

- `Configuration/` - configuration POCOs (e.g., `EmailConfiguration`)
- `Contracts/` - cross-layer contracts (base entities, service contracts, messaging contracts)
- `Controllers/` - MVC controllers and HTTP endpoints
- `Exceptions/` - domain-specific exceptions (mail sender, razor engine)
- `Extensions/` - helper registries/utilities (e.g., email inline asset mapping)
- `Filters/` - MVC filters (registration step guard)
- `Identity/` - custom ASP.NET Core Identity stores + identity helpers
- `Implementation/` - concrete implementations for services, repositories, and template engine
- `Interface/` - interfaces for repositories/services/template engine
- `Messaging/` - messaging implementations (Brevo mail sender) + email models
- `Migrations/` - EF Core migrations + model snapshot
- `Models/` - entities, DTOs, validation, enums, and view models
- `Persistence/` - EF Core `DbContext` and persistence wiring
- `Properties/` - dev launch profiles (`launchSettings.json`)
- `Views/` - Razor views (UI + email template view)
- `wwwroot/` - static assets (Bootstrap, JS/CSS, email assets, uploaded files)

Generated/tooling folders you may see locally:

- `.dotnet_cli_home/` - local .NET CLI state (environment-dependent)
- `.vs/` - Visual Studio workspace settings
- `bin/`, `obj/` - build outputs and intermediate artifacts

## Core user flows

### Student registration (3 steps)

1) Step 1 - Personal + account details
   - Controller: `StudentController.StepOneRegistration`
   - Service: `StudentService.StepOneRegistration`
   - Persists both Session state and a DB-backed draft (`RegistrationDraft`) for resilience.

2) Step 2 - Document upload (WAEC certificate + passport photo)
   - Controller: `StudentController.StepTwoRegistration`
   - Service: `StudentService.StepTwoRegistration`
   - Saves files under `wwwroot/uploads/{certificates|passports}` and updates the `RegistrationDraft`.

3) Step 3 - Payment (Paystack)
   - Controller: `PaymentController.StepThreeRegistration` initializes payment and redirects to Paystack.
   - Paystack returns to `PaymentController.CallBack` where verification happens.
   - If payment verifies, the app finalizes the student creation:
     - Preferred path: `StudentService.CreateFromDraftAsync` (draft -> User + Student + Payment -> delete draft)
     - Fallback path: `StudentService.CreateAsync` (legacy session-backed flow)

### Paystack webhook (optional)

- Endpoint: `POST /webhooks/paystack` (see `WebhookController`)
- Validates `x-paystack-signature` (HMAC SHA512 over raw JSON body with your secret key)
- Updates `Payment.PaymentStatus` to `Successful` or `Failed` idempotently

### Admission status update email

- Service: `StudentService.UpdateAdmissionStatus` triggers `IMailService.SendAdmissionStatus(...)`
- Template: `Views/SendAdmissionStatus.cshtml` (rendered by `Implementation/TemplateEngine/RazorEngine.cs`)
- Sender: `Messaging/MailSender.cs` via Brevo API

## Database and seeding

- `Persistence/Context/StudentDbContext.cs` defines relationships and seed data.
- Seeded roles:
  - `Admin`
  - `Student`
- Seeded admin user + profile are added in `SeedAdminData(...)`.

See `docs/DATABASE.md` for entity relationships and migration notes.

## Documentation index

- `docs/SETUP.md` - local setup, run, troubleshooting
- `docs/ARCHITECTURE.md` - architecture, request flow, folder responsibilities
- `docs/CONFIGURATION.md` - configuration keys, secrets management, examples
- `docs/DATABASE.md` - schema overview, migrations, seed data
- `docs/PAYMENTS.md` - Paystack initialize/verify/webhook behavior
- `docs/EMAILS.md` - Brevo integration + Razor email templates
- `docs/SECURITY.md` - security notes, production hardening checklist
