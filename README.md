# MITC Registration Portal

[![.NET 9](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![MySQL](https://img.shields.io/badge/Database-MySQL-orange.svg)](https://www.mysql.com/)
[![Paystack](https://img.shields.io/badge/Payments-Paystack-teal.svg)](https://paystack.com/)
[![Brevo](https://img.shields.io/badge/Emails-Brevo-darkblue.svg)](https://www.brevo.com/)

An enterprise-ready, multi-step ASP.NET Core MVC web application designed for student registration, certificate/passport document upload, Paystack secure payment integration, and transactional email-based admission workflows.

---

## 🚀 Key Features & Dynamic Capabilities

### 1. Unified Multi-Step Student Registration
*   **Step 1: Personal & Account Setup** – Collects applicant information (validating **minimum age of 16 years**) and securely initializes credentials.
*   **Step 2: Certificate & Document Upload** – Facilitates passport photographs and exam certificate uploads.
*   **Step 3: Paystack Secure Checkout** – Automated payment generation, redirect, and synchronous validation/asynchronous webhooks.

### 2. Multi-Sitting Examination & Subject Management
*   **Support for Multiple Exam Bodies** – Full integration for **WAEC**, **NECO**, **NABTEB**, **GCE**, **JUPEB**, and **IJMB**.
*   **Dynamic Result Entry UI** – Interactive form allowing applicants to dynamically add, edit, and remove subject sittings.
*   **Contextual Filtering** – Dynamically adjusts grading systems (e.g., standard A1–F9 for O'Level vs. A–F for A'Level JUPEB/IJMB) based on the chosen exam body.

### 3. Strict Admin Dashboard & Admission Finality
*   **Admission Status Transitions** – Enforces strict business rules preventing an `Admitted` student from being changed to `Rejected` (and vice versa) once a final decision is made.
*   **Inline Department Assignment** – Allows administrators to reassess and change the assigned department/course directly in the admin dashboard before confirming admission status.
*   **Optimized UI Controls** – Switched to Bootstrap 5 `form-select` inputs for dropdown navigation and styled action states.

### 4. Professional Transactional Notifications
*   **Razor-Engine Email Templates** – Rendered dynamically using actual view files for rich HTML styling.
*   **Hosted Asset Embedding** – High-performance inline logos directly served from the portal's public assets (`/assets/mitc_logo.png`) preventing blocking or showing as file attachments in popular email clients.

---

## 🛠️ Technology Stack

*   **Runtime & Framework:** .NET `net9.0` (ASP.NET Core MVC)
*   **Database:** MySQL Server via `Pomelo.EntityFrameworkCore.MySql`
*   **Object-Relational Mapper:** Entity Framework Core
*   **Identity System:** ASP.NET Core Identity (custom customized `UserStore` and `RoleStore` mappings)
*   **Validation Layer:** FluentValidation
*   **Payment Gateway:** Paystack REST API & HMAC-SHA512 Webhook signature validation
*   **Email Client:** Brevo Transactional Email Service

---

## 💻 Quick Start & Local Setup

### 1. Prerequisites
*   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
*   MySQL Server (v8.0+)
*   Git CLI

### 2. Configuration Setup
Create or edit your local `appsettings.json` (note: `appsettings.json` is ignored from git tracking for security). The minimum key-value structure required is:

```json
{
  "ConnectionStrings": {
    "MITCContext": "server=localhost;port=3306;database=mitc_db;user=root;password=your_password"
  },
  "Paystack": {
    "SecretKey": "sk_test_...",
    "CallbackUrl": "https://your-domain.com/Payment/Callback"
  },
  "MITCAPIs": {
    "BrevoApiKey": "xkeysib-..."
  },
  "EmailConfiguration": {
    "FromEmail": "admissions@mitc.edu",
    "FromName": "MITC Admissions"
  }
}
```

### 3. Restore & Run
Initialize dependencies and start the local development server:
```powershell
dotnet restore
dotnet run
```
The application will listen on:
*   **HTTP:** `http://localhost:5210`
*   **HTTPS:** `https://localhost:7188`

*Note: Database migrations are automatically checked and applied at application startup in local development.*

---

## 📂 Project Structure

```
├── Configuration/       # Application configuration POCO bindings
├── Contracts/           # Cross-cutting entity, repository, and service interfaces
├── Controllers/         # MVC Controllers & API Endpoints
├── Exceptions/          # Custom Domain-specific Exceptions
├── Extensions/          # Utility classes & Asset registries
├── Filters/             # Action filters (e.g. Registration step guard)
├── Identity/            # Custom ASP.NET Core Identity stores & password hashing
├── Implementation/      # Concrete Business Logic (Services & Repositories)
├── Interface/           # Main business contract interfaces
├── Messaging/           # Email Service & Brevo sender integrations
├── Migrations/          # Entity Framework database migrations
├── Models/              # Domain entities, DTOs, Enums, and Request models
├── Persistence/         # EF Core DbContext & Database context registration
├── Properties/          # Launch configurations (launchSettings.json)
├── Views/               # Razor pages (Main Web Interface & HTML Email Templates)
└── wwwroot/             # Client-side CSS, JavaScript, Images, and File Uploads
```

---

## 🛡️ License

This project is licensed under the MIT License.
