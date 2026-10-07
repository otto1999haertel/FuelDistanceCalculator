using System.ComponentModel.DataAnnotations;
using FuelDistanceCalculator.Interfaces;
using FuelDistanceCalculator.Pages;
using FuelDistanceCalculator.Services;
using Moq;

namespace FuelDistanceCalculatorTest.SecurityTests;

[TestFixture]
public class IndexModelValidationUnitTests
{
    private IndexModel CreateIndexModel()
    {
        var loggerMock = new Mock<ILogger<IndexModel>>();
        var fuelPriceService = new FuelPriceService();
        var marketServiceMock = new Mock<IMarketFuelPriceService>();
        var geoServiceMock = new Mock<IGeoLocationService>();
        var oilServiceMock = new Mock<IOilPriceService>();
        var configMock = new Mock<IConfiguration>();

        var model = new IndexModel(
            loggerMock.Object,
            fuelPriceService,
            marketServiceMock.Object,
            geoServiceMock.Object,
            oilServiceMock.Object,
            configMock.Object)
        {
            // Ensure collections and required defaults are instantiated so custom attributes don't throw NullReferenceException
            NamePlaces = new List<string> { "" },
            RadiusPlaces = new List<double> { 10 }
        };
        model.Radius = 1;
        model.Place = "TestPlace";

        return model;
    }

    private bool ValidateModel(IndexModel model, out List<ValidationResult> results)
    {
        var context = new ValidationContext(model, serviceProvider: null, items: null);
        results = new List<ValidationResult>();
        return Validator.TryValidateObject(model, context, results, validateAllProperties: true);
    }

    [TestCase("<script>alert(1)</script>")]
    [TestCase("Berlin; DROP TABLE Stations;--")]
    [TestCase("PlaceWith'Quote")]
    public void Place_WithInvalidCharacters_FailsValidation(string invalidPlace)
    {
        var model = CreateIndexModel();
        model.Place = invalidPlace;

        bool isValid = ValidateModel(model, out var results);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(IndexModel.Place))), Is.True);
    }

    [TestCase("Berlin")]
    [TestCase("München - Stadtzentrum / 12345")]
    [TestCase("St. Gallen, Parkplatz 2.")]
    public void Place_WithValidCharacters_PassesValidation(string validPlace)
    {
        var model = CreateIndexModel();
        model.Place = validPlace;

        bool isValid = ValidateModel(model, out var results);

        Assert.That(isValid, Is.True);
        Assert.That(results, Is.Empty);
    }

    [TestCase("10 %")]
    [TestCase("5")]
    [TestCase("12.5 %")]
    public void Discount_WithValidFormat_PassesValidation(string? validDiscount)
    {
        var model = CreateIndexModel();
        model.DiscountPercentOrAbsolute = validDiscount;

        bool isValid = ValidateModel(model, out _);

        Assert.That(isValid, Is.True);
    }

    [TestCase("INVALID_TEXT")]
    [TestCase("1000000000%")]
    [TestCase("10%<script>")]
    public void Discount_WithInvalidFormat_FailsValidation(string invalidDiscount)
    {
        var model = CreateIndexModel();
        model.DiscountPercentOrAbsolute = invalidDiscount;

        bool isValid = ValidateModel(model, out var results);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(IndexModel.DiscountPercentOrAbsolute))), Is.True);
    }

    [Test]
    public void StationBrand_ExceedingMaxLength_FailsValidation()
    {
        var model = CreateIndexModel();
        model.StationBrand = new string('A', 51);

        bool isValid = ValidateModel(model, out var results);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(IndexModel.StationBrand))), Is.True);
    }

    [TestCase("<script>alert('Aral')</script>")]
    [TestCase("Aral; DROP TABLE Aral;--")]
    [TestCase("Aral'Aral")]
    public void StationBrand_WithInvalidCharacters_FailsValidation(string invalidBrand)
    {
        var model = CreateIndexModel();
        model.StationBrand = invalidBrand;

        bool isValid = ValidateModel(model, out var results);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(IndexModel.StationBrand))), Is.True);
    }

    [TestCase("Passat")]
    public void SelectedCarType_WithInvalidCarType_FailsValidation(string validCarType)
    {
        var model = CreateIndexModel();
        model.SelectedCarType = validCarType;
        bool isValid = ValidateModel(model, out _);

        Assert.That(isValid, Is.False);
    }

    [TestCase("Mercedes-Benz C 300 e T-Modell Avantgarde Advanced 9G-TRONIC 230 kW")]
    public void SelectedCarType_WithValidCarType_PassesValidation(string validCarType)
    {
        var model = CreateIndexModel();
        model.SelectedCarType = validCarType;
        bool isValid = ValidateModel(model, out _);

        Assert.That(isValid, Is.True);
    }
}