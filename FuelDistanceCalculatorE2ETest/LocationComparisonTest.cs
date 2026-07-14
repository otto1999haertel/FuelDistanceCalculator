using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace FuelDistanceCalculatorE2ETest;

public class LocationComparisonTest : E2EBaseTest
{
    private static List<string> locationNames;

    [Test]
    public async Task MaxAmountOfCompareLocationsShouldBeFour()
    {
        var manualTab = Page.Locator("label[for='mode_man']").First;
        await manualTab.ClickAsync();

        var addLocationButton = Page.Locator("#addStandort");

        for (int i = 0; i < 2; i++)
        {
            Console.WriteLine($"Adding location {i}");

            await Expect(addLocationButton).ToBeEnabledAsync();

            var textBtn = await addLocationButton.InnerTextAsync();
            Assert.That(textBtn.Trim(), Does.Contain("Standort hinzufügen"));

            await addLocationButton.ClickAsync();
        }

        await Expect(addLocationButton).ToBeDisabledAsync();
        await Expect(addLocationButton).ToHaveTextAsync("Maximale Standorte erreicht");
    }

    [Test]
    public async Task ComparingTwoLocationsShouldDisplayAverageCosts()
    {
        locationNames = new List<string>();
        var manualTab = Page.Locator("label[for='mode_man']").First;

        await manualTab.ClickAsync();

        await SettingUpTwoCompareLocations();

        await Page.Locator("#compareLocations").ClickAsync();


        var averageCheckElement = Page.Locator("#averageCost");
        await Expect(averageCheckElement).ToBeVisibleAsync();
        await Expect(averageCheckElement)
            .ToContainTextAsync("Durchschnittspreise");

        // id 0: compareLocationItem-0
        //id 1: compareLocationItem-1
        locationNames.Reverse();
        for(int i=0; i<locationNames.Count; i++)
        {
            var averageLocationElement = Page.Locator($"#compareLocationItem-{i}");
            string averageLocationText = await averageLocationElement.InnerTextAsync();
            string expectedPattern = @$"{locationNames[i]}: [0-9]+,[0-9]+ €";
            Assert.That(averageLocationText, Does.Match(expectedPattern), $"Erwarteter Text nicht gefunden. Tatsächlicher Text: '{averageLocationText}'");
        }

    }

    private async Task SettingUpTwoCompareLocations()
    {
        var standort1Radius = Page.Locator("#RadiusPlace0");
        await standort1Radius.FillAsync("15");

        var standort2Radius = Page.Locator("#RadiusPlace1");
        await standort2Radius.FillAsync("20");

        var standort1Input = Page.Locator("#FirstComparePlace");
        await standort1Input.FillAsync("02994");
        locationNames.Add("02994");

        var standort2Input = Page.Locator("#SecondComparePlace");
        await standort2Input.FillAsync("91052");
        locationNames.Add("91052");

    }

}