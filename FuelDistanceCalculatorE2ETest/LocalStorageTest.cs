using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace FuelDistanceCalculatorE2ETest;

public class LocalStorageTest : E2EBaseTest
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
}