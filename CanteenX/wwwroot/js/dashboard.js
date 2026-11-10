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


document.addEventListener("DOMContentLoaded",async function () {
try{
  const response = await fetch('/CanteenOwner/CanteenProfile');
   if(response.ok){
     const result = await response.json();
     const profile = result.data || result;
     
     if(profile && profile.ImageUrl){
      const imageElement = document.getElementById("navbarProfileImg");

      let finalUrl = profile.ImageUrl.startsWith("http")
        ? profile.ImageUrl
        : "https://localhost:7150" + profile.ImageUrl;

        imageElement.src = finalUrl;
     }

     if(profile && profile.CanteenName){
        document.getElementById("navbarCanteenName").textContent = profile.CanteenName;
   }
}
}
catch(error){
console.error("Profile load nahi ho payi:", error);
            document.getElementById("navbarProfileName").textContent = "Owner";
}
});