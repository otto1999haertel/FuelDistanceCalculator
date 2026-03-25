using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services;

namespace FuelDistanceCalculatorTest.ServiceTests;

public class SortServiceTests : ServiceTestBase
{
    public override async Task Setup()
    {
        _fakeGasStationList = new List<GasStation>
            {
                new GasStation
                {
                    Id = "1",
                    Name = "Station A",
                    FuelTypePrice = 1.50m,
                    TotalCalculatedCoast = 10.00m,
                    Dist = 5.0,
                    Fuels = new List<Fuel>
                    {
                        new Fuel { Name = "Diesel", Price = (double?)1.50m, LastChange = new LastChange { Amount = (double)-0.01m, Timestamp = "2025-10-23T10:00:00+02" } }
                    },
                    LastUpdate = "2025-10-23T10:00:00+02",
                    Country = "de",
                    Brand = "SUPOL",
                    Street = "Street A",
                    PostalCode = "12345",
                    Place = "City A",
                    Coords = new Coordinates { Lat = 49.0, Lng = 11.0 },
                    IsOpen = true,
                    ClosesAt = "2025-10-23T22:00:00+02",
                    Volatility = 200
                },
                new GasStation
                {
                    Id = "2",
                    Name = "Station B",
                    FuelTypePrice = 1.40m,
                    TotalCalculatedCoast = 12.00m,
                    Dist = 3.0,
                    Fuels = new List<Fuel>
                    {
                        new Fuel { Name = "Diesel", Price = (double?)1.40m, LastChange = new LastChange { Amount = (double)-0.02m, Timestamp = "2025-10-23T10:00:00+02" } }
                    },
                    LastUpdate = "2025-10-23T10:00:00+02",
                    Country = "de",
                    Brand = "ARAL",
                    Street = "Street B",
                    PostalCode = "67890",
                    Place = "City B",
                    Coords = new Coordinates { Lat = 49.1, Lng = 11.1 },
                    IsOpen = true,
                    ClosesAt = "2025-10-23T22:00:00+02",
                    Volatility = 210
                },
                new GasStation
                {
                    Id = "3",
                    Name = "Station C",
                    FuelTypePrice = 1.60m,
                    TotalCalculatedCoast = 8.00m,
                    Dist = 7.0,
                    Fuels = new List<Fuel>
                    {
                        new Fuel { Name = "Diesel", Price = (double?)1.60m, LastChange = new LastChange { Amount = (double)-0.03m, Timestamp = "2025-10-23T10:00:00+02" } }
                    },
                    LastUpdate = "2025-10-23T10:00:00+02",
                    Country = "de",
                    Brand = "ELO",
                    Street = "Street C",
                    PostalCode = "54321",
                    Place = "City C",
                    Coords = new Coordinates { Lat = 49.2, Lng = 11.2 },
                    IsOpen = true,
                    ClosesAt = "2025-10-23T22:00:00+02",
                    Volatility = 220
                }
            };
    }
    [Test]
    public void SortStations_FuelPrice_ReturnsStationsSortedByFuelPrice()
    {
        // Arrange
        string sortMode = "fuelPrice";

        // Act
        var result = SortService.SortStations(_fakeGasStationList, sortMode);

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.ByVal(result.Select(s => s.FuelTypePrice), Is.Ordered);
        Assert.That(result[0].Name.Equals("Station B"), Is.True); // FuelTypePrice: 1.40
        Assert.That(result[1].Name.Equals("Station A"), Is.True); // FuelTypePrice: 1.50
        Assert.That(result[2].Name.Equals("Station C"), Is.True); // FuelTypePrice: 1.60
    }

    [Test]
    public void SortStations_TotalCost_ReturnsStationsSortedByTotalCost()
    {
        // Arrange
        string sortMode = "totalCost";

        // Act
        var result = SortService.SortStations(_fakeGasStationList, sortMode);

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.ByVal(result.Select(s => s.TotalCalculatedCoast), Is.Ordered);
        Assert.That(result[0].Name.Equals("Station C"), Is.True); // FuelTypePrice: 1.40
        Assert.That(result[1].Name.Equals("Station A"), Is.True); // FuelTypePrice: 1.50
        Assert.That(result[2].Name.Equals("Station B"), Is.True); // FuelTypePrice: 1.60
    }

    [Test]
    public void SortStations_Distance_ReturnsStationsSortedByDistance()
    {
        // Arrange
        string sortMode = "distance";

        // Act
        var result = SortService.SortStations(_fakeGasStationList, sortMode);

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.ByVal(result.Select(s => s.Dist), Is.Ordered);
        Assert.That(result[0].Name.Equals("Station B"), Is.True); // FuelTypePrice: 1.40
        Assert.That(result[1].Name.Equals("Station A"), Is.True); // FuelTypePrice: 1.50
        Assert.That(result[2].Name.Equals("Station C"), Is.True); // FuelTypePrice: 1.60
    }

    [Test]
    public void SortStations_InvalidSortMode_ReturnsUnmodifiedList()
    {
        // Arrange
        string sortMode = "invalid";
        var originalOrder = _fakeGasStationList.ToList();

        // Act
        var result = SortService.SortStations(_fakeGasStationList, sortMode);

        // Assert
        Assert.That(result.Count.Equals(3), Is.True);
        Assert.That(result[0].Name.Equals(originalOrder[0].Name), Is.True);
    }

    [Test]
    public void SortStations_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        string sortMode = "fuelPrice";
        var emptyList = new List<GasStation>();

        // Act
        var result = SortService.SortStations(emptyList, sortMode);

        // Assert
        Assert.That(result.Count.Equals(0), Is.True);
    }

    [Test]
    public void SortStations_NullSortMode_ReturnsUnmodifiedList()
    {
        // Arrange
        string sortMode = null;
        var originalOrder = _fakeGasStationList.ToList();

        // Act
        var result = SortService.SortStations(_fakeGasStationList, sortMode);

        // Assert
        Assert.That(result.Count.Equals(3), Is.True);
        Assert.That(result[0].Id, Is.EqualTo(originalOrder[0].Id));
    }
}