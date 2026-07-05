window.trakmark = window.trakmark || {};

window.trakmark.formatLocalTimes = function () {
    document.querySelectorAll("time[data-utc]").forEach(function (el) {
        var parsed = new Date(el.getAttribute("data-utc"));
        if (!isNaN(parsed.getTime())) {
            el.textContent = parsed.toLocaleString();
        }
    });
};
