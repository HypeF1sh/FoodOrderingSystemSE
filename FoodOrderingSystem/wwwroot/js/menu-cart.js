// UX1: friendlier "Add" on /Menu.
// Tapping Add opens a small dialog to pick the quantity. Confirming posts the same form in the background
// (fetch), so the page does not reload and the customer keeps their place in the menu.
// The result shows as a floating toast and the "View cart" bar count updates.
// Without JavaScript the Add forms still post normally (PB14 behaviour), so nothing breaks.
(function () {
    const modalEl = document.getElementById('addToCartModal');
    if (!modalEl || typeof bootstrap === 'undefined') {
        return;   // no dialog on this page, or Bootstrap failed to load: fall back to the plain form post
    }

    const maxQty = parseInt(document.currentScript.dataset.maxQty, 10) || 20;
    const modal = new bootstrap.Modal(modalEl);
    const toastEl = document.getElementById('menuToast');
    const toast = new bootstrap.Toast(toastEl);

    const nameEl = document.getElementById('addToCartName');
    const priceEl = document.getElementById('addToCartPrice');
    const totalEl = document.getElementById('addToCartTotal');
    const qtyEl = document.getElementById('qtyValue');
    const minusBtn = document.getElementById('qtyMinus');
    const plusBtn = document.getElementById('qtyPlus');
    const confirmBtn = document.getElementById('addToCartConfirm');
    const cartBar = document.getElementById('menuCartBar');
    const cartCount = document.getElementById('menuCartCount');

    let activeForm = null;   // the dish's Add form the dialog was opened from
    let qty = 1;
    let price = 0;

    function render() {
        qtyEl.textContent = qty;
        totalEl.textContent = (price * qty).toFixed(2);
        minusBtn.disabled = qty <= 1;        // stops at 1 and at the cart limit, no wrap-around
        plusBtn.disabled = qty >= maxQty;
    }

    function showToast(message, isError) {
        document.getElementById('menuToastText').textContent = message;
        toastEl.classList.toggle('is-error', isError);
        toast.show();
    }

    // 1. Catch every Add form's submit and open the dialog instead
    document.querySelectorAll('form.menu-card-add').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            activeForm = form;
            qty = 1;
            price = parseFloat(form.dataset.dishPrice) || 0;
            nameEl.textContent = form.dataset.dishName;
            priceEl.textContent = price.toFixed(2);
            render();
            modal.show();
        });
    });

    minusBtn.addEventListener('click', function () { qty = Math.max(1, qty - 1); render(); });
    plusBtn.addEventListener('click', function () { qty = Math.min(maxQty, qty + 1); render(); });

    // 2. Confirm: post the dish's own form (it carries the anti-forgery token) with the chosen quantity
    confirmBtn.addEventListener('click', async function () {
        if (!activeForm) return;
        activeForm.querySelector('input[name="quantity"]').value = qty;
        confirmBtn.disabled = true;

        try {
            const response = await fetch(activeForm.action, {
                method: 'POST',
                body: new FormData(activeForm),
                headers: { 'X-Requested-With': 'fetch' }   // tells Cart.OnPostAddAsync to answer with JSON
            });
            if (!response.ok) throw new Error('HTTP ' + response.status);
            const result = await response.json();

            // 3. Update the cart bar and show the outcome (the service's own message, e.g. sold out or limit reached)
            cartCount.textContent = result.cartCount;
            cartBar.classList.toggle('d-none', result.cartCount === 0);
            modal.hide();
            showToast(result.message, !result.success);
        } catch (err) {
            modal.hide();
            showToast('Could not add the dish. Please try again.', true);
        } finally {
            confirmBtn.disabled = false;
        }
    });
})();
