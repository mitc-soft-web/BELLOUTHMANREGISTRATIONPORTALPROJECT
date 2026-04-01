# Security notes / hardening checklist

This repo includes sensitive configuration values. Treat this document as a quick checklist for deploying safely.

## Secrets management

- Remove real values from `appsettings.json` and store them as environment variables, user-secrets, or a secret manager.
- Rotate any exposed keys (DB password, Paystack secret key, Brevo API key).

## Authentication & session

- Cookie auth is configured in `Program.cs`.
- Session is in-memory by default; in production with multiple instances use a distributed session store.

## Paystack webhook verification

- Webhook verifies `x-paystack-signature` using HMAC SHA512 and your Paystack secret key.
- Ensure your webhook endpoint is HTTPS-only and reachable from Paystack.

## Uploads

- Uploaded files are written under `wwwroot/uploads` and served publicly.
- Current validators enforce image MIME type/extension/signature and a size limit (2 MB).
- Consider additional controls:
  - virus/malware scanning
  - storing uploads outside the web root (and serving via controlled endpoints)
  - stricter content-type handling and image re-encoding

## Recommended production settings

- Enforce HTTPS, HSTS, and secure cookies.
- Add request logging/monitoring and rate limiting for `/webhooks/paystack`.
- Add an admin UI flow to rotate seeded credentials and manage roles.

