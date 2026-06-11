# PayPal setup

The checkout uses PayPal Orders v2 with server-side order creation and capture.

## Sandbox credentials

1. Create a REST app in the PayPal Developer Dashboard.
2. From the `PhoneShop/PhoneShop` directory, store the sandbox credentials:

```powershell
dotnet user-secrets set "PayPal:ClientId" "YOUR_SANDBOX_CLIENT_ID"
dotnet user-secrets set "PayPal:ClientSecret" "YOUR_SANDBOX_CLIENT_SECRET"
```

3. Run the project and use a PayPal sandbox buyer account at checkout:

```powershell
dotnet run
```

The default settings use `Sandbox` mode and `USD`.

## Production

Set these environment variables in the deployment environment:

```text
PayPal__ClientId
PayPal__ClientSecret
PayPal__Mode=Live
PayPal__Currency=USD
```

Never commit the PayPal client secret to `appsettings.json`.
