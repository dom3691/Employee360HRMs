#!/bin/sh
set -eu

if [ -z "${ConnectionStrings__DefaultConnection:-}" ]; then
  echo "ERROR: ConnectionStrings__DefaultConnection is not set; cannot apply migrations."
  exit 1
fi

echo "Applying database migrations..."
./efbundle --connection "$ConnectionStrings__DefaultConnection"

echo "Starting Employee360 API..."
exec dotnet Employee360.API.dll
