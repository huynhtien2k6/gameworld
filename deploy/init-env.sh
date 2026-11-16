#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
umask 077
if [ -e .env ]; then
  echo '.env already exists; keeping credentials.'
  exit 0
fi
read -r -p 'Domain pointing to this server (example: game.example.com): ' domain
if [[ ! "$domain" =~ ^[A-Za-z0-9]([A-Za-z0-9.-]*[A-Za-z0-9])?$ || "$domain" != *.* ]]; then
  echo 'Enter a domain name only, without https:// or a path.' >&2
  exit 1
fi
command -v openssl >/dev/null
jwt_key=$(openssl rand -hex 64)
admin_password=$(openssl rand -hex 24)
printf 'GAME_DOMAIN=%s\nJWT_KEY=%s\nADMIN_USERNAME=admin\nADMIN_PASSWORD=%s\n' \
  "$domain" "$jwt_key" "$admin_password" > .env
echo 'Created private deploy/.env. Read ADMIN_PASSWORD locally; never paste it into chat.'
