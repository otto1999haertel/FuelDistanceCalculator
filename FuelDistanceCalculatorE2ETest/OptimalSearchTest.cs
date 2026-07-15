using System.Reflection;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace FuelDistanceCalculatorE2ETest;

public class OptimalSearchTest : E2EBaseTest
{
    [Test]
    public async Task OptimalSearch_Without_Max_FuelAmount_Displays_PricePerLiter_And_Details()
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

        CheckDetailsWithoutClosing(0);
        CheckDetailsWithoutClosing(2);
    }

    [Test]
    public async Task OptimalSearch_With_Max_FuelAmount_Displays_GesamtKosten_And_Ersparnis_And_Details()
    {
        await Page.GotoAsync("/");

        var expendingSettingsButton = Page.Locator("#advancedToggleButton");

        await expendingSettingsButton.ClickAsync();

        var priceInput = Page.Locator("#PricePerKm");

        await priceInput.FillAsync("0.23");

        await priceInput.PressAsync("Tab");

        // 3. Start- und Zielort setzen
        await Page.Locator("#generalLocationInput").FillAsync("Berlin");

        // 1. Maximale Tankmenge setzen
        await Page.Locator("#MaximumFuelAmount").FillAsync("60");
        await Page.Locator("#MaximumFuelAmount").PressAsync("Tab");

        await Page.Locator("#FuelAmountRange").EvaluateAsync(
            "el => { el.value = '30'; el.dispatchEvent(new Event('input', { bubbles: true })); }"
        );

        await Expect(Page.Locator("#fuelAmountValue")).ToHaveTextAsync("30 l");


        // 4. Berechnung starten SearchBtn
        await Page.Locator("#SearchBtn").ClickAsync();

        string html = await Page.ContentAsync();
        Console.WriteLine("=== PAGE HTML ===");
        Console.WriteLine(html);
        Console.WriteLine("=== END HTML ===");

        string savingsText = await Page.Locator("#savings-to-cheapest-station").InnerTextAsync();
        Assert.That(savingsText, Does.StartWith("Ersparnis zum günstigsten Einzelpreis im Radius:"), $"Erwarteter Text nicht gefunden. Tatsächlicher Text: '{savingsText}'");

        string nearestSavingsText = await Page.Locator("#savings-to-nearest-station").InnerTextAsync();
        Assert.That(nearestSavingsText, Does.StartWith("Ersparnis zur Nächsten im Radius:"), $"Erwarteter Text nicht gefunden. Tatsächlicher Text: '{nearestSavingsText}'");

        // 5. Warten auf die Anzeige der Ergebnisse
        string regexPattern = @".*Gesamtkosten:\s*\d+,\d+\s*€";

        var firstSummary = Page.Locator("#station-summary-item-0");
        string summaryText = await firstSummary.InnerTextAsync();
        summaryText = summaryText.Replace("\n", " ").Trim();

        Assert.That(summaryText, Does.Match(regexPattern),
            $"Erwarteter Text nicht gefunden. Tatsächlicher Text: '{summaryText}'");
        
        CheckDetailsWithoutClosing(0);
        CheckDetailsWithoutClosing(2);
    }

    private async Task CheckDetailsWithoutClosing(int itemIndex)
    {
        Console.WriteLine("Check Details without closing executed");
        await Page.Locator($"#station-summary-item-{itemIndex}").ClickAsync();
        //station-details-item
        var stationDetails = Page.Locator($"#station-details-item-{itemIndex}");
        string lastUpdate = await Page.Locator($"#lastUpdateDisplay-{itemIndex}").InnerTextAsync();
        string lastUpdatePattern = @"Letzte Aktualisierung: [0-9]{2}:[0-9]{2} Uhr um * \s*\d+,\d+\s*€";
        Assert.That(lastUpdate, Does.Match(lastUpdatePattern), $"Erwarteter Text nicht gefunden. Tatsächlicher Text: '{lastUpdate}'");

        var mapsLink = Page.Locator($"#gMapsLinkLocation-{itemIndex}");
        await Expect(mapsLink).ToBeVisibleAsync();

        var href = await mapsLink.GetAttributeAsync("href");
        Assert.That(href, Does.StartWith("https://www.google.com/maps?q="));

        await Expect(mapsLink).ToHaveAttributeAsync("target", "_blank");
        await Expect(mapsLink).ToBeEnabledAsync();

    }
}