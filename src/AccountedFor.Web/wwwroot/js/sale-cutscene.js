(() => {
    "use strict";

    const open = document.getElementById("cutscene-open");
    const dialog = document.getElementById("sale-cutscene");
    if (!open || !dialog || typeof dialog.showModal !== "function") return;

    const slides = [...dialog.querySelectorAll(".cutscene-slide")];
    const progress = [...dialog.querySelectorAll(".cutscene-progress li")];
    const forms = [...dialog.querySelectorAll("[data-cutscene-entry]")];
    const previous = dialog.querySelector("[data-cutscene-prev]");
    const next = dialog.querySelector("[data-cutscene-next]");
    const status = dialog.querySelector("[data-cutscene-status]");
    const recorded = new Map();
    let index = 0;

    const render = () => {
        slides.forEach((slide, i) => {
            slide.hidden = i !== index;
            slide.classList.toggle("is-active", i === index);
        });
        progress.forEach((step, i) => {
            if (i === index) step.setAttribute("aria-current", "step");
            else step.removeAttribute("aria-current");
            step.classList.toggle("is-complete", i < index);
        });
        previous.disabled = index === 0;
        next.hidden = index !== 0;
        status.textContent = `STEP ${index + 1} / ${slides.length}`;
        slides[index].focus();
    };

    const reset = () => {
        recorded.clear();
        forms.forEach((form) => {
            form.reset();
            form.querySelector(".cutscene-feedback").textContent = "";
            form.querySelectorAll("[aria-invalid]").forEach((field) => field.removeAttribute("aria-invalid"));
        });
        dialog.querySelectorAll("output").forEach((output) => { output.textContent = ""; });
        index = 0;
        render();
        slides[0].getAnimations({ subtree: true }).forEach((animation) => { animation.currentTime = 0; });
    };

    forms.forEach((form) => {
        const account = form.elements.namedItem("account");
        const amount = form.elements.namedItem("amount");
        const feedback = form.querySelector(".cutscene-feedback");
        const side = form.dataset.cutsceneEntry;

        form.addEventListener("input", () => {
            recorded.delete(side);
            if (side === "debit") recorded.delete("credit");
            feedback.textContent = "";
            account.removeAttribute("aria-invalid");
            amount.removeAttribute("aria-invalid");
        });
        form.addEventListener("submit", (event) => {
            event.preventDefault();
            const validAccount = account.value === form.dataset.account;
            const value = amount.value.trim();
            const validAmount = /^\d+(?:\.\d{1,2})?$/.test(value)
                && Number(value) === Number(form.dataset.amount);
            account.setAttribute("aria-invalid", String(!validAccount));
            amount.setAttribute("aria-invalid", String(!validAmount));
            if (!validAccount || !validAmount) {
                feedback.textContent = !validAccount
                    ? `Choose GL ${form.dataset.account} for the ${side}. Follow the account guidance above.`
                    : `Enter £${form.dataset.amount} for the ${side}. Both sides must match the customer's payment.`;
                (!validAccount ? account : amount).focus();
                return;
            }
            recorded.set(side, Number(value));
            if (side === "credit") {
                if (!recorded.has("debit")) {
                    index = 1;
                    render();
                    forms[0].querySelector(".cutscene-feedback").textContent =
                        "Record the Bank debit before completing the Sales credit.";
                    return;
                }
                for (const entrySide of ["debit", "credit"]) {
                    dialog.querySelector(`[data-cutscene-${entrySide}]`).textContent =
                        `£${recorded.get(entrySide).toFixed(2)}`;
                }
            }
            index++;
            render();
        });
    });

    open.hidden = false;
    open.addEventListener("click", () => {
        reset();
        dialog.showModal();
        slides[index].focus();
    });
    dialog.querySelector("[data-cutscene-close]").addEventListener("click", () => dialog.close());
    dialog.addEventListener("close", () => { open.focus(); });
    dialog.addEventListener("keydown", (event) => {
        if (/^F([1-9]|1[0-2])$/.test(event.key)) {
            event.preventDefault();
            event.stopPropagation();
        }
    });
    previous.addEventListener("click", () => { if (index > 0) { index--; render(); } });
    next.addEventListener("click", () => { if (index === 0) { index++; render(); } });
    dialog.querySelector("[data-cutscene-replay]").addEventListener("click", reset);
})();
