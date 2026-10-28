using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace DataBaseSDK.Core;

/// <summary>Lógica común: abrir, ejecutar consultas de catálogo con parámetros y envolver errores.</summary>
public abstract class ExplorerBase : IDbExplorer
{
    protected const int MaxObjects = 2000;
    protected const int MaxSearch = 200;

    public abstract DatabaseKind Kind { get; }

    public abstract bool HasDatabases { get; }

    public abstract IReadOnlyList<DbObjectKind> Kinds { get; }

    protected abstract DbConnection CreateConnection(string connectionString);

    /// <summary>SQL Server cambia de base con <c>ChangeDatabase</c> (el driver cita el nombre): así no se arman nombres de 3 partes con texto del cliente.</summary>
    protected virtual Task SelectDatabaseAsync(DbConnection connection, string? database, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual void Configure(DbCommand command)
    {
    }

    protected virtual object? ReadValue(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);

    public abstract Task<ConnectionTestResult> TestAsync(DbConnectionInfo connection, CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyList<ExplorerDatabase>> ListDatabasesAsync(DbConnectionInfo connection, CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyList<ExplorerSchema>> ListSchemasAsync(DbConnectionInfo connection, string? database, bool includeSystem, CancellationToken cancellationToken = default);

    public abstract Task<ExplorerPage<string>> ListObjectsAsync(DbConnectionInfo connection, string? database, string schema, DbObjectKind kind, CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyList<ExplorerItem>> ListGroupAsync(DbConnectionInfo connection, string? database, string schema, DbObjectKind kind, string name, ObjectGroup group, CancellationToken cancellationToken = default);

    public abstract Task<string?> GetDefinitionAsync(DbConnectionInfo connection, string? database, string schema, DbObjectKind kind, string name, CancellationToken cancellationToken = default);

    public abstract Task<ExplorerPage<ExplorerObject>> SearchAsync(DbConnectionInfo connection, string? database, string filter, CancellationToken cancellationToken = default);

    public abstract Task<TableMetadata?> DescribeTableAsync(DbConnectionInfo connection, string? database, string? schema, string table, CancellationToken cancellationToken = default);

    protected async Task<DbConnection> OpenAsync(DbConnectionInfo info, string? database, CancellationToken cancellationToken)
    {
        var connection = CreateConnection(info.ConnectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            await SelectDatabaseAsync(connection, database, cancellationToken);

            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <summary>Corre una consulta de catálogo (texto fijo del código, valores siempre como parámetros) y devuelve hasta <paramref name="maxRows"/> filas.</summary>
    protected async Task<List<object?[]>> QueryAsync(
        DbConnection connection, string sql, int maxRows, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;
        Configure(command);

        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        var rows = new List<object?[]>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (rows.Count < maxRows && await reader.ReadAsync(cancellationToken))
        {
            var row = new object?[reader.FieldCount];

            for (var i = 0; i < row.Length; i++)
                row[i] = ReadValue(reader, i);

            rows.Add(row);
        }

        return rows;
    }

    protected static string Text(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    protected static int Int(object? value) => value == null ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);

    protected static bool Flag(object? value) => value switch
    {
        null => false,
        bool b => b,
        string s => s is "Y" or "YES" or "1" or "true",
        _ => Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0
    };

    /// <summary>Escapa % _ \ para usar el texto como parte de un LIKE ... ESCAPE '\'.</summary>
    protected static string LikeContains(string filter) => "%" + EscapeLike(filter) + "%";

    /// <summary>Lo que empieza con el texto: se usa para mostrar primero las coincidencias de prefijo (el autocompletado).</summary>
    protected static string LikePrefix(string filter) => EscapeLike(filter) + "%";

    private static string EscapeLike(string filter) =>
        filter.Trim().ToUpperInvariant().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    protected static async Task<ConnectionTestResult> GuardTestAsync(Func<Task<string>> test)
    {
        try
        {
            return new ConnectionTestResult(true, await test());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(false, ex.Message);
        }
    }
}

public class SqlServerExplorer : ExplorerBase
{
    private const string AllCodes = "'U','V','P','PC','FN','IF','TF','FS','FT','SO','SN'";

    private static readonly IReadOnlyList<DbObjectKind> SupportedKinds =
        [DbObjectKind.Table, DbObjectKind.View, DbObjectKind.Procedure, DbObjectKind.Function, DbObjectKind.Sequence, DbObjectKind.Synonym];

    public override DatabaseKind Kind => DatabaseKind.SqlServer;

    public override bool HasDatabases => true;

    public override IReadOnlyList<DbObjectKind> Kinds => SupportedKinds;

    protected override DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    protected override async Task SelectDatabaseAsync(DbConnection connection, string? database, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(database))
            await connection.ChangeDatabaseAsync(database, cancellationToken);
    }

    // Códigos de sys.objects.type por tipo de objeto (texto fijo, nunca viene del cliente).
    private static string Codes(DbObjectKind kind) => kind switch
    {
        DbObjectKind.Table => "'U'",
        DbObjectKind.View => "'V'",
        DbObjectKind.Procedure => "'P','PC'",
        DbObjectKind.Function => "'FN','IF','TF','FS','FT'",
        DbObjectKind.Sequence => "'SO'",
        DbObjectKind.Synonym => "'SN'",
        _ => "''"
    };

    private static DbObjectKind? KindFromCode(string code) => code.Trim() switch
    {
        "U" => DbObjectKind.Table,
        "V" => DbObjectKind.View,
        "P" or "PC" => DbObjectKind.Procedure,
        "FN" or "IF" or "TF" or "FS" or "FT" => DbObjectKind.Function,
        "SO" => DbObjectKind.Sequence,
        "SN" => DbObjectKind.Synonym,
        _ => null
    };

    private const string ObjectIdExpression = "OBJECT_ID(QUOTENAME(@p_schema) + '.' + QUOTENAME(@p_name))";

    public override Task<ConnectionTestResult> TestAsync(DbConnectionInfo connection, CancellationToken cancellationToken = default) =>
        GuardTestAsync(async () =>
        {
            await using var c = await OpenAsync(connection, null, cancellationToken);
            var row = (await QueryAsync(c,
                "SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(50)), CAST(SERVERPROPERTY('Edition') AS nvarchar(100)), SUSER_SNAME(), DB_NAME()",
                1, cancellationToken)).First();

            return $"Conexión correcta. SQL Server {Text(row[0])} ({Text(row[1])}), usuario {Text(row[2])}, base {Text(row[3])}.";
        });

    public override async Task<IReadOnlyList<ExplorerDatabase>> ListDatabasesAsync(DbConnectionInfo connection, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(connection, null, cancellationToken);

        // state = 0 → ONLINE; HAS_DBACCESS descarta las bases a las que este usuario no puede entrar.
        var rows = await QueryAsync(c,
            "SELECT name, database_id FROM sys.databases WHERE state = 0 AND HAS_DBACCESS(name) = 1 ORDER BY name",
            MaxObjects, cancellationToken);

        return rows.Select(r => new ExplorerDatabase(Text(r[0]), Int(r[1]) <= 4)).ToList();
    }

    public override async Task<IReadOnlyList<ExplorerSchema>> ListSchemasAsync(DbConnectionInfo connection, string? database, bool includeSystem, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(connection, database, cancellationToken);

        // Solo esquemas que tienen objetos propios del usuario (los demás son ruido, como en SSMS).
        var rows = await QueryAsync(c,
            "SELECT DISTINCT s.name, CASE WHEN s.name = SCHEMA_NAME() THEN 1 ELSE 0 END " +
            "FROM sys.objects o JOIN sys.schemas s ON s.schema_id = o.schema_id " +
            $"WHERE o.is_ms_shipped = 0 AND o.type IN ({AllCodes}) ORDER BY s.name",
            MaxObjects, cancellationToken);

        return rows.Select(r => new ExplorerSchema(Text(r[0]), false, Flag(r[1]))).ToList();
    }

    public override async Task<ExplorerPage<string>> ListObjectsAsync(DbConnectionInfo connection, string? database, string schema, DbObjectKind kind, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(connection, database, cancellationToken);

        var rows = await QueryAsync(c,
            "SELECT o.name FROM sys.objects o JOIN sys.schemas s ON s.schema_id = o.schema_id " +
            $"WHERE s.name = @p_schema AND o.is_ms_shipped = 0 AND o.type IN ({Codes(kind)}) ORDER BY o.name",
            MaxObjects + 1, cancellationToken, ("p_schema", schema));

        return new ExplorerPage<string>(rows.Take(MaxObjects).Select(r => Text(r[0])).ToList(), rows.Count > MaxObjects);
    }

    public override async Task<IReadOnlyList<ExplorerItem>> ListGroupAsync(
        DbConnectionInfo connection, string? database, string schema, DbObjectKind kind, string name, ObjectGroup group, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(connection, database, cancellationToken);

        var parameters = new (string Name, object? Value)[] { ("p_schema", schema), ("p_name", name) };

        switch (group)
        {
            case ObjectGroup.Columns:
            {
                var rows = await QueryAsync(c,
                    "SELECT c.name, TYPE_NAME(c.user_type_id), c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity, c.is_computed, " +
                    "       CASE WHEN EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id " +
                    "                         WHERE i.is_primary_key = 1 AND ic.object_id = c.object_id AND ic.column_id = c.column_id) THEN 1 ELSE 0 END " +
                    $"FROM sys.columns c WHERE c.object_id = {ObjectIdExpression} ORDER BY c.column_id",
                    MaxObjects, cancellationToken, parameters);

                return rows.Select(r =>
                {
                    var flags = new List<string> { TypeText(Text(r[1]), Int(r[2]), Int(r[3]), Int(r[4])) };

                    if (Flag(r[8])) flags.Add("PK");
                    if (Flag(r[6])) flags.Add("identity");
                    if (Flag(r[7])) flags.Add("calculada");
                    flags.Add(Flag(r[5]) ? "NULL" : "NOT NULL");

                    return new ExplorerItem(Text(r[0]), string.Join(" · ", flags));
                }).ToList();
            }

            case ObjectGroup.Indexes:
            {
                var rows = await QueryAsync(c,
                    "SELECT i.name, i.type_desc, i.is_unique, i.is_primary_key, " +
                    "  STUFF((SELECT ', ' + col.name FROM sys.index_columns ic JOIN sys.columns col ON col.object_id = ic.object_id AND col.column_id = ic.column_id " +
                    "         WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0 ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 2, '') " +
                    $"FROM sys.indexes i WHERE i.object_id = {ObjectIdExpression} AND i.name IS NOT NULL ORDER BY i.index_id",
                    MaxObjects, cancellationToken, parameters);

                return rows.Select(r => new ExplorerItem(Text(r[0]),
                    string.Join(" · ", new[] { Text(r[1]).ToLowerInvariant(), Flag(r[3]) ? "PK" : Flag(r[2]) ? "único" : null, $"({Text(r[4])})" }.Where(p => p != null)))).ToList();
            }

            case ObjectGroup.ForeignKeys:
            {
                var rows = await QueryAsync(c,
                    "SELECT fk.name, OBJECT_SCHEMA_NAME(fk.referenced_object_id) + '.' + OBJECT_NAME(fk.referenced_object_id) " +
                    $"FROM sys.foreign_keys fk WHERE fk.parent_object_id = {ObjectIdExpression} ORDER BY fk.name",
                    MaxObjects, cancellationToken, parameters);

                return rows.Select(r => new ExplorerItem(Text(r[0]), $"→ {Text(r[1])}")).ToList();
            }

            case ObjectGroup.Triggers:
            {
                var rows = await QueryAsync(c,
                    $"SELECT t.name, t.is_disabled FROM sys.triggers t WHERE t.parent_id = {ObjectIdExpression} ORDER BY t.name",
                    MaxObjects, cancellationToken, parameters);

                return rows.Select(r => new ExplorerItem(Text(r[0]), Flag(r[1]) ? "deshabilitado" : "habilitado")).ToList();
            }

            case ObjectGroup.Parameters:
            {
                var rows = await QueryAsync(c,
                    "SELECT p.name, TYPE_NAME(p.user_type_id), p.max_length, p.precision, p.scale, p.is_output, p.parameter_id " +
                    $"FROM sys.parameters p WHERE p.object_id = {ObjectIdExpression} ORDER BY p.parameter_id",
                    MaxObjects, cancellationToken, parameters);

                return rows.Select(r => new ExplorerItem(
                    Int(r[6]) == 0 ? "(retorno)" : Text(r[0]),
                    TypeText(Text(r[1]), Int(r[2]), Int(r[3]), Int(r[4])) + (Flag(r[5]) && Int(r[6]) != 0 ? " · OUTPUT" : string.Empty))).ToList();
            }

            default:
                return [];
        }
    }

    public override async Task<string?> GetDefinitionAsync(DbConnectionInfo connection, string? database, string schema, DbObjectKind kind, string name, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(connection, database, cancellationToken);

        var parameters = new (string Name, object? Value)[] { ("p_schema", schema), ("p_name", name) };

        switch (kind)
        {
            case DbObjectKind.View:
            case DbObjectKind.Procedure:
            case DbObjectKind.Function:
            {
                var rows = await QueryAsync(c, $"SELECT OBJECT_DEFINITION({ObjectIdExpression})", 1, cancellationToken, parameters);

                // null = cifrado (WITH ENCRYPTION) o sin permiso VIEW DEFINITION.
                return rows.FirstOrDefault()?[0] is { } text ? Text(text) : null;
            }

            case DbObjectKind.Synonym:
            {
                var rows = await QueryAsync(c,
                    "SELECT base_object_name FROM sys.synonyms WHERE name = @p_name AND schema_id = SCHEMA_ID(@p_schema)",
                    1, cancellationToken, parameters);

                return rows.Count == 0 ? null : $"CREATE SYNONYM [{schema}].[{name}] FOR {Text(rows[0][0])}";
            }

            case DbObjectKind.Sequence:
            {
                var rows = await QueryAsync(c,
                    "SELECT CAST(start_value AS nvarchar(40)), CAST(increment AS nvarchar(40)), CAST(minimum_value AS nvarchar(40)), " +
                    "       CAST(maximum_value AS nvarchar(40)), is_cycling, CAST(current_value AS nvarchar(40)) " +
                    "FROM sys.sequences WHERE name = @p_name AND schema_id = SCHEMA_ID(@p_schema)",
                    1, cancellationToken, parameters);

                if (rows.Count == 0)
                    return null;

                var r = rows[0];

                return $"CREATE SEQUENCE [{schema}].[{name}]\n  START WITH {Text(r[0])}\n  INCREMENT BY {Text(r[1])}\n  MINVALUE {Text(r[2])}\n  MAXVALUE {Text(r[3])}\n  {(Flag(r[4]) ? "CYCLE" : "NO CYCLE")};\n\n-- valor actual: {Text(r[5])}";
            }

            default:
                return null; // SQL Server no guarda el DDL de una tabla: se ven sus columnas, índices y claves en el árbol.
        }
    }

    public override async Task<ExplorerPage<ExplorerObject>> SearchAsync(DbConnectionInfo connection, string? database, string filter, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(connection, database, cancellationToken);

        var rows = await QueryAsync(c,
            "SELECT s.name, o.name, o.type FROM sys.objects o JOIN sys.schemas s ON s.schema_id = o.schema_id " +
            $"WHERE o.is_ms_shipped = 0 AND o.type IN ({AllCodes}) AND UPPER(o.name) LIKE @p_filter ESCAPE '\\' " +
            // Primero lo que EMPIEZA con el texto (lo que busca quien escribe "Bg_..."), después lo que solo lo contiene.
            "ORDER BY CASE WHEN UPPER(o.name) LIKE @p_prefix ESCAPE '\\' THEN 0 ELSE 1 END, o.name, s.name",
            MaxSearch + 1, cancellationToken, ("p_filter", LikeContains(filter)), ("p_prefix", LikePrefix(filter)));

        var items = rows
            .Select(r => (Schema: Text(r[0]), Name: Text(r[1]), Kind: KindFromCode(Text(r[2]))))
            .Where(x => x.Kind != null)
            .Take(MaxSearch)
            .Select(x => new ExplorerObject(x.Schema, x.Name, x.Kind!.Value))
            .ToList();

        return new ExplorerPage<ExplorerObject>(items, rows.Count > MaxSearch);
    }

    public override async Task<TableMetadata?> DescribeTableAsync(DbConnectionInfo connection, string? database, string? schema, string table, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(connection, database, cancellationToken);

        // OBJECT_ID(..., 'U') solo resuelve TABLAS de usuario; sin esquema resuelve como lo haría un SELECT (esquema por defecto, luego dbo).
        var rows = await QueryAsync(c,
            "SELECT SCHEMA_NAME(o.schema_id), o.name, c.name, TYPE_NAME(c.user_type_id), c.is_nullable, c.is_identity, c.is_computed, c.scale, " +
            "       CASE WHEN EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id " +
            "                         WHERE i.is_primary_key = 1 AND ic.object_id = c.object_id AND ic.column_id = c.column_id) THEN 1 ELSE 0 END " +
            "FROM sys.objects o JOIN sys.columns c ON c.object_id = o.object_id " +
            "WHERE o.object_id = OBJECT_ID(CASE WHEN @p_schema IS NULL THEN QUOTENAME(@p_name) ELSE QUOTENAME(@p_schema) + '.' + QUOTENAME(@p_name) END, 'U') " +
            "ORDER BY c.column_id",
            MaxObjects, cancellationToken, ("p_schema", schema), ("p_name", table));

        if (rows.Count == 0)
            return null;

        var columns = rows.Select(r =>
        {
            var type = Text(r[3]);

            return new TableColumn(Text(r[2]), type, ColumnCategories.From(DatabaseKind.SqlServer, type, Int(r[7])), Flag(r[4]), Flag(r[8]), Flag(r[5]), Flag(r[6]));
        }).ToList();

        return new TableMetadata(Text(rows[0][0]), Text(rows[0][1]), columns);
    }

    internal static string TypeText(string type, int maxLength, int precision, int scale)
    {
        switch (type.ToLowerInvariant())
        {
            case "nchar":
            case "nvarchar":
                return $"{type}({(maxLength == -1 ? "max" : (maxLength / 2).ToString(CultureInfo.InvariantCulture))})";

            case "char":
            case "varchar":
            case "binary":
            case "varbinary":
                return $"{type}({(maxLength == -1 ? "max" : maxLength.ToString(CultureInfo.InvariantCulture))})";

            case "decimal":
            case "numeric":
                return $"{type}({precision},{scale})";

            default:
                return type;
        }
    }
}

public class OracleExplorer : ExplorerBase
{
    // Esquemas que Oracle crea y mantiene — se ocultan salvo que se pida verlos.
    private static readonly HashSet<string> SystemOwners = new(StringComparer.OrdinalIgnoreCase)
    {
        "SYS", "SYSTEM", "XDB", "MDSYS", "CTXSYS", "ORDSYS", "ORDDATA", "ORDPLUGINS", "WMSYS", "OUTLN", "DBSNMP",
        "APPQOSSYS", "AUDSYS", "GSMADMIN_INTERNAL", "LBACSYS", "DVSYS", "OJVMSYS", "SI_INFORMTN_SCHEMA", "DVF",
        "ANONYMOUS", "DIP", "EXFSYS", "FLOWS_FILES", "GGSYS", "MDDATA", "ORACLE_OCM", "XS$NULL", "REMOTE_SCHEDULER_AGENT",
        "SYSBACKUP", "SYSDG", "SYSKM", "SYSRAC", "SYS$UMF", "DBSFWUSER", "PUBLIC", "OLAPSYS", "SPATIAL_CSW_ADMIN_USR",
        "SPATIAL_WFS_ADMIN_USR", "WK_TEST", "TSMSYS", "DMSYS"
    };

    private static readonly IReadOnlyList<DbObjectKind> SupportedKinds =
    [
        DbObjectKind.Table, DbObjectKind.View, DbObjectKind.Procedure, DbObjectKind.Function, DbObjectKind.Package,
        DbObjectKind.Sequence, DbObjectKind.Synonym, DbObjectKind.Type
    ];

    public override DatabaseKind Kind => DatabaseKind.Oracle;

    public override bool HasDatabases => false;

    public override IReadOnlyList<DbObjectKind> Kinds => SupportedKinds;

    protected override DbConnection CreateConnection(string connectionString) => new OracleConnection(connectionString);

    protected override void Configure(DbCommand command)
    {
        if (command is OracleCommand oracle)
        {
            oracle.BindByName = true;
            oracle.InitialLOBFetchSize = -1;  // el DDL de GET_DDL (CLOB) llega completo
        }
    }

    protected override object? ReadValue(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;

        try
        {
            return reader.GetValue(ordinal) switch
            {
                OracleClob clob => clob.Value,
                var other => other
            };
        }
        catch (OverflowException) when (reader is OracleDataReader oracle)
        {
            return oracle.GetOracleDecimal(ordinal).ToString();
        }
    }

    private static string OracleType(DbObjectKind kind) => kind switch
    {
        DbObjectKind.Table => "TABLE",
        DbObjectKind.View => "VIEW",
        DbObjectKind.Procedure => "PROCEDURE",
        DbObjectKind.Function => "FUNCTION",
        DbObjectKind.Package => "PACKAGE",
        DbObjectKind.Sequence => "SEQUENCE",
        DbObjectKind.Synonym => "SYNONYM",
        DbObjectKind.Type => "TYPE",
        _ => string.Empty
    };

    private static DbObjectKind? KindFromOracle(string type) => type switch
    {
        "TABLE" => DbObjectKind.Table,
        "VIEW" => DbObjectKind.View,
        "PROCEDURE" => DbObjectKind.Procedure,
        "FUNCTION" => DbObjectKind.Function,
        "PACKAGE" => DbObjectKind.Package,
        "SEQUENCE" => DbObjectKind.Sequence,
        "SYNONYM" => DbObjectKind.Synonym,
        "TYPE" => DbObjectKind.Type,
        _ => null
    };

    private const string AllTypes = "'TABLE','VIEW','PROCEDURE','FUNCTION','PACKAGE','SEQUENCE','SYNONYM','TYPE'";

    // Lista fija del código (nunca del cliente): los esquemas de Oracle salvo PUBLIC, cuyos sinónimos (DUAL...) sí se quieren sugerir.
    private static readonly string SearchExcludedOwners =
        string.Join(", ", SystemOwners.Where(o => !o.Equals("PUBLIC", StringComparison.OrdinalIgnoreCase)).Select(o => $"'{o}'"));

    public override Task<ConnectionTestResult> TestAsync(DbConnectionInfo connection, CancellationToken cancellationToken = default) =>
        GuardTestAsync(async () =>
        {
            await using var c = await OpenAsync(connection, null, cancellationToken);
            var row = (await QueryAsync(c,
                "SELECT SYS_CONTEXT('USERENV', 'SESSION_USER'), SYS_CONTEXT('USERENV', 'DB_NAME'), SYS_CONTEXT('USERENV', 'SERVICE_NAME') FROM dual",
                1, cancellationToken)).First();

            return $"Conexión correcta. Oracle {c.ServerVersion}, usuario {Text(row[0])}, base {Text(row[1])}, servicio {Text(row[2])}.";
        });

    // Oracle tiene una sola base por conexión: el árbol arranca en los esquemas.
    public override Task<IReadOnlyList<ExplorerDatabase>> ListDatabasesAsync(DbConnectionInfo connection, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ExplorerDatabase>>([]);

    public override async Task<IReadOnlyList<ExplorerSchema>> ListSchemasAsync(DbConnectionInfo connection, string? database, bool includeSystem, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(connection, null, cancellationToken);

        var current = Text((await QueryAsync(c, "SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') FROM dual", 1, cancellationToken))[0][0]);
        var rows = await QueryAsync(c, "SELECT username FROM all_users ORDER BY username", MaxObjects, cancellationToken);

        return rows
            .Select(r => Text(r[0]))
            .Select(name => new ExplorerSchema(name, IsSystem(name), name.Equals(current, StringComparison.OrdinalIgnoreCase)))
            .Where(s => includeSystem || !s.IsSystem || s.IsCurrent)
            .OrderByDescending(s => s.IsCurrent)   // el propio esquema primero, como SQL Developer
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsSystem(string owner) =>
        SystemOwners.Contains(owner) || owner.StartsWith("APEX_", StringComparison.OrdinalIgnoreCase) || owner.StartsWith("FLOWS_", StringComparison.OrdinalIgnoreCase);

    public override async Task<ExplorerPage<string>> ListObjectsAsync(DbConnectionInfo connection, string? database, string schema, DbObjectKind kind, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(connection, null, cancellationToken);

        // El subselect con ROWNUM sirve en cualquier versión (FETCH FIRST exige 12c).
        var rows = await QueryAsync(c,
            "SELECT object_name FROM (" +
            "  SELECT object_name FROM all_objects WHERE owner = :p_owner AND object_type = :p_type " +
            "  AND object_name NOT LIKE 'BIN$%' AND generated = 'N' ORDER BY object_name" +
            ") WHERE ROWNUM <= :p_max",
            MaxObjects + 1, cancellationToken, ("p_owner", schema), ("p_type", OracleType(kind)), ("p_max", MaxObjects + 1));

        return new ExplorerPage<string>(rows.Take(MaxObjects).Select(r => Text(r[0])).ToList(), rows.Count > MaxObjects);
    }

    public override async Task<IReadOnlyList<ExplorerItem>> ListGroupAsync(
        DbConnectionInfo connection, string? database, string schema, DbObjectKind kind, string name, ObjectGroup group, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(connection, null, cancellationToken);

        var parameters = new (string Name, object? Value)[] { ("p_owner", schema), ("p_name", name) };

        switch (group)
        {
            case ObjectGroup.Columns:
            {
                var rows = await QueryAsync(c,
                    "SELECT c.column_name, c.data_type, c.data_length, c.data_precision, c.data_scale, c.nullable, " +
                    "       CASE WHEN pk.column_name IS NULL THEN 0 ELSE 1 END " +
                    "FROM all_tab_columns c " +
                    "LEFT JOIN (SELECT cc.owner, cc.table_name, cc.column_name FROM all_constraints k " +
                    "           JOIN all_cons_columns cc ON cc.owner = k.owner AND cc.constraint_name = k.constraint_name " +
                    "           WHERE k.constraint_type = 'P') pk " +
                    "  ON pk.owner = c.owner AND pk.table_name = c.table_name AND pk.column_name = c.column_name " +
                    "WHERE c.owner = :p_owner AND c.table_name = :p_name ORDER BY c.column_id",
                    MaxObjects, cancellationToken, parameters);

                return rows.Select(r =>
                {
                    var flags = new List<string> { TypeText(Text(r[1]), r[2], r[3], r[4]) };

                    if (Flag(r[6])) flags.Add("PK");
                    flags.Add(Flag(r[5]) ? "NULL" : "NOT NULL");

                    return new ExplorerItem(Text(r[0]), string.Join(" · ", flags));
                }).ToList();
            }

            case ObjectGroup.Indexes:
            {
                var rows = await QueryAsync(c,
                    "SELECT i.index_name, i.index_type, i.uniqueness, " +
                    "  (SELECT LISTAGG(ic.column_name, ', ') WITHIN GROUP (ORDER BY ic.column_position) " +
                    "     FROM all_ind_columns ic WHERE ic.index_owner = i.owner AND ic.index_name = i.index_name) " +
                    "FROM all_indexes i WHERE i.table_owner = :p_owner AND i.table_name = :p_name ORDER BY i.index_name",
                    MaxObjects, cancellationToken, parameters);

                return rows.Select(r => new ExplorerItem(Text(r[0]),
                    string.Join(" · ", new[] { Text(r[1]).ToLowerInvariant(), Text(r[2]) == "UNIQUE" ? "único" : null, $"({Text(r[3])})" }.Where(p => p != null)))).ToList();
            }

            case ObjectGroup.ForeignKeys:
            {
                var rows = await QueryAsync(c,
                    "SELECT k.constraint_name, r.owner || '.' || r.table_name FROM all_constraints k " +
                    "JOIN all_constraints r ON r.owner = k.r_owner AND r.constraint_name = k.r_constraint_name " +
                    "WHERE k.owner = :p_owner AND k.table_name = :p_name AND k.constraint_type = 'R' ORDER BY k.constraint_name",
                    MaxObjects, cancellationToken, parameters);

                return rows.Select(r => new ExplorerItem(Text(r[0]), $"→ {Text(r[1])}")).ToList();
            }

            case ObjectGroup.Triggers:
            {
                var rows = await QueryAsync(c,
                    "SELECT trigger_name, triggering_event, status FROM all_triggers " +
                    "WHERE table_owner = :p_owner AND table_name = :p_name ORDER BY trigger_name",
                    MaxObjects, cancellationToken, parameters);

                return rows.Select(r => new ExplorerItem(Text(r[0]), $"{Text(r[1]).Trim().ToLowerInvariant()} · {(Text(r[2]) == "ENABLED" ? "habilitado" : "deshabilitado")}")).ToList();
            }

            case ObjectGroup.Parameters:
            {
                var rows = await QueryAsync(c,
                    "SELECT argument_name, data_type, in_out, position FROM all_arguments " +
                    "WHERE owner = :p_owner AND object_name = :p_name AND package_name IS NULL AND data_level = 0 ORDER BY sequence",
                    MaxObjects, cancellationToken, parameters);

                return rows.Select(r => new ExplorerItem(
                    r[0] == null ? "(retorno)" : Text(r[0]),
                    $"{Text(r[1])}{(r[0] == null ? string.Empty : $" · {Text(r[2])}")}")).ToList();
            }

            case ObjectGroup.Members:
            {
                var rows = await QueryAsync(c,
                    "SELECT procedure_name FROM all_procedures " +
                    "WHERE owner = :p_owner AND object_name = :p_name AND procedure_name IS NOT NULL ORDER BY subprogram_id",
                    MaxObjects, cancellationToken, parameters);

                return rows.Select(r => new ExplorerItem(Text(r[0]), null)).ToList();
            }

            default:
                return [];
        }
    }

    public override async Task<string?> GetDefinitionAsync(DbConnectionInfo connection, string? database, string schema, DbObjectKind kind, string name, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(connection, null, cancellationToken);

        if (kind is DbObjectKind.Procedure or DbObjectKind.Function or DbObjectKind.Package or DbObjectKind.Type)
        {
            // Fuente PL/SQL: all_source guarda el texto sin "CREATE OR REPLACE"; se le antepone. Paquetes y tipos tienen
            // especificación y cuerpo ("PACKAGE" < "PACKAGE BODY" alfabéticamente, así que quedan en ese orden).
            var rows = await QueryAsync(c,
                "SELECT type, text FROM all_source WHERE owner = :p_owner AND name = :p_name " +
                "AND type IN ('PROCEDURE','FUNCTION','PACKAGE','PACKAGE BODY','TYPE','TYPE BODY') ORDER BY type, line",
                50_000, cancellationToken, ("p_owner", schema), ("p_name", name));

            if (rows.Count == 0)
                return null;

            var builder = new StringBuilder();

            foreach (var block in rows.GroupBy(r => Text(r[0])))
            {
                if (builder.Length > 0)
                    builder.Append("\n/\n\n");

                builder.Append("CREATE OR REPLACE ");

                foreach (var line in block)
                    builder.Append(Text(line[1]));
            }

            return builder.ToString().TrimEnd() + "\n/";
        }

        // Tablas, vistas, secuencias y sinónimos: DDL completo con DBMS_METADATA.
        var ddl = await QueryAsync(c, "SELECT DBMS_METADATA.GET_DDL(:p_type, :p_name, :p_owner) FROM dual", 1, cancellationToken,
            ("p_type", OracleType(kind)), ("p_name", name), ("p_owner", schema));

        return ddl.FirstOrDefault()?[0] is { } text ? Text(text).Trim() : null;
    }

    public override async Task<ExplorerPage<ExplorerObject>> SearchAsync(DbConnectionInfo connection, string? database, string filter, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(connection, null, cancellationToken);

        var rows = await QueryAsync(c,
            "SELECT owner, object_name, object_type FROM (" +
            $"  SELECT owner, object_name, object_type FROM all_objects WHERE object_type IN ({AllTypes}) " +
            // Sin los esquemas que mantiene Oracle (SYS, XDB...), pero con los sinónimos PUBLIC (DUAL, etc.).
            $"  AND owner NOT IN ({SearchExcludedOwners}) AND owner NOT LIKE 'APEX\\_%' ESCAPE '\\' " +
            "  AND object_name NOT LIKE 'BIN$%' AND UPPER(object_name) LIKE :p_filter ESCAPE '\\' " +
            "  ORDER BY CASE WHEN UPPER(object_name) LIKE :p_prefix ESCAPE '\\' THEN 0 ELSE 1 END, object_name, owner" +
            ") WHERE ROWNUM <= :p_max",
            MaxSearch + 1, cancellationToken, ("p_filter", LikeContains(filter)), ("p_prefix", LikePrefix(filter)), ("p_max", MaxSearch + 1));

        var items = rows
            .Select(r => (Schema: Text(r[0]), Name: Text(r[1]), Kind: KindFromOracle(Text(r[2]))))
            .Where(x => x.Kind != null)
            .Take(MaxSearch)
            .Select(x => new ExplorerObject(x.Schema, x.Name, x.Kind!.Value))
            .ToList();

        return new ExplorerPage<ExplorerObject>(items, rows.Count > MaxSearch);
    }

    public override async Task<TableMetadata?> DescribeTableAsync(DbConnectionInfo connection, string? database, string? schema, string table, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(connection, null, cancellationToken);

        schema ??= Text((await QueryAsync(c, "SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') FROM dual", 1, cancellationToken))[0][0]);

        // all_tables: solo tablas (no vistas ni sinónimos). La clave primaria es la restricción 'P' habilitada.
        var rows = await QueryAsync(c,
            "SELECT c.owner, c.table_name, c.column_name, c.data_type, c.nullable, c.data_scale, c.virtual_column, " +
            "       CASE WHEN pk.column_name IS NULL THEN 0 ELSE 1 END " +
            "FROM all_tab_columns c " +
            "JOIN all_tables t ON t.owner = c.owner AND t.table_name = c.table_name " +
            "LEFT JOIN (SELECT cc.owner, cc.table_name, cc.column_name FROM all_constraints k " +
            "           JOIN all_cons_columns cc ON cc.owner = k.owner AND cc.constraint_name = k.constraint_name " +
            "           WHERE k.constraint_type = 'P' AND k.status = 'ENABLED') pk " +
            "  ON pk.owner = c.owner AND pk.table_name = c.table_name AND pk.column_name = c.column_name " +
            "WHERE c.owner = :p_owner AND c.table_name = :p_name ORDER BY c.column_id",
            MaxObjects, cancellationToken, ("p_owner", schema), ("p_name", table));

        if (rows.Count == 0)
            return null;

        // Las columnas IDENTITY existen desde 12c: en una 11g la vista no está y simplemente no hay identity que marcar.
        var identity = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            var identityRows = await QueryAsync(c,
                "SELECT column_name FROM all_tab_identity_cols WHERE owner = :p_owner AND table_name = :p_name",
                MaxObjects, cancellationToken, ("p_owner", schema), ("p_name", table));

            foreach (var row in identityRows)
                identity.Add(Text(row[0]));
        }
        catch (DbException)
        {
        }

        var columns = rows.Select(r =>
        {
            var name = Text(r[2]);
            var type = Text(r[3]);
            var scale = r[5] == null ? (int?)null : Int(r[5]);

            return new TableColumn(name, type, ColumnCategories.From(DatabaseKind.Oracle, type, scale), Text(r[4]) == "Y", Flag(r[7]), identity.Contains(name), Text(r[6]) == "YES");
        }).ToList();

        return new TableMetadata(Text(rows[0][0]), Text(rows[0][1]), columns);
    }

    internal static string TypeText(string type, object? length, object? precision, object? scale)
    {
        if (type == "NUMBER" && precision != null)
            return Int(scale) != 0 ? $"NUMBER({Int(precision)},{Int(scale)})" : $"NUMBER({Int(precision)})";

        if (type is "CHAR" or "NCHAR" or "RAW" || type.StartsWith("VARCHAR", StringComparison.Ordinal) || type == "NVARCHAR2")
            return length != null ? $"{type}({Int(length)})" : type;

        return type;
    }
}
