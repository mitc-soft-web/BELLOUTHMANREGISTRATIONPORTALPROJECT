# Payments (Paystack)

Payment is handled in three layers:

- Controller/UI: `Controllers/PaymentController.cs`
- Application logic: `Implementation/Services/PaymentService.cs` + `Implementation/Services/StudentService.cs`
- External API wrapper: `Implementation/Services/PayStackService.cs`

## Initialize flow

1) Student finishes Step 2 and opens the payment page (`Payment/Index`).
2) `POST Payment/StepThreeRegistration` calls `StudentService.InitiateRegistration()`.
3) `StudentService` calls `PaymentService.IniatilizeRegistrationPayment(email)`:
   - Creates a `Payment` row in DB (`Pending`) with a generated reference
   - Calls Paystack `transaction/initialize`
4) App redirects the browser to Paystack’s `authorization_url`.

## Callback + verification flow

1) Paystack redirects to `Paystack:CallbackUrl`, which should point to `GET /Payment/CallBack?reference=...`.
2) `PaymentController.CallBack` stores the reference in session and calls `StudentService.VerifyPaymentAsync(reference)`.
3) `StudentService` verifies via `PaymentService.VerifyPaymentAsync(reference)` and then finalizes:
   - Preferred: looks up a matching `RegistrationDraft` and calls `CreateFromDraftAsync(...)`
   - Fallback: uses session-backed `CreateAsync()`

## Webhook (recommended for reliability)

Endpoint: `POST /webhooks/paystack`

Behavior:

- Validates `x-paystack-signature` (HMAC SHA512 over raw request body).
- For events:
  - `charge.success` → marks payment `Successful`
  - `charge.failed` → marks payment `Failed` (won’t downgrade a previously successful payment)

Even with webhooks, the callback verification flow remains useful for an immediate user experience.

## Common gotchas

- Paystack amounts are in **kobo**; the app sends `amount * 100`.
- If `Paystack:SecretKey` is missing, webhook returns 500 and verify/initialize will fail.
- Ensure your Paystack dashboard webhook URL points to your deployed `/webhooks/paystack`.

