# FuelDistanceCalculator

**Currently hosted on https://fuelgo.de/**

ASP .NET-Core Web-App with razor pages (.cshtml) for calculating the optimal gas station regarding the price, the amount you have to buy and your average cost per kilometer
To start the app navigate to the root folder, where docker-compose.yml file is located and enter:
docker compose up --build
For a successfull connection to the gas station price service you have to create a appsettings.json file and enter your API Key in the following format:  
``````````json
{
  "ApiSettings": {  
    "TankApiKey": "[your api key for Tankerkönig]",
    "OpenRouteServiceApiKey": "[your api key for openrouteservice]"
  }
}
``````````

# Design Updates
- to update the bootstrap design run 'libman restore' in the FuelDistanceCalculator folder on your machine

# Build
- appsettings.Development.json (with tankerkoenig API Key) need to be in FuelDistanceCalculator
- appsettings.json (with tankerkoenig API Key)  need to be in FuelDistanceCalculator

# Building local
- copy/ create localhost certificates (*.cert/ *.key) to nginx/certs
- execute: docker compose --env-file .env.local up --build
- test output will be stored in the container: fuelgo-webapp\app\test-output

# Building on the server
-  execute: sudo docker compose --env-file .env.server up --build -d

# Update certificate with certbot automatically via deployment hook for certbot
- edit /usr/local/bin/certbot-deploy.sh
- use:  
-- Authentifcator: webroot  
-- Webrootmap: [[webroot_map]]  
      fuelgo.de = /var/www/certbot  
      www.fuelgo.de = /var/www/certbot  
- create webroot directory: sudo mkdir -p /var/www/certbot  
- introduce webroot directory in nginx default.conf  
- adapt docke compose for certbot and lets encrypt config
- introcude: sudo nano /etc/letsencrypt/renewal-hooks/deploy/reload-nginx.sh  

```bash
#!/bin/bash

# Reload nginx im Container (ohne Neustart)
docker exec fuelgo-nginx nginx -s reload

# Optional: Logging
echo "$(date): SSL certificates renewed, nginx reloaded" >> /var/log/certbot-nginx-reload.log
```
