@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo Preparando a versao Windows x64 que nao exige instalacao do SDK ou runtime...
dotnet publish "FinControl.WinForms\FinControl.WinForms.csproj" -c Release -r win-x64 --self-contained true -o "dist\FinControl-Windows" --nologo
if errorlevel 1 (
  echo Nao foi possivel publicar. Confira as mensagens acima.
  pause
  exit /b 1
)
echo Pronto. Copie a pasta dist\FinControl-Windows inteira para o computador de destino.
echo Abra FinControl.WinForms.exe dentro dessa pasta.
explorer "dist\FinControl-Windows"
pause
