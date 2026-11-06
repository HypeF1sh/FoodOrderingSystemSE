// PB15 (Jordan) - staff pages: auto-refresh the live boards
// Staff Floor and The Pass: every 20 seconds, fetch just the live part of the page and swap it in.
// New orders and bookings appear without a full reload, so an open table drawer or the phone tabs stay as they are.
// Any element with data-live-url is refreshed; the server returns a Razor partial (e.g. ?handler=Grid).
(function () {
    const pollMs = 20000;

    document.querySelectorAll('[data-live-url]').forEach(function (root) {
        const url = root.dataset.liveUrl;

        async function refresh() {
            try {
                const response = await fetch(url, { headers: { 'X-Requested-With': 'fetch' } });
                // redirected = the login page came back because the session ended; treat it as a failure
                if (!response.ok || response.redirected) {
                    throw new Error('Refresh failed');
                }
                root.innerHTML = await response.text();
            } catch {
                const status = root.querySelector('[data-live-status]');
                if (status) {
                    status.textContent = "Couldn't refresh, last updated " + status.dataset.updated + '. Trying again…';
                    status.classList.add('is-offline');
                }
            }
        }

        setInterval(refresh, pollMs);
    });
})();
