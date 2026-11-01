// PB22: admin dashboard behaviour.
// 1. The filter form reloads the report as soon as From, To or Group by changes (no "Show report" button).
// 2. Draws the charts with Chart.js. The page puts the numbers in a <script type="application/json"
//    id="dashboardData"> block; this file only draws them. Every chart is a single series in the site
//    accent colour, so no legend is needed (the card title names it).
(function () {
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

    // Shared look: thin rounded bars, quiet grid, no legend, hover tooltip on each bar
    function barChart(canvasId, series, options) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        const horizontal = options.horizontal === true;
        const valueAxis = horizontal ? 'x' : 'y';
        const labelAxis = horizontal ? 'y' : 'x';

        new Chart(canvas, {
            type: 'bar',
            data: {
                labels: series.labels,
                datasets: [{
                    label: options.label,
                    data: series.values,
                    backgroundColor: accent,
                    hoverBackgroundColor: accentDark,
                    borderRadius: 4,
                    borderSkipped: 'start',   // round only the data end, the end away from the baseline
                    maxBarThickness: 40
                }]
            },
            options: {
                indexAxis: horizontal ? 'y' : 'x',
                maintainAspectRatio: false,
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
                        ticks: { autoSkip: true, maxRotation: 0 }
                    }
                }
            }
        });
    }

    barChart('salesChart', data.sales, {
        label: 'Sales',
        format: money,
        tick: value => 'RM ' + value
    });

    barChart('bookingsChart', data.bookings, {
        label: 'Bookings',
        format: value => value + (value === 1 ? ' booking' : ' bookings'),
        tick: value => value
    });

    barChart('dishesChart', data.dishes, {
        label: 'Sold',
        horizontal: true,
        format: value => value + ' sold',
        tick: value => value
    });
})();
