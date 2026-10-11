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
        document.querySelector(".sidebar-overlay")?.remove();
    };

    toggle.addEventListener("click", () => {
        const open = document.body.classList.toggle("sidebar-open");
        toggle.setAttribute("aria-expanded", String(open));

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
