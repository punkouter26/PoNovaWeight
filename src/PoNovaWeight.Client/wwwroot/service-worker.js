// Nova Food Journal Service Worker
// Implements cache-first strategy for static assets and network-first for API calls

const CACHE_NAME = 'nova-journal-v1';
const STATIC_CACHE_NAME = 'nova-static-v1';
const DATA_CACHE_NAME = 'nova-data-v1';

// Static assets to cache immediately on install
const STATIC_ASSETS = [
    '/',
    '/index.html',
    '/css/app.css',
    '/js/imageCompressor.js',
    '/manifest.json',
    '/icons/icon-192x192.svg',
    '/icons/icon-512x512.svg'
];

// API endpoints to cache with network-first strategy
const API_CACHE_PATTERNS = [
    '/api/daily-logs/',
    '/api/weekly-summary'
];

// Install event - cache static assets
self.addEventListener('install', (event) => {
    console.log('[Service Worker] Installing...');
    event.waitUntil(
        caches.open(STATIC_CACHE_NAME)
            .then((cache) => {
                console.log('[Service Worker] Caching static assets');
                return cache.addAll(STATIC_ASSETS);
            })
            .then(() => {
                // Skip waiting to activate immediately
                return self.skipWaiting();
            })
    );
});

// Activate event - clean up old caches
self.addEventListener('activate', (event) => {
    console.log('[Service Worker] Activating...');
    event.waitUntil(
        caches.keys()
            .then((cacheNames) => {
                return Promise.all(
                    cacheNames
                        .filter((name) => {
                            return name.startsWith('nova-') && 
                                   name !== STATIC_CACHE_NAME && 
                                   name !== DATA_CACHE_NAME;
                        })
                        .map((name) => {
                            console.log('[Service Worker] Deleting old cache:', name);
                            return caches.delete(name);
                        })
                );
            })
            .then(() => {
                // Take control of all pages immediately
                return self.clients.claim();
            })
    );
});

// Fetch event - handle requests with appropriate strategy
self.addEventListener('fetch', (event) => {
    const url = new URL(event.request.url);

    // Skip non-GET requests
    if (event.request.method !== 'GET') {
        return;
    }

    // Skip external requests
    if (url.origin !== location.origin) {
        return;
    }

    // API requests - network first, cache fallback
    if (isApiRequest(url.pathname)) {
        event.respondWith(networkFirstStrategy(event.request));
        return;
    }

    // Static assets - cache first, network fallback
    event.respondWith(cacheFirstStrategy(event.request));
});

// Check if request is an API call
function isApiRequest(pathname) {
    return API_CACHE_PATTERNS.some(pattern => pathname.startsWith(pattern));
}

// Cache-first strategy for static assets
async function cacheFirstStrategy(request) {
    try {
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            // Return cached response and update cache in background
            updateCache(request);
            return cachedResponse;
        }

        // Not in cache, fetch from network
        const networkResponse = await fetch(request);
        
        if (networkResponse.ok) {
            const cache = await caches.open(STATIC_CACHE_NAME);
            cache.put(request, networkResponse.clone());
        }

        return networkResponse;
    } catch (error) {
        console.error('[Service Worker] Cache-first error:', error);
        
        // Return offline fallback for navigation requests
        if (request.mode === 'navigate') {
            return caches.match('/index.html');
        }
        
        throw error;
    }
}

// Network-first strategy for API calls
async function networkFirstStrategy(request) {
    try {
        const networkResponse = await fetch(request);
        
        if (networkResponse.ok) {
            // Cache successful API responses
            const cache = await caches.open(DATA_CACHE_NAME);
            cache.put(request, networkResponse.clone());
        }

        return networkResponse;
    } catch (error) {
        console.log('[Service Worker] Network failed, trying cache:', request.url);
        
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }

        // Return error response if nothing in cache
        return new Response(JSON.stringify({ error: 'Offline - no cached data available' }), {
            status: 503,
            statusText: 'Service Unavailable',
            headers: { 'Content-Type': 'application/json' }
        });
    }
}

// Update cache in background
async function updateCache(request) {
    try {
        const networkResponse = await fetch(request);
        if (networkResponse.ok) {
            const cache = await caches.open(STATIC_CACHE_NAME);
            cache.put(request, networkResponse);
        }
    } catch (error) {
        // Silently fail background updates
        console.log('[Service Worker] Background update failed:', request.url);
    }
}

// Handle messages from the main thread
self.addEventListener('message', (event) => {
    if (event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }

    if (event.data.type === 'CACHE_DAILY_LOG') {
        // Pre-cache a specific daily log for offline access
        const { date } = event.data;
        const url = `/api/daily-logs/${date}`;
        caches.open(DATA_CACHE_NAME)
            .then(cache => fetch(url).then(response => cache.put(url, response)))
            .catch(err => console.log('[Service Worker] Failed to cache daily log:', err));
    }

    if (event.data.type === 'CLEAR_DATA_CACHE') {
        caches.delete(DATA_CACHE_NAME)
            .then(() => console.log('[Service Worker] Data cache cleared'));
    }
});

// Background sync for offline data submission (if supported)
self.addEventListener('sync', (event) => {
    if (event.tag === 'sync-entries') {
        event.waitUntil(syncPendingEntries());
    }
});

// Sync pending entries stored in IndexedDB
async function syncPendingEntries() {
    // This would be implemented with IndexedDB to store pending entries
    // and sync them when back online. For MVP, we rely on network connectivity.
    console.log('[Service Worker] Sync triggered - would sync pending entries');
}
