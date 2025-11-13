## Manual Verification: iOS "Add to Home Screen"

Target: iPhone / iPad running Safari

Steps:

1. Open Safari and navigate to the app URL (e.g., <your-app-url> such as https://localhost:5235 or https://localhost:5236, depending on your environment).
2. Verify the page loads and the site shows the normal header and content.
3. Tap the Share button (square with upward arrow) in the Safari toolbar.
4. In the share sheet, scroll the action row and select "Add to Home Screen".
5. Confirm the suggested name and tap "Add".
6. Return to the home screen and locate the new shortcut icon. Launch it.
7. Confirm the app opens in a standalone window (no browser chrome) when launched from the home screen.

Expected results:

- The "Add to Home Screen" action is present in Safari's share sheet.
- A shortcut icon is added to the home screen with the configured app name and icon.
- Launching from the home screen opens the site in a standalone context.
- No user-sensitive data is cached by the service worker (verify service-worker.js only caches static assets).

Notes:

- On iOS the PWA experience is controlled by Safari; some features (e.g., install prompt) are not available.
- Document any platform-specific caveats in `specs/008-install-web-app/research.md`.
