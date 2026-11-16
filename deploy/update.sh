#!/usr/bin/env bash
# Run on the existing Lightsail server. Never prints or replaces deploy/.env.
set -euo pipefail
umask 077

gw_repo_dir="${GW_REPO_DIR:-/home/ubuntu/gameworld}"
cd "$gw_repo_dir"
for gw_tool in git sudo curl flock; do
  command -v "$gw_tool" >/dev/null || { printf 'Missing command: %s\n' "$gw_tool" >&2; exit 1; }
done
exec 9>deploy/.update.lock
flock -n 9 || { echo 'Another game update is running.' >&2; exit 1; }
[[ -f deploy/.env ]] || { echo 'Missing deploy/.env. Keep the original server configuration.' >&2; exit 1; }
if ! git diff --quiet || ! git diff --cached --quiet; then
  echo 'Tracked files have local changes. Update stopped so your work is preserved.' >&2
  exit 1
fi
[[ "$(git branch --show-current)" == main ]] || { echo 'Switch the server checkout to main before updating.' >&2; exit 1; }
sudo -n docker info >/dev/null
sudo -n docker compose version >/dev/null
gw_compose() { sudo -n docker compose --project-directory deploy -f deploy/compose.yaml "$@"; }
gw_compose config --quiet
gw_container=$(gw_compose ps -a -q api)
[[ -n "$gw_container" ]] || { echo 'Existing API container not found. This updater is for an existing deployment.' >&2; exit 1; }
gw_previous_image=$(sudo -n docker inspect --format '{{.Image}}' "$gw_container")

echo '1/4: Fetching main from GitHub...'
git pull --ff-only origin main
[[ -f GameWorld.Api/wwwroot/characters.html ]] || { echo 'Character page missing from source.' >&2; exit 1; }
git log -1 --oneline
gw_compose config --quiet

echo '2/4: Building game image. The current game stays running during the build...'
sudo -n docker build -t gameworld:local .

gw_ready() {
  curl -fsS --max-time 2 -H 'X-Forwarded-Proto: https' http://127.0.0.1:8080/health >/dev/null 2>&1 &&
    curl -fsS --max-time 2 -H 'X-Forwarded-Proto: https' http://127.0.0.1:8080/characters.html >/dev/null 2>&1
}
gw_rollback() {
  echo 'Update failed. Restoring the previous API image... local configuration and volumes are retained.' >&2
  sudo -n docker tag "$gw_previous_image" gameworld:local
  gw_compose up -d --no-build --force-recreate api
}

echo '3/4: Starting the updated API with the existing database volume...'
if ! gw_compose up -d --no-build --force-recreate api; then
  gw_rollback
  exit 1
fi

echo '4/4: Checking the database and character page...'
for ((gw_attempt=0; gw_attempt<40; gw_attempt++)); do
  if gw_ready; then
    gw_compose ps
    echo 'SUCCESS: the updated API serves /health and /characters.html.'
    echo 'Open https://52-65-168-70.sslip.io/characters.html (if the server IP and domain are unchanged).'
    exit 0
  fi
  sleep 1
done
gw_rollback
echo 'The new API did not pass checks; inspect server logs before retrying.' >&2
exit 1
