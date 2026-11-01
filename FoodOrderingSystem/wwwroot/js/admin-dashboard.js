// PB22: admin dashboard behaviour (also used by Dish sales).
// 1. The filter form reloads the report as soon as From, To or Group by changes (no "Show report" button).
// 2. Picking a month in the month picker opens that month (each option's value is the page link for it).
// 3. Draws the charts with Chart.js. The page puts the numbers in a <script type="application/json"
//    id="dashboardData"> block; this file only draws them. Every chart is a single series in the site
//    accent colour, so no legend is needed (the card title names it).
(function () {
    const monthPicker = document.querySelector('[data-range-month]');
    if (monthPicker) {
        monthPicker.addEventListener('change', function () {
            if (monthPicker.value) {
                window.location.href = monthPicker.value;
            }
        });
    }

    const filter = document.getElementById('dashboardFilter');
    if (filter) {
        filter.addEventListener('change', function (event) {
            // A half-typed date reports an empty value; wait until the field holds a full date
            if (event.target.type === 'date' && !event.target.value) {
                return;
            }
            filter.requestSubmit();
        });
    }

    const dataBlock = document.getElementById('dashboardData');
    if (!dataBlock || typeof Chart === 'undefined') {
        return;   // no report on screen (e.g. an invalid range), or Chart.js failed to load
    }

    const data = JSON.parse(dataBlock.textContent);

    // Colours from the site palette (site.css), so the charts match the rest of the site
    const css = getComputedStyle(document.documentElement);
    const accent = css.getPropertyValue('--rr-accent').trim();
    const accentDark = css.getPropertyValue('--rr-accent-dark').trim();
    const muted = css.getPropertyValue('--rr-muted').trim();
    const line = css.getPropertyValue('--rr-line').trim();

    Chart.defaults.font.family = css.getPropertyValue('--rr-sans').trim();
    Chart.defaults.color = muted;

    const money = value => 'RM ' + Number(value).toFixed(2);

    // Long dish names are cut with "…" on the axis so they fit on a phone; the tooltip keeps the full name.
    // A plain function (not an arrow) because Chart.js passes the axis as 'this'.
    const maxLabelLength = 20;
    function shortLabel(value) {
        const label = this.getLabelForValue(value);
        return label.length > maxLabelLength ? label.slice(0, maxLabelLength - 1) + '…' : label;
    }

    // "#A3334A" -> "rgba(163, 51, 74, 0.12)", for the soft area under the sales line
    function withAlpha(hex, alpha) {
        const n = parseInt(hex.replace('#', ''), 16);
        return 'rgba(' + (n >> 16) + ', ' + ((n >> 8) & 255) + ', ' + (n & 255) + ', ' + alpha + ')';
    }

    // Shared look: quiet grid, no legend, hover tooltip on each point.
    // Bars for counts (bookings, dishes); a line for sales, where the trend over time is the point.
    function drawChart(canvasId, series, options) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        const isLine = options.line === true;
        const horizontal = options.horizontal === true;
        const valueAxis = horizontal ? 'x' : 'y';
        const labelAxis = horizontal ? 'y' : 'x';

        const dataset = isLine
            ? {
                borderColor: accent,
                borderWidth: 2,
                backgroundColor: withAlpha(accent, 0.12),
                fill: 'origin',                    // shade down to zero so the size of each period reads at a glance
                cubicInterpolationMode: 'monotone',   // smooth curve that never overshoots, so it can't dip below RM 0
                pointRadius: series.values.length > 40 ? 0 : 3,   // dots only while they don't crowd the line
                pointHoverRadius: 5,
                pointBackgroundColor: accent
            }
            : {
                backgroundColor: accent,
                hoverBackgroundColor: accentDark,
                borderRadius: 4,
                borderSkipped: 'start',            // round only the data end, the end away from the baseline
                maxBarThickness: 40
            };

        new Chart(canvas, {
            type: isLine ? 'line' : 'bar',
            data: {
                labels: series.labels,
                datasets: [Object.assign({ label: options.label, data: series.values }, dataset)]
            },
            options: {
                indexAxis: horizontal ? 'y' : 'x',
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },   // hovering anywhere above a period shows its value
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: context => options.format(horizontal ? context.parsed.x : context.parsed.y)
                        }
                    }
                },
                scales: {
                    [valueAxis]: {
                        beginAtZero: true,
                        grid: { color: line },
                        border: { display: false },
                        ticks: { precision: 0, callback: options.tick }
                    },
                    [labelAxis]: {
                        grid: { display: false },
                        ticks: Object.assign({ autoSkip: true, maxRotation: 0 },
                            options.shortLabels ? { callback: shortLabel } : {})
                    }
                }
            }
        });
    }

    drawChart('salesChart', data.sales, {
        label: 'Sales',
        line: true,
        format: money,
        tick: value => 'RM ' + value
    });

    drawChart('bookingsChart', data.bookings, {
        label: 'Bookings',
        format: value => value + (value === 1 ? ' booking' : ' bookings'),
        tick: value => value
    });

    drawChart('dishesChart', data.dishes, {
        label: 'Sold',
        horizontal: true,
        shortLabels: true,
        format: value => value + ' sold',
        tick: value => value
    });
})();
