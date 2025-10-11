document.addEventListener("DOMContentLoaded", function () {
    const toggleButton = document.getElementById("darkModeToggle");
    const autoButton = document.getElementById("autoDarkModeToggle");
    if (!toggleButton || !autoButton) return;  // Früher Abbruch, falls Buttons nicht existieren

    const body = document.body;
    const table = document.getElementById("fuelTable");
    const mapContainer = document.getElementById("map_div");

    // Funktion, um Dark Mode zu aktivieren/deaktivieren
    function setDarkMode(enabled) {
        if (enabled) {
            body.classList.add("dark-mode");
            table?.classList.add("table-dark");
            if (mapContainer && mapContainer.classList) {
                mapContainer.classList.add("dark-map");
            }
        } else {
            body.classList.remove("dark-mode");
            table?.classList.remove("table-dark");
            if (mapContainer && mapContainer.classList) {
                mapContainer.classList.remove("dark-map");
            }
        }

        // Button-Styles anpassen basierend auf dem Modus
        toggleButton.classList.toggle("btn-outline-light", enabled);
        toggleButton.classList.toggle("btn-outline-dark", !enabled);
        autoButton.classList.toggle("btn-outline-light", enabled);
        autoButton.classList.toggle("btn-outline-dark", !enabled);
    }

    // Funktion, um den aktiven Modus zu highlighten
    function highlightActiveMode(mode) {
        if (mode === "auto") {
            autoButton.classList.add("active");
            toggleButton.classList.remove("active");
            toggleButton.textContent = "Toggle Mode";  // Standard-Text für Toggle im Auto-Modus
        } else {
            autoButton.classList.remove("active");
            toggleButton.classList.add("active");
            toggleButton.textContent = (mode === "enabled") ? "☀️ Mode" : "🌙 Mode";
        }
    }

    // Dark Mode Status aus dem Local Storage abrufen
    const storedMode = localStorage.getItem("darkMode");

    if (storedMode) {
        // Manueller Override: enabled oder disabled
        setDarkMode(storedMode === "enabled");
        highlightActiveMode(storedMode);
    } else {
        // System-Präferenz prüfen (Auto-Modus)
        const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
        setDarkMode(prefersDark);
        highlightActiveMode("auto");
    }

    // Explizite Initial-Anpassung für Tabelle und Karte (wie im alten Code)
    if (table) {
        if (body.classList.contains("dark-mode")) {
            table.classList.add("table-dark");
        } else {
            table.classList.remove("table-dark");
        }
    }
    if (mapContainer && mapContainer.classList) {
        if (body.classList.contains("dark-mode")) {
            mapContainer.classList.add("dark-map");
        } else {
            mapContainer.classList.remove("dark-map");
        }
    }

    // Toggle-Button-Event: Wechselt zwischen Dark und Light (manuell)
    toggleButton.addEventListener("click", function () {
        const storedMode = localStorage.getItem("darkMode");
        const isEnabled = storedMode === "enabled";
        const newMode = isEnabled ? "disabled" : "enabled";
        setDarkMode(!isEnabled);
        localStorage.setItem("darkMode", newMode);
        highlightActiveMode(newMode);
    });

    // Auto-Button-Event: Schaltet auf Auto-Modus
    autoButton.addEventListener("click", function () {
        localStorage.removeItem("darkMode");
        const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
        setDarkMode(prefersDark);
        highlightActiveMode("auto");
    });

    // Listener für Änderungen am System-Modus: Nur im Auto-Modus
    window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", function (e) {
        if (!localStorage.getItem("darkMode")) {  // Nur im "System-Modus"
            setDarkMode(e.matches);
        }
    });

    // Optional: Reset auf System-Modus via Contextmenu (auf Toggle-Button)
    toggleButton.addEventListener("contextmenu", function (e) {
        e.preventDefault();
        localStorage.removeItem("darkMode");
        const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
        setDarkMode(prefersDark);
        highlightActiveMode("auto");
        alert("Zurückgesetzt auf System-Modus");  // Oder eine Toast-Nachricht
    });
});