using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculator.Services {

    public static class TankCostService
    {
        public static List<GasStation> GetCheapestStations(List<GasStation> stations, decimal fuelAmount, decimal costPerKm)
        {
            if (stations == null || !stations.Any())
            {
                return new List<GasStation>();
            }
            if (fuelAmount <= 0)
            {
                return stations.Where(station => station.IsOpen)
                   .OrderBy(station => station.FuelTypePrice)  // Primär: Aufsteigend nach Preis
                   .ThenBy(station => station.Dist)            // Sekundär: Bei gleichem Preis aufsteigend nach Entfernung
                   .Take(10)
                   .ToList();
            }
            Console.WriteLine("Parallel working started");
            var stationCosts = stations
                .AsParallel() // Aktiviert parallele Verarbeitung
                .Where(station => station.IsOpen && station.FuelTypePrice.HasValue && station.Dist.HasValue) // Filtert offene Tankstellen
                .Select(station => (
                Station: station,
                TotalCost: station.CalculateTotalCostDoubleWay(fuelAmount, costPerKm)
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
}