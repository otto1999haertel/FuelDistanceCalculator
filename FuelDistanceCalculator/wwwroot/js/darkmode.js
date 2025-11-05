document.addEventListener("DOMContentLoaded", function () {
    const toggleButton = document.getElementById("darkModeToggle");
    const autoButton = document.getElementById("autoDarkModeToggle");
    if (!toggleButton || !autoButton) return;

    const body = document.body;
    const table = document.getElementById("fuelTable");
    const mapContainer = document.getElementById("map_div");

    // Funktion, um Mode zu setzen
    function setMode(mode) {  // 'dark', 'light', or 'auto'
        body.classList.remove("dark-mode", "light-mode");
        table?.classList.remove("table-dark");  // Optional, since vars handle it
        mapContainer?.classList.remove("dark-map");

        if (mode === "dark") {
            body.classList.add("dark-mode");
            table?.classList.add("table-dark");
            mapContainer?.classList.add("dark-map");
        } else if (mode === "light") {
            body.classList.add("light-mode");
        }  // For 'auto', do nothing—let media query handle

        // Button-Styles anpassen (basierend auf aktuellen Mode)
        const isDark = body.classList.contains("dark-mode");
        toggleButton.classList.toggle("btn-outline-light", isDark);
        toggleButton.classList.toggle("btn-outline-dark", !isDark);
        autoButton.classList.toggle("btn-outline-light", isDark);
        autoButton.classList.toggle("btn-outline-dark", !isDark);
    }

    // Funktion, um den aktiven Modus zu highlighten
    function highlightActiveMode(storedMode) {
        if (storedMode === "auto") {
            autoButton.classList.add("active");
            toggleButton.classList.remove("active");
            toggleButton.textContent = "Toggle Mode";
        } else {
            autoButton.classList.remove("active");
            toggleButton.classList.add("active");
            toggleButton.textContent = (storedMode === "dark") ? "☀️ Mode" : "🌙 Mode";
        }
    }

    // Status aus Local Storage abrufen
    const storedMode = localStorage.getItem("darkMode");  // Now 'dark', 'light', or null for auto

    if (storedMode) {
        setMode(storedMode);
        highlightActiveMode(storedMode);
    } else {
        // Auto: System prüfen
        const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
        setMode(prefersDark ? "dark" : "light");  // But since auto, actually set nothing
        setMode(null);  // Clear classes for auto
        highlightActiveMode("auto");
    }

    // Initial-Anpassung für Tabelle und Karte
    if (table && body.classList.contains("dark-mode")) {
        table.classList.add("table-dark");
    }
    if (mapContainer && body.classList.contains("dark-mode")) {
        mapContainer.classList.add("dark-map");
    }

    // Toggle-Button-Event: Wechselt manuell
    toggleButton.addEventListener("click", function () {
        let currentMode = localStorage.getItem("darkMode");
        let newMode = (currentMode === "dark" || !currentMode) ? "light" : "dark";  // Toggle between dark/light
        setMode(newMode);
        localStorage.setItem("darkMode", newMode);
        highlightActiveMode(newMode);
    });

    // Auto-Button-Event
    autoButton.addEventListener("click", function () {
        localStorage.setItem("darkMode", "auto");
        const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
        setMode(null);  // Clear classes
        highlightActiveMode("auto");
    });

    // Listener für System-Änderungen (nur in Auto)
    window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", function (e) {
        if (localStorage.getItem("darkMode") === "auto") {
            setMode(null);  // Rely on media
        }
    });

    // Contextmenu Reset
    toggleButton.addEventListener("contextmenu", function (e) {
        e.preventDefault();
        localStorage.setItem("darkMode", "auto");
        setMode(null);
        highlightActiveMode("auto");
        alert("Zurückgesetzt auf System-Modus");
    });
});