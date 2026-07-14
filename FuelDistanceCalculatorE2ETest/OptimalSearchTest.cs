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

    [Test]
    public async Task OptimalSearch_With_Max_FuelAmount_Displays_GesamtKosten_And_Ersparnis()
    {
        await Page.GotoAsync("/");

        // 3. Start- und Zielort setzen
        await Page.Locator("#generalLocationInput").FillAsync("Dresden");

        // 1. Maximale Tankmenge setzen
        await Page.Locator("#MaximumFuelAmount").FillAsync("60");
        await Page.Locator("#MaximumFuelAmount").PressAsync("Tab");

        await Page.Locator("#FuelAmountRange").EvaluateAsync(
            "el => { el.value = '30'; el.dispatchEvent(new Event('input', { bubbles: true })); }"
        );

        await Expect(Page.Locator("#fuelAmountValue")).ToHaveTextAsync("30 l");


        // 4. Berechnung starten SearchBtn
        await Page.Locator("#SearchBtn").ClickAsync();

        await Page.Locator("#savings-to-cheapest-station").WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible
            });



        string savingsText = await Page.Locator("#savings-to-cheapest-station").InnerTextAsync();
        Assert.That(savingsText, Does.StartWith("Ersparnis zum günstigsten Einzelpreis im Radius:"), $"Erwarteter Text nicht gefunden. Tatsächlicher Text: '{savingsText}'");

        string nearestSavingsText = await Page.Locator("#savings-to-nearest-station").InnerTextAsync();
        Assert.That(nearestSavingsText, Does.StartWith("Ersparnis zur Nächsten im Radius:"), $"Erwarteter Text nicht gefunden. Tatsächlicher Text: '{nearestSavingsText}'");

        // 5. Warten auf die Anzeige der Ergebnisse
        string regexPattern = @".* Gesamtkosten .*: [0-9]+,[0-9]+ €";

        var firstSummary = Page.Locator("#station-summary-item-0");
        string summaryText = await firstSummary.InnerTextAsync();
        summaryText = summaryText.Replace("\n", " ").Trim();

        Assert.That(summaryText, Does.Match(regexPattern),
            $"Erwarteter Text nicht gefunden. Tatsächlicher Text: '{summaryText}'");
    }
}