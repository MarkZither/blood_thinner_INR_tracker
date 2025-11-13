# quickstart.md

This quickstart shows the minimal steps to add installability to the Blazor Server app for Phase 1.

1. Add manifest file at `wwwroot/manifest.webmanifest` with content similar to:

```json
{
  "name": "Blood Thinner Tracker",
  "short_name": "INR Tracker",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#1976d2",
  "icons": [
    { "src": "icons/icon-192.svg", "sizes": "192x192", "type": "image/svg+xml" },
    { "src": "icons/icon-512.svg", "sizes": "512x512", "type": "image/svg+xml" }
  ]
}
```

2. Add icons to `wwwroot/icons/` (192x192 and 512x512 SVG). Ensure images are optimized and appropriately licensed.

3. Add `service-worker.js` to `wwwroot/` with a minimal caching strategy that ONLY caches whitelisted static assets:

```javascript
const CACHE_NAME = 'inr-static-v1';
const ASSETS = [
  '/',
  '/manifest.webmanifest',
  '/icons/icon-192.svg',
  '/icons/icon-512.svg',
  '/css/app.css',
  '/js/app.js'
];

self.addEventListener('install', event => {
  event.waitUntil(caches.open(CACHE_NAME).then(cache => cache.addAll(ASSETS)));
});

self.addEventListener('fetch', event => {
  const url = new URL(event.request.url);
  // Only serve cached assets from our static list
  if (ASSETS.includes(url.pathname) || ASSETS.includes(url.pathname + '/')) {
    event.respondWith(caches.match(event.request).then(r => r || fetch(event.request)));
  }
  // Otherwise, do nothing (network-first). Never cache API responses or user data.
});
```

4. Register the service worker in the `MainLayout.razor` component by adding a script block at the bottom:

```html
<script>
  if ('serviceWorker' in navigator) {
    window.addEventListener('load', function() {
      navigator.serviceWorker.register('/service-worker.js').then(function(reg) {
        console.log('Service worker registered.', reg);
      }).catch(function(err) { console.error('SW reg failed:', err); });
    });
  }
</script>
```

5. Reference the manifest in the HTML head (can be added dynamically via script in `MainLayout.razor` or statically in `App.razor`):

```html
<link rel="manifest" href="/manifest.webmanifest">
<meta name="theme-color" content="#594AE2">
```

6. Add inline contextual help (Phase 1 canonical UX): small link/button in the header that opens a modal with platform-specific steps for Add to Home Screen / Create Shortcut.

7. Tests: run manual installation flows on Chrome/Edge (desktop + Android) and Safari (iOS). Add a Playwright test to ensure manifest is served and service worker registration endpoint returns 200.
