globalThis.trakmark = globalThis.trakmark || {};

globalThis.trakmark.formatLocalTimes = function () {
    document.querySelectorAll("time[data-utc]").forEach((el) => {
        const parsed = new Date(el.dataset.utc);
        if (Number.isNaN(parsed.getTime())) {
            return;
        }

        const local = parsed.toLocaleString();
        if (el.textContent !== local) {
            el.textContent = local;
        }
    });
};
