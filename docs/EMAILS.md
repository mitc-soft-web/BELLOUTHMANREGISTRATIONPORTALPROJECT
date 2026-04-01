# Emails (Brevo) and templates

Email delivery uses Brevo's transactional email API via the `brevo_csharp` package.

## Components

- Template rendering: `Implementation/TemplateEngine/RazorEngine.cs`
- Mail orchestration: `Implementation/MailingService/MailService.cs`
- Mail sending: `Messaging/MailSender.cs`
- Email template view: `Views/SendAdmissionStatus.cshtml`
- Inline assets registry: `Extensions/EmailAssetRegistry.cs`

## How rendering works

`RazorEngine.ParseAsync(viewName, model)` renders a Razor view into a string without requiring a live MVC request.

`MailService.SendAdmissionStatus(...)`:

1) Creates a `SendAdmissionUpdate` model (`Messaging/Models/SendAdmissionUpdate.cs`)
2) Renders the `SendAdmissionStatus` view into HTML
3) Sends HTML using `MailSender.Send(...)`

## Inline assets

`MailSender` checks outgoing HTML for references like `cid:mitc_logo.png`.

When found, it:

- Resolves the file path under `wwwroot` using `EmailAssetRegistry.AssetMap`
- Reads the file bytes
- Adds it as a Brevo attachment with `name` set to the asset filename

Make sure the mapped files exist at runtime (deploy `wwwroot`).

## Asset map note

Inline assets are resolved relative to `wwwroot` by combining `env.WebRootPath` and the `AssetMap` values.
Ensure `Extensions/EmailAssetRegistry.cs` points to the actual file location under `wwwroot` (for example, `EmailAssets/assets/mitc_logo.png` if that is where the image lives).

