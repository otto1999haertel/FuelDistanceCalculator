using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace FuelDistanceCalculatorE2ETest;

public class LocationComparisonTest : E2EBaseTest
{
    private static List<string> locationNames;
    private static List<int> locationRadius;

    [Test]
    public async Task MaxAmountOfCompareLocationsShouldBeFour()
    {
        OpenComparisonTab();

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
        locationRadius = new List<int>();
        OpenComparisonTab();
        await SettingUpTwoCompareLocations();

        await ExecuteCompareAndCheckHeading();

        // id 0: compareLocationItem-0
        //id 1: compareLocationItem-1
        locationNames.Reverse();
        CheckResultView();

    }

    [Test]
    public async Task CompareMaxLocationShouldWork()
    {
        locationNames = new List<string>();
        locationRadius = new List<int>();
        OpenComparisonTab();
        SettingUpTwoCompareLocations();
        var addLocationButton = Page.Locator("#addStandort");
        await addLocationButton.ClickAsync();
        await addLocationButton.ClickAsync();
        await SettingUpRestForMaxAmountCompareLocations();
        await ExecuteCompareAndCheckHeading();
        locationNames.Reverse();
        CheckPlacesAndRadiusInputField();
        CheckResultView();
    }

    private async Task OpenComparisonTab()
    {
        var manualTab = Page.Locator("label[for='mode_man']").First;
        await manualTab.ClickAsync();
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
        //NamePlaces[2] RadiusPlace2
        //NamePlaces[3] RadiusPlace3

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
        await Page.Locator("#compareLocations").ClickAsync();
        var averageCheckElement = Page.Locator("#averageCost");
        await Expect(averageCheckElement).ToBeVisibleAsync();
        await Expect(averageCheckElement)
            .ToContainTextAsync("Durchschnittspreise");
    }

    private async Task CheckPlacesAndRadiusInputField()
    {
        List<string> comparePlacesAfterExecution = new List<string>();
        List<int> compareRadiusAfterExecution = new List<int>();
        string comparePlace = await Page.Locator("#FirstComparePlace").InputValueAsync();
        comparePlacesAfterExecution.Add(comparePlace);
        int.TryParse((await Page.Locator("#RadiusPlace0Lbl").InnerTextAsync()).Split(' ')[0], out int radius);
        compareRadiusAfterExecution.Add(radius);

        comparePlace = await Page.Locator("#SecondComparePlace").InputValueAsync();
        comparePlacesAfterExecution.Add(comparePlace);
        int.TryParse((await Page.Locator("#RadiusPlace1Lbl").InnerTextAsync()).Split(' ')[0],out radius);
        compareRadiusAfterExecution.Add(radius);

        comparePlace = await Page.Locator("#NamePlaces\\[2\\]").InputValueAsync();
        comparePlacesAfterExecution.Add(comparePlace);
        int.TryParse((await Page.Locator("#outputRadius2").InnerTextAsync()).Split(' ')[0],out radius);
        compareRadiusAfterExecution.Add(radius);

        comparePlace = await Page.Locator("#NamePlaces\\[3\\]").InputValueAsync();
        comparePlacesAfterExecution.Add(comparePlace);
        int.TryParse((await Page.Locator("#outputRadius3").InnerTextAsync()).Split(' ')[0],out radius);
        compareRadiusAfterExecution.Add(radius);

        for(int i=0; i<locationNames.Count(); i++)
        {
            Assert.That(comparePlacesAfterExecution[i].Equals(locationNames[i]));
            Assert.That(compareRadiusAfterExecution[i].Equals(locationRadius[i]));
        }
    }

    private async Task CheckResultView()
    {
        for(int i=0; i<locationNames.Count; i++)
        {
            var averageLocationElement = Page.Locator($"#compareLocationItem-{i}");
            string averageLocationText = await averageLocationElement.InnerTextAsync();
            string expectedPattern = @$"{locationNames[i]}: [0-9]+,[0-9]+ €";
            Assert.That(averageLocationText, Does.Match(expectedPattern), $"Erwarteter Text nicht gefunden. Tatsächlicher Text: '{averageLocationText}'");
        }
    }
}