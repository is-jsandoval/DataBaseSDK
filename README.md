# DataBaseSDK

Cliente web local de bases de datos **SQL Server** y **Oracle** (estilo SSMS / SQL Developer): explorador de objetos, editor de consultas con pestañas y archivos `.sql`, autocompletado, grilla de resultados con exportación a CSV y — opcional, apagado por defecto — edición de filas celda por celda. Corre en tu máquina (Windows), solo en `localhost`.

> **Este documento está escrito para que un agente de IA (o una persona nueva) entienda todo lo técnico del proyecto sin tener que reconstruirlo leyendo el código.** Si algo del código contradice lo que dice acá, manda el código: corrige este documento. Creado el 2026-10-28 copiando el módulo de bases de datos de `AzureDevOpsSdk` (ver §16).

---

## 0. Lo esencial en 30 segundos

| | |
|---|---|
| **Stack** | .NET 8 (SDK 8.0.423), ASP.NET Core Razor Pages, JavaScript plano (sin framework, sin bundler, sin npm), Bootstrap 5.1 vendored |
| **Drivers** | `Oracle.ManagedDataAccess.Core` 23.9.0, `Microsoft.Data.SqlClient` 5.2.2 — **solo** en `DataBaseSDK.Core` |
| **Solo Windows** | Las contraseñas guardadas se cifran con DPAPI (`ProtectedData`, alcance usuario actual) |
| **Arrancar** | doble clic a [start.cmd](start.cmd) → compila, levanta el sitio en `http://localhost:5130` y lo abre en una ventana de Edge tipo aplicación |
| **Probar** | `dotnet test` (256 casos) y `node --test DataBaseSDK.Web/js-tests/query-complete.test.js DataBaseSDK.Web/js-tests/query-edit.test.js DataBaseSDK.Web/js-tests/query-explorer.test.js DataBaseSDK.Web/js-tests/query-mru.test.js DataBaseSDK.Web/js-tests/qc-workspace.test.js` (57 casos; pasar los **archivos**, no la carpeta) |
| **Atajos** | `Ctrl+Enter` / **`Ctrl+E`** ejecutar (sin recargar la página) · `Ctrl+S` / `Ctrl+Shift+S` guardar · `Ctrl+O` abrir · `Ctrl+B` panel izquierdo · **`Ctrl+K` explorador de archivos (derecha)** · **`Ctrl+Tab` archivo anterior** (como SSMS; solo en la ventana tipo aplicación de `start.cmd`, ver §7.1) · `Ctrl+Espacio` autocompletar |
| **Regla 1** | Las consultas del editor son **de solo lectura** (guard de texto + transacción que nunca se confirma). |
| **Regla 2** | La **única** ruta de escritura es la edición de filas de la grilla: opt-in por conexión, solo `UPDATE` por clave primaria, validada contra el catálogo, con verificación de valores originales, bitácora previa al commit y rollback ante cualquier fallo. |
| **Regla 3** | Ningún texto que venga del navegador entra jamás al SQL como texto: solo parámetros y nombres leídos del catálogo de la base. |
| **Regla 4** | Solo responde a peticiones de la misma máquina (loopback + cabecera `Host` de loopback). No hay opción para abrirlo a la red. |
| **No leer ni imprimir** | `.env`, `.local\db-connections.json` (claves cifradas), `.local\data-edits.log` (datos reales de las bases) |
| **No probado** | Contra servidores reales Oracle/SQL Server (ver §11). Todo lo demás está probado con conexiones en memoria y en el navegador. |

---

## 1. Qué es y qué no es

**Es:** una herramienta personal de un QA bancario sénior para consultar y revisar datos en bases Oracle y SQL Server sin abrir SSMS / SQL Developer. Dos módulos (páginas) con la misma interfaz: `/SqlServer` y `/Oracle`. "Por ahora" son esos dos motores; el diseño separa lo común de lo específico de cada motor para poder sumar otros (ver receta en §14.1).

**No es:** un cliente completo. No hay plan de ejecución, no hay varios resultados por script, no hay ejecución de procedimientos ni DDL/DML desde el editor, no hay resaltado de sintaxis (el editor es un `<textarea>` con una barra de números de línea dibujada en un `<canvas>`), no hay autenticación (no hace falta: solo escucha en localhost).

**Idioma:** toda la interfaz, los mensajes de error, los comentarios del código y este documento están en **español**. Mantenerlo.

---

## 2. Cómo se ejecuta

### 2.1 Normal

Doble clic a [start.cmd](start.cmd). Hace, en orden:
1. `dotnet build DataBaseSDK.Web -c Debug` (incremental; si el sitio ya está corriendo falla por DLL bloqueadas y lo explica).
2. `cd DataBaseSDK.Web` (**obligatorio**: el sitio busca `wwwroot` y el `.env` desde el directorio actual).
3. `DBSDK_OPEN_BROWSER=1` y `dotnet bin\Debug\net8.0\DataBaseSDK.Web.dll`. Se ejecuta el **`.dll` con `dotnet`**, no el `.exe` generado: el *Control de aplicaciones* de Windows bloquea de forma intermitente los `.exe` propios compilados (pasó repetidas veces en el proyecto hermano `AzureDevOpsSdk`); `dotnet.exe` nunca se bloquea.
4. Cuando Kestrel ya escucha, [BrowserLauncher](DataBaseSDK.Web/BrowserLauncher.cs) abre `msedge --app=http://localhost:5130/` (ventana sin pestañas ni barra de direcciones) o, si no hay Edge, el navegador predeterminado. Nunca lanza excepción: si no puede abrirlo, el sitio sigue y se abre a mano.

Cerrar la ventana negra apaga el sitio.

### 2.2 A mano

```bash
dotnet build
cd DataBaseSDK.Web
dotnet bin/Debug/net8.0/DataBaseSDK.Web.dll        # o: dotnet run --project DataBaseSDK.Web (desde la raíz)
```

Si `dotnet run` falla con `MSB4062 ... Microsoft.NET.ProjectData.Tasks.WriteProjectDataSliceTask ... System.Runtime 10.0.0.0`, no es un error del código: es la extensión **C# Dev Kit de VS Code** inyectando tareas de MSBuild compiladas para .NET 10 en la terminal integrada (la máquina tiene el SDK 8). Ejecutar desde una terminal **fuera de VS Code**, o usar `start.cmd`.

### 2.3 Puerto y otras variables

Ver §8. Puerto por defecto **5130** (el sitio hermano `AzureDevOpsSdk.Web` usa 5124; así pueden convivir).

### 2.4 Compilar con el sitio corriendo

`dotnet build` falla (`MSB3027`, DLL bloqueadas por el proceso) si el sitio está corriendo. Detener primero el proceso que escucha en el 5130 (cerrar la ventana negra de `start.cmd`).

---

## 3. Estructura del repositorio

```
DataBaseSDK/
├─ DataBaseSDK.sln
├─ README.md                      este documento
├─ CLAUDE.md                      punteros y reglas duras para Claude Code
├─ start.cmd                      compila + levanta + abre ventana (§2.1)
├─ .env.example                   variables soportadas, todas opcionales (§8)
├─ .gitignore                     bin/ obj/ .env .vs/ .vscode/ .local/
├─ db-connections.shared.json     (versionado) conexiones guardadas SIN contraseñas: servidor, puerto, servicio, usuario (§5.5)
├─ db-workspace.json              (versionado, se crea al usar la consola) pestañas abiertas y conexión elegida por motor (§5.11)
├─ .local/                        (se crea solo, ignorada) conexiones con clave cifrada y bitácora (§8.2)
│
├─ DataBaseSDK.Core/              ← NÚCLEO, sin interfaz gráfica, namespace DataBaseSDK.Core
│  ├─ Models.cs                   DatabaseKind, DbConnectionInfo, QueryOptions, QueryColumn, QueryResult, QueryRejectedException
│  ├─ ReadOnlySqlGuard.cs         chequeo de "solo lectura" del texto SQL
│  ├─ QueryRunners.cs             ISqlQueryRunner, QueryRunnerBase, OracleQueryRunner, SqlServerQueryRunner, QueryResultCsv
│  ├─ CellText.cs                 cómo se normaliza/formatea una celda (grilla, CSV y edición comparten esto)
│  ├─ ConnectionStore.cs          StoredConnection, ConnectionStringFactory, ConnectionValidation, DPAPI, DbConnectionStore
│  ├─ DbConnectionCatalog.cs      une conexiones del .env + guardadas
│  ├─ Explorer.cs                 enums/records del explorador + IDbExplorer
│  ├─ Explorers.cs                ExplorerBase, SqlServerExplorer, OracleExplorer (SQL de catálogo)
│  ├─ RowEditing.cs               modelos de edición, ITableDescriber, SingleTableAnalyzer, bitácora, IRowEditor
│  ├─ RowEditors.cs               RowEditorBase, SqlServerRowEditor, OracleRowEditor (la ruta de escritura)
│  ├─ SqlFiles.cs                 ISqlFileService / SqlFileService (abrir, guardar, navegar, localizar .sql)
│  ├─ Workspace.cs                IWorkspaceStore / WorkspaceStore: sesión versionada (db-workspace.json, §5.11)
│  ├─ LocalAccess.cs              ¿la petición es de esta máquina? (lógica pura)
│  ├─ ProjectPaths.cs             raíz del repo (carpeta con DataBaseSDK.sln) y .local
│  └─ ServiceCollectionExtensions.cs   AddDatabaseConsole(): registra todo en DI
│
├─ DataBaseSDK.Web/               ← INTERFAZ, Razor Pages, namespace DataBaseSDK.Web
│  ├─ Program.cs                  host, puerto, middleware solo-local, LoadDotEnv, abrir navegador
│  ├─ BrowserLauncher.cs          abre la ventana "app" de Edge
│  ├─ Pages/
│  │  ├─ _ViewImports.cshtml, _ViewStart.cshtml
│  │  ├─ Index.cshtml(.cs)        portada: dos tarjetas (motor, nº de conexiones, nº con edición)
│  │  ├─ SqlServer.cshtml / Oracle.cshtml   pocas líneas c/u: la parcial de la consola + la parcial de scripts
│  │  ├─ Error.cshtml(.cs)
│  │  ├─ QueryConsolePageModel.cs base abstracta + OracleModel + SqlServerModel + DTOs; TODOS los endpoints (§6)
│  │  └─ Shared/
│  │     ├─ _Layout.cshtml        navbar (SQL Server, Oracle), tema oscuro, Bootstrap
│  │     ├─ _QueryConsole.cshtml  toda la UI de una consola (HTML + <style>), compartida por los dos motores
│  │     ├─ _QueryResults.cshtml  el bloque de resultados/error (lo usa la página y lo devuelve solo RunPartial)
│  │     └─ _QueryScripts.cshtml  los 9 <script> de la consola EN SU ORDEN de carga (una sola copia para las dos páginas)
│  ├─ wwwroot/
│  │  ├─ css/site.css, css/dark-theme.css
│  │  ├─ js/qc-common.js, qc-mru.js, qc-workspace.js, query-editor.js, query-complete.js, query-edit.js, query-results.js, query-explorer.js, query-console.js   (§7)
│  │  └─ lib/bootstrap/dist/{css,js}   Bootstrap 5.1.0 (solo min.css y bundle.min.js + mapas)
│  └─ js-tests/query-complete.test.js, query-edit.test.js, query-explorer.test.js, query-mru.test.js, qc-workspace.test.js   pruebas de Node (sin navegador)
│
└─ DataBaseSDK.Tests/             ← xUnit; referencia solo a Core (InternalsVisibleTo)
   ├─ QueryRunnerTests.cs         + los FAKES de DbConnection/DbCommand/DbTransaction/DbParameter (§10.2)
   ├─ DatabaseConsoleTests.cs     guard, catálogo, CSV, contexto de consulta
   ├─ ExplorerAndStoreTests.cs    exploradores, almacén cifrado, cadenas de conexión, validación
   ├─ RowEditingTests.cs          analizador de SELECT, editor de filas, valores, bitácora
   ├─ TableDescribeTests.cs       DescribeTableAsync por motor, permisos de escritura
   ├─ SqlFileServiceTests.cs      archivos .sql, codificaciones, conflictos, Locate, ProjectPaths
   ├─ LocalAccessAndImportTests.cs  LocalAccess, importación única de conexiones, copia versionada sin contraseñas
   └─ WorkspaceStoreTests.cs      sesión versionada: secciones por motor, rutas relativas, validación, sello
```

A diferencia de `AzureDevOpsSdk`, **no hay `<Compile Include>` explícitos**: cada proyecto compila su propia carpeta con las reglas por defecto del SDK. Una carpeta o archivo nuevo dentro de un proyecto se compila solo.

Tamaño aproximado: Core ≈ 3.500 líneas de C#; `QueryConsolePageModel.cs` ≈ 600; `_QueryConsole.cshtml` ≈ 540; JS ≈ 3.100.

---

## 4. Arquitectura

```
Navegador (localhost)
   │  HTML (Razor) + 9 scripts JS planos
   ▼
Kestrel (solo loopback) ── middleware LocalAccess (403 si no es local) ── archivos estáticos ── Razor Pages
   │
   ▼
QueryConsolePageModel (OracleModel | SqlServerModel)          ← "front": traduce HTTP ⇄ llamadas al núcleo
   │   inyecta: IDbConnectionCatalog, IDbConnectionStore, IEnumerable<ISqlQueryRunner>,
   │            IEnumerable<IDbExplorer>, IEnumerable<IRowEditor>, ISqlFileService
   ▼
DataBaseSDK.Core                                              ← lógica, sin ASP.NET
   ├─ catálogo/almacén de conexiones ──► DbConnectionInfo (cadena de conexión + AllowWrite)
   ├─ ISqlQueryRunner  (uno por motor) ──► guard → abre → contexto → transacción de solo lectura → lee con tope
   ├─ IDbExplorer      (uno por motor) ──► consultas de catálogo parametrizadas (y DescribeTableAsync)
   ├─ IRowEditor       (uno por motor) ──► Prepare (¿editable?) y Apply (UPDATE verificado en transacción)
   └─ ISqlFileService  ──► disco local, solo .sql
   │
   ▼
Oracle.ManagedDataAccess.Core / Microsoft.Data.SqlClient  ──►  la base real
```

**Patrón por motor.** Cada motor aporta una implementación de tres interfaces (`ISqlQueryRunner`, `IDbExplorer`, `IRowEditor`) y se distingue por `DatabaseKind { Oracle, SqlServer }`. Las páginas (`OracleModel`, `SqlServerModel`) son la **misma** clase base con `kind` distinto: eligen sus implementaciones con `.First(x => x.Kind == kind)`. La interfaz (`_QueryConsole.cshtml` + JS) es una sola; lo que cambia entre motores se decide en el servidor (`HasDatabases`, `ObjectKinds`, `EnvPrefix`) o con `kind` en el JS (nombres calificados, palabras clave).

**Diferencias de modelo entre los motores** (todo el diseño las respeta):
| | SQL Server | Oracle |
|---|---|---|
| Niveles | servidor → **bases** → esquemas → objetos | conexión → **esquemas** (= usuarios) → objetos (una sola base por conexión) |
| Contexto de la consulta | selector **Base de datos** (`ChangeDatabase`) | selector **Esquema** (`ALTER SESSION SET CURRENT_SCHEMA`) |
| "Solo lectura" a nivel base | no existe; transacción `ReadCommitted` que nunca se confirma | `SET TRANSACTION READ ONLY` como primera sentencia |
| Parámetros | `@nombre` | `:nombre` (con `BindByName = true`) |
| Citado de identificadores | `[nombre]` | `"NOMBRE"` (sin comillas se guarda en MAYÚSCULAS) |
| Cadena vacía | texto vacío | **equivale a `NULL`** |
| Bloqueo de fila | `WITH (UPDLOCK, ROWLOCK)` + `SET LOCK_TIMEOUT 5000` | `FOR UPDATE WAIT 5` |

### 4.1 Flujo de "ejecutar una consulta"

1. El usuario pulsa **▶ Ejecutar** / `Ctrl+Enter` / `Ctrl+E`. El JS (`query-editor.js`, listener de `submit` en fase de captura) copia al input oculto `Sql` **lo seleccionado, o todo el texto si no hay selección**, y guarda la sesión.
2. `query-results.js` intercepta el `submit` y manda el formulario con **`fetch` a `POST ?handler=RunPartial`** (campos `Alias`, `Sql`, `TabId`, `MaxRows`, `Database` o `Schema` y el token antiforgery): la respuesta es **solo** el bloque de resultados ([_QueryResults.cshtml](DataBaseSDK.Web/Pages/Shared/_QueryResults.cshtml)), que se pone en la pestaña que ejecutó; la página no se recarga. `?handler=Run` (página entera) queda como respaldo sin JS.
3. `QueryConsolePageModel.OnPostRunAsync` → `ExecuteAsync`: busca la conexión en el catálogo; llama a `ISqlQueryRunner.ExecuteAsync(connection, Sql, QueryOptions)`.
4. `QueryRunnerBase.ExecuteAsync` (§5.3): `ReadOnlySqlGuard.Validate` → abre → `ApplyContextAsync` → `BeginReadOnlyAsync` → `ExecuteReaderAsync` → lee filas hasta `MaxRows` (si hay más, cancela el comando y marca `Truncated`) → devuelve `QueryResult`.
5. Si el guard rechaza: `QueryRejectedException` → `Error` + `Rejected = true` (se muestra con 🔒, nunca llegó a la base). Otros errores: `ex.Message` tal cual (`ORA-…` / `Msg …`). Cancelación: "Consulta cancelada.".
6. Con resultado, `PrepareEditAsync` decide si la grilla es editable (§5.7). Se renderiza `_QueryConsole.cshtml`: tabla con `CellText.Format` en cada celda, y `data-edit` (JSON) en el contenedor si es editable.
7. `OnPostCsvAsync` hace lo mismo pero devuelve el CSV (UTF-8 **con BOM** para Excel) como `{Alias}-{yyyyMMdd-HHmmss}.csv`.

### 4.2 Flujo de "guardar una edición de la grilla"

Ver §5.7.4. Resumen: `fetch POST ?handler=ApplyEdits` → `RowEditorBase.ApplyAsync` (validar contra el catálogo → una transacción → por fila: bloquear y comparar originales → `UPDATE` por PK → bitácora → commit) → JSON `{ok, rowsAffected, dryRun, statements}` o `{error, code}`.

---

## 5. El núcleo (`DataBaseSDK.Core`), pieza por pieza

### 5.1 [Models.cs](DataBaseSDK.Core/Models.cs)
- `DatabaseKind { Oracle, SqlServer }`.
- `ConnectionOrigin { Env, Saved }`: conexiones del `.env` (solo lectura en la UI) o guardadas desde la pantalla (editables).
- `DbConnectionInfo(Alias, Kind, ConnectionString, Description)` + `Origin` + **`AllowWrite`** (init). `ConnectionString` es **secreta**: nunca se muestra; `Description` es el resumen sin clave.
- `QueryOptions`: `MaxRows` (defecto 500, tope 10.000, se clampa 1..10.000), `TimeoutSeconds` (defecto 30, tope 300), `Database` (SQL Server), `Schema` (Oracle).
- `QueryColumn(Name, TypeName, IsNumeric)`, `QueryResult {Columns, Rows (object?[]), Truncated, ElapsedMilliseconds}`. Cada celda es `null`, `string`, número, `bool` o `DateTime` (ya normalizada, ver §5.4).
- `QueryRejectedException`: el texto no pasó el guard (no llegó a la base).

### 5.2 [ReadOnlySqlGuard.cs](DataBaseSDK.Core/ReadOnlySqlGuard.cs)
`Validate(DatabaseKind, string?) → GuardResult(Allowed, Reason, Sql)`. Acepta **una sola sentencia** que empiece con `SELECT` o `WITH`.
- Recorre el texto y arma una copia **sin comentarios** (`--`, `/* */`), con los **literales** `'…'` vaciados (también `q'[…]'` de Oracle y sus delimitadores `[ { ( <` o cualquier otro) y los **identificadores entre comillas** (`"x"`, y `[x]` en SQL Server) reemplazados por una letra. Así `'delete me'` o una columna `"UPDATE"` no disparan el chequeo.
- Rechaza palabras (fuera de comentarios/literales/identificadores citados): `INSERT UPDATE DELETE MERGE DROP ALTER CREATE TRUNCATE GRANT REVOKE EXEC EXECUTE CALL BEGIN DECLARE COMMIT ROLLBACK SAVEPOINT LOCK RENAME INTO SHUTDOWN KILL BACKUP RESTORE DBCC WAITFOR OPENROWSET OPENQUERY OPENDATASOURCE BULK SP_EXECUTESQL`, cualquier `XP_*`, y paquetes/tipos Oracle con efectos: `DBMS_LOCK DBMS_SCHEDULER DBMS_JOB DBMS_SQL DBMS_PIPE DBMS_ALERT DBMS_FILE_TRANSFER UTL_HTTP UTL_TCP UTL_SMTP UTL_FILE UTL_INADDR HTTPURITYPE`. (`FOR UPDATE` cae por `UPDATE`; `SELECT … INTO` por `INTO`.)
- Un `;` final se acepta y **se quita** del texto a ejecutar (Oracle da `ORA-00911` con él). Más de un `;` o texto después del `;` → rechazo. Comentario/literal/identificador sin cerrar → rechazo con motivo.
- Mensaje de rechazo sugiere citar la palabra si es el nombre de una columna (`"call"` / `[call]`).
- **Es una red contra errores, NO una frontera de seguridad**: una función de la base con efectos secundarios invocada desde un `SELECT` no se detecta leyendo texto. La protección real es un usuario de base de datos de solo lectura + la transacción que nunca se confirma.

### 5.3 [QueryRunners.cs](DataBaseSDK.Core/QueryRunners.cs)
`QueryRunnerBase.ExecuteAsync` (plantilla común):
1. `ReadOnlySqlGuard.Validate` (si rechaza → `QueryRejectedException`, **sin abrir la conexión**: hay una prueba que lo verifica).
2. `CreateConnection(connectionString)` → `OpenAsync` → `ApplyContextAsync(options)` (antes de la transacción).
3. `BeginReadOnlyAsync` → comando con `CommandTimeout = options.TimeoutSeconds` y `ConfigureCommand`.
4. `ExecuteReaderAsync`; columnas con `GetFieldType` / `GetDataTypeName` envueltos en try (un tipo raro no rompe la consulta); filas con `ReadCell` hasta `MaxRows`; al pasar el tope se hace `command.Cancel()` y `Truncated = true`.
5. **Nunca hay `Commit`**: al salir, la transacción se revierte.

`OracleQueryRunner`: `ApplyContextAsync` ejecuta `ALTER SESSION SET CURRENT_SCHEMA = "X"` solo si `options.Schema` cumple `^[A-Za-z0-9_$#]{1,128}$` (el nombre se **interpola** porque Oracle no admite bind ahí; la lista blanca impide inyección; si no cumple → `QueryRejectedException`); `BeginReadOnlyAsync` abre la transacción y ejecuta `SET TRANSACTION READ ONLY` (debe ser la primera sentencia: por eso el contexto va antes); `InitialLOBFetchSize = 4001`; `ReadCell` convierte `OracleClob`/`OracleBlob`/`OracleBFile`/`OracleXmlType` a texto y, ante `OverflowException` (NUMBER con más precisión que `decimal`), usa `GetOracleDecimal(i).ToString()`.

`SqlServerQueryRunner`: `ApplyContextAsync` → `connection.ChangeDatabaseAsync(options.Database)` (el driver cita el nombre; no se concatena); transacción `ReadCommitted`.

`QueryResultCsv.Write/Format`: separador coma, fechas `yyyy-MM-dd HH:mm:ss`, números con punto (cultura invariante), `true/false`, escapado con comillas cuando hay `, " \r \n`.

### 5.4 [CellText.cs](DataBaseSDK.Core/CellText.cs)
**Una sola definición** de cómo se convierte una celda en lo que se ve y se compara. La grilla, el CSV y la edición pasan por aquí; la edición compara el valor *actual* de la base contra el que el usuario *vio*, y para que esa comparación sea justa ambos lados usan exactamente este formato.
- `Normalize(object?)`: `null`/`DBNull` → `null`; `string` → `Truncate`; `byte[]` → `0x` + hex de los primeros 32 bytes (+ `… (N bytes)`); `Guid` → texto; `TimeSpan` → texto; `DateTimeOffset` → `yyyy-MM-dd HH:mm:ss zzz`; números/`bool`/`DateTime` sin cambio; cualquier otro → `Convert.ToString(invariante)` truncado.
- `Truncate`: tope **4000** caracteres + `…`. `IsTruncated(text)`: `Length == 4001` y termina en `…` → esa celda **no se puede editar** (guardarla borraría el resto).
- `Format`: `null` → `null`; `DateTime` → `yyyy-MM-dd HH:mm:ss` (**sin fracciones de segundo**: límite conocido); `bool` → `true`/`false`; `IFormattable` → invariante; resto `ToString()`.

### 5.5 Conexiones — [ConnectionStore.cs](DataBaseSDK.Core/ConnectionStore.cs) y [DbConnectionCatalog.cs](DataBaseSDK.Core/DbConnectionCatalog.cs)

**Dos orígenes** que se mezclan en la misma lista (`IDbConnectionCatalog.List(kind)`, que se lee en cada consulta, no se cachea):
1. **`.env`**: variables `ORACLE_CONN_<ALIAS>` / `SQLSERVER_CONN_<ALIAS>` con la cadena de conexión normal del driver. Origen `Env` (solo lectura en la UI). `AllowWrite` = el alias figura en `DBSDK_WRITE` (coma o `;`, sin distinguir mayúsculas; `*` = todos).
2. **Guardadas desde la UI** (`＋ Conexión`): `IDbConnectionStore` → `<repo>\.local\db-connections.json`. Origen `Saved` (editables y borrables). `AllowWrite` = casilla de la conexión.
Si un alias está en los dos lados **gana el del `.env`**.

`StoredConnection` (se guarda por **campos**, no como cadena): `Alias, Kind, Host, Port?, Database?` (SQL Server, base inicial), `OracleMode` (`ServiceName` | `Sid` | `Tns`), `OracleTarget` (servicio / SID / alias de `tnsnames.ora`), `WindowsAuth`, `User`, `Password`, `TrustServerCertificate` (defecto `true`), `AllowWrite` (defecto `false`).

`ConnectionStringFactory.Build` usa los *builders* de cada driver (escapan bien claves con `; = " '`): SQL Server → `DataSource = host[,puerto]`, `ApplicationName = "DataBaseSDK.Web"`, `ConnectTimeout = 15`, `TrustServerCertificate`, `IntegratedSecurity` o `UserID/Password`; Oracle → `DataSource` = `host:puerto/servicio` (ServiceName, puerto por defecto 1521) | descriptor `(DESCRIPTION=(ADDRESS=…)(CONNECT_DATA=(SID=…)))` | alias TNS tal cual, `ConnectionTimeout = 15`. `Describe` arma el resumen **sin secretos** para la UI.

`ConnectionValidation.Validate`: alias `^[A-Za-z0-9][A-Za-z0-9_.\-]{0,39}$`; servidor obligatorio (salvo Oracle TNS); puerto 1..65535; Oracle exige servicio/SID/alias y usuario; SQL Server exige usuario salvo autenticación de Windows; en Oracle `ServiceName`/`Sid`, host y servicio **no** pueden tener espacios, paréntesis ni `=` (terminan dentro de un descriptor TNS: bloquea inyectar otro bloque).

**Cifrado.** `DpapiSecretProtector` usa `ProtectedData` con `DataProtectionScope.CurrentUser` y un texto de **entropía** fijo. ⚠️ Ese texto es `"AzureDevOpsSdk.Database.v1"` **a propósito** (heredado del proyecto de origen, para que las contraseñas importadas se puedan descifrar). **No cambiarlo**: dejaría ilegibles las claves ya guardadas (habría que volver a escribirlas). Solo el mismo usuario de Windows en la misma máquina las descifra; si falla el descifrado (`CryptographicException`) la conexión sigue en la lista pero sin clave (hay que reescribirla).

`DbConnectionStore`:
- JSON `{"connections":[{alias, kind (texto del enum), host, port, database, oracleMode, oracleTarget, windowsAuth, user, trustServerCertificate, allowWrite, passwordProtected (base64 DPAPI)}]}`.
- `Save`: crea o reemplaza por `Kind + Alias` (sin distinguir mayúsculas). `Password == null` → **conserva** la clave guardada; `""` → la borra.
- Escritura **atómica** (`.tmp` + `File.Move(overwrite)`); archivo corrupto → copia `.corrupto` y arranca vacío; todo bajo un `lock`.
- Ruta: `DBSDK_CONNECTIONS_FILE` o `<repo>\.local\db-connections.json`.
- **Copia versionada sin contraseñas** (`<repo>\db-connections.shared.json`; `DBSDK_SHARED_CONNECTIONS_FILE` cambia la ruta, `none` la apaga; con una ruta explícita del almacén —las pruebas— no se usa salvo que se pase `sharedPath`). Mismos campos que el JSON local **menos** `passwordProtected` y `allowWrite` (la clase `SharedRow` ni siquiera los tiene). `Save` la actualiza, `Delete` quita de ambos lados, y al construir el almacén se agregan las locales que falten (sin pisar lo que ya trae el repo). `List` une las dos: las que solo están en la copia versionada (repo recién clonado en otra máquina) aparecen **sin clave**; al escribirla y guardar, la clave queda solo en el local cifrada. Orden estable (motor, alias) y sin reescribir si nada cambió, para que el diff de git sea limpio. Archivo dañado → no se toca (se arregla con git) y lo local sigue funcionando.
- **Importación única** (§8.3): la primera vez, si aún no existe el archivo propio, trae las conexiones de `..\AzureDevOpsSdk\.local\db-connections.json` (o de `DBSDK_IMPORT_CONNECTIONS_FROM`; `none` la apaga), **forzando `allowWrite = false`**: la edición de datos nunca se importa. No se vuelve a importar aunque después se borren todas (el archivo propio ya existe). El archivo de origen no se toca. Origen ilegible → se ignora sin fallar.

### 5.6 Explorador — [Explorer.cs](DataBaseSDK.Core/Explorer.cs) y [Explorers.cs](DataBaseSDK.Core/Explorers.cs)
`IDbExplorer : ITableDescriber` — el "explorador de objetos" de SSMS / SQL Developer. Solo **lee el catálogo**; **todo nombre que llega del cliente viaja como parámetro** (SQL Server cambia de base con `ChangeDatabase`; el esquema de Oracle, en `ALTER SESSION`, se valida con lista blanca).

Operaciones: `TestAsync` (nunca lanza: devuelve `ConnectionTestResult(Ok, Message)`), `ListDatabasesAsync`, `ListSchemasAsync(database, includeSystem)`, `ListObjectsAsync(database, schema, kind)` → `ExplorerPage<string>` (tope 2000, `Truncated`), `ListGroupAsync(…, group)` (`Columns`, `Indexes`, `ForeignKeys`, `Triggers`, `Parameters`, `Members`), `GetDefinitionAsync`, `SearchAsync(database, filter)` (tope 200), `DescribeTableAsync` (§5.7.2).

`ExplorerBase`: abre la conexión, `QueryAsync(sql, maxRows, params (Name, Value)[])` crea los parámetros por nombre y lee hasta el tope; `LikeContains/LikePrefix` escapan `% _ \` (`ESCAPE '\'`) y ponen en mayúsculas; `GuardTestAsync` envuelve el test. Timeout de catálogo 30 s.

**SQL Server** (`SqlServerExplorer`): tipos `Table View Procedure Function Sequence Synonym` (códigos de `sys.objects.type`: `U`, `V`, `P/PC`, `FN/IF/TF/FS/FT`, `SO`, `SN`). Bases: `sys.databases WHERE state = 0 AND HAS_DBACCESS(name) = 1` (las de `database_id <= 4` son "de sistema"). Esquemas: los que tienen objetos propios (`is_ms_shipped = 0`); marca el actual con `SCHEMA_NAME()`. Columnas: `sys.columns` + PK vía `sys.indexes/index_columns` (`is_primary_key`), `is_identity`, `is_computed`, `TYPE_NAME`. Índices, FK (`sys.foreign_keys`), triggers (`sys.triggers`), parámetros (`sys.parameters`). Definición: `OBJECT_DEFINITION(OBJECT_ID(QUOTENAME(@p_schema)+'.'+QUOTENAME(@p_name)))` (vistas/procedimientos/funciones); sinónimos y secuencias se reconstruyen; **las tablas no tienen DDL** (se ven columnas/índices/claves). Búsqueda: `LIKE @p_filter` y orden "empieza con" primero (`@p_prefix`).

**Oracle** (`OracleExplorer`): `Kinds` = SQL Server + `Package` y `Type`. Sin nivel "bases" (`HasDatabases = false`; `ListDatabasesAsync` devuelve vacío): el árbol arranca en los esquemas (`all_users`; el esquema actual — `SYS_CONTEXT('USERENV','CURRENT_SCHEMA')` — primero; los de sistema —lista `SystemOwners` + `APEX_%`/`FLOWS_%`— ocultos salvo `includeSystem`). Objetos: `all_objects` (`generated = 'N'`, sin `BIN$%`), con subselect `ROWNUM` (sirve en cualquier versión; `FETCH FIRST` exige 12c). Columnas: `all_tab_columns` + PK vía `all_constraints`/`all_cons_columns`. Índices (`all_indexes`/`all_ind_columns`, `LISTAGG`), FK, triggers (`all_triggers`), parámetros (`all_arguments`), miembros de paquete (`all_procedures`). Definición: PL/SQL desde `all_source` (se antepone `CREATE OR REPLACE`; paquetes y tipos con spec y body separados por `/`), el resto con `DBMS_METADATA.GET_DDL`. Búsqueda excluye esquemas de sistema pero **deja los sinónimos `PUBLIC`** (`DUAL`). `BindByName = true` y `InitialLOBFetchSize = -1` (el DDL llega completo).

### 5.7 Edición de filas — [RowEditing.cs](DataBaseSDK.Core/RowEditing.cs) y [RowEditors.cs](DataBaseSDK.Core/RowEditors.cs)

La **única** ruta de escritura. Se activa por conexión (`AllowWrite`).

#### 5.7.1 ¿Cuándo una grilla es editable? — `SingleTableAnalyzer.Analyze(sql)`
Análisis **de texto y deliberadamente conservador** (ante la duda, no es editable). Enmascara comentarios y literales `'…'`; tokeniza; exige: empieza con `SELECT`; a profundidad 0 **no** hay `JOIN UNION INTERSECT EXCEPT MINUS GROUP HAVING DISTINCT PIVOT UNPIVOT APPLY INTO`; **no** hay agregados (`COUNT SUM AVG MIN MAX`) antes del `FROM`; el `FROM` no es subconsulta ni variable (`@x`) ni tabla remota (`t@link`); una sola tabla (cadena de 1 a 4 partes, citada con `[ ]` o `" "`) con alias opcional (con o sin `AS`) y pista `WITH (…)`; **lo que siga debe ser** `WHERE ORDER FETCH OFFSET OPTION LIMIT ; )` o el fin (si no, "no se pudo interpretar el FROM"). Devuelve `SingleTable(Parts, Quoted)` o un motivo en español. Las subconsultas en el `SELECT`/`WHERE` (profundidad > 0) se toleran.

#### 5.7.2 Estructura de la tabla — `ITableDescriber.DescribeTableAsync`
Implementada por ambos exploradores → `TableMetadata(Schema, Name, Columns)` con **nombres exactos del catálogo**, o `null` si no es una **tabla** (vistas y sinónimos → `null`). Cada `TableColumn(Name, DataType, Category, Nullable, IsPrimaryKey, IsIdentity, IsComputed)`; `IsEditable = !PK && !identity && !computed && Category != Unsupported`.
- SQL Server: `OBJECT_ID(…, 'U')` (sin esquema resuelve como un `SELECT`: esquema por defecto y luego `dbo`).
- Oracle: `all_tab_columns` ⨝ `all_tables`; sin esquema usa `CURRENT_SCHEMA` de la sesión; `virtual_column` = calculada; `all_tab_identity_cols` (12c+) en `try/catch(DbException)`: en 11g la vista no existe y simplemente no hay identity que marcar.
- `ColumnCategories.From(kind, dataType, scale)`: `Integer` (`tinyint/smallint/int/bigint`; Oracle `NUMBER` con escala 0), `Decimal` (`decimal/numeric/money/float/real`; Oracle `NUMBER` con escala o sin ella, `FLOAT`, `BINARY_FLOAT/DOUBLE`), `String` (`char/varchar/nchar/nvarchar`; Oracle `VARCHAR2/NVARCHAR2/CHAR/NCHAR`), `DateTime` (`date/datetime/datetime2/smalldatetime`; Oracle `DATE` y `TIMESTAMP` **sin** zona), `Bool` (`bit`), `Guid` (`uniqueidentifier`), y **`Unsupported`** para todo lo demás (binarios, LOB, XML, `rowversion/timestamp`, intervalos, zonas horarias, tipos propios).

#### 5.7.3 `IRowEditor.PrepareAsync` — ¿se puede editar *este* resultado?
`RowEditorBase.PrepareAsync(connection, context, sql, resultColumns)` devuelve `EditInfo(Editable, Reason, Database, Schema, Table, PrimaryKey[], Columns[])`. **No lanza por "no se puede"**: explica en `Reason`. Orden de comprobaciones: conexión con `AllowWrite` (si no, `Reason = null`: no se dice nada) → `SingleTableAnalyzer` → `ResolveName` del motor → `DescribeTableAsync` (un `DbException` se vuelve motivo) → es tabla → tiene PK → PK de tipo soportado → los nombres de columna del resultado no se repiten (sin distinguir mayúsculas) → **toda la PK está en el `SELECT`** → al menos una columna editable.
`ResolveName`: SQL Server — 1 parte = (base elegida, esquema por defecto, tabla); 2 = (base elegida, esquema, tabla); 3 = (base de la consulta, esquema, tabla); más → "servidor vinculado". Oracle — sin comillas se pasa a **MAYÚSCULAS**, con comillas se respeta; 1 parte usa el esquema elegido (o el de la sesión), 2 = esquema.tabla; más → no editable.
Cada columna del resultado se describe con `EditColumn(Name, Editable, Nullable, Category, Reason)`; las no editables explican por qué ("clave primaria", "automática (identity)", "calculada", "tipo X no soportado", "no es una columna de la tabla (calculada o con alias)").

#### 5.7.4 `IRowEditor.ApplyAsync` — guardar
Entrada `RowEditRequest { Database, Schema, Table, DryRun, Changes: [RowChange { PrimaryKey: {col: valor}, Set: {col: CellEdit{Original, Value}} }] }`; todos los valores viajan como **texto** (`null` = NULL).
1. `connection.AllowWrite` obligatorio, si no `RowEditError.WriteNotAllowed` **sin abrir nada**. Lote no vacío y ≤ **200** filas (`RowEditorBase.MaxRows`).
2. **Vuelve a leer la estructura** de la tabla con `DescribeTableAsync` (lo que dijo el navegador solo sirve para buscarla).
3. **`Plan`** (valida todo *antes* de abrir la conexión de escritura): PK completa y sin columnas extra; ninguna fila repetida; cada columna a cambiar existe y es editable; se omiten las celdas sin cambio real (`Original == Value`); un `Original` truncado (§5.4) se rechaza; `NULL` solo si la columna lo admite; `ParseValue` convierte el texto al tipo (`Integer` → `long`, `Decimal` → `decimal` o `double`, `DateTime` → formatos ISO `yyyy-MM-dd[ HH:mm[:ss[.fff]]]` con `T` o espacio, `Bool` → `true/false/1/0`, `Guid`, `String` → texto sin tocar). Cada fallo es `RowEditError.Invalid` con un mensaje claro.
4. Abre **una conexión** y **una transacción**. SQL Server ejecuta antes `SET LOCK_TIMEOUT 5000` (esperar 5 s por un bloqueo y avisar, en vez de colgarse).
5. **Por cada fila:** (a) **bloquear y leer**: `SELECT <cols a cambiar> FROM <t> [WITH (UPDLOCK, ROWLOCK)] WHERE <PK completa> [FOR UPDATE WAIT 5]` — 0 filas → `NotFound`; más de 1 → `RowCount`. (b) **comparar**: cada columna a cambiar, pasada por `CellText.Normalize/Format`, debe ser igual al `Original` que vio el usuario; si no → `Conflict` con el valor actual en el mensaje (otra persona la cambió; **no se pisa y se descarta todo el lote**). (c) `UPDATE <t> SET <solo columnas cambiadas> WHERE <PK completa>` con **parámetros** (`v0…`, `k0…`) tipados por categoría (`DbType.Int64/Decimal/DateTime(2)/Boolean/Guid/String`); debe afectar **exactamente 1 fila** (`affected != 1` → `RowCount`; incluye el caso de un trigger que cuente filas: se rechaza y revierte).
6. **`DryRun`** → rollback y devuelve las sentencias SQL (sin valores). Bitácora: `prueba (revertida)`.
7. Real → escribe la **bitácora ANTES del commit** (`aplicando`): si no puede (disco lleno, sin permiso) → `RowEditError.Audit` y **rollback** (sin constancia no se guarda). Luego `Commit` y anota `confirmado`.
8. Cualquier `RowEditException` o `DbException` → rollback (silencioso: si la conexión ya cayó, la base descarta sola), anota `rechazado`/`fallido` y relanza como `RowEditException` (`Database` para errores del driver). Cancelación → rollback y relanza.
Las anotaciones posteriores al hecho (`TryAudit`) **nunca cambian el resultado**: un fallo de la bitácora ahí no deshace nada.

El SQL se arma **solo** con `Quote(meta.Schema)`, `Quote(meta.Name)` y los nombres de columna del catálogo (nunca con texto del cliente; hay una prueba con un nombre de tabla malicioso). Oracle rechaza (`Invalid`) un nombre con `"` o caracteres de control.

#### 5.7.5 Bitácora — `IEditAuditLog` / `FileEditAuditLog`
`<repo>\.local\data-edits.log`, **una línea JSON por evento**: `AtUtc, Connection, Database?, Schema, Table, Outcome, DryRun, Error?, Changes[{PrimaryKey, From, To}]` (valores recortados a 200 caracteres). `Outcome` ∈ `aplicando`, `confirmado`, `rechazado`, `fallido`, `prueba (revertida)`. **Contiene datos reales de las bases**: está en `.local/` (ignorada por git); tratarla como dato sensible. Escritura con `lock` y `AppendAllText`.

**Límites conocidos:** una fecha con fracciones de segundo se muestra sin ellas, así que editarla la guarda sin fracciones; en Oracle un texto vacío es `NULL`; un alias de columna que coincide con *otra* columna real (`SELECT a AS b …`) lo frena la verificación de originales (conflicto), no el análisis; solo `UPDATE` (no hay `INSERT`/`DELETE`); máximo 200 filas por vez.

### 5.8 Archivos `.sql` — [SqlFiles.cs](DataBaseSDK.Core/SqlFiles.cs)
`ISqlFileService` / `SqlFileService` (lo que hacen SSMS / SQL Developer con "Abrir" y "Guardar"). Toca **solo archivos `.sql` de hasta 5 MB** (`MaxFileBytes`).
- `Browse(path)`: lista subcarpetas (sin ocultas/sistema) y archivos `*.sql` (extensión exacta; `EnumerateFiles("*.sql")` en Windows también acepta `.sqlx`), tope 2000 entradas; vacío = unidades de disco; `Parent` vacío = "Este equipo".
- `Places()`: Proyecto (raíz del repo), Documentos, Escritorio, Descargas (las que existan).
- `Read(path)`: detecta **codificación** (`utf8-bom`, `utf16le`, `utf16be`, `utf8` estricto, si no es UTF-8 válido → `latin1`) y el **salto de línea dominante** (CRLF/LF); devuelve el texto normalizado a `\n`, el `Eol`, la codificación y la **versión** (`LastWriteTimeUtc.Ticks`).
- `Save(path, text, eol, encoding, expectedVersion, overwrite)`: reescribe con el mismo `Eol` y codificación. Si el archivo cambió en disco desde que se abrió (`version` distinta) → `Conflict`; si no existe pero se esperaba (`expectedVersion != null`) → `NotFound` (no lo recrea en silencio); si existe y no hay versión esperada exige `overwrite` (`AlreadyExists`). `overwrite = true` salta ambas comprobaciones (el JS lo manda tras confirmar con el usuario).
- `Locate(name, size, folders)`: el navegador entrega el **contenido** de un archivo arrastrado pero nunca su **ruta**; esto busca `name` con exactamente `size` bytes en las carpetas dadas (carpetas de trabajo y recientes). Límites: profundidad 5, 4000 carpetas, 2 s, 5 coincidencias, máx. 30 carpetas raíz, sin ocultas/sistema/reparse points; el nombre no puede traer ruta (`\ / : * ?`) y debe terminar en `.sql`. Un disco enorme puede no encontrarlo (no es un error).
- Errores tipados `SqlFileError { NotFound, AccessDenied, TooLarge, NotSql, InvalidPath, Conflict, AlreadyExists, Io }` con mensajes en español (la UI muestra el mensaje; el código decide el comportamiento).

### 5.9 Pequeños
- [LocalAccess.cs](DataBaseSDK.Core/LocalAccess.cs): `IsLocal(IPAddress? remote, string? host)` — loopback **y** `Host` de loopback (`localhost`, `127.x`, `::1`, con o sin corchetes; IPv4 mapeada a IPv6 incluida). Lo del `Host` evita el *DNS rebinding*. Pura (sin ASP.NET) para poder probarla.
- [ProjectPaths.cs](DataBaseSDK.Core/ProjectPaths.cs): `Root` = primera carpeta hacia arriba (desde `AppContext.BaseDirectory` y desde el directorio actual) que contenga **`DataBaseSDK.sln`**; si no la encuentra, el directorio actual. `DataDirectory` = `<Root>\.local`. ⚠️ Renombrar o mover el `.sln` rompe esto.

### 5.10 DI — `AddDatabaseConsole()` ([ServiceCollectionExtensions.cs](DataBaseSDK.Core/ServiceCollectionExtensions.cs))
Todo **singleton**: `ISecretProtector` (DPAPI), `IDbConnectionStore`, `IDbConnectionCatalog`, dos `ISqlQueryRunner`, `ISqlFileService`, `IWorkspaceStore`, dos `IDbExplorer`, `IEditAuditLog` (`FileEditAuditLog`), dos `IRowEditor` (cada uno recibe el explorador de su motor como `ITableDescriber`). Los consumidores reciben `IEnumerable<…>` y eligen por `Kind`.

### 5.11 Sesión versionada — [Workspace.cs](DataBaseSDK.Core/Workspace.cs)
`IWorkspaceStore` / `WorkspaceStore` guarda en **`<repo>\db-workspace.json` (versionado)** lo que estaba abierto en cada consola, para que (1) al pasar de Oracle a SQL Server y volver no se pierda la conexión elegida, y (2) al subir a git y abrir el proyecto en otra computadora aparezcan las mismas pestañas.
- Formato: una sección por motor (`"Oracle"`, `"SqlServer"`) con `alias`, `database` (SQL Server), `schema` (Oracle), `files[]` (cada uno `{path, alias, database, schema}`: la conexión de esa pestaña) y `activeFile`; el `alias`/`database`/`schema` de la sección es la última conexión elegida, la que heredan las pestañas nuevas. **Solo rutas y nombres**: nunca el texto de las consultas ni contraseñas (el texto sin guardar sigue en la caché del navegador, `qc-session-<kind>`).
- Rutas dentro del repo se guardan **relativas con `/`** (`QuerysJean/…/x.sql`) y vuelven como ruta completa de la máquina que lee; las de fuera del repo quedan absolutas (en otra máquina simplemente no se abren). Una relativa con `..` que sale del repo se ignora.
- Validación: solo rutas completas a `.sql` (≤ 1024 caracteres, sin caracteres de control), sin repetir (sin distinguir mayúsculas), máximo **50** (`MaxFiles`); el activo debe estar en la lista; nombres de conexión/base/esquema ≤ 128 sin caracteres de control. `SaveWorkspace` con `context: true` solo acepta un alias que exista en el catálogo.
- `Stamp`: hash del contenido de la sección (con rutas relativas, así es igual en todas las máquinas). El navegador lo guarda en su sesión; si al arrancar el del repo es otro, el archivo cambió por fuera (git pull) y las pestañas se igualan (§7.1).
- Sin reescribir si nada cambió, secciones en orden fijo, escritura atómica: diff de git limpio. Archivo dañado (p. ej. conflicto de merge) → no se pisa y se ignora. Ruta: `DBSDK_WORKSPACE_FILE` (`none` lo apaga).
- La página, al cargar sin `Alias` (GET), toma la conexión de su sección si sigue existiendo (y su base/esquema); si no, la primera. `Run` la recuerda; el JS también al cambiar conexión, base o esquema.

---

## 6. La capa web

### 6.1 [Program.cs](DataBaseSDK.Web/Program.cs)
`LoadDotEnv()` (busca `.env` desde el directorio actual hacia arriba; `KEY=VALUE`; comillas externas se quitan; **no pisa** variables ya definidas) → puerto (`DBSDK_PORT`, defecto 5130, `UseUrls("http://localhost:{port}")`: solo loopback) → `AddRazorPages` + `AddDatabaseConsole` → fuera de Development `UseExceptionHandler("/Error")` → **middleware solo-local para TODO** (páginas, endpoints y estáticos): si `!LocalAccess.IsLocal(...)` responde `403 text/plain` → `UseStaticFiles` → `UseRouting` → `MapRazorPages`. Si `DBSDK_OPEN_BROWSER=1`, al arrancar abre el navegador. No hay `UseAuthorization`: no hay auth.
El antiforgery de Razor Pages está activo por defecto en los POST.

### 6.2 Páginas
- `/` ([Index](DataBaseSDK.Web/Pages/Index.cshtml)): portada con una tarjeta por motor (cuántas conexiones, cuántas con edición habilitada) y un recuadro de "Primeros pasos" si no hay ninguna.
- `/SqlServer`, `/Oracle`: `@page` + `@model SqlServerModel|OracleModel` + `<partial name="_QueryConsole">` + `<partial name="_QueryScripts">`, que carga los 9 scripts **en este orden** (importa: cada uno depende de los anteriores): `qc-common.js`, `qc-mru.js`, `qc-workspace.js`, `query-editor.js`, `query-complete.js`, `query-edit.js`, `query-results.js`, `query-explorer.js`, `query-console.js`.
- `/Error`.

### 6.3 [QueryConsolePageModel.cs](DataBaseSDK.Web/Pages/QueryConsolePageModel.cs)
Clase base abstracta (`OracleModel`, `SqlServerModel` solo fijan `kind`). Propiedades enlazadas (`[BindProperty]`): `Alias`, `Sql`, `MaxRows`, `Database`, `Schema`. `OnPageHandlerExecuting`: segunda barrera `LocalAccess` (403 con texto), carga `Connections`, y si no hay `Alias` toma la primera conexión. Propiedades de resultado: `Result`, `Edit` (`EditInfo?`), `EditHint`, `Error`, `Rejected`.

**Todos los endpoints** (ruta `/<Pagina>?handler=<Nombre>`; los POST con cuerpo JSON llevan la cabecera `RequestVerificationToken`, que `QC.post` lee del `<input name="__RequestVerificationToken">` de la página):

| Handler | Verbo | Entrada | Respuesta |
|---|---|---|---|
| `Run` | POST (formulario) | `Alias, Sql, TabId, MaxRows, Database/Schema` | página entera con resultado/Error (respaldo sin JS) |
| `RunPartial` | POST (`fetch`, FormData) | ídem | **solo** `_QueryResults.cshtml` (lo que usa el editor: no recarga la página; abortar el `fetch` cancela la consulta) |
| `Csv` | POST (formulario) | ídem | archivo `.csv` (UTF-8 con BOM) |
| `Connections` | GET | — | `[{alias, origin: "saved"|"env", description, editable, allowWrite}]` |
| `Connection` | GET | `alias` | campos del formulario (**sin contraseña**; `hasPassword`); 404 si es del `.env` |
| `SaveConnection` | POST JSON | `ConnectionForm` (`isEdit`) | `{ok}` / 400 `{error}` |
| `TestConnection` | POST JSON | `ConnectionForm` | `{ok, message}` |
| `DeleteConnection` | POST JSON | `{alias}` | `{ok}` / 400 (solo las guardadas) |
| `Databases` | GET | `alias` | `[{name, isSystem}]` |
| `Schemas` | GET | `alias, database, system` | `[{name, isSystem, isCurrent}]` |
| `Objects` | GET | `alias, database, schema, kind` | `{items:[{name, hasChildren}], truncated}` |
| `Group` | GET | `alias, database, schema, kind, name, group` | `[{name, detail}]` |
| `Definition` | GET | `alias, database, schema, kind, name` | `{text}` (null si no hay) |
| `Search` | GET | `alias, database, filter` | `{items:[{schema, name, kind}], truncated}` |
| `Browse` / `Places` | GET | `path` / — | listado de carpetas / accesos rápidos |
| `ReadFile` / `StatFile` | GET | `path` | contenido / existencia+versión |
| `LocateFile` | POST JSON | `{name, size, folders[]}` | `{matches[]}` |
| `SaveFile` | POST JSON | `{path, text, eol, encoding, expectedVersion, overwrite}` | `{exists, version, size}` |
| `ApplyEdits` | POST JSON | `ApplyEditsBody` = `RowEditRequest` + `alias` | `{ok, rowsAffected, dryRun, statements}` |
| `Workspace` | GET | — | `{alias, database, schema, files[], activeFile, stamp}` (rutas completas; §5.11) |
| `SaveWorkspace` | POST JSON | `WorkspaceBody`: `{context: true, alias, database, schema}` o `{files[{path, alias, database, schema}], activeFile}` | el mismo objeto que `Workspace` |

**Contratos de error.** Explorador: cualquier excepción → 400 `{error: mensaje del driver}`; cancelación → 499; conexión desconocida → 400. Archivos (`FileCall`): `{error, code}` con `not_found` 404, `access_denied` 403, `too_large` 413, `not_sql` 400, `invalid_path` 400, `conflict` 409, `exists` 409, `io` 500. `ApplyEdits`: `{error, code}` con `write_not_allowed` 403, `not_editable` 400, `invalid` 400, `not_found` 404, `conflict` 409, `row_count` 409, `audit` 500, `database` 400. El JS (`QC.get/post`) convierte toda respuesta no-OK en un `Error` con `.code` y `.status`; si no hay respuesta (`fetch` falla) → `code = 'network'` ("¿sigue corriendo el sitio?").

**Seguridad en los handlers.** Los nombres del cliente nunca se concatenan en SQL; `SaveConnection` valida con `ConnectionValidation` y la contraseña nunca vuelve; los handlers de archivos solo tocan `.sql`; `ApplyEdits` busca la conexión por alias en el **catálogo del servidor** (si `AllowWrite` es falso, el núcleo la rechaza aunque el navegador diga otra cosa).

### 6.4 [_QueryConsole.cshtml](DataBaseSDK.Web/Pages/Shared/_QueryConsole.cshtml)
Una sola vista para los dos motores (el `@model` es la base `QueryConsolePageModel`). Contiene: cabecera con dos badges (rojo "✏️ Edición de filas habilitada" — se muestra según la conexión elegida —, gris "🔒 Consultas de solo lectura"); layout de hasta 5 columnas (**panel izquierdo** con pestañas *Objetos* / *Archivos .sql*, **divisor** arrastrable, **editor + resultados**, otro **divisor** y el **explorador de archivos** de la derecha, oculto por defecto: §7.4); formulario `Run` con selectores (conexión, base de datos *o* esquema, filas máx., historial) y el botón *🗂 Explorador* (`data-role="toggle-explorer"`); pestañas de documentos; barra de botones del editor (con **▶ Ejecutar** al principio: no hay otra fila de botones ni texto de ayuda debajo del editor —la ayuda está en el tooltip— para que el resultado quede pegado a la consulta en los pantallazos de evidencia; el **⬇ CSV** es un botón pequeño en la línea de resumen del resultado, fuera del formulario y asociado con `form="qc-run-form"`); el editor (`<textarea data-role="sql">` + `<canvas data-role="gutter">`); la grilla; tres modales (*conexión*, *confirmar cambios de la grilla*, *selector de rutas*); y un bloque `<style>` con todo el CSS propio (clases `qc-*`).
El contenedor raíz lleva los datos para el JS: `data-query-console data-kind="oracle|sqlserver" data-base-url data-kinds (JSON) data-has-databases data-selected-alias data-selected-database data-selected-schema`. Los elementos con comportamiento se localizan por **`data-role="…"`** (no por id/clase).
La grilla (`<div class="qc-results" data-role="results-grid" data-edit="…JSON…">`): `data-edit` **solo existe si el resultado es editable** y trae `{alias, database, schema, table, primaryKey[], columns[{name, editable, nullable, category, reason}]}`; las celdas nulas llevan la clase `qc-null`; las cabeceras editables llevan `✏️`. Cada celda usa `CellText.Format` (el mismo texto que se compara al guardar).

---

## 7. El front-end (JavaScript plano)

Sin framework, sin transpilar. Cada archivo es una IIFE; comparten el espacio global `window.QC`. Los archivos con lógica pura (`qc-mru.js`, `qc-workspace.js`, `query-complete.js`, `query-edit.js`, `query-explorer.js`) se dividen en **lógica sin DOM** (exportada con `module.exports` y probada con `node --test`) e **interfaz** (solo si hay `window` y `document`). Orden de carga: ver §6.2.

| Archivo | Responsabilidad |
|---|---|
| [qc-common.js](DataBaseSDK.Web/wwwroot/js/qc-common.js) | `window.QC`: `el`, `qs`, `readJson/writeJson` (localStorage con try/catch), `get(handler, params)`, `post(handler, body)` (JSON + token antiforgery, errores con `.code`/`.status`), `toast`, `node` (nodo de árbol perezoso), `selectRow`, `clearSelection`, `muted`; `QC.editor` lo define query-editor |
| [qc-mru.js](DataBaseSDK.Web/wwwroot/js/qc-mru.js) | orden de uso reciente de las pestañas para `Ctrl+Tab` (lógica pura: `window.QC.mru`) |
| [qc-workspace.js](DataBaseSDK.Web/wwwroot/js/qc-workspace.js) | sesión versionada (lógica pura: `window.QC.workspace`): `snapshot` (qué mandar), `signature` (evita mandar lo mismo en cada tecla), `reconcile` (qué abrir/cerrar si el repo cambió), caché de resultados por pestaña (`putResult`, `dropOldest`, `removeResults`) |
| [query-editor.js](DataBaseSDK.Web/wwwroot/js/query-editor.js) | pestañas, archivos `.sql`, selector de rutas, gutter, caché de sesión, arrastrar y soltar, atajos, `Ctrl+Tab`, panel izquierdo |
| [query-complete.js](DataBaseSDK.Web/wwwroot/js/query-complete.js) | autocompletado (lógica + ventanita) |
| [query-edit.js](DataBaseSDK.Web/wwwroot/js/query-edit.js) | edición de celdas de la grilla (validación + armado del pedido + UI) |
| [query-results.js](DataBaseSDK.Web/wwwroot/js/query-results.js) | resultado propio por pestaña (§7.3b): guarda el de la pestaña que ejecutó y muestra el de la activa |
| [query-explorer.js](DataBaseSDK.Web/wwwroot/js/query-explorer.js) | explorador de archivos de la derecha (carpeta abierta como árbol, `Ctrl+K`) |
| [query-console.js](DataBaseSDK.Web/wwwroot/js/query-console.js) | conexiones (diálogo, probar/guardar/borrar), explorador en árbol, panel de detalle, búsqueda, historial, selectores de contexto |

### 7.1 Editor ([query-editor.js](DataBaseSDK.Web/wwwroot/js/query-editor.js))
- **Es un `<textarea>`** (sin resaltado de sintaxis) con un `<canvas>` de 58 px a la izquierda que dibuja los números de línea y las **marcas de cambio**: barra **naranja `#e2a03f` = modificada y sin guardar**, **verde `#4cc38a` = modificada desde que se abrió y ya guardada**. Se calculan con un diff de líneas (LCS) contra la línea base del archivo abierto y contra el último guardado; en archivos muy grandes (> ~2000 líneas) el LCS se corta y se marca el archivo completo como cambiado.
- **Pestañas:** cada archivo abierto y cada "Consulta N" sin guardar; `●` = cambios sin guardar. `QC.editor` expone `showQuery(text)` (pone una consulta sin pisar un archivo abierto: si la pestaña activa es un archivo abre una nueva), `insertAtCursor(text)` (vía `execCommand('insertText')` para conservar Ctrl+Z), `getRunText()` (selección o todo), `focus()`; y, para el explorador de archivos: `openFile(path)` (abre o activa la pestaña de ese archivo), `pickFolder({title, folder})` (el selector de rutas en modo carpeta → ruta o `null`), `tabsInfo()` y `onTabsChanged(fn)` (`fn({activePath, open[], dirty[]})` se llama ya y en cada cambio de pestañas — `renderTabs` corre en cada tecla, así que los oyentes deben ser baratos).
- **Atajos:** `Ctrl+Enter` o `Ctrl+E` (como SSMS; funciona con el foco en cualquier parte, se le gana al navegador con `preventDefault`) ejecutar (**la selección, o todo el texto si no hay selección**), `Ctrl+S` guardar, `Ctrl+Shift+S` guardar como, `Ctrl+O` abrir, `Ctrl+B` mostrar/ocultar el panel izquierdo, `Ctrl+K` mostrar/ocultar el explorador de archivos de la derecha (§7.4), `Ctrl+Tab` / `Ctrl+Mayús+Tab` cambiar al archivo anterior / al más antiguo (más abajo), `Tab` inserta 4 espacios, `Ctrl+Espacio` fuerza el autocompletado. (En el navegador `Ctrl+N/W/T` están reservados y no se pueden usar; `Ctrl+K` sí se puede secuestrar con `preventDefault`.)
- **`Ctrl+Tab`: cambiar al archivo anterior, como SSMS.** Las pestañas llevan un **orden de uso reciente** (`mru`, lógica pura en [qc-mru.js](DataBaseSDK.Web/wwwroot/js/qc-mru.js): `touch` al activar una pestaña, `remove` al cerrarla, `restore` al arrancar). `Ctrl+Tab` salta a la pestaña usada justo antes —repetido, **alterna entre las dos últimas**—; **manteniendo `Ctrl`** y pulsando `Tab` otra vez se recorren las anteriores (`Mayús+Tab` en sentido contrario, empezando por la más antigua), **al soltar `Ctrl` se cambia** a la elegida y `Esc` cancela. Si `Ctrl` se mantiene 220 ms aparece la lista **"Archivos activos"** (`.qc-switcher`: nombre, `●` si hay cambios, carpeta; las filas se pueden pulsar con el mouse); un toque rápido cambia sin mostrarla. El orden se guarda en la sesión (`qc-session-<kind>.mru`) y **sobrevive a las recargas** (cada consulta ejecutada recarga la página). Con un diálogo abierto no actúa; si la ventana pierde el foco con `Ctrl` apretado se cancela; si se pierde el "soltar Ctrl", cualquier tecla sin `Ctrl` cierra la lista aplicando la elección. **Limitación del navegador:** Chromium **reserva `Ctrl+Tab`** en las pestañas normales (la página ni lo recibe) pero **no** en una ventana tipo aplicación (`msedge --app`, la que abre `start.cmd`): ahí funciona; en una pestaña normal del navegador el atajo no existe.
- **Sesión versionada (§5.11):** además de la caché, las rutas de las pestañas (sin texto) y la activa se mandan a `SaveWorkspace` (solo si cambió la firma, con *debounce*). Al arrancar, tras revisar el disco, se pide `Workspace`: si su `stamp` no es el que esta sesión vio por última vez (git pull, otra computadora) se abren los archivos del repo que falten y se cierran las pestañas **limpias** que el repo ya no tiene (nunca las con cambios, las consultas sin archivo ni las que conservan la caché); una "Consulta 1" vacía sobra y se cierra. Si el repo aún no tiene nada, manda lo local. Antes de esa comparación no se envía nada.
- **Caché de sesión:** las pestañas abiertas (ruta, texto sin guardar, cursor, marcas) se guardan en `localStorage` (`qc-session-<kind>`, una sesión por motor) con *debounce* y al salir (`pagehide`/`beforeunload`); si el almacenamiento está lleno se avisa una vez. Al restaurar se comprueba cada archivo en disco (`StatFile`): **si ya no existe** la pestaña queda marcada ⚠ y tachada **conservando el contenido de la caché** (reintentar / guardar como / cerrar); **si cambió** y no hay cambios locales se recarga; si hay cambios locales se avisa y al guardar se pide confirmación (pisar lo del otro programa).
- **Archivos:** abrir/guardar por el servidor (`ReadFile`/`SaveFile`), con detección de conflicto por `version`; **selector de rutas** propio (modal con accesos rápidos, escribir/pegar una ruta, "Ir", árbol de carpetas) porque el navegador no da rutas reales; panel *Archivos .sql* con **carpetas agregadas** (`qc-roots`) y **recientes** (`qc-recent`, 12).
- **Arrastrar y soltar:** hasta **10** archivos `.sql` de ≤ 5 MB a cualquier parte de la página; se ignora lo que no es `.sql`. Como el navegador no entrega la ruta: `LocateFile` busca por nombre+tamaño en las carpetas de trabajo/recientes/pestañas y el cliente **confirma que el contenido sea idéntico**; si lo encuentra, queda enlazado al archivo real; si no, se abre como "archivo arrastrado" sin ruta y *Guardar* pregunta la carpeta.
- **Panel izquierdo:** ancho y visibilidad en `qc-side` (`{hidden, width}`, defecto 380); el divisor se arrastra (doble clic = ancho original); pestaña activa en `qc-pane`.

### 7.2 Autocompletado ([query-complete.js](DataBaseSDK.Web/wwwroot/js/query-complete.js))
Estilo IntelliSense. Se abre solo al escribir 2+ letras o un punto (debounce 150 ms; 0 tras un punto) y se fuerza con `Ctrl+Espacio`; ↑/↓ eligen, `Enter`/`Tab` aceptan, `Esc` cierra.
- **Lógica pura** (exportada: `maskSql, tokenize, parseTables, analyze, splitChain, quoteName, objectInsert, keywordItems, rank, KEYWORDS`): `analyze(text, caret)` decide el modo — `none | object | column | qualified | keyword` — según la cláusula (`FROM/JOIN/coma` → objetos; `SELECT/WHERE/ON/AND/ORDER BY…` → columnas de las tablas del `FROM`; `x.` → calificado), con alias (con/sin `AS`), `JOIN`s, listas con comas y subconsultas, y aunque el `FROM` esté *después* del cursor. Excluye lo que se está tecleando (no sugiere en comentarios ni en textos entre comillas). Bug conocido y cubierto por prueba: `FROM ventas.` no toma `ventas` como tabla.
- **Fuentes** (las mismas consultas del explorador vía `QC.get`): `Databases`, `Schemas`, `Objects`, `Group/Columns`, `Search` (filtro por prefijo). **Caché de 5 min** en memoria del navegador (`CACHE_MS`), con filtrado local al afinar el texto. Tope visible de 60 ítems.
- **Qué inserta:** el nombre solo si el objeto está en el esquema por defecto (o es sinónimo `PUBLIC` de Oracle), `esquema.nombre` si no; los nombres con espacios o palabras reservadas salen entre `[ ]` (SQL Server) o `" "` (Oracle).
- La ventanita se posiciona bajo el cursor midiendo columnas con la fuente monoespaciada; la inserción usa `execCommand` (conserva Ctrl+Z). Si el servidor falla, el editor sigue sin sugerencias.

### 7.3 Edición de la grilla ([query-edit.js](DataBaseSDK.Web/wwwroot/js/query-edit.js))
Se engancha a cada grilla `[data-role="results-grid"][data-edit]` con `QC.editGrid.attach(grid)` (la de la carga de la página y cada una que trae `RunPartial`): cada grilla tiene su propio estado de cambios pendientes, su barra (`edit-bar` del mismo bloque) y el diálogo de confirmación es uno solo que trabaja con la grilla que lo abrió. `QC.editGrid.release(nodo)` la olvida (resultado reemplazado o pestaña cerrada) y `pendingIn(nodo)` cuenta cambios sin guardar (se pide confirmación antes de reemplazarlos con otra ejecución). **El navegador no decide qué es editable**: lo dice el servidor en `data-edit`.
- **Lógica pura** (exportada: `isTruncated, validateInput, findColumn, keyText, describeValue, buildRequest`): `validateInput(column, text)` valida al instante por categoría (`Integer`, `Decimal`, `DateTime`, `Bool`, `Guid`; `String` acepta todo; `null` solo si `nullable`) — **el servidor repite la validación**; `buildRequest(info, rows, dryRun)` arma el cuerpo de `ApplyEdits` enviando **solo las celdas realmente cambiadas** con su valor original.
- **Interfaz:** doble clic en una celda `td.qc-editable` abre un editor en la celda (`<input>` + botones ∅ NULL / ✓ / ✕; los botones usan `mousedown.preventDefault` para no robar el foco al campo); Enter/✓ acepta, Esc/✕ cancela, salir de la celda acepta; un valor inválido avisa con un *toast* y deja el editor abierto. Estado: `Map<tr, {columna → {original, value}}>`; el "original" es lo que devolvió el servidor, no lo que se ve (`data-orig` / `data-orig-null` guardados al primer cambio). Celda **naranja** = pendiente; barra roja "N cambio(s) sin guardar en M fila(s)" con *Revisar y confirmar…* y *Descartar cambios*; `beforeunload` avisa si hay pendientes.
- **Diálogo de confirmación** (modal estático): tabla *Fila (clave primaria) · Columna · Antes (tachado) · Después*; casilla **"Entiendo que esto modifica datos reales de {alias}"** (sin marcarla el botón *Confirmar y guardar* está deshabilitado); **Probar (no guarda)** (`dryRun: true`); al guardar con éxito las celdas pasan a **verde** y su valor pasa a ser el nuevo original (sin volver a consultar), con un *toast*. Errores: se muestran en el diálogo (los de `conflict`/`not_found` ya dicen que no se guardó nada); el cambio sigue pendiente.
- El alias que viaja es el **de la consulta** (`data-edit.alias`), no el del selector actual (que pudo cambiar después).

### 7.3b Cada pestaña es su propia consola (como las ventanas de consulta de SSMS)
- **Conexión por pestaña.** Cada pestaña guarda `alias`, `database` (SQL Server) y `schema` (Oracle) en `qc-session-<kind>` y, si es un archivo, en `db-workspace.json` (§5.11). Al activar una pestaña, `query-console.js` (`followActiveTab`, enganchado con `QC.editor.onTabsChanged` recién cuando la lista de conexiones cargó) pone esos valores en los selectores sin disparar "cambio del usuario" (`applying`); lo que el usuario elige (o el árbol con *SELECT 100*) cambia solo la pestaña activa (`QC.editor.setActiveContext`). Una pestaña sin contexto (nueva, o un archivo recién abierto) **hereda** la conexión actual. Si la conexión de una pestaña no existe en esta máquina, se queda la actual. Como el formulario `Run` manda lo que muestran los selectores, cada pestaña ejecuta contra su propia conexión.
- **Resultado por pestaña** ([query-results.js](DataBaseSDK.Web/wwwroot/js/query-results.js)). El formulario manda `TabId` (la pestaña activa); el servidor envuelve error/resultado en `<div data-role="results-area" data-tab data-alias data-database data-schema>`. Al cargar, ese bloque se asigna a la pestaña `data-tab`: el nodo vivo queda en memoria (la edición de celdas sigue funcionando, aunque se cambie de pestaña y se vuelva) y una **copia de solo lectura** (sin `data-edit` ni barra de edición) va a **`sessionStorage`** `qc-results-<kind>` (máx. 12 pestañas; si no entra se descartan las más viejas; un resultado de más de ~1,5 M caracteres se guarda solo como "demasiado grande"). Al mostrar una copia se antepone "🕘 Resultado conservado de las HH:MM (conexión)". Cerrar una pestaña olvida su resultado. `sessionStorage` y no `localStorage` a propósito: son datos reales, se borran al cerrar la ventana y nunca quedan en disco ni en git.
- **Ejecutar sin recargar.** Mientras corre, la pestaña muestra "Ejecutando… [Cancelar]" y el botón pasa a "⏳ Ejecutando…" (solo en esa pestaña): **cada pestaña puede tener su consulta en curso a la vez**, y el resultado llega a la pestaña que la lanzó aunque se haya cambiado de pestaña. Cancelar (o cerrar la pestaña) aborta el `fetch` y el servidor corta la consulta (token de la petición). El CSV sigue siendo un `submit` normal (descarga; la página no cambia).
- **Límite:** la edición de celdas solo funciona sobre los resultados obtenidos en esta carga de la página; tras recargar o volver de la otra consola, los resultados conservados son de solo lectura hasta volver a ejecutar.

### 7.4 Explorador de archivos de la derecha ([query-explorer.js](DataBaseSDK.Web/wwwroot/js/query-explorer.js), `Ctrl+K`)
Como el explorador de VS Code: **una carpeta abierta** mostrada como árbol. `Ctrl+K` (o el botón *🗂 Explorador*) lo muestra y lo oculta; no mueve el foco. Es una columna más del layout (`.qc-layout.qc-explorer-open`; ver el CSS `.qc-explorer` / `.qx-*` en `_QueryConsole.cshtml`; en pantallas < 992 px se apila debajo del editor). Con el panel izquierdo (380 px) y el explorador (300 px) abiertos, el editor se angosta en pantallas chicas: `Ctrl+B` oculta el izquierdo.
- **Qué muestra:** subcarpetas y archivos **`.sql`** únicamente (es lo único que `SqlFileService.Browse` lista y `ReadFile` abre), carpetas primero. No hay endpoints nuevos: usa `Browse` y, para abrir, `QC.editor.openFile`.
- **Abrir carpeta:** *📂* del panel (o *Abrir carpeta...* si no hay ninguna) usa el selector de rutas en modo carpeta (`QC.editor.pickFolder`). Sin carpeta se ofrecen las usadas antes (`recent` del explorador + `qc-roots` del panel izquierdo). *✕* de la barra con el nombre cierra la carpeta (no borra nada). Si la carpeta ya no existe sale el error con *Reintentar* / *Cerrar carpeta* y se quita de las recientes (si el fallo es otro, p. ej. permisos, se conserva).
- **Árbol:** click en una carpeta la despliega/pliega (los hijos se piden la primera vez y quedan en memoria); click en un archivo lo abre en una pestaña (`openFile` activa la existente si ya estaba). *⟳* vuelve a leer el disco conservando lo desplegado; *⊟* contrae todo.
- **Archivo activo:** el de la pestaña activa se resalta (`qx-active`) y sus carpetas se despliegan solas al cambiar de pestaña (*auto reveal*); los abiertos van en azul (`qx-opened`) y los modificados sin guardar llevan **●** naranja. Se actualiza con `QC.editor.onTabsChanged` solo si cambió algo (`renderTabs` corre en cada tecla).
- **Teclado** (con el foco en el árbol; `keyAction`, lógica pura): ↑ ↓ Home End mueven la selección; → despliega (o entra al primer hijo si ya está abierta); ← pliega (o sube a la carpeta que contiene); Enter / Espacio abren el archivo o alternan la carpeta; Esc devuelve el foco al editor. Una tecla de navegación pulsada mientras una carpeta aún carga **espera** a que termine (así `→ ↓` cae en su primer hijo).
- **Estado persistente** (`qc-explorer`, compartido por Oracle y SQL Server): `{open, width (200–640, defecto 300), folder, expanded[], recent[] (máx. 8), scroll}`. Cada ejecución de una consulta **recarga la página**, así que el árbol se reconstruye desde ese estado (por niveles, cada nivel en paralelo, `groupByDepth`). Con el panel oculto **no se pide nada al servidor**; la lista `expanded` se mantiene de a una carpeta (no se recalcula desde el árbol: con el árbol aún vacío borraría lo que hay que restaurar — error ya corregido y probado en navegador).
- **Ancho:** el divisor de la izquierda del panel se arrastra (hacia la izquierda ensancha); doble clic = 300 px.
- **Lógica pura** (exportada, probada en `js-tests/query-explorer.test.js`): `normalizePath` (separador `\`, sin barras finales salvo `C:\`), `isInside`, `relativeSegments`, `folderName`, `normalizeState` (lo guardado dañado vuelve a los valores por defecto), `rememberFolder`, `groupByDepth`, `keyAction`. Las rutas se comparan **sin distinguir mayúsculas**.
- **No hace:** crear/renombrar/borrar archivos o carpetas, varias carpetas a la vez (VS Code tampoco en su modo normal), mostrar archivos que no sean `.sql`, ni vigilar el disco (cambios externos se ven con *⟳*).

### 7.5 Consola ([query-console.js](DataBaseSDK.Web/wwwroot/js/query-console.js))
- **Selectores de contexto:** *Base de datos* (SQL Server) o *Esquema* (Oracle); sus listas se piden **al abrir el selector**, no al cargar (no abre una conexión sin que el usuario lo pida).
- **Árbol** perezoso (cada nodo pide sus hijos al expandirse): conexión → [bases →] esquemas → grupos por tipo de objeto → objetos → Columnas / Índices / Claves foráneas / Triggers / Parámetros / Miembros. Panel de detalle con **SELECT 100** (`SELECT TOP 100 *` en SQL Server; `… WHERE ROWNUM <= 100` en Oracle; inserta y ejecuta en el contexto correcto), **Definición** y **Copiar nombre**; doble clic en tabla/vista ejecuta el SELECT, doble clic en una columna la inserta. Buscador por nombre (mín. 2 letras). Oracle: casilla "Mostrar esquemas del sistema".
- **Diálogo de conexión:** mismos campos que SSMS / SQL Developer; **Probar conexión** antes de guardar; al editar, contraseña vacía = conservar la guardada; casilla **"Permitir editar datos…"** (apagada por defecto; al guardar con ella marcada pide confirmación). Actualiza el badge rojo según la conexión elegida.
- **Historial:** últimas 25 consultas ejecutadas por motor (`qc-history-<kind>`).

### 7.6 `localStorage` (todo es conveniencia del navegador; nunca estado crítico)
`qc-session-<kind>` (pestañas con su conexión, su orden de uso `mru` y el sello del repo), `qc-recent`, `qc-roots`, `qc-lastdir`, `qc-pane`, `qc-side`, `qc-explorer` (explorador de archivos, §7.4), `qc-history-<kind>`. En **`sessionStorage`** (se borra al cerrar la ventana: son datos reales): `qc-results-<kind>` (el último resultado de cada pestaña, §7.3b). Cada lectura/escritura va en `try/catch`: sin `localStorage` la página funciona igual.

### 7.7 Estilos y tema
Bootstrap **5.1.0** (no trae tema oscuro: `[data-bs-theme]` llegó en 5.3). [dark-theme.css](DataBaseSDK.Web/wwwroot/css/dark-theme.css) fija las variables `--db-bg/-surface/-surface-2/-border/-text/-text-muted/-link` y pisa a mano navbar, cards, tablas, formularios, botones outline, alertas, badges y modales. **Todo CSS nuevo debe usar `--db-*`**: usar `--bs-body-bg` & co. deja blanco sobre blanco (así falló la cabecera de la tabla de resultados). Los estilos propios de la consola (`qc-*`) están en el `<style>` de `_QueryConsole.cshtml`.

---

## 8. Configuración

### 8.1 Variables (todas opcionales; del entorno o del `.env` de la raíz — ver [.env.example](.env.example))
| Variable | Efecto |
|---|---|
| `SQLSERVER_CONN_<ALIAS>` / `ORACLE_CONN_<ALIAS>` | una conexión por variable (cadena normal del driver); aparecen como solo lectura con la etiqueta `.env` |
| `TNS_ADMIN` | carpeta de `tnsnames.ora` (Oracle con alias TNS) |
| `DBSDK_WRITE` | alias del `.env` con **edición de filas** habilitada (`DEV,QA` / `*`); sin ella, ninguna |
| `DBSDK_PORT` | puerto (defecto **5130**); solo escucha en localhost |
| `DBSDK_CONNECTIONS_FILE` | ruta del archivo de conexiones guardadas (defecto `.local\db-connections.json`) |
| `DBSDK_IMPORT_CONNECTIONS_FROM` | origen de la importación única de conexiones (`none` la apaga) |
| `DBSDK_SHARED_CONNECTIONS_FILE` | ruta de la copia versionada sin contraseñas (defecto `db-connections.shared.json` en la raíz; `none` la apaga) |
| `DBSDK_WORKSPACE_FILE` | ruta de la sesión versionada (pestañas y conexión por motor; defecto `db-workspace.json` en la raíz; `none` la apaga) |
| `DBSDK_OPEN_BROWSER` | `1` = abrir la ventana de Edge al arrancar (lo pone `start.cmd`) |

### 8.2 Datos locales (`.local\`, ignorada por git)
- `db-connections.json`: conexiones guardadas, contraseña cifrada con DPAPI (solo este usuario de Windows en esta máquina las descifra). **Nunca leerla, imprimirla ni copiarla.**
- `data-edits.log`: bitácora de ediciones de filas (§5.7.5). **Datos reales: tratarla como sensible.**
- `*.corrupto`, `*.tmp`: copias de seguridad / escritura atómica del almacén.

### 8.3 Importación de conexiones desde AzureDevOpsSdk
Este proyecto nació del módulo de bases de datos de `AzureDevOpsSdk`. La **primera vez** que arranca, si todavía no existe `.local\db-connections.json` y existe `..\AzureDevOpsSdk\.local\db-connections.json`, copia esas conexiones (con la clave aún cifrada; se descifra porque es el mismo usuario y la misma entropía) **con la edición de datos apagada**. Es una copia: de ahí en adelante los dos proyectos divergen.

---

## 9. Seguridad (modelo de amenazas)

| Riesgo | Medida |
|---|---|
| Otro equipo de la red usa el sitio | solo escucha en loopback (`UseUrls localhost`) + middleware `LocalAccess` para todo + segunda barrera en las páginas |
| Página web externa habla al sitio (DNS rebinding / CSRF) | se exige `Host` de loopback; los POST llevan token antiforgery |
| Consulta que escribe o ejecuta cosas | `ReadOnlySqlGuard` + transacción nunca confirmada + `SET TRANSACTION READ ONLY` (Oracle) — **red contra errores, no frontera**: usar un usuario de solo lectura |
| Inyección de SQL desde el navegador | nombres siempre como parámetros o por `ChangeDatabase`; esquema de Oracle con lista blanca; en edición el SQL usa solo nombres del catálogo |
| Escritura accidental o a la base equivocada | opt-in por conexión (apagado por defecto), confirmación con casilla, "Probar" sin guardar, bitácora previa al commit, verificación de valores originales, exactamente 1 fila por `UPDATE`, 200 filas máx. |
| Fuga de contraseñas | DPAPI en disco, nunca vuelven al navegador, nunca en `Description`, `.local/` y `.env` ignorados por git; la copia versionada (`db-connections.shared.json`) no tiene campo de contraseña. **Ojo:** sí expone servidores y usuarios, y el repo de GitHub es público |
| Lectura/escritura arbitraria de archivos | el servicio solo toca `.sql` ≤ 5 MB; `Locate` rechaza nombres con ruta |
| Datos sensibles en la bitácora | valores recortados a 200 caracteres; el archivo está en `.local/` (no se versiona) |

---

## 10. Pruebas

```bash
dotnet test                                                         # 256 casos, ~1 s
node --test DataBaseSDK.Web/js-tests/query-complete.test.js DataBaseSDK.Web/js-tests/query-edit.test.js DataBaseSDK.Web/js-tests/query-explorer.test.js DataBaseSDK.Web/js-tests/query-mru.test.js DataBaseSDK.Web/js-tests/qc-workspace.test.js   # 57 casos
```
(Node 20+. Se pasan los **archivos**: pasar la carpeta falla en esta versión de Node.)

### 10.1 Qué cubre cada archivo de .NET
- **DatabaseConsoleTests**: `ReadOnlySqlGuard` (rechazos, literales, `q'[]'`, comentarios, `;`), `DbConnectionCatalog` (unión `.env` + guardadas), `QueryResultCsv`.
- **ExplorerAndStoreTests**: exploradores (nombres como parámetros, `LIKE` escapado, tope de lista, tipos, esquemas de sistema, búsqueda por prefijo, paquetes), contexto de consulta de los ejecutores (Oracle `CURRENT_SCHEMA` antes de la transacción de solo lectura y con lista blanca; SQL Server `ChangeDatabase`), almacén cifrado (clave no aparece en el JSON, `Save` con `null`/vacío, archivo corrupto), cadenas de conexión por modo, validación (incluida la inyección TNS).
- **QueryRunnerTests**: columnas/celdas, `;` final, tope de filas, **nunca se confirma la transacción**, el rechazo no abre la conexión, celdas largas truncadas. **Contiene los fakes** (§10.2).
- **RowEditingTests**: `SingleTableAnalyzer` (aceptados y rechazados), `PrepareAsync` (nombres por motor, PK en el SELECT, vistas, sin PK…), `ApplyAsync` (SQL exacto, bloqueo, conflicto, lote atómico, 1 fila exacta, prueba en seco, bitácora previa, fallo de bitácora → nada se guarda, fallos de base, validaciones, nombres maliciosos), `ParseValue`, `ColumnCategories`, `FileEditAuditLog`.
- **TableDescribeTests**: `DescribeTableAsync` de ambos motores (parámetros, flags, identity de 11g, nulo si no es tabla), `AllowWrite` en almacén y catálogo, `DBSDK_WRITE`.
- **SqlFileServiceTests**: navegación, codificaciones (UTF-8/BOM/UTF-16/Latin-1), CRLF/LF, conflictos de versión, archivo desaparecido, `Locate`, `ProjectPaths` (exige que exista `.gitignore` con `.local` en la raíz).
- **LocalAccessAndImportTests**: `LocalAccess` (loopback/`Host`, DNS rebinding, IPv4 mapeada), importación única (clave descifrable, `allowWrite` forzado a `false`, una sola vez, origen ausente/corrupto, sin origen cuando se da ruta explícita) y copia versionada (`SharedConnectionsFileTests`: sin clave ni `allowWrite`, otra máquina la ve sin clave y guarda la suya local, publicación al arrancar sin pisar, borrado en ambos lados, archivo dañado intacto).
- **WorkspaceStoreTests**: sección por motor, rutas relativas que sirven en otra carpeta/máquina (mismo sello), rutas fuera del repo, rechazo de lo que no es `.sql` completo, duplicados y tope, conexión propia por archivo, sello estable y sin reescritura innecesaria, `..` que sale del repo y archivo con conflicto de merge intacto, nombres inválidos.

### 10.1b Qué cubre cada archivo de Node (`js-tests/`)
- **query-complete.test.js** (23): análisis del texto para el autocompletado (`analyze`, `parseTables`, alias, `FROM ventas.`…).
- **query-edit.test.js** (7): `validateInput` por categoría, `isTruncated`, `buildRequest` (solo celdas cambiadas).
- **query-explorer.test.js** (13): rutas (`normalizePath`, `isInside`, `relativeSegments`, `folderName`), `normalizeState`, `rememberFolder`, `groupByDepth`, `keyAction` (todas las teclas, bordes y "nada seleccionado").
- **query-mru.test.js** (7): `touch`, `remove`, `restore` (pestañas que ya no existen / sin repetir / activa al frente), `startIndex`, `step` (dar la vuelta).
- **qc-workspace.test.js** (7): `snapshot` (sin consultas sueltas ni archivos desaparecidos, con la conexión de cada archivo), `signature`, `reconcile` (nunca cierra trabajo sin guardar; contexto para las ya abiertas), caché de resultados (`putResult` con tope, `dropOldest`, `removeResults`, `normalizeCache`).
La interfaz (DOM) de estos módulos no tiene pruebas automáticas: se verifica en el navegador.

### 10.2 Los fakes (en [QueryRunnerTests.cs](DataBaseSDK.Tests/QueryRunnerTests.cs))
`FakeConnection`/`FakeCommand`/`FakeTransaction`/`FakeParameter(Collection)` implementan `DbConnection` y compañía sobre un `DataTable` (`DataTableReader` es un lector real). Permiten probar **el SQL y los parámetros que realmente se mandarían** sin una base: `Table`/`TableFor(sql)` eligen qué devuelve cada consulta; `ExecutedSql`, `ExecutedParameters`, `NonQueries` (SQL + parámetros + `DbType`), `ChangedDatabases`, `Transaction.Committed/RolledBack`, `RowsAffected`, `FailOn(sql)` (simula un fallo de la base). Las clases `Test*Explorer/Runner/Editor` sobrescriben `CreateConnection` (`protected`) para inyectar el fake. **Lo específico de cada driver real no se puede probar así.**

---

## 11. Qué NO está probado y límites

**Sin probar contra servidores reales (Oracle / SQL Server):** (1) las consultas de catálogo (`sys.*` / `all_*`) con los permisos reales; (2) la conversión de tipos propios de Oracle (`NUMBER` grande, `CLOB`/`BLOB`); (3) `SET TRANSACTION READ ONLY`; (4) en la edición: los `UPDATE` con `UPDLOCK` / `FOR UPDATE WAIT 5`, la lectura de la PK de `sys.indexes` / `all_constraints`, y los tipos de parámetro (sobre todo fechas). **Probar la edición primero en una base de desarrollo** y revisar `data-edits.log` tras el primer guardado.

**Verificado:** compilación sin advertencias; 256 + 57 pruebas automáticas; en esta copia, la portada, `/SqlServer` y `/Oracle` cargan sin errores de consola, los estilos con `--db-*` se aplican, la importación de conexiones funciona (llegaron con `allowWrite = false`) y `start.cmd` levanta el sitio y abre la ventana. El **explorador de archivos** (`Ctrl+K`) sí se verificó entero en el navegador acá, con una carpeta real de scripts anidados: mostrar/ocultar con la tecla real, abrir carpeta desde el selector, desplegar/plegar con mouse y teclado, abrir archivos (pestaña + resaltado + ● de modificado), auto-desplegado al cambiar de pestaña, contraer todo, actualizar (archivo nuevo creado desde fuera), cerrar carpeta y recientes, carpeta inexistente, restauración tras recargar (con el panel visible y oculto — oculto no hace ninguna petición), la página de Oracle y el diseño en pantalla angosta. Las **interacciones completas** de la interfaz (árbol, abrir/guardar archivos, autocompletado, arrastrar y soltar, editar celdas con prueba en seco / guardado / conflicto) se verificaron en el navegador en el proyecto de origen (`AzureDevOpsSdk`) con exploradores y editores de demostración en memoria; el código es el mismo (solo cambian nombres y estilos), pero **no se repitió esa pasada completa acá**.

**Límites conocidos:** `Ctrl+Tab` solo funciona en la ventana tipo aplicación de `start.cmd` (en una pestaña normal el navegador lo reserva); el selector se probó en el navegador integrado con la tecla real (que entra a la página) y con eventos de teclado simulados para "mantener Ctrl" — **no** se probó en la ventana de Edge del usuario; el editor no tiene resaltado de sintaxis; archivos > ~2000 líneas marcan todo como cambiado en el gutter; no probado en Firefox ni en un *drop* real del escritorio (se usó un `DragEvent` sintético); una fecha con fracciones se guarda sin ellas al editarla; en Oracle `''` = `NULL`; sin plan de ejecución, sin varios resultados por script, sin `EXEC`; resultados de hasta 10.000 filas en el DOM (pesado); no hay autocompletado de procedimientos.

---

## 12. Gotchas del entorno (aprendidos a la mala)

- **Control de aplicaciones de Windows** bloquea de forma **intermitente** los `.exe` propios compilados (no `dotnet.exe`/`testhost.exe`). Workaround fijo: ejecutar el **`.dll`** con `dotnet` (lo que hace `start.cmd`).
- **C# Dev Kit de VS Code** rompe `dotnet build/run` en su terminal integrada (`MSB4062 … System.Runtime 10.0.0.0`, §2.2). Usar otra terminal.
- **DLL bloqueadas:** compilar con el sitio corriendo falla (`MSB3027`). Detener primero el proceso (puerto 5130).
- **Acentos:** reescribir archivos con tildes con `Get-Content … | Set-Content` de **PowerShell 5.1** **corrompe** la codificación (ya pasó). Editar con herramientas que respeten UTF-8 (Edit/Write), o `[IO.File]` con UTF-8 explícito. En `sed`, evitar `\n` y barras invertidas en reemplazos con acentos.
- **Heredocs enormes** en Git Bash fallan (`unexpected EOF`): escribir archivos largos con una herramienta de archivos, no con `cat <<EOF`.
- **PowerShell:** `Set-Location` **no** cambia el directorio de proceso de los programas nativos (`cmd /c start.cmd` no lo encuentra): usar rutas completas. Las variables de entorno de una llamada no persisten a la siguiente.
- **Node:** `node --test <carpeta>` falla en esta versión; pasar los archivos.
- **Repositorio git** con `origin` en `github.com/is-jsandoval/DataBaseSDK` (**público**): `.env` y `.local/` están ignorados; `db-connections.shared.json` sí se sube.
- **Ruta fija de la importación:** `..\AzureDevOpsSdk\.local\…` (carpeta hermana). Si se mueve el proyecto, la importación simplemente no encuentra origen (no falla).

---

## 13. Convenciones de código

- C#: `net8.0`, `Nullable` y `ImplicitUsings` activos, *file-scoped namespaces*, `record` para datos, `static` para utilidades puras; `InternalsVisibleTo("DataBaseSDK.Tests")` en Core.
- **Comentarios en español que explican el *porqué*** (la restricción, el bug que evitan), no lo que el código ya dice. Mantener ese tono y densidad.
- Errores de negocio con **excepciones tipadas y códigos estables** (`SqlFileError`, `RowEditError`) → el controlador los traduce a HTTP → el JS los explica; nunca mostrar una excepción cruda al usuario salvo el mensaje del driver de la base.
- La UI nunca debe mostrar acrónimos que el equipo no use; mensajes en español claro.
- JS: IIFEs, `var`/funciones (sin módulos ES), `data-role` para engancharse al DOM, `textContent` (nunca `innerHTML` con datos), `try/catch` alrededor de `localStorage`.
- Pruebas: xUnit con asserts nativos (sin Moq ni FluentAssertions); fakes de `DbConnection` en vez de mocks; cada regla de seguridad tiene su prueba.

---

## 14. Recetas

### 14.1 Agregar un motor (ej. PostgreSQL)
1. `Models.cs`: valor nuevo en `DatabaseKind`; `ColumnCategories.From` (tipos del motor).
2. Paquete NuGet del driver en `DataBaseSDK.Core.csproj`.
3. `QueryRunners.cs`: `PostgresQueryRunner : QueryRunnerBase` (conexión, `ApplyContextAsync`, `BeginReadOnlyAsync` — en Postgres `SET TRANSACTION READ ONLY` — y `ReadCell` si hace falta).
4. `ReadOnlySqlGuard`: revisar `Scan` (comillas y literales del dialecto, p. ej. `$$…$$`) y `ForbiddenWords`.
5. `Explorers.cs`: `PostgresExplorer : ExplorerBase` (catálogo `pg_catalog`/`information_schema` **con parámetros**), incluida `DescribeTableAsync`.
6. `RowEditors.cs`: `PostgresRowEditor : RowEditorBase` (`Quote`, `Marker`, `LockHint`/`LockSuffix`, `ResolveName`, `DbTypeFor`).
7. `ConnectionStore.cs`: campos/validación/cadena de conexión del motor; `DbConnectionCatalog`: prefijo `POSTGRES_CONN_`.
8. `ServiceCollectionExtensions.AddDatabaseConsole`: registrar runner, explorador y editor.
9. Web: `PostgresModel` + `Postgres.cshtml` (copiar `SqlServer.cshtml`), enlace en `_Layout`, tarjeta en `Index`; JS: ramas por `kind` en `query-console.js` (`qualified`, `selectTemplate`) y palabras clave/funciones en `query-complete.js` (`KEYWORDS`).
10. Pruebas: copiar el patrón de `ExplorerAndStoreTests`/`RowEditingTests` con fakes. Actualizar este README.

### 14.2 Agregar un endpoint
Handler `OnGet…/OnPost…` en `QueryConsolePageModel` (los GET devuelven `JsonResult`; los POST con cuerpo JSON usan `[FromBody]`); si lee/escribe disco, pasar por `FileCall`; si toca la base, resolver la conexión **por alias en el catálogo** y nunca confiar en datos del navegador; documentarlo en la tabla de §6.3; el JS lo llama con `QC.get/post`.

### 14.3 Agregar una categoría de columna editable
`ColumnCategory` + `ColumnCategories.From` (ambos motores) + `RowEditorBase.ParseValue` + `DbTypeFor` + `PATTERNS`/`HINTS` en `query-edit.js` + pruebas en `RowEditingTests` y `query-edit.test.js`.

### 14.4 Cambiar el cifrado de contraseñas
No cambiar la entropía sin una migración: leer con la antigua y reescribir con la nueva, o pedir que se reescriban las claves.

---

## 15. Decisiones y por qué

- **Web local y no escritorio.** El *Control de aplicaciones* bloquea los `.exe` propios (mismo problema que en el proyecto hermano) y ya existía una interfaz web completa y verificada. El núcleo (`Core`) no depende de la interfaz: si algún día se quiere un cliente de escritorio (WPF + AvalonEdit), reutiliza todo el núcleo. Ventajas del escritorio que se renunciaron: diálogos nativos con rutas reales (de ahí `Locate` y el selector de rutas propio), resaltado de sintaxis, tabla virtualizada.
- **Solo localhost, sin opción de red.** Maneja credenciales y puede modificar datos; no tiene login. En el proyecto de origen había una variable para abrirlo a la LAN; acá se quitó a propósito.
- **Edición opt-in y conservadora.** Un error de edición en una base real es caro. Por eso: apagada por defecto, analizador que prefiere decir "no", verificación de valores originales (concurrencia optimista con bloqueo de fila), exactamente 1 fila, bitácora previa al commit, y la importación de conexiones **nunca** trae el permiso de edición.
- **`CellText` único.** La grilla, el CSV y la comparación de la edición deben formatear igual; si divergen, la verificación de originales daría falsos conflictos (o, peor, falsos "sin cambio").
- **Datos del navegador no se confían.** El navegador propone; el servidor vuelve a leer el catálogo, valida y arma el SQL.
- **Un solo `QueryConsolePageModel` y una sola vista para los dos motores.** Evita duplicar ~1.100 líneas; la diferencia vive en las implementaciones por motor y en unos pocos `kind` del JS.

---

## 16. Relación con `AzureDevOpsSdk`

`C:\Automations\AzureDevOpsSdk` es un SDK .NET + sitio web para Azure DevOps (QA bancario). Su módulo de consolas de bases de datos (`AzureDevOpsSdk.Database` + páginas `/Oracle` y `/SqlServer`) se **copió** acá el 2026-10-28 y se renombró: `AzureDevOpsSdk.Database` → `DataBaseSDK.Core`, `AzureDevOpsSdk.Web` (parte SQL) → `DataBaseSDK.Web`, variables `AZDO_DB_*` → `DBSDK_*`, variables CSS `--ado-*` → `--db-*`. Cambios deliberados respecto al original: portada propia, `LocalAccess` extraído y aplicado como middleware a todo el sitio, sin modo LAN, puerto 5130, `start.cmd` + `BrowserLauncher`, importación única de conexiones (sin el `%LOCALAPPDATA%` antiguo), tema oscuro reducido a lo que se usa. **No hay vínculo en tiempo de ejecución ni de compilación entre los dos proyectos**: el código original sigue existiendo en `AzureDevOpsSdk` y puede divergir; una corrección en uno no llega sola al otro.
