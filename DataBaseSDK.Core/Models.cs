namespace DataBaseSDK.Core;

public enum DatabaseKind
{
    Oracle,
    SqlServer
}

/// <summary>De dónde sale una conexión: del .env (solo lectura en la UI) o guardada desde la interfaz (editable).</summary>
public enum ConnectionOrigin
{
    Env,
    Saved
}

/// <summary>Una conexión disponible. <see cref="ConnectionString"/> es secreta: nunca se muestra, usar <see cref="Description"/>.</summary>
public record DbConnectionInfo(string Alias, DatabaseKind Kind, string ConnectionString, string Description)
{
    public ConnectionOrigin Origin { get; init; } = ConnectionOrigin.Env;

    /// <summary>
    /// La conexión admite editar filas desde la grilla de resultados (<see cref="IRowEditor"/>). Está APAGADO por defecto y se
    /// enciende conexión por conexión: el editor de consultas sigue siendo de solo lectura aunque esté prendido.
    /// </summary>
    public bool AllowWrite { get; init; }
}

public class QueryOptions
{
    public const int DefaultMaxRows = 500;
    public const int HardMaxRows = 10_000;
    public const int DefaultTimeoutSeconds = 30;
    public const int HardTimeoutSeconds = 300;

    private int _maxRows = DefaultMaxRows;
    private int _timeoutSeconds = DefaultTimeoutSeconds;

    /// <summary>Filas que se traen como máximo (1..10.000) — como el "fetch first N rows" de los clientes de escritorio.</summary>
    public int MaxRows
    {
        get => _maxRows;
        set => _maxRows = Math.Clamp(value, 1, HardMaxRows);
    }

    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set => _timeoutSeconds = Math.Clamp(value, 1, HardTimeoutSeconds);
    }

    /// <summary>SQL Server: base de datos sobre la que corre la consulta (como el selector "Available Databases" de SSMS). Vacío = la de la conexión.</summary>
    public string? Database { get; set; }

    /// <summary>Oracle: esquema por defecto de la sesión (<c>CURRENT_SCHEMA</c>), para escribir las tablas sin el prefijo del dueño. Vacío = el usuario de la conexión.</summary>
    public string? Schema { get; set; }
}

public record QueryColumn(string Name, string TypeName, bool IsNumeric);

/// <summary>
/// Resultado de una consulta. Cada celda es null, string, número (decimal/long/double...), bool o
/// DateTime — los binarios y LOBs ya vienen convertidos a texto (y recortados) para poder mostrarse.
/// </summary>
public class QueryResult
{
    public IReadOnlyList<QueryColumn> Columns { get; init; } = [];

    public IReadOnlyList<object?[]> Rows { get; init; } = [];

    /// <summary>true si había más filas que <see cref="QueryOptions.MaxRows"/> y se cortó la lectura.</summary>
    public bool Truncated { get; init; }

    public long ElapsedMilliseconds { get; init; }
}

/// <summary>La consulta no pasó el chequeo de solo lectura (ver <see cref="ReadOnlySqlGuard"/>); no llegó a la base.</summary>
public class QueryRejectedException : Exception
{
    public QueryRejectedException(string message) : base(message)
    {
    }
}
