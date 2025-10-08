public static class TankCostService{
    public static List<GasStation> GetCheapestStations(List<GasStation> stations, double fuelAmount, double costPerKm)
    {
            if (fuelAmount <= 0)
            {
                return stations.OrderBy(sc => sc.Price).Take(10).ToList();
            }
            Console.WriteLine("Parallel working started");
            var stationCosts = stations
                .AsParallel() // Aktiviert parallele Verarbeitung
                .Where(station => station.IsOpen) // Filtert offene Tankstellen
                .Select(station => (
                    Station: station,
                    TotalCost: (station.Price ?? 0.0) * fuelAmount + (station.Distance ?? 0.0) * costPerKm
                ))
                .ToList();

            Console.WriteLine("Anzahl TS in Total Cost Calculation: " + stationCosts.Count);

            // Sortiere die Tankstellen nach den Gesamtkosten (aufsteigend)
            return stationCosts.OrderBy(sc => sc.TotalCost)
                            .Take(10)
                            .Select(sc => sc.Station)
                            .ToList();
    }
}