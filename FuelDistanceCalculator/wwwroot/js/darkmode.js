document.addEventListener("DOMContentLoaded", function () {
    const toggleButton = document.getElementById("darkModeToggle");
    const body = document.body;
    const table = document.getElementById("fuelTable");
    const mapContainer = document.getElementById("map_div");
    

    // Dark Mode Status aus dem Local Storage abrufen
    if (localStorage.getItem("darkMode") === "enabled") {
        body.classList.add("dark-mode");
        toggleButton.textContent = "☀️ Mode";
        localStorage.setItem("darkMode", "enabled");
        toggleButton.classList.remove("btn-outline-dark");
        toggleButton.classList.add("btn-outline-light");
    }

    if(table){
        if (body.classList.contains("dark-mode")){
            table.classList.add("table-dark");
        }
        else{
            table.classList.remove("table-dark");
        }
    }

    toggleButton.addEventListener("click", function () {
        body.classList.toggle("dark-mode");

        // Zustand speichern und Button-Text ändern
        if (body.classList.contains("dark-mode")) {
            localStorage.setItem("darkMode", "enabled");
            toggleButton.textContent = "☀️ Mode";
            toggleButton.classList.remove("btn-outline-dark");
            toggleButton.classList.add("btn-outline-light");
            table?.classList.add("table-dark");
            if (mapContainer && mapContainer.classList) {
                mapContainer.classList.add("dark-map");
            }
        } else {
            localStorage.setItem("darkMode", "disabled");
            toggleButton.textContent = "🌙 Mode";
            toggleButton.classList.remove("btn-outline-light");
            toggleButton.classList.add("btn-outline-dark");
            table?.classList.remove("table-dark");
            // 💡 Kartenlayer umschalten
            if (mapContainer && mapContainer.classList) {
                mapContainer.classList.remove("dark-map");
            }
        }
    });
    


});