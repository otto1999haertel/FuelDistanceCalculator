using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace FuelDistanceCalculatorE2ETest;

public class OptimalSerachTest : E2EBaseTest
{
    [Test]
    [Ignore("Test is not implemented yet")]
    public async Task OptimalSearch_Without_Max_FuelAmount_Displays_PricePerLiter()
    {
        await Page.GotoAsync("/");

        // 3. Start- und Zielort setzen
        await Page.Locator("#generalLocationInput").FillAsync("Dresdem");

        // 4. Berechnung starten SearchBtn
        await Page.Locator("#SearchBtn").ClickAsync();

        // 5. Warten auf die Anzeige der Ergebnisse
        //TODO: Regex Einzelelement teste: * Einzelpreis pro Liter * : [0-9]+,[0-9]+ €
        string regexPattern = @".* Einzelpreis pro Liter .*: [0-9]+,[0-9]+ €";


    }
}