// PB18: keeps the "In progress" badges on My orders up to date without a page reload.
// Every 15 seconds it asks /Orders?handler=Status for the customer's orders and updates each row's badges.
// When an order leaves the kitchen (Completed or Cancelled) the page reloads once so it moves to "Past orders".
(function () {
    const root = document.getElementById('myOrders');
    if (!root || root.dataset.hasActive !== 'true') {
        return;   // nothing in progress, so nothing to poll
    }

    const statusUrl = root.dataset.statusUrl;
    const lastUpdated = document.getElementById('lastUpdated');
    const pollMs = 15000;

    function setBadge(badge, text, cssClass) {
        if (badge) {
            badge.className = 'badge ' + cssClass;
            badge.textContent = text;
        }
    }

    async function refresh() {
        try {
            const response = await fetch(statusUrl, { headers: { 'Accept': 'application/json' } });
            if (!response.ok) {
                return;   // e.g. logged out; keep showing the last known status
            }

            const orders = await response.json();
            let movedToPast = false;

            for (const order of orders) {
                const row = document.querySelector('#activeOrders [data-order-id="' + order.orderId + '"]');
                if (!row) {
                    continue;
                }
                if (!order.isActive) {
                    movedToPast = true;
                }
                setBadge(row.querySelector('[data-role="status"]'), order.status, order.statusClass);
                setBadge(row.querySelector('[data-role="payment"]'), order.payment, order.paymentClass);
            }

            if (movedToPast) {
                window.location.reload();
                return;
            }

            if (lastUpdated) {
                lastUpdated.textContent = 'last checked ' + new Date().toLocaleTimeString();
            }
        } catch {
            // Network blip: try again on the next tick
        }
    }

    refresh();
    setInterval(refresh, pollMs);
})();
