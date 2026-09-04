document.addEventListener("submit", event => {
    const form = event.target;

    if (!(form instanceof HTMLFormElement) || form.dataset.submissionLoading !== "true" || !form.checkValidity()) {
        return;
    }

    form.querySelectorAll("button[type='submit'], input[type='submit']").forEach(button => {
        if (button.disabled) {
            return;
        }

        button.disabled = true;
        button.setAttribute("aria-busy", "true");
        button.setAttribute("aria-label", "Processando");
    });
});
