@echo off
setlocal
rem DataBaseSDK: compila (rapido si no hubo cambios), levanta el sitio y lo abre en una ventana tipo aplicacion (Edge).
rem Cierra esta ventana negra para apagar el sitio.
rem Se ejecuta el .dll con "dotnet" y no el .exe: el Control de aplicaciones de Windows bloquea a veces los .exe propios.

cd /d "%~dp0"

dotnet build DataBaseSDK.Web -c Debug --nologo -v q
if errorlevel 1 (
    echo.
    echo No se pudo compilar. Si DataBaseSDK ya esta corriendo, cierra la otra ventana negra y vuelve a intentar.
    pause
    exit /b 1
)

rem El sitio busca wwwroot y el .env desde el directorio actual: hay que estar en DataBaseSDK.Web.
cd DataBaseSDK.Web
set DBSDK_OPEN_BROWSER=1

echo.
echo DataBaseSDK corriendo (solo en esta maquina). Cierra esta ventana para apagarlo.
dotnet bin\Debug\net8.0\DataBaseSDK.Web.dll

echo.
echo El sitio se detuvo.
pause
