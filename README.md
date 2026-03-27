# FuelDistanceCalculator

**Currently hosted on https://fuelgo.de/**

ASP .NET-Core Web-App with razor pages (.cshtml) for calculating the optimal gas station regarding the price, the amount you have to buy and your average cost per kilometer
To start the app navigate to the root folder, where docker-compose.yml file is located and enter:
docker compose up --build
For a successfull connection to the gas station price service you have to create a env.server file and enter your API Key in the following format, as shown in FuelDistanceCalcultaor/.env.local

# Design Updates
- to update the bootstrap design run 'libman restore' in the FuelDistanceCalculator folder on your machine  
  
# Data update
- to update car data download https://www.adac.de/rund-ums-fahrzeug/auto-kaufen-verkaufen/autokosten/uebersicht/ into download folder and run createJSON.py in Scripts folder of repository  

# Build
- in executable code only use IConfiguration configuration["XAttributeX"] for most flexibility no Environment.Get  
- then it does not matter whether the configuration is an .env-file or local test context

# Building local
- copy/ create localhost certificates (*.cert/ *.key) to nginx/certs
- execute: docker compose --env-file .env.local up --build
- test output will be stored in the container: fuelgo-webapp\app\test-output  
- .env.local (with API Keys) need to be in FuelDistanceCalculator

# Building on the server
-  is triggered by github actions configured in deploy-nighty.yml
-  manual execuion: sudo docker compose --env-file .env.server up --build -d  
- .env.server (with API Keys) need to be in FuelDistanceCalculator

# Update certificate with certbot automatically via deployment hook for certbot
- specific certbot renew config under: sudo nano /etc/letsencrypt/renewal/[webpage]  
-- delete installer = nginx  
-- authenticator = webroot  
-- introdcue webroot-map
- introcude script under: sudo nano /etc/letsencrypt/renewal-hooks/deploy/
- certbot will execute every script under /etc/letsencrypt/renewal-hooks/deploy/
- e.g. : reload-nginx.sh  

```bash
#!/bin/bash

# do you action e.g. stop nginx, start containers ...
```
- test with: sudo certbot renew --dry-run  
- forcfule re-run (max 5 times a wekk): certbot renew --force-renewal
