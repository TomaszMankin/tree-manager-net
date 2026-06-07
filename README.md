# tree-manager-net

## Documentation

- [docs/domain.md](docs/domain.md) — Polish domain glossary (Lista osób, Rody, Drzewo, Poczekalnia, etc.)
- [docs/testing.md](docs/testing.md) — test tier definitions and CI gate

## Email escalation setup

When the app crashes it sends an alert email. To enable this, create `appsettings.user.json` in the same folder as the application executable and fill it in as shown below. If the file is absent or incomplete the app still works — crash alerts are silently skipped.

```json
{
  "emailSettings": {
    "host": "smtp.gmail.com",
    "port": 587,
    "useSsl": true,
    "fromAddress": "your-gmail-address@gmail.com",
    "appPassword": "xxxx xxxx xxxx xxxx",
    "toAddress": "address-to-receive-alerts@example.com"
  }
}
```

Fields:
- `host` — SMTP server. Use `smtp.gmail.com` for Gmail.
- `port` — SMTP port. Use `587` with StartTLS (recommended) or `465` with SSL.
- `useSsl` — set to `true`.
- `fromAddress` — the Gmail account that sends the alert.
- `appPassword` — a Gmail App Password (16 characters, spaces allowed). To generate one: Google Account → Security → 2-Step Verification → App passwords. A normal account password will not work.
- `toAddress` — where crash alerts are delivered. Can be the same address as `fromAddress`.

The file is gitignored and never committed.