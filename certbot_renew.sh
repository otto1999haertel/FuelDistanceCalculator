#!/bin/bash

# Stop the fuelgo-nginx container (if it exists)
if docker ps -q -f name=fuelgo-nginx > /dev/null; then
    docker stop fuelgo-nginx
fi

# Renew SSL certificates
sudo /usr/bin/certbot renew --quiet

# Stop native NGINX (if running)
if pgrep nginx > /dev/null; then
    sudo nginx -s stop
fi

# Start Docker Compose services
cd /home/ottohartel/FuelDistanceCalculator
sudo docker compose --env-file /home/ottohartel/FuelDistanceCalculator/.env.server up --build -d