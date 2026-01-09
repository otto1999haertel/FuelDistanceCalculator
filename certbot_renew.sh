#!/bin/bash

cd /home/XXXX/FuelDistanceCalculator


# Stop native NGINX if running
if pgrep nginx > /dev/null; then
    sudo nginx -s stop
fi

# Start den docker NGINX container neu
sudo docker restart fuelgo-nginx

# Optional: Log the restart
echo "$(date): Certificates renewed and Docker containers restarted" >> /var/log/certbot-renew.log