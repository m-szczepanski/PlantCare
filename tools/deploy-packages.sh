#!/usr/bin/env bash
#
# Local-only NAS deploy helper (gitignored, never committed).
#
# Rebuilds the PlantCare images for the NAS architecture and pushes them to
# GHCR. The NAS itself never builds: it pulls the published images, so the
# final step is always "Pull images and Recreate" in Dockge.
#
# Usage:
#   tools/deploy-nas.sh [api|web|both]     # default: both
#
# Env overrides:
#   GHCR_OWNER=<owner>   GitHub account owning ghcr.io/<owner>/plantcare-*
#                        (defaults to the currently logged-in gh account)
#   SKIP_TESTS=1         Skip the pre-push test/build verification
#   PLATFORM=linux/arm64 Override the target platform (NAS is amd64)

set -euo pipefail

cd "$(dirname "$0")/.."

TARGET="${1:-both}"
PLATFORM="${PLATFORM:-linux/amd64}"
OWNER="${GHCR_OWNER:-$(gh api user --jq .login)}"

case "$TARGET" in
  api | web | both) ;;
  *)
    echo "usage: $0 [api|web|both]" >&2
    exit 2
    ;;
esac

echo "==> owner=$OWNER target=$TARGET platform=$PLATFORM"

# Images must mirror main, never an unmerged branch.
git fetch origin
git checkout main
git pull --ff-only

if [[ "${SKIP_TESTS:-0}" != "1" ]]; then
  echo "==> backend tests"
  dotnet test backend/PlantCare.Api.Tests
  echo "==> frontend build + tests"
  (cd frontend && npm run build && npm run test)
fi

echo "==> ghcr.io login"
gh auth token | docker login ghcr.io -u "$OWNER" --password-stdin

push_image() { # $1 = build context, $2 = image name
  echo "==> push ghcr.io/$OWNER/$2:latest"
  docker buildx build --platform "$PLATFORM" \
    -t "ghcr.io/$OWNER/$2:latest" --push "$1"
}

if [[ "$TARGET" == "api" || "$TARGET" == "both" ]]; then
  push_image backend/PlantCare.Api plantcare-api
fi
if [[ "$TARGET" == "web" || "$TARGET" == "both" ]]; then
  push_image frontend plantcare-web
fi

cat <<'EOF'

==> images pushed. Now on the NAS:
    1. Dockge -> plantcare stack -> "Pull images and Recreate"
       (the digest next to each image should change)
    2. api container logs should show "Applying migration ..." for new migrations
    3. open the app and confirm it loads

    Persistent data lives in the named volumes (plant-data, plant-photos,
    ntfy-data) and survives a recreate. Never run "docker compose down -v"
    unless you intentionally want a factory reset.
EOF
