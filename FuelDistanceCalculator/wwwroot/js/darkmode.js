document.addEventListener("DOMContentLoaded", function () {
    const toggleButton = document.getElementById("darkModeToggle");
    const autoButton = document.getElementById("autoDarkModeToggle");
    if (!toggleButton || !autoButton) return;

    const root = document.documentElement;  // Änderung: Verwende <html> statt <body> für Klassen
    const table = document.getElementById("fuelTable");
    const mapContainer = document.getElementById("map_div");

    // Funktion, um Mode zu setzen
    function setMode(mode) {  // 'dark', 'light', or 'auto'
        root.classList.remove("dark-mode", "light-mode");  // Änderung: Auf root anwenden
        table?.classList.remove("table-dark");
        mapContainer?.classList.remove("dark-map");

        if (mode === "dark") {
            root.classList.add("dark-mode");  // Änderung: Auf root
            table?.classList.add("table-dark");
            mapContainer?.classList.add("dark-map");
        } else if (mode === "light") {
            root.classList.add("light-mode");  // Änderung: Auf root
        }  // For 'auto', do nothing—let media query handle

        // Button-Styles anpassen (basierend auf dem Modus)
        const isDark = root.classList.contains("dark-mode");  // Änderung: Auf root prüfen
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
    const storedMode = localStorage.getItem("darkMode");  // 'dark', 'light', or null for auto

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
    if (table && root.classList.contains("dark-mode")) {  // Änderung: Auf root prüfen
        table.classList.add("table-dark");
    }
    if (mapContainer && root.classList.contains("dark-mode")) {  // Änderung: Auf root prüfen
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