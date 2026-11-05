using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace FuelDistanceCalculatorE2ETest;

public class LocationComparisonTest : E2EBaseTest
{
    // Instance fields (no longer static) and reset before every test,
    // so tests cannot leak state into each other.
    private List<string> locationNames = new();
    private List<int> locationRadius = new();

    // Input field selectors, index = location number (0..3)
    private static readonly string[] PlaceInputSelectors =
    {
        "#FirstComparePlace",
        "#SecondComparePlace",
        "#NamePlaces\\[2\\]",
        "#NamePlaces\\[3\\]"
    };

    [SetUp]
    public void ResetLocationState()
    {
        locationNames = new List<string>();
        locationRadius = new List<int>();
    }

    [Test]
    public async Task MaxAmountOfCompareLocationsShouldBeFour()
    {
        await OpenComparisonTab();

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
        await OpenComparisonTab();
        await SettingUpTwoCompareLocations();

        await ExecuteCompareAndCheckHeading();

        // The order of the result items is not guaranteed to match the input order,
        // so results are matched by location name (no Reverse()).
        await CheckResultView();
    }

    [Test]
    public async Task CompareMaxLocationShouldWork()
    {
        await OpenComparisonTab();
        await SettingUpTwoCompareLocations();

        var addLocationButton = Page.Locator("#addStandort");
        await addLocationButton.ClickAsync();
        await addLocationButton.ClickAsync();

        await SettingUpRestForMaxAmountCompareLocations();
        await ExecuteCompareAndCheckHeading();

        // The input fields keep the input order -> compare against the original order.
        await CheckPlacesAndRadiusInputField();
        await CheckResultView();
    }

    [Test]
    public async Task SwitchingFuelTypeInCompareShouldWork()
    {
        // Switch to manual comparison mode
        await OpenComparisonTab();

        // Select Super E5
        await Page.Locator("label[for='fuel_SuperE5']").ClickAsync();

        // Set up the two locations
        await SettingUpTwoCompareLocations();

        // Make sure the manual comparison form is visible
        await Expect(Page.Locator("#manualInputFields")).ToBeVisibleAsync();
        await Expect(Page.Locator("#compareLocations")).ToBeVisibleAsync();

        // Act - First comparison with Super E5
        Console.WriteLine("FIRST COMPARISON");

        await ExecuteCompareAndCheckHeading();

        Dictionary<string, string> averageCostsBeforeSwitch = await CheckResultView();

        // Switch fuel type to Diesel
        await Page.Locator("label[for='fuel_Diesel']").ClickAsync();

        // Verify that Diesel is selected
        await Expect(Page.Locator("#fuel_Diesel")).ToBeCheckedAsync();

        // Make sure the comparison button is still visible
        await Expect(Page.Locator("#compareLocations")).ToBeVisibleAsync();

        // Act - Second comparison with Diesel
        Console.WriteLine("SECOND COMPARISON");

        await ExecuteCompareAndCheckHeading();

        Dictionary<string, string> averageCostsAfterSwitch = await CheckResultView();

        // Assert - compare the same location before and after the switch
        var name = locationNames[0];
        Assert.That(
            averageCostsAfterSwitch[name],
            Is.Not.EqualTo(averageCostsBeforeSwitch[name]),
            $"Average costs for {name} should differ when switching from Super E5 to Diesel."
        );
    }

    private async Task OpenComparisonTab()
    {
        var manualTab = Page.Locator("label[for='mode_man']").First;

        await manualTab.ClickAsync();

        await Expect(Page.Locator("#manualInputFields")).ToBeVisibleAsync();
    }

    private async Task SettingUpTwoCompareLocations()
    {
        var standort1Radius = Page.Locator("#RadiusPlace0");
        await standort1Radius.FillAsync("15");
        locationRadius.Add(15);

        var standort2Radius = Page.Locator("#RadiusPlace1");
        await standort2Radius.FillAsync("20");
        locationRadius.Add(20);

        var standort1Input = Page.Locator("#FirstComparePlace");
        await standort1Input.FillAsync("02994");
        locationNames.Add("02994");

        var standort2Input = Page.Locator("#SecondComparePlace");
        await standort2Input.FillAsync("91052");
        locationNames.Add("91052");
    }

    private async Task SettingUpRestForMaxAmountCompareLocations()
    {
        // NamePlaces[2] / RadiusPlace2
        // NamePlaces[3] / RadiusPlace3

        var standort2Radius = Page.Locator("#RadiusPlace2");
        await standort2Radius.FillAsync("5");
        locationRadius.Add(5);

        var standort3Radius = Page.Locator("#RadiusPlace3");
        await standort3Radius.FillAsync("10");
        locationRadius.Add(10);

        var standort2Input = Page.Locator("#NamePlaces\\[2\\]");
        await standort2Input.FillAsync("01936");
        locationNames.Add("01936");

        var standort3Input = Page.Locator("#NamePlaces\\[3\\]");
        await standort3Input.FillAsync("01067");
        locationNames.Add("01067");
    }

    private async Task ExecuteCompareAndCheckHeading()
    {
        var compareButton = Page.Locator("#compareLocations");

        await Expect(compareButton).ToBeVisibleAsync();

        // The compare button does a full form POST. Wait for that response and the
        // page load, otherwise on the second comparison we could still be looking at
        // the OLD result page (#averageCost is already visible there).
        await Page.RunAndWaitForResponseAsync(
            async () => await compareButton.ClickAsync(),
            r => r.Url.Contains("handler=CalculateAverageCost") && r.Request.Method == "POST");
        await Page.WaitForLoadStateAsync(LoadState.Load);

        var averageCheckElement = Page.Locator("#averageCost");

        await Expect(averageCheckElement).ToBeVisibleAsync();

        await Expect(averageCheckElement)
            .ToContainTextAsync("Durchschnittspreise");
    }

    private async Task CheckPlacesAndRadiusInputField()
    {
        var comparePlacesAfterExecution = new List<string>();
        var compareRadiusAfterExecution = new List<int>();

        for (int i = 0; i < locationNames.Count; i++)
        {
            string place = await Page.Locator(PlaceInputSelectors[i]).InputValueAsync();
            comparePlacesAfterExecution.Add(place);

            string radiusText = await Page.Locator($"#outputRadius{i}").InnerTextAsync();
            Assert.That(
                int.TryParse(radiusText.Trim().Split(' ')[0], out int radius),
                Is.True,
                $"Could not parse radius of location {i} from '{radiusText}'");
            compareRadiusAfterExecution.Add(radius);
        }

        for (int i = 0; i < locationNames.Count; i++)
        {
            Assert.That(
                comparePlacesAfterExecution[i],
                Is.EqualTo(locationNames[i]),
                $"Place input {i} has an unexpected value after the comparison.");
            Assert.That(
                compareRadiusAfterExecution[i],
                Is.EqualTo(locationRadius[i]),
                $"Radius of location {i} has an unexpected value after the comparison.");
        }
    }

    /// <summary>
    /// Checks that every entered location appears in the result list with a price
    /// like "02994: 1,710 €", independent of the order of the result items.
    /// Returns location name -> price text.
    /// </summary>
    private async Task<Dictionary<string, string>> CheckResultView()
    {
        var items = Page.Locator("[id^='compareLocationItem-']");
        await Expect(items).ToHaveCountAsync(locationNames.Count);

        var texts = await items.AllInnerTextsAsync();
        var costs = new Dictionary<string, string>();

        foreach (var name in locationNames)
        {
            var match = texts
                .Select(t => Regex.Match(t.Trim(), $@"^{Regex.Escape(name)}:\s*([0-9]+,[0-9]+)\s*€"))
                .FirstOrDefault(m => m.Success);

            Assert.That(
                match,
                Is.Not.Null,
                $"No result found for '{name}'. Actual items: {string.Join(" | ", texts)}");

            costs[name] = match!.Groups[1].Value;
        }

        return costs;
    }
}