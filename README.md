# FuelDistanceCalculator

**Currently hosted on https://fuelgo.de/**

A modern ASP.NET Core web application following an API-first architecture for calculating the optimal gas station based on fuel prices, purchase amounts, and average cost per kilometer. The app provides both a RESTful API for programmatic access and a user-friendly Razor Pages web interface.

## Features

- **Optimal Gas Station Search**: Find the cheapest gas stations considering fuel costs, travel expenses, and optional brand discounts.
- **Multi-Location Comparison**: Compare average fuel costs across multiple locations.
- **Flexible Sorting**: Sort results by total cost, fuel price, or distance.
- **Open Stations Only**: Automatically filters to show only currently open gas stations.
- **API Throttling**: Built-in rate limiting to prevent API abuse.
- **Robust Testing**: Comprehensive unit and integration tests covering services, controllers, and pages.
- **Docker Support**: Easy deployment with Docker Compose.

## Architecture

The application follows an API-first design where all core business logic is encapsulated in reusable services. The same services power both the REST API endpoints and the web UI, ensuring consistency and maintainability.

### Key Components

- **Services**: `SearchService`, `GeoLocationService`, `MarketFuelPriceService`, `TankCostService`, etc.
- **API Controllers**: RESTful endpoints under `/api/`
- **Razor Pages**: User interface for interactive use
- **Middleware**: Request protection and rate limiting
- **Tests**: NUnit-based test suite with Moq for mocking

## API Documentation

The API provides endpoints for searching, comparing, and sorting gas stations. All endpoints return JSON responses.

### Base URL
```
https://fuelgo.de/api/
```

### Endpoints

#### 1. Search Gas Stations
Find optimal gas stations for a given location.

**Endpoint:** `POST /api/search`

**Request Body:**
```json
{
  "place": "Erlangen, Germany",
  "radius": 10,
  "fuelAmount": 50,
  "pricePerKm": 0.25,
  "fuelType": "Diesel",
  "stationBrand": "Aral",
  "discountPercent": 5.0,
  "sortMode": "totalCost"
}
```

**Response:**
```json
{
  "parameters": { ... },
  "coordinates": {
    "latitude": 49.5897,
    "longitude": 11.0078
  },
  "stations": [
    {
      "id": "station-id",
      "name": "Aral Tankstelle",
      "brand": "Aral",
      "street": "Street Name",
      "place": "Erlangen",
      "coords": { "lat": 49.5897, "lng": 11.0078 },
      "isOpen": true,
      "fuels": [
        {
          "name": "Diesel",
          "price": 1.50,
          "lastChange": {
            "timestamp": "2025-10-15T09:00:00Z",
            "amount": -0.05
          }
        }
      ],
      "dist": 2.5,
      "totalCalculatedCoast": 75.00,
      "fuelTypePrice": 1.425,
      "lastUpdate": "2025-10-15T09:00:00Z",
      "updateAmount": -0.05
    }
  ],
  "savingsToNearestStation": 2.50,
  "savingsToCheapestStation": 1.25
}
```

#### 2. Compare Locations
Compare fuel costs across multiple locations.

**Endpoint:** `POST /api/compare`

**Request Body:**
```json
{
  "locations": [
    {
      "place": "Erlangen",
      "radius": 5,
      "fuelAmount": 40,
      "pricePerKm": 0.30,
      "fuelType": "SuperE10"
    },
    {
      "place": "Nürnberg",
      "radius": 5,
      "fuelAmount": 40,
      "pricePerKm": 0.30,
      "fuelType": "SuperE10"
    }
  ]
}
```

**Response:** Array of `SearchResult` objects (same as search endpoint).

#### 3. Sort Stations
Sort a list of gas stations by various criteria.

**Endpoint:** `POST /api/sort`

**Request Body:**
```json
{
  "stations": [
    { "id": "1", "fuelTypePrice": 1.60, "dist": 5.0, "totalCalculatedCoast": 80.0 },
    { "id": "2", "fuelTypePrice": 1.50, "dist": 3.0, "totalCalculatedCoast": 75.0 }
  ],
  "sortMode": "fuelPrice"
}
```

**Response:**
```json
{
  "stations": [
    { "id": "2", "fuelTypePrice": 1.50, ... },
    { "id": "1", "fuelTypePrice": 1.60, ... }
  ]
}
```

### Data Types

#### SearchParameters
```csharp
public class SearchParameters
{
    public string Place { get; set; }
    public double Radius { get; set; } = 5;
    public decimal FuelAmount { get; set; }
    public decimal PricePerKm { get; set; }
    public FuelType FuelType { get; set; } = FuelType.Diesel;
    public string StationBrand { get; set; }
    public decimal DiscountPercent { get; set; }
    public string SortMode { get; set; }
}
```

#### FuelType Enum
- `Diesel`
- `SuperE5`
- `SuperE10`

#### Sort Modes
- `totalCost`: Sort by total calculated cost (default)
- `fuelPrice`: Sort by fuel price per liter
- `distance`: Sort by distance

## Getting Started

### Prerequisites
- Docker and Docker Compose
- Tankerkönig API key (for fuel price data)

### Quick Start
1. Clone the repository
2. Create a `.env.server` file with your API key:
   ```
   TANKERKOENIG_API_KEY=your-api-key-here
   REDIS_HOST=redis:6379
   MODE_TYPE=Production
   ```
3. Run the application:
   ```bash
   docker compose up --build
   ```
4. Access the web UI at `http://localhost:8080` or use the API endpoints.

### Local Development
1. Copy/create localhost certificates to `nginx/certs`
2. Create `.env.local` file
3. Run: `docker compose --env-file .env.local up --build`
4. Test output is stored in the container at `/app/test-output`

### Building for Server
- Triggered by GitHub Actions in `deploy-nightly.yml`

## Testing

The application includes comprehensive tests covering services, API controllers, and pages.

Run tests:
```bash
dotnet test
```

Test results are available in `test-output/` when running in Docker.

## Design Updates

To update Bootstrap design, run `libman restore` in the `FuelDistanceCalculator` folder.

## Certificate Management

Automatic certificate renewal with Certbot:
- Config: `/etc/letsencrypt/renewal/[webpage]`
- Deployment hooks: `/etc/letsencrypt/renewal-hooks/deploy/`
- Test renewal: `sudo certbot renew --dry-run`

## Contributing

1. Follow the API-first approach for new features
2. Add comprehensive tests for new functionality
3. Update API documentation for new endpoints
4. Ensure Docker builds succeed