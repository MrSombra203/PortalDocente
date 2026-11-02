// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// The dashboard remains usable without JavaScript; this only enhances mobile navigation.
(() => {
    const toggle = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("sidebar");

    if (!toggle || !sidebar) return;

    const closeMenu = () => {
        document.body.classList.remove("sidebar-open");
        toggle.setAttribute("aria-expanded", "false");
        toggle.setAttribute("aria-label", "Abrir menú");
        document.querySelector(".sidebar-overlay")?.remove();
    };

    toggle.addEventListener("click", () => {
        const open = document.body.classList.toggle("sidebar-open");
        toggle.setAttribute("aria-expanded", String(open));
        toggle.setAttribute("aria-label", open ? "Cerrar menú" : "Abrir menú");

        if (open) {
            const overlay = document.createElement("button");
            overlay.type = "button";
            overlay.className = "sidebar-overlay";
            overlay.setAttribute("aria-label", "Cerrar menú");
            overlay.addEventListener("click", closeMenu, { once: true });
            document.body.append(overlay);
        } else {
            document.querySelector(".sidebar-overlay")?.remove();
        }
    });

    sidebar.addEventListener("click", (event) => {
        if (event.target.closest("a")) closeMenu();
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") closeMenu();
    });
})();

(() => {
    document.addEventListener("click", (event) => {
        if (!(event.target instanceof Element)) return;

        const clearButton = event.target.closest("[data-clear-student-search]");
        if (!(clearButton instanceof HTMLButtonElement)) return;

        const form = clearButton.closest("form");
        const input = form?.querySelector('input[name="q"]');
        if (!(form instanceof HTMLFormElement) || !(input instanceof HTMLInputElement)) return;

        input.value = "";
        form.requestSubmit();
    });
})();

(() => {
    const loader = document.getElementById("pageLoader");
    const message = loader?.querySelector("[data-loading-message]");
    const announcement = loader?.querySelector("[data-loading-announcement]");
    if (!loader || !(message instanceof HTMLElement) || !(announcement instanceof HTMLElement)) return;

    let navegacionEnCurso = false;

    const showLoader = (loadingMessage) => {
        message.textContent = loadingMessage;
        announcement.textContent = loadingMessage;
        loader.setAttribute("aria-label", loadingMessage);
        loader.hidden = false;
        document.body.setAttribute("aria-busy", "true");
    };

    const restoreForm = (form) => {
        form.removeAttribute("aria-busy");
        delete form.dataset.submitting;
        form.querySelectorAll("[data-previously-disabled]").forEach((control) => {
            if (!(control instanceof HTMLButtonElement || control instanceof HTMLInputElement)) return;

            control.disabled = control.dataset.previouslyDisabled === "true";
            delete control.dataset.previouslyDisabled;
        });
    };

    const hideLoader = () => {
        loader.hidden = true;
        document.body.removeAttribute("aria-busy");
        navegacionEnCurso = false;
        document.querySelectorAll("form[data-submitting='true']").forEach(restoreForm);
    };

    document.addEventListener("submit", (event) => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement)) return;

        if (navegacionEnCurso || form.dataset.submitting === "true") {
            event.preventDefault();
            return;
        }

        if (event.defaultPrevented
            || form.method.toLowerCase() === "dialog"
            || form.target && form.target.toLowerCase() !== "_self"
            || event.submitter instanceof HTMLElement
                && "formTarget" in event.submitter
                && event.submitter.formTarget
                && event.submitter.formTarget.toLowerCase() !== "_self") {
            return;
        }

        if (form.hasAttribute("data-confirm-delete")
            && !window.confirm("¿Eliminar esta cuenta?")) {
            event.preventDefault();
            return;
        }

        const submitter = event.submitter;
        const loadingMessage = form.dataset.loadingMessage
            || submitter?.dataset.loadingMessage
            || "Procesando solicitud…";
        navegacionEnCurso = true;
        form.dataset.submitting = "true";
        form.setAttribute("aria-busy", "true");
        showLoader(loadingMessage);

        form.querySelectorAll("button[type='submit'], input[type='submit'], input[type='image']")
            .forEach((control) => {
                if (!(control instanceof HTMLButtonElement || control instanceof HTMLInputElement)
                    || control === submitter && control.name) {
                    return;
                }

                control.dataset.previouslyDisabled = String(control.disabled);
                control.disabled = true;
            });
    });

    document.addEventListener("click", (event) => {
        if (!(event.target instanceof Element)
            || event.defaultPrevented
            || event.button !== 0
            || event.metaKey
            || event.ctrlKey
            || event.shiftKey
            || event.altKey) {
            return;
        }

        const link = event.target.closest("a[href]");
        if (!link
            || link.hasAttribute("download")
            || link.target && link.target.toLowerCase() !== "_self"
            || link.relList.contains("external")) {
            return;
        }

        if (navegacionEnCurso) {
            event.preventDefault();
            return;
        }

        const destination = new URL(link.href, window.location.href);
        if (destination.origin !== window.location.origin
            || destination.pathname === window.location.pathname
                && destination.search === window.location.search) {
            return;
        }

        navegacionEnCurso = true;
        showLoader(link.dataset.loadingMessage || "Cargando información…");
    });

    window.addEventListener("pageshow", hideLoader);
})();
