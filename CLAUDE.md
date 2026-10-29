# CLAUDE.md

Guía para Claude Code (claude.ai/code) en este repositorio. **El detalle técnico completo está en [README.md](README.md): léelo antes de tocar código** (arquitectura, cada pieza del núcleo, endpoints, JS, seguridad, pruebas, gotchas, recetas). Acá solo lo imprescindible.

## Qué es
Cliente web local de **SQL Server y Oracle** (estilo SSMS / SQL Developer) en .NET 8 + Razor Pages + JS plano. `DataBaseSDK.Core` (núcleo, sin interfaz) · `DataBaseSDK.Web` (interfaz, solo localhost, puerto 5130) · `DataBaseSDK.Tests` (xUnit). Windows únicamente (DPAPI). La interfaz tiene editor con pestañas y archivos `.sql`, panel de objetos a la izquierda (`Ctrl+B`) **explorador de archivos a la derecha (`Ctrl+K`)** y **cada pestaña con su propia conexión (base/esquema) y su último resultado**, como las ventanas de SSMS (`query-results.js`, resultados en `sessionStorage`), y `Ctrl+Tab` para volver al archivo anterior (como SSMS; el navegador lo reserva en pestañas normales: solo funciona en la ventana tipo aplicación de `start.cmd`); los 9 scripts se cargan en orden desde `Pages/Shared/_QueryScripts.cshtml` (un script nuevo se agrega ahí, una sola vez).

## Comandos
```bash
dotnet build                                   # compila la solución (con el sitio cerrado: si corre, las DLL están bloqueadas)
dotnet test                                    # 256 casos
node --test DataBaseSDK.Web/js-tests/query-complete.test.js DataBaseSDK.Web/js-tests/query-edit.test.js DataBaseSDK.Web/js-tests/query-explorer.test.js DataBaseSDK.Web/js-tests/query-mru.test.js DataBaseSDK.Web/js-tests/qc-workspace.test.js   # 57 casos; pasar los ARCHIVOS, no la carpeta
start.cmd                                      # compila + levanta + abre la ventana (doble clic, o: cmd /c "C:\Automations\DataBaseSDK\start.cmd")
```
Correr el sitio a mano: `cd DataBaseSDK.Web` y `dotnet bin/Debug/net8.0/DataBaseSDK.Web.dll` (el `.dll`, no el `.exe`: el Control de aplicaciones de Windows bloquea los `.exe` propios de forma intermitente). `dotnet run` desde la terminal de VS Code falla con `MSB4062 … System.Runtime 10.0.0.0` (C# Dev Kit): usar otra terminal.

## Reglas duras
1. **Nunca leer, imprimir ni copiar** `.env`, `.local\db-connections.json` (claves cifradas) ni `.local\data-edits.log` (datos reales de bases).
2. **No ejecutar consultas contra conexiones reales ni habilitar `AllowWrite` / `DBSDK_WRITE`** sin que el usuario lo pida explícitamente. Para verificar la interfaz usar conexiones/ejecutores de demostración en memoria (temporales, borrarlos después).
3. El editor de consultas es **solo lectura**. La única escritura es la edición de filas de la grilla (opt-in por conexión, solo `UPDATE` por PK completa). No sumar `INSERT`/`DELETE`/DML al editor, no aflojar `SingleTableAnalyzer`, y **nunca meter texto del navegador en el SQL** (solo parámetros y nombres del catálogo).
4. **No abrir el sitio a la red** (solo loopback + `LocalAccess`).
   `db-connections.shared.json` (raíz, **versionado**) es la copia sin contraseñas (servidor, puerto, servicio, usuario): **nunca** agregarle contraseñas ni `allowWrite` (el repo de GitHub es público). Igual `db-workspace.json` (raíz, **versionado**): pestañas abiertas y conexión elegida por motor; solo rutas y nombres, **nunca** texto de consultas.
5. **No cambiar la entropía DPAPI** (`"AzureDevOpsSdk.Database.v1"` en `ConnectionStore.cs`): dejaría ilegibles las contraseñas guardadas.
6. El `.sln` debe llamarse `DataBaseSDK.sln` y estar en la raíz (`ProjectPaths` lo usa para ubicar `.local`).
7. Estilos nuevos con las variables `--db-*` de `dark-theme.css` (nunca `--bs-body-bg`): Bootstrap 5.1 no tiene tema oscuro.
8. UI, mensajes, comentarios y docs en **español**; los comentarios explican el *porqué*.

## Gotchas de herramientas
- Reescribir archivos con tildes con PowerShell 5.1 (`Get-Content | Set-Content`) **corrompe** los acentos: usar Edit/Write. Heredocs enormes en Git Bash fallan: usar Write.
- PowerShell: `Set-Location` no cambia el directorio de proceso de `cmd`/programas nativos (usar rutas completas); las variables de entorno no persisten entre llamadas.
- Hay repo git (`origin` = github.com/is-jsandoval/DataBaseSDK, **público**): `.env` y `.local/` están ignorados.
- Si agregas un archivo o característica, actualiza [README.md](README.md) (§3 estructura, §6.3 endpoints, §10 pruebas…).
