#!/usr/bin/env bash
set -Eeuo pipefail
source "$(dirname "$0")/common.sh"
require_root
previous="$(readlink -f "$ROOT/previous" 2>/dev/null || true)"
[[ "$previous" == "$ROOT/releases/"* && -f "$previous/release.env" ]] || { echo 'No previous release.' >&2; exit 1; }
current="$(readlink -f "$ROOT/current")"
# Registry tooling can restore either format selectively, without restarting Mongo.
for registry_tool in "$current/scripts/registry_host.py" "$previous/scripts/registry_host.py"; do
  if [[ -f "$registry_tool" && -f "$(dirname "$registry_tool")/registry_release.py" ]]; then
    exec python3 "$registry_tool" rollback --seconds 600
  fi
done

# Rollback không được tự đổi major DB hoặc mở lại image lỗi kernel của release cũ.
release_major() (
  unset MONGO_MAJOR
  [[ ! -f "$1/base-images.env" ]] || source "$1/base-images.env"
  source "$1/release.env"
  printf '%s' "${MONGO_MAJOR:-8}"
)
previous_major="$(release_major "$previous")"
current_major="$(release_major "$current")"
if [[ "${previous_major:-8}" != "${current_major:-8}" ]]; then
  echo 'Rollback across MongoDB major versions is blocked; database migration needs a separate backup/restore plan.' >&2
  exit 1
fi
ln -sfn "$previous" "$ROOT/current"
ln -sfn "$current" "$ROOT/previous"
# Image cũ được giữ trong Docker; không đổi config hoặc MongoDB schema/volume.
systemctl restart junglemaze.service
echo 'Previous release restored; database volume preserved.'
