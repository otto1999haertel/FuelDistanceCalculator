document.addEventListener("DOMContentLoaded", function () {
    const toggleButton = document.getElementById("darkModeToggle");
    const autoButton = document.getElementById("autoDarkModeToggle");
    if (!toggleButton || !autoButton) return;

    const root = document.documentElement;  // Änderung: Verwende <html> statt <body> für Klassen
    const table = document.getElementById("fuelTable");
    const mapContainer = document.getElementById("map_div");

    // Funktion, um Mode zu setzen
    function setMode(mode) {
            root.classList.remove("dark-mode", "light-mode");
            table?.classList.remove("table-dark");
            mapContainer?.classList.remove("dark-map");

            let effectiveMode = mode;

            // Neu: Auto-Modus → prüfe System-Preference
            if (mode === "auto" || mode === null) {
                const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
                effectiveMode = prefersDark ? "dark" : "light";
            }

            if (effectiveMode === "dark") {
                root.classList.add("dark-mode");
                table?.classList.add("table-dark");
                mapContainer?.classList.add("dark-map");
            } else if (effectiveMode === "light") {
                root.classList.add("light-mode");
            }

            // Button-Styles
            const isDark = root.classList.contains("dark-mode");
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

    if (storedMode === "dark" || storedMode === "light") {
        setMode(storedMode);
        highlightActiveMode(storedMode);
    } else {
        // Auto: System prüfen
        localStorage.setItem("darkMode", "auto"); // optional: explizit setzen
        setMode("auto"); // ← Jetzt wird Klasse gesetzt!
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

    // Auto-Button
    autoButton.addEventListener("click", function () {
        localStorage.setItem("darkMode", "auto");
        setMode("auto"); // ← Klasse wird gesetzt
        highlightActiveMode("auto");
    });

    // Listener für System-Änderungen (nur in Auto)
        window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", function () {
            if (localStorage.getItem("darkMode") === "auto") {
                setMode("auto"); // ← Klasse wird neu gesetzt
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