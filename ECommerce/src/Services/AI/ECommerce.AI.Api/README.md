# ECommerce AI API

This service provides:

- Microsoft Foundry powered shopping chat.
- Local hybrid keyword and vector product retrieval in memory.
- Personalized recommendations based on the authenticated customer's cart and recent orders.
- Read-only access to the current customer's profile, cart, and orders through the existing commerce API.

## Local configuration

Edit the ignored `appsettings.Local.json` file and replace these values:

- `Foundry:OpenAiEndpoint`
- `Foundry:ApiKey`
The configured Foundry deployment names are `gpt-4.1-mini` and `text-embedding-3-small`. They can be changed in the same file.

## Local startup order

1. Start `ECommerce.Api` on `http://localhost:5167`.
2. Start `ECommerce.AI.Api` on `http://localhost:5300`.
3. Send `POST http://localhost:5300/api/ai/admin/reindex` once in Development.
4. Start the Angular frontend on `http://localhost:4200`.

The reindex endpoint is available only in the Development environment. Run it again after every AI API restart and after relevant storefront product, price, or availability changes. Product vectors are intentionally stored only in the local AI API process for this development setup.

## Customer data boundary

The browser JWT is forwarded only to read-only endpoints on the commerce API. That API remains responsible for validating the token and limiting cart/order results to the token subject. Access tokens, row versions, full addresses, phone numbers, and payment identifiers are not included in Foundry prompts.
