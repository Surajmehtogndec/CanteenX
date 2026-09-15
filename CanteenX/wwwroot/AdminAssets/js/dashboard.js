const menuToggle = document.getElementById("menuToggle");
const adminSidebar = document.getElementById("adminSidebar");
const sidebarOverlay = document.getElementById("sidebarOverlay");

if (menuToggle) {
    menuToggle.addEventListener("click", function () {
        adminSidebar.classList.toggle("show");
        sidebarOverlay.classList.toggle("show");
    });
}

if (sidebarOverlay) {
    sidebarOverlay.addEventListener("click", function () {
        adminSidebar.classList.remove("show");
        sidebarOverlay.classList.remove("show");
    });
}