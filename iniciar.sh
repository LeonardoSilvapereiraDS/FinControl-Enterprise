#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")"
if ! command -v dotnet >/dev/null; then
  echo 'Instale o SDK .NET 8: https://dotnet.microsoft.com/download/dotnet/8.0'
  exit 1
fi
dotnet build FinControl.Web/FinControl.Web.csproj -c Release --nologo
cd FinControl.Web
printf '%s\n' 'Abra http://localhost:5078 no navegador. Deixe este terminal aberto.'
exec dotnet bin/Release/net8.0/FinControl.Web.dll --urls "${FINCONTROL_LISTEN_URL:-http://localhost:5078}"
