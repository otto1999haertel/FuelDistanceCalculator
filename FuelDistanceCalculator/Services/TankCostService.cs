using FuelDistanceCalculator.Model;

public static class TankCostService
{
    public static void CaluclateSavings(List<GasStation> stations, ref decimal SavingsToNearestStation, ref decimal SavingsToCheapestStation)
    {
        GasStation cheapestStationTotalCost = stations.OrderBy(x => x.TotalCalculatedCoast).FirstOrDefault();
        GasStation nearestStation = stations.OrderBy(x => x.Dist).FirstOrDefault();
        GasStation cheapestFuelCost = stations.OrderBy(x => x.FuelTypePrice).FirstOrDefault();
        SavingsToCheapestStation = cheapestFuelCost.TotalCalculatedCoast - cheapestStationTotalCost.TotalCalculatedCoast;
        SavingsToNearestStation = nearestStation.TotalCalculatedCoast - cheapestStationTotalCost.TotalCalculatedCoast;
    }
    public static List<GasStation> GetCheapestStations(List<GasStation> stations, decimal fuelAmount, decimal costPerKm, string fuelType)
    {
        if (stations == null || !stations.Any())
        {
            Console.WriteLine("Keine Tankstellen vorhanden, leere Liste wird zurückgegeben.");
            return new List<GasStation>();
        }

        // Wenn fuelAmount <= 0, sortiere nach FuelTypePrice und Dist
        if (fuelAmount <= 0)
        {
            Console.WriteLine("FuelAmount <= 0, sortiere nach FuelTypePrice und Dist.");
            return stations
                .AsParallel() // Parallele Verarbeitung
                .Where(station => station.IsOpen && station.Fuels != null)
                .Select(station =>
                {
                    station.SetPrice(fuelType); // Setze FuelTypePrice basierend auf fuelType
                    station.SetUpdateTime(fuelType); // Setze LastUpdate
                    return station;
                })
                .OrderBy(station => station.FuelTypePrice ?? decimal.MaxValue) // Primär: Aufsteigend nach Preis
                .ThenBy(station => station.Dist ?? double.MaxValue) // Sekundär: Aufsteigend nach Entfernung
                .ToList();
        }

        Console.WriteLine($"Parallel working started for {stations.Count} stations with fuelType: {fuelType}");
        var stationCosts = stations
            .AsParallel() // Aktiviert parallele Verarbeitung
            .Where(station => station.IsOpen && station.Fuels != null && station.Dist.HasValue) // Filtert offene Tankstellen mit gültigem Dist
            .Select(station =>
            {
                // Setze FuelTypePrice und LastUpdate
                station.SetPrice(fuelType);
                station.SetUpdateTime(fuelType);

                // Berechne TotalCalculatedCoast
                decimal totalCost = 0;
                if (station.FuelTypePrice.HasValue && station.FuelTypePrice > 0)
                {
                    try
                    {
                        totalCost = station.CalculateTotalCostDoubleWay(fuelAmount, costPerKm);
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