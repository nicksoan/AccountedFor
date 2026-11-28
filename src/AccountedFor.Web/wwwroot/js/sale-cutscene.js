(() => {
    "use strict";

    const open = document.getElementById("cutscene-open");
    const dialog = document.getElementById("sale-cutscene");
    if (!open || !dialog || typeof dialog.showModal !== "function") return;

    const slides = [...dialog.querySelectorAll(".cutscene-slide")];
    const previous = dialog.querySelector("[data-cutscene-prev]");
    const next = dialog.querySelector("[data-cutscene-next]");
    const play = dialog.querySelector("[data-cutscene-play]");
    const status = dialog.querySelector("[data-cutscene-status]");
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    let index = 0;
    let playing = false;
    let timer;

    const render = () => {
        slides.forEach((slide, i) => { slide.hidden = i !== index; });
        previous.setAttribute("aria-disabled", String(index === 0));
        next.setAttribute("aria-disabled", String(index === slides.length - 1));
        play.textContent = playing ? "PAUSE" : "PLAY";
        status.textContent = `SCENE ${index + 1} / ${slides.length}`;
    };
    const stop = () => {
        clearTimeout(timer);
        playing = false;
        render();
    };
    const schedule = () => {
        clearTimeout(timer);
        if (playing && index < slides.length - 1) {
            timer = setTimeout(() => {
                index++;
                if (index === slides.length - 1) playing = false;
                render();
                schedule();
            }, 5000);
        }
    };
    const start = () => {
        index = 0;
        playing = !reducedMotion.matches;
        render();
        schedule();
    };

    open.hidden = false;
    open.addEventListener("click", () => {
        start();
        dialog.showModal();
    });
    dialog.querySelector("[data-cutscene-close]").addEventListener("click", () => dialog.close());
    dialog.addEventListener("close", () => { stop(); open.focus(); });
    dialog.addEventListener("keydown", (event) => {
        if (/^F([1-9]|1[0-2])$/.test(event.key)) {
            event.preventDefault();
            event.stopPropagation();
        }
    });
    previous.addEventListener("click", () => { if (index > 0) { stop(); index--; render(); } });
    next.addEventListener("click", () => { if (index < slides.length - 1) { stop(); index++; render(); } });
    play.addEventListener("click", () => {
        if (playing) {
            stop();
        } else {
            if (index === slides.length - 1) index = 0;
            playing = true;
            render();
            schedule();
        }
    });
    dialog.querySelector("[data-cutscene-replay]").addEventListener("click", start);
})();
