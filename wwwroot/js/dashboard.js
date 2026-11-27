// Dashboard sidebar behaviour. Kept in a static file so the
// Content-Security-Policy can forbid inline scripts.
document.addEventListener("DOMContentLoaded", function () {
    const sidebar = document.getElementById("sidebar");
    const wrapper = document.getElementById("wrapper");
    const overlay = document.getElementById("sidebarOverlay");
    const desktopToggleBtn = document.getElementById("sidebarCollapseDesktop");
    const mobileToggleBtn = document.getElementById("sidebarCollapseMobile");

    if (!sidebar || !wrapper || !overlay || !desktopToggleBtn || !mobileToggleBtn) {
        return;
    }

    desktopToggleBtn.addEventListener("click", function () {
        sidebar.classList.toggle("collapsed");
        wrapper.classList.toggle("sidebar-collapsed");

        const icon = desktopToggleBtn.querySelector("i");
        if (!icon) {
            return;
        }
        if (sidebar.classList.contains("collapsed")) {
            icon.classList.remove("fa-bars-staggered");
            icon.classList.add("fa-bars");
        } else {
            icon.classList.remove("fa-bars");
            icon.classList.add("fa-bars-staggered");
        }
    });

    function toggleMobileSidebar() {
        sidebar.classList.toggle("active");
        overlay.classList.toggle("active");
    }

    mobileToggleBtn.addEventListener("click", toggleMobileSidebar);
    overlay.addEventListener("click", toggleMobileSidebar);
});
