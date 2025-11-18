#!/bin/bash

# Change to the directory first to ensure the correct Docker Compose context
cd /home/ottohartel/FuelDistanceCalculator

# Stop everything because of performance (stops all services from docker-compose.yml)
sudo docker compose stop

# Renew SSL certificates
sudo /usr/bin/certbot renew --quiet

# Stop native NGINX (if running)
if pgrep nginx > /dev/null; then
    sudo nginx -s stop
fi

# Start Docker Compose services
sudo docker compose --env-file /home/ottohartel/FuelDistanceCalculator/.env.server up --build -d