using System.Text;
using DataBaseSDK.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataBaseSDK.Web.Pages;

/// <summary>Formulario de "Nueva conexión" / "Editar conexión" (los mismos campos que el diálogo de SSMS o SQL Developer).</summary>
public class ConnectionForm
{
    public string Alias { get; set; } = string.Empty;

    public bool IsEdit { get; set; }

    public string Host { get; set; } = string.Empty;

    public int? Port { get; set; }

    public string? Database { get; set; }

    public string? OracleMode { get; set; }

    public string? OracleTarget { get; set; }

    public bool WindowsAuth { get; set; }

    public string? User { get; set; }

    /// <summary>En una edición, vacío = conservar la clave guardada (la clave nunca se devuelve al navegador).</summary>
    public string? Password { get; set; }

    public bool TrustServerCertificate { get; set; } = true;

    /// <summary>Habilita editar filas desde la grilla de resultados con esta conexión (apagado por defecto).</summary>
    public bool AllowWrite { get; set; }
}

/// <summary>Cambios a guardar desde la grilla: la conexión con la que se consultó y las filas editadas.</summary>
public class ApplyEditsBody : RowEditRequest
{
    public string Alias { get; set; } = string.Empty;
}

public class LocateFileBody
{
    public string? Name { get; set; }

    public long Size { get; set; }

    public List<string>? Folders { get; set; }
}

public class SaveFileBody
{
    public string? Path { get; set; }

    /// <summary>Con saltos de línea <c>\n</c>; el servidor los convierte a <see cref="Eol"/>.</summary>
    public string? Text { get; set; }

    public string? Eol { get; set; }

    public string? Encoding { get; set; }

    /// <summary>Versión del archivo cuando se abrió; si cambió en disco, el guardado devuelve un conflicto.</summary>
    public string? ExpectedVersion { get; set; }

    public bool Overwrite { get; set; }
}

/// <summary>Lo que el navegador manda para recordar: o la conexión elegida (<see cref="Context"/>) o las pestañas abiertas.</summary>
public class WorkspaceBody
{
    public bool Context { get; set; }

    public string? Alias { get; set; }

    public string? Database { get; set; }

    public string? Schema { get; set; }

    public List<WorkspaceFile?>? Files { get; set; }

    public string? ActiveFile { get; set; }
}

public class AliasBody
{
    public string Alias { get; set; } = string.Empty;
}

/// <summary>
/// Base de las consolas de consultas (Oracle y SQL Server), con el mismo esquema que SSMS / SQL
/// Developer: un explorador (servidor → bases → esquemas → tablas, vistas, procedimientos... →
/// columnas, índices, claves), conexiones que se agregan desde la propia pantalla, un editor de
/// consultas de SOLO LECTURA, grilla de resultados y exportación a CSV. La lógica vive en
/// <c>DataBaseSDK.Core</c>; esto es solo el front.
///
/// Seguridad: son consultas contra bases reales con credenciales (del .env o guardadas), y el sitio
/// no tiene login, así que solo responde a peticiones locales (<see cref="LocalAccess"/>: conexión Y
/// cabecera Host de loopback — el chequeo del Host evita que una página web externa use "DNS rebinding"
/// para hablarle al sitio). Eso ya lo impone un middleware para TODO el sitio (ver Program.cs); el chequeo
/// de acá es una segunda barrera. Las contraseñas nunca vuelven al navegador.
/// </summary>
public abstract class QueryConsolePageModel : PageModel
{
    private readonly IDbConnectionCatalog _catalog;
    private readonly IDbConnectionStore _store;
    private readonly ISqlQueryRunner _runner;
    private readonly IDbExplorer _explorer;
    private readonly IRowEditor _editor;
    private readonly ISqlFileService _files;
    private readonly IWorkspaceStore _workspace;

    protected QueryConsolePageModel(
        IDbConnectionCatalog catalog,
        IDbConnectionStore store,
        IEnumerable<ISqlQueryRunner> runners,
        IEnumerable<IDbExplorer> explorers,
        IEnumerable<IRowEditor> editors,
        ISqlFileService files,
        IWorkspaceStore workspace,
        DatabaseKind kind)
    {
        _catalog = catalog;
        _store = store;
        _files = files;
        _workspace = workspace;
        Kind = kind;
        _runner = runners.First(r => r.Kind == kind);
        _explorer = explorers.First(e => e.Kind == kind);
        _editor = editors.First(e => e.Kind == kind);
    }

    public DatabaseKind Kind { get; }

    public string Title => Kind == DatabaseKind.Oracle ? "Oracle" : "SQL Server";

    /// <summary>Prefijo de las variables del .env para esta consola.</summary>
    public string EnvPrefix => Kind == DatabaseKind.Oracle ? DbConnectionCatalog.OraclePrefix : DbConnectionCatalog.SqlServerPrefix;

    public bool HasDatabases => _explorer.HasDatabases;

    /// <summary>Tipos de objeto que muestra el árbol de este motor, en orden.</summary>
    public IReadOnlyList<string> ObjectKinds => _explorer.Kinds.Select(k => k.ToString()).ToList();

    public IReadOnlyList<DbConnectionInfo> Connections { get; private set; } = [];

    [BindProperty]
    public string? Alias { get; set; }

    [BindProperty]
    public string Sql { get; set; } = string.Empty;

    [BindProperty]
    public int MaxRows { get; set; } = QueryOptions.DefaultMaxRows;

    /// <summary>SQL Server: base sobre la que corre la consulta.</summary>
    [BindProperty]
    public string? Database { get; set; }

    /// <summary>Oracle: esquema por defecto de la sesión.</summary>
    [BindProperty]
    public string? Schema { get; set; }

    /// <summary>
    /// Pestaña del editor desde la que se ejecutó: el resultado se le asigna a ella (cada pestaña conserva el suyo,
    /// como las ventanas de consulta de SSMS). Solo vuelve al HTML como atributo; nunca toca el SQL.
    /// </summary>
    [BindProperty]
    public string? TabId { get; set; }

    public QueryResult? Result { get; private set; }

    /// <summary>Si el resultado se puede editar en la grilla (null = la conexión no admite edición; ver <see cref="EditHint"/>).</summary>
    public EditInfo? Edit { get; private set; }

    /// <summary>Por qué la grilla es de solo lectura, cuando la causa es la conexión y no el SELECT.</summary>
    public string? EditHint { get; private set; }

    public string? Error { get; private set; }

    /// <summary>true si el error es del chequeo de solo lectura (no llegó a la base).</summary>
    public bool Rejected { get; private set; }

    public DbConnectionInfo? SelectedConnection => _catalog.Find(Kind, Alias);

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (!IsLocalRequest())
        {
            context.Result = new ContentResult
            {
                StatusCode = StatusCodes.Status403Forbidden,
                ContentType = "text/plain; charset=utf-8",
                Content = "La consola de bases de datos solo se puede usar desde esta misma máquina (http://localhost)."
            };

            return;
        }

        Connections = _catalog.List(Kind);

        if (string.IsNullOrWhiteSpace(Alias))
            RestoreContext();

        base.OnPageHandlerExecuting(context);
    }

    // Al entrar a la página (p. ej. al volver de la otra consola) se retoma la conexión que se usó la última vez en
    // este motor, con su base o esquema; antes siempre caía en la primera de la lista.
    private void RestoreContext()
    {
        var saved = _workspace.Get(Kind);
        var known = Connections.FirstOrDefault(c => c.Alias.Equals(saved.Alias, StringComparison.OrdinalIgnoreCase));

        Alias = known?.Alias ?? Connections.FirstOrDefault()?.Alias;

        // Solo en la carga de la página: los handlers JSON reciben su contexto como parámetro.
        if (known != null && HttpMethods.IsGet(Request.Method) && string.IsNullOrEmpty(Request.Query["handler"]))
        {
            Database ??= saved.Database;
            Schema ??= saved.Schema;
        }
    }

    public void OnGet()
    {
    }

    // --- Consultas ---

    public async Task<IActionResult> OnPostRunAsync(CancellationToken cancellationToken)
    {
        await RunAsync(cancellationToken);
        return Page();
    }

    // Lo que usa el editor: la misma ejecución, pero devuelve SOLO el bloque de resultados (_QueryResults) para no recargar
    // la página entera (árbol, pestañas, scripts). Cancelar el fetch corta la consulta: el token es el de la petición.
    public async Task<IActionResult> OnPostRunPartialAsync(CancellationToken cancellationToken)
    {
        await RunAsync(cancellationToken);
        return Partial("_QueryResults", this);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        if (SelectedConnection != null)
            _workspace.SaveContext(Kind, Alias, Database, Schema);

        Result = await ExecuteAsync(cancellationToken);

        if (Result != null)
            Edit = await PrepareEditAsync(Result, cancellationToken);
    }

    public async Task<IActionResult> OnPostCsvAsync(CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(cancellationToken);

        if (result == null)
            return Page();

        // BOM para que Excel abra las tildes bien.
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(QueryResultCsv.Write(result)))
            .ToArray();

        return File(bytes, "text/csv", $"{Alias}-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
    }

    // --- Conexiones ---

    public IActionResult OnGetConnections() =>
        new JsonResult(_catalog.List(Kind).Select(c => new
        {
            alias = c.Alias,
            origin = c.Origin == ConnectionOrigin.Saved ? "saved" : "env",
            description = c.Description,
            editable = c.Origin == ConnectionOrigin.Saved,
            allowWrite = c.AllowWrite
        }));

    // Datos para el formulario de edición: nunca incluye la clave (solo si hay una guardada).
    public IActionResult OnGetConnection(string? alias)
    {
        var stored = string.IsNullOrWhiteSpace(alias) ? null : _store.Get(Kind, alias);

        if (stored == null)
            return NotFound(new { error = "Solo se pueden editar las conexiones guardadas desde esta pantalla (las del .env se editan en el .env)." });

        return new JsonResult(new
        {
            alias = stored.Alias,
            host = stored.Host,
            port = stored.Port,
            database = stored.Database,
            oracleMode = stored.OracleMode,
            oracleTarget = stored.OracleTarget,
            windowsAuth = stored.WindowsAuth,
            user = stored.User,
            hasPassword = !string.IsNullOrEmpty(stored.Password),
            trustServerCertificate = stored.TrustServerCertificate,
            allowWrite = stored.AllowWrite
        });
    }

    public IActionResult OnPostSaveConnection([FromBody] ConnectionForm form)
    {
        var connection = ToStored(form, out var problem);

        if (connection == null)
            return BadRequest(new { error = problem });

        var existing = _catalog.Find(Kind, connection.Alias);

        if (form.IsEdit)
        {
            if (existing is not { Origin: ConnectionOrigin.Saved })
                return BadRequest(new { error = "Esa conexión no existe o es del .env (no editable desde acá)." });

            // Sin clave nueva se conserva la guardada.
            connection.Password = string.IsNullOrEmpty(form.Password) ? null : form.Password;
        }
        else if (existing != null)
        {
            return BadRequest(new { error = $"Ya existe una conexión llamada \"{connection.Alias}\"." });
        }

        _store.Save(connection);

        return new JsonResult(new { ok = true });
    }

    public async Task<IActionResult> OnPostTestConnectionAsync([FromBody] ConnectionForm form, CancellationToken cancellationToken)
    {
        var connection = ToStored(form, out var problem);

        if (connection == null)
            return BadRequest(new { error = problem });

        // Probando una conexión ya guardada sin reescribir la clave: se usa la guardada.
        if (form.IsEdit && string.IsNullOrEmpty(form.Password))
            connection.Password = _store.Get(Kind, connection.Alias)?.Password ?? string.Empty;

        var info = new DbConnectionInfo(connection.Alias, Kind, ConnectionStringFactory.Build(connection), ConnectionStringFactory.Describe(connection));
        var result = await _explorer.TestAsync(info, cancellationToken);

        return new JsonResult(new { ok = result.Ok, message = result.Message });
    }

    public IActionResult OnPostDeleteConnection([FromBody] AliasBody body)
    {
        var existing = _catalog.Find(Kind, body.Alias);

        if (existing is not { Origin: ConnectionOrigin.Saved })
            return BadRequest(new { error = "Solo se pueden borrar las conexiones guardadas desde esta pantalla." });

        _store.Delete(Kind, existing.Alias);

        return new JsonResult(new { ok = true });
    }

    // --- Edición de filas desde la grilla ---
    // Única ruta de escritura de la consola (el editor de consultas es de solo lectura). Toda la validación y las
    // salvaguardas viven en RowEditorBase; acá solo se traducen sus errores a códigos HTTP que el front sabe explicar.

    public async Task<IActionResult> OnPostApplyEditsAsync([FromBody] ApplyEditsBody body, CancellationToken cancellationToken)
    {
        var connection = _catalog.Find(Kind, body.Alias);

        if (connection == null)
            return BadRequest(new { error = "Conexión desconocida.", code = "invalid" });

        try
        {
            var result = await _editor.ApplyAsync(connection, body, cancellationToken);

            return new JsonResult(new { ok = true, rowsAffected = result.RowsAffected, dryRun = result.DryRun, statements = result.Statements });
        }
        catch (RowEditException ex)
        {
            var (status, code) = ex.Code switch
            {
                RowEditError.WriteNotAllowed => (StatusCodes.Status403Forbidden, "write_not_allowed"),
                RowEditError.NotEditable => (StatusCodes.Status400BadRequest, "not_editable"),
                RowEditError.Invalid => (StatusCodes.Status400BadRequest, "invalid"),
                RowEditError.NotFound => (StatusCodes.Status404NotFound, "not_found"),
                RowEditError.Conflict => (StatusCodes.Status409Conflict, "conflict"),
                RowEditError.RowCount => (StatusCodes.Status409Conflict, "row_count"),
                RowEditError.Audit => (StatusCodes.Status500InternalServerError, "audit"),
                _ => (StatusCodes.Status400BadRequest, "database")
            };

            return new JsonResult(new { error = ex.Message, code }) { StatusCode = status };
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
    }

    // --- Explorador de objetos (cada nodo del árbol pide sus hijos) ---

    public Task<IActionResult> OnGetDatabasesAsync(string? alias, CancellationToken ct) =>
        Explore(alias, async connection =>
            (await _explorer.ListDatabasesAsync(connection, ct)).Select(d => new { name = d.Name, isSystem = d.IsSystem }));

    public Task<IActionResult> OnGetSchemasAsync(string? alias, string? database, bool system, CancellationToken ct) =>
        Explore(alias, async connection =>
            (await _explorer.ListSchemasAsync(connection, database, system, ct)).Select(s => new { name = s.Name, isSystem = s.IsSystem, isCurrent = s.IsCurrent }));

    public Task<IActionResult> OnGetObjectsAsync(string? alias, string? database, string? schema, string? kind, CancellationToken ct) =>
        Explore(alias, async connection =>
        {
            var parsed = ParseKind(kind);

            if (string.IsNullOrWhiteSpace(schema))
                throw new ArgumentException("Falta el esquema.");

            var page = await _explorer.ListObjectsAsync(connection, database, schema, parsed, ct);

            return new
            {
                items = page.Items.Select(name => new { name, hasChildren = ExplorerRules.GroupsFor(parsed).Count > 0 }),
                truncated = page.Truncated
            };
        });

    public Task<IActionResult> OnGetGroupAsync(string? alias, string? database, string? schema, string? kind, string? name, string? group, CancellationToken ct) =>
        Explore(alias, async connection =>
        {
            if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Faltan datos.");

            if (!Enum.TryParse<ObjectGroup>(group, ignoreCase: true, out var parsedGroup))
                throw new ArgumentException("Grupo desconocido.");

            return (await _explorer.ListGroupAsync(connection, database, schema, ParseKind(kind), name, parsedGroup, ct))
                .Select(i => new { name = i.Name, detail = i.Detail });
        });

    public Task<IActionResult> OnGetDefinitionAsync(string? alias, string? database, string? schema, string? kind, string? name, CancellationToken ct) =>
        Explore(alias, async connection =>
        {
            if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Faltan datos.");

            return new { text = await _explorer.GetDefinitionAsync(connection, database, schema, ParseKind(kind), name, ct) };
        });

    public Task<IActionResult> OnGetSearchAsync(string? alias, string? database, string? filter, CancellationToken ct) =>
        Explore(alias, async connection =>
        {
            var page = await _explorer.SearchAsync(connection, database, filter ?? string.Empty, ct);

            return new
            {
                items = page.Items.Select(o => new { schema = o.Schema, name = o.Name, kind = o.Kind.ToString() }),
                truncated = page.Truncated
            };
        });

    // --- Sesión de trabajo (db-workspace.json, versionado) ---

    public IActionResult OnGetWorkspace() => new JsonResult(_workspace.Get(Kind));

    public IActionResult OnPostSaveWorkspace([FromBody] WorkspaceBody body)
    {
        if (body.Context)
        {
            // Solo conexiones que existen: el navegador no inventa nombres que terminen en el repo.
            var known = _catalog.Find(Kind, body.Alias);

            return new JsonResult(known == null
                ? _workspace.Get(Kind)
                : _workspace.SaveContext(Kind, known.Alias, body.Database, body.Schema));
        }

        return new JsonResult(_workspace.SaveTabs(Kind, body.Files, body.ActiveFile));
    }

    // --- Archivos .sql (abrir, guardar, navegar carpetas) ---
    // El sitio corre en tu máquina y estas páginas solo responden a localhost, así que "la ruta" es una ruta
    // del disco local. Solo se tocan archivos .sql (ver SqlFileService).

    public IActionResult OnGetBrowse(string? path) => FileCall(() => _files.Browse(path));

    public IActionResult OnGetPlaces() => FileCall(() => _files.Places());

    public IActionResult OnGetReadFile(string? path) => FileCall(() => _files.Read(path ?? string.Empty));

    public IActionResult OnGetStatFile(string? path) => FileCall(() => _files.Stat(path ?? string.Empty));

    // Al arrastrar un archivo a la página el navegador entrega el contenido pero NO la ruta: se intenta encontrarlo
    // por nombre y tamaño en las carpetas de trabajo/recientes del usuario (ver SqlFileService.Locate).
    public IActionResult OnPostLocateFile([FromBody] LocateFileBody body) =>
        FileCall(() => new { matches = _files.Locate(body.Name ?? string.Empty, body.Size, body.Folders ?? []) });

    public IActionResult OnPostSaveFile([FromBody] SaveFileBody body) =>
        FileCall(() => _files.Save(body.Path ?? string.Empty, body.Text ?? string.Empty, body.Eol, body.Encoding, body.ExpectedVersion, body.Overwrite));

    // Cada fallo vuelve con un código estable ("not_found", "conflict"...) que el front sabe explicar.
    private IActionResult FileCall(Func<object> action)
    {
        try
        {
            return new JsonResult(action());
        }
        catch (SqlFileException ex)
        {
            var (status, code) = ex.Code switch
            {
                SqlFileError.NotFound => (StatusCodes.Status404NotFound, "not_found"),
                SqlFileError.AccessDenied => (StatusCodes.Status403Forbidden, "access_denied"),
                SqlFileError.TooLarge => (StatusCodes.Status413PayloadTooLarge, "too_large"),
                SqlFileError.NotSql => (StatusCodes.Status400BadRequest, "not_sql"),
                SqlFileError.InvalidPath => (StatusCodes.Status400BadRequest, "invalid_path"),
                SqlFileError.Conflict => (StatusCodes.Status409Conflict, "conflict"),
                SqlFileError.AlreadyExists => (StatusCodes.Status409Conflict, "exists"),
                _ => (StatusCodes.Status500InternalServerError, "io")
            };

            return new JsonResult(new { error = ex.Message, code }) { StatusCode = status };
        }
    }

    // --- Internos ---

    private async Task<IActionResult> Explore(string? alias, Func<DbConnectionInfo, Task<object>> action)
    {
        var connection = _catalog.Find(Kind, alias);

        if (connection == null)
            return BadRequest(new { error = "Conexión desconocida." });

        try
        {
            return new JsonResult(await action(connection));
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            // Los drivers no incluyen la contraseña en sus mensajes; sí el código ORA-/Msg de la base.
            return BadRequest(new { error = ex.Message });
        }
    }

    private static DbObjectKind ParseKind(string? kind) =>
        Enum.TryParse<DbObjectKind>(kind, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ArgumentException("Tipo de objeto desconocido.");

    private StoredConnection? ToStored(ConnectionForm form, out string? problem)
    {
        var connection = new StoredConnection
        {
            Alias = form.Alias?.Trim() ?? string.Empty,
            Kind = Kind,
            Host = form.Host?.Trim() ?? string.Empty,
            Port = form.Port,
            Database = string.IsNullOrWhiteSpace(form.Database) ? null : form.Database.Trim(),
            OracleMode = form.OracleMode is OracleModes.Sid or OracleModes.Tns ? form.OracleMode : OracleModes.ServiceName,
            OracleTarget = form.OracleTarget?.Trim(),
            WindowsAuth = Kind == DatabaseKind.SqlServer && form.WindowsAuth,
            User = form.User?.Trim(),
            Password = form.Password ?? string.Empty,
            TrustServerCertificate = form.TrustServerCertificate,
            AllowWrite = form.AllowWrite
        };

        problem = ConnectionValidation.Validate(connection);

        return problem == null ? connection : null;
    }

    private async Task<QueryResult?> ExecuteAsync(CancellationToken cancellationToken)
    {
        var connection = _catalog.Find(Kind, Alias);

        if (connection == null)
        {
            Error = Connections.Count == 0
                ? "No hay conexiones: agrega una con \"＋ Conexión\" en el explorador."
                : "Elige una conexión.";

            return null;
        }

        try
        {
            return await _runner.ExecuteAsync(connection, Sql, CurrentOptions(), cancellationToken);
        }
        catch (QueryRejectedException ex)
        {
            Error = ex.Message;
            Rejected = true;
        }
        catch (OperationCanceledException)
        {
            Error = "Consulta cancelada.";
        }
        catch (Exception ex)
        {
            // Los drivers no incluyen la contraseña en sus mensajes; sí el código ORA-/Msg de la base.
            Error = ex.Message;
        }

        return null;
    }

    private QueryOptions CurrentOptions() => new()
    {
        MaxRows = MaxRows,
        Database = Database,
        Schema = Schema
    };

    // ¿Se puede editar este resultado? Nunca debe romper la consulta: si algo falla, la grilla queda de solo lectura con el motivo.
    private async Task<EditInfo?> PrepareEditAsync(QueryResult result, CancellationToken cancellationToken)
    {
        var connection = SelectedConnection;

        if (connection == null)
            return null;

        if (!connection.AllowWrite)
        {
            EditHint = connection.Origin == ConnectionOrigin.Saved
                ? "Para editar filas desde la grilla, activa \"Permitir editar datos\" en Editar conexión."
                : $"Para editar filas desde la grilla, agrega el alias {connection.Alias} a {DbConnectionCatalog.WriteEnvVariable} en el .env.";

            return null;
        }

        try
        {
            return await _editor.PrepareAsync(connection, CurrentOptions(), Sql, result.Columns, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return EditInfo.No($"No se pudo preparar la edición: {ex.Message}");
        }
    }

    private bool IsLocalRequest() => LocalAccess.IsLocal(HttpContext.Connection.RemoteIpAddress, Request.Host.Host);
}

public class OracleModel : QueryConsolePageModel
{
    public OracleModel(IDbConnectionCatalog catalog, IDbConnectionStore store, IEnumerable<ISqlQueryRunner> runners, IEnumerable<IDbExplorer> explorers, IEnumerable<IRowEditor> editors, ISqlFileService files, IWorkspaceStore workspace)
        : base(catalog, store, runners, explorers, editors, files, workspace, DatabaseKind.Oracle)
    {
    }
}

public class SqlServerModel : QueryConsolePageModel
{
    public SqlServerModel(IDbConnectionCatalog catalog, IDbConnectionStore store, IEnumerable<ISqlQueryRunner> runners, IEnumerable<IDbExplorer> explorers, IEnumerable<IRowEditor> editors, ISqlFileService files, IWorkspaceStore workspace)
        : base(catalog, store, runners, explorers, editors, files, workspace, DatabaseKind.SqlServer)
    {
    }
}
