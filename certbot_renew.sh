#!/bin/bash

# Stoppe den fuelgo-nginx Container
sudo docker stop fuelgo-nginx

# Führe Certbot-Renew aus (ohne --force-renewal für normale Erneuerungen)
sudo /usr/bin/certbot renew --quiet

# Optional: Stoppe native NGINX-Instanz (falls vorhanden, sonst auskommentieren)
sudo nginx -s stop

# Starte Docker Compose mit der angegebenen .env-Datei
sudo docker compose --env-file /home/FuelDistanceCalculator/.env.server up --build -d