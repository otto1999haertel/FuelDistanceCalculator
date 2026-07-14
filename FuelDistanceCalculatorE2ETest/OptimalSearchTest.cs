using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace FuelDistanceCalculatorE2ETest;

public class OptimalSearchTest : E2EBaseTest
{
    [Test]
    public async Task OptimalSearch_Without_Max_FuelAmount_Displays_PricePerLiter()
    {
        await Page.GotoAsync("/");

        // 3. Start- und Zielort setzen
        await Page.Locator("#generalLocationInput").FillAsync("Dresden");

        // 4. Berechnung starten SearchBtn
        await Page.Locator("#SearchBtn").ClickAsync();

        // 5. Warten auf die Anzeige der Ergebnisse
        string regexPattern = @".* Einzelpreis pro Liter .*: [0-9]+,[0-9]+ €";

        var firstSummary = Page.Locator("#station-summary-item-0");

        string summaryText = await firstSummary.InnerTextAsync();
        summaryText = summaryText.Replace("\n", " ").Trim();

        Assert.That(summaryText, Does.Match(regexPattern),
            $"Erwarteter Text nicht gefunden. Tatsächlicher Text: '{summaryText}'");
    }
}