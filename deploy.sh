#!/bin/bash
set -e

echo "Deploying application..."

REMOTE_USER="${SSH_USER}"
REMOTE_HOST="${SSH_HOST}"
REMOTE_PORT="${SSH_PORT:-2222}"
TARGET_DIR="/var/www/fuel-distance-calculator"

# Dateien übertragen (ohne löschen)
rsync -avz \
    -e "ssh -p ${REMOTE_PORT}" \
    ./ "$REMOTE_USER@$REMOTE_HOST:$TARGET_DIR"

echo "Deploy done!"