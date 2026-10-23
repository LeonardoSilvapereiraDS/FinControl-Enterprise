@echo off
chcp 65001 >nul
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo Para executar o codigo-fonte, instale o SDK .NET 8.
  echo Para usar sem SDK, baixe o pacote Windows-portatil na aba Actions do GitHub.
  pause
  exit /b 1
)
dotnet build "FinControl.WinForms\FinControl.WinForms.csproj" -c Release --nologo
if errorlevel 1 (
  echo A compilacao falhou. Confira as mensagens acima.
  pause
  exit /b 1
)
start "" "FinControl.WinForms\bin\Release\net8.0-windows\FinControl.WinForms.exe"
