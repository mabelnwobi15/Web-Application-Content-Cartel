#!/bin/sh
set -e

# Render provides PORT; the app must listen on 0.0.0.0 at that port.
export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-10000}"

# Render "Secret Files" are mounted at /etc/secrets/<filename>.
# FirebaseService looks for firebase-service-account.json next to the DLL.
SECRET=/etc/secrets/firebase-service-account.json
if [ -f "$SECRET" ]; then
  cp "$SECRET" /app/firebase-service-account.json
fi

exec dotnet "/app/$1"