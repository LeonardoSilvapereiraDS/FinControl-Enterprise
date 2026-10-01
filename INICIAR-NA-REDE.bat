@echo off
chcp 65001 >nul
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo Instale o SDK .NET 8 em https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)
echo Este modo permite acesso pela mesma rede Wi-Fi. Use somente uma rede de confianca.
dotnet build "FinControl.Web\FinControl.Web.csproj" -c Release --nologo
if errorlevel 1 (
  pause
  exit /b 1
)
echo No celular, abra http://IP-DO-COMPUTADOR:5078
echo Confira o endereco IPv4 da sua conexao abaixo:
ipconfig
cd FinControl.Web
start "" "http://localhost:5078"
echo Deixe esta janela aberta. Se o Windows perguntar sobre firewall, permita somente rede privada.
dotnet "bin\Release\net8.0\FinControl.Web.dll" --urls "http://0.0.0.0:5078"
pause
