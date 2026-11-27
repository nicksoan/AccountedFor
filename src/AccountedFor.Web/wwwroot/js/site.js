// FIDOS terminal behaviour: PF keys, cursor placement, clock and journal totals.
(() => {
    "use strict";

    // PF keys: any element tagged data-pf="F3" etc. is "pressed" by the matching function key.
    document.addEventListener("keydown", (e) => {
        if (!/^F([1-9]|1[0-2])$/.test(e.key) || e.ctrlKey || e.altKey || e.metaKey) {
            return;
        }
        const target = document.querySelector(`[data-pf="${e.key}"]`);
        if (target) {
            e.preventDefault();
            target.click();
        }
    });

    // Prefer the command line, or the first screen input when it is unavailable.
    const command = document.getElementById("cmd");
    const first = command
        || document.querySelector("main [autofocus]")
        || document.querySelector("main input.fld:not([type=hidden]):not([readonly]), main select.fld");
    first?.focus({ preventScroll: true });

    if (command) {
        document.addEventListener("click", (e) => {
            if (!e.target.closest("a, button, input, select, textarea, label, summary, [contenteditable]")) {
                command.focus({ preventScroll: true });
            }
        });
    }

    const guide = document.querySelector('[data-remember-guide="true"]');
    if (guide) {
        try {
            guide.open = sessionStorage.getItem("fidos.guide-open") === "1";
            guide.addEventListener("toggle", () => sessionStorage.setItem("fidos.guide-open", guide.open ? "1" : "0"));
        } catch {
            // The guide still opens without browser storage.
        }
    }

    // Clock.
    const clock = document.getElementById("clock");
    if (clock) {
        const pad = (n) => String(n).padStart(2, "0");
        const tick = () => {
            const d = new Date();
            clock.textContent = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
        };
        setInterval(tick, 15000);
    }

    // Show account names next to account code fields.
    const names = new Map([...document.querySelectorAll("#accounts option")].map((o) => [o.value, o.label]));
    const showName = (input) => {
        const out = document.getElementById(input.dataset.name);
        if (out) {
            const code = input.value.trim();
            out.textContent = code ? (names.get(code) || "** UNKNOWN ACCOUNT **") : "";
            out.classList.toggle("rd", !!code && !names.has(code));
        }
    };
    document.querySelectorAll("input[data-name]").forEach((input) => {
        input.addEventListener("input", () => showName(input));
        showName(input);
    });

    // Live debit/credit totals on journal entry.
    const form = document.querySelector("form[data-journal]");
    if (form) {
        const parse = (v) => {
            const n = parseFloat(String(v).replace(/,/g, ""));
            return Number.isFinite(n) ? Math.round(n * 100) : 0;
        };
        const fmt = (cents) => (cents / 100).toLocaleString("en-GB", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        const update = () => {
            let dr = 0, cr = 0;
            form.querySelectorAll("input.dr").forEach((i) => { dr += parse(i.value); });
            form.querySelectorAll("input.cr").forEach((i) => { cr += parse(i.value); });
            document.getElementById("tot-dr").textContent = fmt(dr);
            document.getElementById("tot-cr").textContent = fmt(cr);
            const status = document.getElementById("tot-status");
            if (dr === 0 && cr === 0) {
                status.textContent = "";
            } else if (dr === cr) {
                status.textContent = "IN BALANCE";
                status.className = "ye";
            } else {
                status.textContent = `OUT OF BALANCE BY ${fmt(Math.abs(dr - cr))}`;
                status.className = "rd";
            }
        };
        form.addEventListener("input", update);
        update();
    }
})();
