using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace FuelDistanceCalculatorE2ETest;

public class CacheTest : E2EBaseTest
{
    [Test]
    public async Task Fuel_Should_Be_Cached_Test()
    {
        await Page.GotoAsync("/");

        var dieselLabel = Page.Locator("label.fuel-label", new() { HasText = "Diesel" });
        var e10Label = Page.Locator("label.fuel-label", new() { HasText = "Super E10" });

        if (await dieselLabel.CountAsync() > 0 && await e10Label.CountAsync() > 0)
        {
            await e10Label.ClickAsync();
            await Page.ReloadAsync();

            var radioIddiesel = await dieselLabel.GetAttributeAsync("for");

            var radioIde10 = await e10Label.GetAttributeAsync("for");

            await Expect(Page.Locator($"#{radioIde10}")).ToBeCheckedAsync();
            await Expect(Page.Locator($"#{radioIddiesel}")).Not.ToBeCheckedAsync();

        }
    }

    [Test]
    public async Task PricePerKm_Should_Be_Cached_After_Reload_Test()
    {
        await Page.GotoAsync("/");
        var expendingSettingsButton = Page.Locator("#advancedToggleButton");

        await expendingSettingsButton.ClickAsync();

        var priceInput = Page.Locator("#PricePerKm");

        await priceInput.FillAsync("1.25");

        await priceInput.PressAsync("Tab");

        await Page.ReloadAsync();

        await expendingSettingsButton.ClickAsync();

        await Expect(priceInput).ToHaveValueAsync("1.25");
    }

    [Test]
    public async Task MaxFuelAmount_Should_Be_Cached_After_Reload_Test()
    {
        await Page.GotoAsync("/");

        var maxFuelInput = Page.Locator("#MaximumFuelAmount");

        await maxFuelInput.FillAsync("40");

        await maxFuelInput.PressAsync("Tab");

        await Page.ReloadAsync();

        await Expect(maxFuelInput).ToHaveValueAsync("40");
    }

    [Test]
    public async Task SettingUpCompareLoctionsShouldCachePLZandRadius_Test()
    {
        await Page.GotoAsync("/");

        ClickCompareLocationsTab();

        var standort1Input = Page.Locator("#FirstComparePlace");

        var standort1Radius = Page.Locator("#RadiusPlace0");

        await standort1Input.FillAsync("01067");

        await standort1Radius.FillAsync("15");

        var standort2Input = Page.Locator("#SecondComparePlace");

        var standort2Radius = Page.Locator("#RadiusPlace1");

        await standort2Input.FillAsync("01069");

        await standort2Radius.FillAsync("20");

        await Page.ReloadAsync();

        ClickCompareLocationsTab();

        await Expect(standort1Input).ToHaveValueAsync("01067");
        await Expect(standort1Radius).ToHaveValueAsync("15");
        await Expect(standort2Input).ToHaveValueAsync("01069");
        await Expect(standort2Radius).ToHaveValueAsync("20");

    }

    private async Task ClickCompareLocationsTab()
    {
        var manualTab = Page.Locator("label[for='mode_man']")
            .First;

        await Expect(manualTab)
            .ToBeVisibleAsync();

        await manualTab.ClickAsync();
    }
}