@echo off
chcp 65001 >nul
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo Instale o SDK .NET 8 em https://dotnet.microsoft.com/download/dotnet/8.0
  echo Depois, abra este arquivo novamente.
  pause
  exit /b 1
)
echo Preparando o FinControl. A primeira execucao precisa de internet para baixar dependencias.
dotnet build "FinControl.Web\FinControl.Web.csproj" -c Release --nologo
if errorlevel 1 (
  echo Nao foi possivel preparar o projeto. Confira a mensagem acima e o LEIA-ME.html.
  pause
  exit /b 1
)
cd FinControl.Web
start "" "http://localhost:5078"
echo Deixe esta janela aberta enquanto estiver usando o sistema.
echo Se o navegador abrir antes do sistema iniciar, atualize a pagina.
dotnet "bin\Release\net8.0\FinControl.Web.dll" --urls "http://localhost:5078"
pause
