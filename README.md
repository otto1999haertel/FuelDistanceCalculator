# FuelDistanceCalculator
ASP .NET-Core Web-App with razor pages (.cshtml) for calculating the optimal gas station regarding the price, the amount you have to buy and your average cost per kilometer
To start the app navigate to the root folder, where docker-compose.yml file is located and enter:
docker compose up --build
For a successfull connection to the gas station price service you have to create a appsettings.json file and enter your API Key in the following format:
"ApiSettings": {
      "TankApiKey": "[your api key]"
    } 

# Design Updates
- to update the bootstrap design run 'libman restore' in the FuelDistanceCalculator folder on your machine

# Build
- appsettings.Development.json (with tankerkoenig API Key) need to be in FuelDistanceCalculator
- appsettings.json (with tankerkoenig API Key)  need to be in FuelDistanceCalculator

# Building local
- copy/ create localhost certificates (*.cert/ *.key) to nginx/certs
- execute: docker compose --env-file .env.local up --build

# Building on the server
-  execute: docker compose --env-file .env.server up --build

# Update certificate with certbot automatically via Cronjob
- script has to have execution rights for user with sudo rights
- sudo docker stop fuelgo-nginx => stops nginx docker container 
- sudo /usr/bin/certbot renew --force-renewal --quiet => renewed certificate and restarts nginx on server
- sudo nginx -s stop => stops nginx on server for docker nginx to be able to start
- sudo docker compose --env-file .env.server up --build
