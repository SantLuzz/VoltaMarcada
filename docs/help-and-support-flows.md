# Help and Support Flows

Authenticated company members can use **Ajuda e suporte** in the internal menu.

- The page explains customers and equipment, work orders, schedules, recurring maintenance, quotes, and public requests.
- When `SUPPORT_WHATSAPP_NUMBER` is configured with a valid Brazilian number, the page shows a WhatsApp shortcut.
- The support form sends the member's stored email as the reply-to address and identifies the current company on the server. It never accepts these values from the browser.
- Up to five PNG, JPEG, or WEBP images are accepted. Each image is limited to 5 MiB and the total is limited to 20 MiB.
- Each authenticated member can send up to five requests every ten minutes.

Support requests are delivered to `Smtp:SenderEmail`. Configure SMTP and the optional WhatsApp number through environment-specific configuration; never commit credentials.
