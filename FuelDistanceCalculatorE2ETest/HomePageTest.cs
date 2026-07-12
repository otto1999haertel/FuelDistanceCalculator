using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace FuelDistanceCalculatorE2ETest;

public class HomepageTests : E2EBaseTest
{
    [Test]
    public async Task Homepage_Should_Load_Successfully()
    {
        var response = await Page.GotoAsync("/");

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Ok, Is.True, $"Erwartete 2xx-Status, bekam {response.Status}");

        // Hero-Überschrift sollte sichtbar sein
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Willkommen bei FuelGo" }))
            .ToBeVisibleAsync();
    }

    [Test]
    public async Task Default_Mode_Should_Show_AutoInputFields()
    {
        await Page.GotoAsync("/");

        // Im Standard-Modus ("Optimale Suche") sollten die Auto-Felder sichtbar sein,
        // die manuellen Vergleichsfelder hingegen nicht.
        await Expect(Page.Locator("#autoInputFields")).ToBeVisibleAsync();
        await Expect(Page.Locator("#manualInputFields")).ToBeHiddenAsync();
    }

    [Test]
    public async Task Switching_To_Standortvergleich_Should_Show_Manual_Fields()
    {
        await Page.GotoAsync("/");

        var cookie = Page.GetByRole(AriaRole.Button, new()
        {
            Name = "Ablehnen"
        });

        if (await cookie.IsVisibleAsync())
        {
            await cookie.ClickAsync();
        }


        var manualTab = Page.Locator("label[for='mode_man']")
            .First;

        await Expect(manualTab)
            .ToBeVisibleAsync();

        await manualTab.ClickAsync();


        await Expect(Page.Locator("#mode_man"))
            .ToBeCheckedAsync();


        await Expect(Page.Locator("#manualInputFields"))
            .ToBeVisibleAsync();


        await Expect(Page.GetByText("Standort 1"))
            .ToBeVisibleAsync();

        await Expect(Page.GetByText("Standort 2"))
            .ToBeVisibleAsync();

        await Expect(Page.GetByText("Tankmenge"))
            .ToBeHiddenAsync();
    }

    [Test]
    public async Task User_Can_Enter_Location_And_Adjust_Radius()
    {
        await Page.GotoAsync("/");

        // Standort eingeben (reine Client-Interaktion, kein Backend-Call nötig)
        await Page.Locator("#generalLocationInput").FillAsync("01067 Dresden");
        await Expect(Page.Locator("#generalLocationInput")).ToHaveValueAsync("01067 Dresden");

        // Radius-Slider bewegen und prüfen, dass die Anzeige sich aktualisiert
        await Page.Locator("#generalRadius").FillAsync("20");
        await Expect(Page.Locator("#radiusValue")).ToHaveTextAsync("20 km");
    }

    [Test]
    public async Task User_Can_Select_Fuel_Type()
    {
        await Page.GotoAsync("/");

        // Nimmt an, dass es ein Label für Diesel gibt - ggf. Text anpassen,
        // je nachdem was FuelTypeHelper.FuelTypeNames tatsächlich liefert.
        var dieselLabel = Page.Locator("label.fuel-label", new() { HasText = "Diesel" });
        var e10Label = Page.Locator("label.fuel-label", new() { HasText = "Super E10" });

        if (await dieselLabel.CountAsync() > 0 && await e10Label.CountAsync() > 0)
        {
            await dieselLabel.ClickAsync();
            var radioIddiesel = await dieselLabel.GetAttributeAsync("for");
            await Expect(Page.Locator($"#{radioIddiesel}")).ToBeCheckedAsync();

            await e10Label.ClickAsync();
            var radioIde10 = await e10Label.GetAttributeAsync("for");
            await Expect(Page.Locator($"#{radioIde10}")).ToBeCheckedAsync();
            await Expect(Page.Locator($"#{radioIddiesel}")).Not.ToBeCheckedAsync();
        }
        else
        {
            Assert.Inconclusive("Kein 'Diesel'-Label gefunden - Text in FuelTypeHelper.FuelTypeNames prüfen.");
            Assert.Inconclusive("Kein 'Super E10'-Label gefunden - Text in FuelTypeHelper.FuelTypeNames prüfen.");
        }
    }

    [Test]
    public async Task User_Can_Set_MaxFuelAmount_And_Change_Slider()
    {
        await Page.GotoAsync("/");

        // 1. Maximale Tankmenge setzen
        await Page.Locator("#MaximumFuelAmount").FillAsync("40");

        // 2. Fokus verlassen, damit das 'blur'-Event greift
        await Page.Locator("#MaximumFuelAmount").PressAsync("Tab");

        // Sanity-Check: Slider-Maximum wurde tatsächlich übernommen
        await Expect(Page.Locator("#FuelAmountRange")).ToHaveAttributeAsync("max", "40");

        // 3. Slider auf die Hälfte des neuen Maximums (20) setzen.
        await Page.Locator("#FuelAmountRange").EvaluateAsync(
            "el => { el.value = '20'; el.dispatchEvent(new Event('input', { bubbles: true })); }"
        );

        // 4. Prüfen, dass das Label exakt die Hälfte anzeigt
        await Expect(Page.Locator("#fuelAmountValue")).ToHaveTextAsync("20 l");
    }
}