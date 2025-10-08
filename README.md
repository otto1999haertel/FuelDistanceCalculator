# FuelDistanceCalculator
ASP .NET-Core Web-App for calculating the optimal gas station regarding the price, the amount you have to buy and your average cost per kilometer
To start the app navigate to the root folder, where docker-compose.yml file is located and enter:
docker compose up --build
For a successfull connection to the gas station price service you have to create a appsettings.json file and enter your API Key in the following format:
"ApiSettings": {
      "TankApiKey": "[your api key]"
    } 

# Design Updates
- to update the bootstrap design run 'libman restore' on your machine

# Building local
- copy localhost certificates (*.cert/ *.key) to nginx/certs
- execute: docker compose --env-file .env.local up --build

# Building on the server
-  execute: docker compose --env-file .env.server up --build

# Update CertB Bot automatically via Crone Job
- sudo docker stop fuelgo-nginx
- sudo /usr/bin/certbot renew --force-renewal --quiet
- sudo nginx -s stop
- sudo docker compose --env-file .env.server up --build
