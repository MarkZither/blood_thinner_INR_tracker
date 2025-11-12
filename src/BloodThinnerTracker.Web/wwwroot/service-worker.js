// Minimal service worker for Phase 1
// PURPOSE: Support manifest discovery and optionally cache a small set of static assets (icons, CSS, JS).
// SECURITY: MUST NOT cache API responses, authentication tokens, or any personal/medical data.
// REVIEWERS: Ensure the `ALLOWED_ASSETS` list contains only static, non-sensitive files.
// REVIEW CHECKLIST:
//  - Confirm ALLOWED_ASSETS contains no /api, /identity, or other user-data paths
//  - Confirm caching strategy is cache-first for listed assets only
//  - Confirm no use of IndexedDB/localStorage to persist sensitive data

const CACHE_NAME = 'btt-static-v1';
const ALLOWED_ASSETS = [
  '/',
  '/index.html',
  '/css/app.css',
  '/js/app.js',
  '/icons/icon-192.svg',
  '/icons/icon-512.svg'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      return cache.addAll(ALLOWED_ASSETS);
    })
  );
});

self.addEventListener('activate', (event) => {
  // Cleanup old caches if necessary
  event.waitUntil(
    caches.keys().then((keys) => Promise.all(
      keys.map((k) => {
        if (k !== CACHE_NAME) return caches.delete(k);
      })
    ))
  );
});

self.addEventListener('fetch', (event) => {
  // Only respond with cache for requests that match our whitelist.
  const requestUrl = new URL(event.request.url);

  // Do not handle API requests — allow network for those
  if (requestUrl.pathname.startsWith('/api') || requestUrl.pathname.startsWith('/identity')) {
    return;
  }

  // If the request is in the allowed list, serve from cache first
  if (ALLOWED_ASSETS.includes(requestUrl.pathname) || ALLOWED_ASSETS.includes(requestUrl.pathname + '/')) {
    event.respondWith(
      caches.match(event.request).then((resp) => resp || fetch(event.request))
    );
  }
  // Other requests fall-through to network
});
