using FuelDistanceCalculator.Model;
using Microsoft.IdentityModel.Tokens;

namespace FuelDistanceCalculator.Services;

public static class TankCostService
{
    public static void CaluclateSavings(List<GasStation> stations, ref decimal SavingsToNearestStation, ref decimal SavingsToCheapestStation)
    {
        if (stations.IsNullOrEmpty()) return;
        Console.WriteLine($"Calculating savings from {stations.Count} Stations");
        GasStation cheapestStationTotalCost = stations.OrderBy(x => x.TotalCalculatedCoast).FirstOrDefault();
        GasStation nearestStation = stations.OrderBy(x => x.Dist).FirstOrDefault();
        GasStation cheapestFuelCost = stations.OrderBy(x => x.FuelTypePrice).FirstOrDefault();
        SavingsToCheapestStation = cheapestFuelCost.TotalCalculatedCoast - cheapestStationTotalCost.TotalCalculatedCoast;
        SavingsToNearestStation = nearestStation.TotalCalculatedCoast - cheapestStationTotalCost.TotalCalculatedCoast;
    }

    public static List<GasStation> GetCheapestStation(List<GasStation> gasStations,decimal pricePerKm, decimal fuelAmount, string fuelTypeForAPI, string stationBrand="", string discountPercentOrAbsolute="")
    {
        List<GasStation> CheapestResultStations = new List<GasStation>();
        if (string.IsNullOrEmpty(discountPercentOrAbsolute))
        {
            discountPercentOrAbsolute = "0";
        }
        if (fuelAmount <= 0 && DiscountParser.TryParseDiscountPercent(discountPercentOrAbsolute, out decimal discouuntValue))
        {
            Console.WriteLine($"Parsed discount value: {discouuntValue}");
            CheapestResultStations = GetCheapestStationDiscountPerCent(gasStations, fuelTypeForAPI, stationBrand, discouuntValue);
        }
        //Prozentuale Rabatt oder Absoluter Rabatt
        else if (fuelAmount > 0)
        {
            if (DiscountParser.TryParseDiscountPercent(discountPercentOrAbsolute, out decimal discountDecimal) || decimal.TryParse(discountPercentOrAbsolute, out discountDecimal))
            {
                Console.WriteLine($"Parsed discount value: {discountDecimal}");
                CheapestResultStations = GetCheapestStationsTotalCostDiscountRelAbs(gasStations, fuelAmount, pricePerKm, fuelTypeForAPI, stationBrand, discountDecimal);
            }
            Console.WriteLine("Could not be parsed no calculation");
        }
        else if (fuelAmount == 0)
        {
            CheapestResultStations = GetCheapestStationDiscountPerCent(gasStations, fuelTypeForAPI, stationBrand);
        }

        return CheapestResultStations;

    }

    private static List<GasStation> GetCheapestStationDiscountPerCent(List<GasStation> stations, string fuelType, string stationBrand = "", decimal dicountPercent = 0)
    {
        if (stations.IsNullOrEmpty() || dicountPercent < 0 || dicountPercent > 100)
        {
            Console.WriteLine("Keine Stationen verfügbar.");
            return stations;
        }
        Console.WriteLine("FuelAmount <= 0, sortiere nach FuelTypePrice und Dist.");
        var gasStations = stations
            .AsParallel() // Parallele Verarbeitung
            .Where(station => station.IsOpen && station.Fuels != null)
            .Select(station =>
            {
                station.SetPriceWithPercentageDiscount(fuelType, stationBrand, dicountPercent); // Setze FuelTypePrice basierend auf fuelType
                station.SetUpdateTime(fuelType); // Setze LastUpdate
                station.SetUpdateAmount(fuelType); // Setze UpdateAmount
                return station;
            })
            .ToList();

        return gasStations
            .OrderBy(station => station.FuelTypePrice ?? decimal.MaxValue).ToList(); // Primär: Aufsteigend nach Preis // Sekundär: Aufsteigend nach Entfernung
    }

    private static List<GasStation> GetCheapestStationsTotalCostDiscountRelAbs(List<GasStation> stations, decimal fuelAmount, decimal costPerKm, string fuelType, string stationBrand = "", decimal discountAmount = 0)
    {
        // Wenn fuelAmount <= 0, sortiere nach FuelTypePrice und Dist
        if (stations.IsNullOrEmpty())
        {
            Console.WriteLine("Keine Stationen verfügbar.");
            return new List<GasStation>();
        }
        if (fuelAmount <= 0)
        {
            return GetCheapestStationDiscountPerCent(stations, fuelType, stationBrand, discountAmount);
        }

        Console.WriteLine($"Parallel working started for {stations.Count} stations with fuelType: {fuelType}");
        var stationCosts = stations
            .AsParallel() // Aktiviert parallele Verarbeitung
            .Where(station => station.IsOpen && station.Fuels != null && station.Dist.HasValue) // Filtert offene Tankstellen mit gültigem Dist
            .Select(station =>
            {
                // Setze FuelTypePrice und LastUpdate
                station.SetPriceWithPercentageDiscount(fuelType, stationBrand, discountAmount);
                station.SetUpdateTime(fuelType);
                station.SetUpdateAmount(fuelType);

                // Berechne TotalCalculatedCoast
                decimal totalCost = 0;
                if (station.FuelTypePrice.HasValue && station.FuelTypePrice > 0)
                {
                    try
                    {
                        totalCost = station.CalculateTotalCostDoubleWayWithDiscountGreaterOne(fuelAmount, costPerKm, stationBrand, discountAmount);
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine($"Fehler bei {station.Name}: {ex.Message}");
                        totalCost = decimal.MaxValue; // Setze hohen Wert für ungültige Stationen
                    }
                }
                else
                {
                    Console.WriteLine($"Kein gültiger FuelTypePrice für {station.Name}, FuelTypePrice: {station.FuelTypePrice}");
                    totalCost = decimal.MaxValue;
                }

                return (Station: station, TotalCost: totalCost);
            })
            .ToList();

        Console.WriteLine($"Anzahl TS in Total Cost Calculation: {stationCosts.Count}");

        // Debugging: Protokolliere die berechneten Werte
        foreach (var sc in stationCosts)
        {
            Console.WriteLine($"Station: {sc.Station.Name}, FuelTypePrice: {sc.Station.FuelTypePrice}, TotalCalculatedCoast: {sc.Station.TotalCalculatedCoast}, LastUpdate: {sc.Station.LastUpdate}");
        }

        // Sortiere nach TotalCalculatedCoast
        return stationCosts
            .OrderBy(sc => sc.TotalCost)
            .Select(sc => sc.Station)
            .ToList();
    }
}