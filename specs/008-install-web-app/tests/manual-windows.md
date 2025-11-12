## Manual Verification: Windows Desktop Shortcut / Install

Target: Windows 10/11 with Chrome or Edge

Steps:

1. Open Chrome or Edge and navigate to the app URL (e.g. http://localhost:5236).
2. Verify the page loads correctly.
3. Open the browser menu and look for "Install app" (Edge) or "Install [site]" (Chrome) or "More tools -> Create shortcut...".
4. Follow the browser's prompts to install the app or create the shortcut on the desktop.
5. Locate the created desktop shortcut and launch the app.
6. Verify the app launches in a separate window without browser chrome (if supported) or opens the URL via the default browser if only shortcut was created.

Expected results:

- Browser shows an install option or "Create shortcut" option in the menu.
- A desktop shortcut or installed app is created with the configured app icon and name.
- Launching from the desktop opens a standalone window or the browser with the app URL.

Notes:

- Behavior depends on browser and OS; include observed differences in `research.md`.
- Confirm service worker scope and caching policy do not store medical data.
