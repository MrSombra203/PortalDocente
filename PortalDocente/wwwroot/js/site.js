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
    document.addEventListener("submit", (event) => {
        const form = event.target;
        if (form instanceof HTMLFormElement
            && form.hasAttribute("data-confirm-delete")
            && !window.confirm("¿Eliminar esta cuenta?")) {
            event.preventDefault();
        }
    });
})();

(() => {
    const loader = document.getElementById("pageLoader");
    if (!loader) return;

    const showLoader = () => {
        loader.hidden = false;
        document.body.setAttribute("aria-busy", "true");
    };

    const hideLoader = () => {
        loader.hidden = true;
        document.body.removeAttribute("aria-busy");
    };

    document.addEventListener("submit", (event) => {
        const form = event.target;
        if (event.defaultPrevented
            || !(form instanceof HTMLFormElement)
            || form.method.toLowerCase() === "dialog"
            || form.target && form.target.toLowerCase() !== "_self"
            || event.submitter instanceof HTMLButtonElement
                && event.submitter.formTarget
                && event.submitter.formTarget.toLowerCase() !== "_self") {
            return;
        }

        showLoader();
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

        const destination = new URL(link.href, window.location.href);
        if (destination.origin !== window.location.origin
            || destination.pathname === window.location.pathname
                && destination.search === window.location.search) {
            return;
        }

        showLoader();
    });

    window.addEventListener("pageshow", hideLoader);
})();
