using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services.Discount;
using Microsoft.IdentityModel.Tokens;

namespace FuelDistanceCalculator.Services;

public static class TankCostService
{
    public static void CaluclateSavings(List<GasStation> stations, ref decimal SavingsToNearestStation, ref decimal SavingsToCheapestStation)
    {
        if (stations.IsNullOrEmpty()) return;
        Console.WriteLine($"Calculating savings from {stations.Count} Stations");
        GasStation cheapestStationTotalCost = stations.OrderBy(x => x.TotalCombinedCosts).FirstOrDefault();
        GasStation nearestStation = stations.OrderBy(x => x.Dist).FirstOrDefault();
        GasStation cheapestFuelCost = stations.OrderBy(x => x.FuelTypePrice).FirstOrDefault();
        SavingsToCheapestStation = cheapestFuelCost.TotalCombinedCosts - cheapestStationTotalCost.TotalCombinedCosts;
        SavingsToNearestStation = nearestStation.TotalCombinedCosts - cheapestStationTotalCost.TotalCombinedCosts;
    }

    public static List<GasStation> GetCheapestStation(List<GasStation> gasStations, decimal pricePerKm, decimal fuelAmount, string fuelType, string stationBrand = "", DiscountApplier discountApplier = null)
    {
        List<GasStation> CheapestResultStations = new List<GasStation>();
        if(gasStations == null || gasStations.Count == 0)
        {
            return CheapestResultStations;
        }
        if (discountApplier != null)
        {
            foreach (var station in gasStations)
            {
                station.SetPrice(fuelType);
                discountApplier.ApplyDiscount(station);
            }
        }
        if (fuelAmount <= 0)
        {
            var stations = gasStations
                   .AsParallel() // Parallele Verarbeitung
                   .Where(station => station.IsOpen && station.Fuels != null)
                   .Select(station =>
                   {
                       station.SetUpdateTime(fuelType); // Setze LastUpdate
                       station.SetUpdateAmount(fuelType); // Setze UpdateAmount
                       return station;
                   })
                   .ToList();

            return gasStations
                .OrderBy(station => station.FuelTypePrice ?? decimal.MaxValue).ToList();
        }
        else
        {
            var stations = gasStations
                .AsParallel() // Parallele Verarbeitung
                .Where(station => station.IsOpen && station.Fuels != null)
                .Select(station =>
                {
                    station.SetUpdateTime(fuelType); // Setze LastUpdate
                    station.SetUpdateAmount(fuelType); // Setze UpdateAmount

                    decimal totalCost = station.CalculateTotalCostDoubleWayWithDiscountGreaterOne(fuelAmount, pricePerKm);
                    return (Station: station, TotalCost: totalCost);

                })
                .ToList();

            return stations
               .OrderBy(sc => sc.TotalCost)
               .ThenBy(sc => sc.Station.Dist)
               .Select(sc => sc.Station)
               .ToList();
        }
    }

}