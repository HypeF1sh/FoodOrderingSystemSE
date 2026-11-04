// Booking picker (Pages/Shared/_BookingPicker.cshtml): builds the time-slot chips for the chosen day.
// Slots are every 30 minutes from opening time to the last seating (closing time minus the dining slot).
// For today, slots sooner than the minimum notice are left out. The server still checks everything again.
(function () {
    const slotMinutes = 30;
    const lunchEndsAt = 17 * 60;   // slots before 5 PM are listed under "Lunch", the rest under "Dinner"

    function toMinutes(hhmm) {
        const [h, m] = hhmm.split(':').map(Number);
        return h * 60 + m;
    }

    function toHhmm(minutes) {
        return String(Math.floor(minutes / 60)).padStart(2, '0') + ':' + String(minutes % 60).padStart(2, '0');
    }

    function toLabel(minutes) {
        const h = Math.floor(minutes / 60);
        const m = String(minutes % 60).padStart(2, '0');
        return ((h + 11) % 12 + 1) + ':' + m + (h < 12 ? ' AM' : ' PM');
    }

    document.querySelectorAll('[data-booking-picker]').forEach(function (picker) {
        const hours = JSON.parse(picker.dataset.hours);
        const timeName = picker.dataset.timeName;
        const today = picker.dataset.today;
        const minAdvance = Number(picker.dataset.minAdvance);
        const dining = Number(picker.dataset.dining);
        const slotsBox = picker.querySelector('[data-slots]');
        const days = picker.querySelector('.bp-days');
        let selectedTime = picker.dataset.selectedTime;

        function chosenDate() {
            const checked = days.querySelector('input:checked');
            return checked ? checked.value : null;
        }

        function renderSlots() {
            const date = chosenDate();
            slotsBox.innerHTML = '';
            if (!date) {
                slotsBox.innerHTML = '<p class="bp-hint">Pick a day first.</p>';
                return;
            }

            // new Date('yyyy-mm-dd') is read as UTC, so build it from parts to get the local weekday
            const [y, mo, d] = date.split('-').map(Number);
            const day = hours[new Date(y, mo - 1, d).getDay()];
            if (!day || day.closed) {
                slotsBox.innerHTML = '<p class="bp-hint">We are closed that day. Pick another day.</p>';
                return;
            }

            let first = toMinutes(day.open);
            const last = toMinutes(day.close) - dining;
            if (date === today) {
                const now = new Date();
                const earliest = now.getHours() * 60 + now.getMinutes() + minAdvance;
                first = Math.max(first, Math.ceil(earliest / slotMinutes) * slotMinutes);
            }

            const slots = [];
            for (let t = Math.ceil(first / slotMinutes) * slotMinutes; t <= last; t += slotMinutes) {
                slots.push(t);
            }
            if (slots.length === 0) {
                slotsBox.innerHTML = '<p class="bp-hint">No times left today. Pick another day.</p>';
                return;
            }

            // Keep the earlier choice if that time exists on the new day; otherwise nothing is pre-selected
            const keep = slots.includes(toMinutes(selectedTime || '00:00'));

            [['Lunch', slots.filter(t => t < lunchEndsAt)], ['Dinner', slots.filter(t => t >= lunchEndsAt)]]
                .filter(group => group[1].length > 0)
                .forEach(function (group) {
                    const title = document.createElement('div');
                    title.className = 'bp-sub';
                    title.textContent = group[0];
                    slotsBox.appendChild(title);

                    const row = document.createElement('div');
                    row.className = 'bp-chips';
                    group[1].forEach(function (t) {
                        const label = document.createElement('label');
                        label.className = 'bp-chip';
                        const input = document.createElement('input');
                        input.type = 'radio';
                        input.name = timeName;
                        input.value = toHhmm(t);
                        input.required = true;
                        input.checked = keep && toHhmm(t) === selectedTime;
                        input.addEventListener('change', function () { selectedTime = input.value; });
                        const text = document.createElement('span');
                        text.textContent = toLabel(t);
                        label.append(input, text);
                        row.appendChild(label);
                    });
                    slotsBox.appendChild(row);
                });
        }

        days.addEventListener('change', renderSlots);

        // "Another date": add that day as a chip at the end of the strip and select it
        const other = picker.querySelector('[data-other-date]');
        if (other) {
            other.addEventListener('change', function () {
                if (!other.value) {
                    return;
                }
                let input = days.querySelector('input[value="' + other.value + '"]');
                if (!input) {
                    const [y, mo, d] = other.value.split('-').map(Number);
                    const when = new Date(y, mo - 1, d);
                    const label = document.createElement('label');
                    label.className = 'bp-day';
                    input = document.createElement('input');
                    input.type = 'radio';
                    input.name = days.querySelector('input').name;
                    input.value = other.value;
                    label.append(input);
                    label.insertAdjacentHTML('beforeend',
                        '<small>' + when.toLocaleDateString('en-MY', { weekday: 'short' }) + '</small>' +
                        '<b>' + d + '</b>' +
                        '<small>' + when.toLocaleDateString('en-MY', { month: 'short' }) + '</small>');
                    days.appendChild(label);
                }
                input.checked = true;
                input.closest('label').scrollIntoView({ block: 'nearest', inline: 'nearest' });
                renderSlots();
            });
        }

        renderSlots();
    });
})();
