using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;

namespace DataBaseSDK.Core;

/// <summary>
/// Edición de filas desde la grilla de resultados. Es la ÚNICA ruta de escritura del módulo (el editor de consultas sigue siendo de
/// solo lectura) y está pensada para que sea difícil equivocarse:
/// <list type="bullet">
/// <item>solo con la conexión habilitada (<see cref="DbConnectionInfo.AllowWrite"/>);</item>
/// <item>solo UPDATE de columnas simples de una tabla, por clave primaria completa y con parámetros — el SQL se arma con los nombres
/// exactos del catálogo, nunca con texto del cliente;</item>
/// <item>antes de cada UPDATE se bloquea la fila y se verifica que siga valiendo lo que el usuario vio; si alguien la tocó, no se pisa;</item>
/// <item>una sola transacción para todo; cada UPDATE debe afectar exactamente 1 fila; cualquier fallo revierte todo;</item>
/// <item>bitácora (<see cref="IEditAuditLog"/>) escrita ANTES del commit: sin bitácora no se guarda;</item>
/// <item>modo de prueba que hace todo y revierte.</item>
/// </list>
/// </summary>
public abstract class RowEditorBase : IRowEditor
{
    public const int MaxRows = 200;

    private const int CommandTimeoutSeconds = 30;

    private static readonly string[] DateTimeFormats =
    [
        "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm", "yyyy-MM-dd",
        "yyyy-MM-dd HH:mm:ss.FFFFFFF", "yyyy-MM-ddTHH:mm:ss.FFFFFFF"
    ];

    private readonly ITableDescriber _describer;
    private readonly IEditAuditLog _audit;

    protected RowEditorBase(ITableDescriber describer, IEditAuditLog audit)
    {
        _describer = describer;
        _audit = audit;
    }

    public abstract DatabaseKind Kind { get; }

    protected abstract DbConnection CreateConnection(string connectionString);

    protected virtual Task SelectDatabaseAsync(DbConnection connection, string? database, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Sentencia que va primero dentro de la transacción (ej. el tiempo máximo de espera por un bloqueo); null = ninguna.</summary>
    protected virtual string? SessionSetupSql => null;

    protected abstract string Quote(string identifier);

    /// <summary>Cómo se escribe un parámetro en el SQL (<c>@k0</c> / <c>:k0</c>).</summary>
    protected abstract string Marker(string name);

    protected abstract string LockHint { get; }

    protected abstract string LockSuffix { get; }

    /// <summary>Nombre (base, esquema, tabla) que el texto del FROM le pide a esta base; null y el motivo si no se puede interpretar.</summary>
    protected abstract (string? Database, string? Schema, string Table, string? Problem) ResolveName(SingleTable table, QueryOptions context);

    protected virtual void ConfigureCommand(DbCommand command)
    {
    }

    protected virtual DbType DbTypeFor(ColumnCategory category) => category switch
    {
        ColumnCategory.Integer => DbType.Int64,
        ColumnCategory.Decimal => DbType.Decimal,
        ColumnCategory.DateTime => DbType.DateTime,
        ColumnCategory.Bool => DbType.Boolean,
        ColumnCategory.Guid => DbType.Guid,
        _ => DbType.String
    };

    /// <summary>Convierte el valor del driver a lo mismo que muestra la grilla (<see cref="CellText.Normalize"/>), para que la comparación sea justa.</summary>
    protected virtual object? ReadCell(DbDataReader reader, int ordinal) => CellText.Normalize(reader.GetValue(ordinal));

    // ======================= ¿Se puede editar este resultado? =======================

    public async Task<EditInfo> PrepareAsync(
        DbConnectionInfo connection, QueryOptions context, string sql, IReadOnlyList<QueryColumn> resultColumns, CancellationToken cancellationToken = default)
    {
        if (!connection.AllowWrite)
            return EditInfo.No(null);

        var analysis = SingleTableAnalyzer.Analyze(sql);

        if (analysis.Table == null)
            return EditInfo.No(analysis.Reason);

        var (database, schema, tableName, problem) = ResolveName(analysis.Table, context);

        if (problem != null)
            return EditInfo.No(problem);

        TableMetadata? meta;

        try
        {
            meta = await _describer.DescribeTableAsync(connection, database, schema, tableName, cancellationToken);
        }
        catch (DbException ex)
        {
            return EditInfo.No($"No se pudo leer la estructura de la tabla: {ex.Message}");
        }

        if (meta == null)
            return EditInfo.No($"\"{tableName}\" no es una tabla (¿es una vista o un sinónimo?) o no se encontró: solo se editan tablas.");

        var key = meta.PrimaryKey;

        if (key.Count == 0)
            return EditInfo.No($"La tabla {meta.Name} no tiene clave primaria: sin ella no se puede identificar una fila de forma segura.");

        if (key.Any(k => k.Category == ColumnCategory.Unsupported))
            return EditInfo.No($"La clave primaria de {meta.Name} usa un tipo de dato que no se puede editar desde la grilla.");

        if (resultColumns.GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            return EditInfo.No("El resultado repite el nombre de una columna: ponle alias distintos para poder editarlo.");

        var missing = key.Where(k => resultColumns.All(c => !c.Name.Equals(k.Name, StringComparison.OrdinalIgnoreCase))).Select(k => k.Name).ToList();

        if (missing.Count > 0)
            return EditInfo.No($"Falta la clave primaria en el SELECT ({string.Join(", ", missing)}): inclúyela (o usa SELECT *) para poder identificar cada fila.");

        var columns = resultColumns.Select(result => Describe(result, meta)).ToList();

        if (!columns.Any(c => c.Editable))
            return EditInfo.No("Ninguna columna del resultado se puede editar (son clave primaria, calculadas, automáticas o de un tipo no soportado).");

        return new EditInfo(true, null, database, meta.Schema, meta.Name, key.Select(k => k.Name).ToList(), columns);
    }

    private static EditColumn Describe(QueryColumn result, TableMetadata meta)
    {
        var column = FindColumn(meta, result.Name);

        if (column == null)
            return new EditColumn(result.Name, false, false, nameof(ColumnCategory.Unsupported), "no es una columna de la tabla (calculada o con alias)");

        var reason =
            column.IsPrimaryKey ? "clave primaria" :
            column.IsIdentity ? "automática (identity)" :
            column.IsComputed ? "calculada" :
            column.Category == ColumnCategory.Unsupported ? $"tipo {column.DataType} no soportado" :
            null;

        return new EditColumn(result.Name, reason == null, column.Nullable, column.Category.ToString(), reason);
    }

    private static TableColumn? FindColumn(TableMetadata meta, string name)
    {
        var exact = meta.Columns.FirstOrDefault(c => c.Name == name);

        if (exact != null)
            return exact;

        var loose = meta.Columns.Where(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();

        return loose.Count == 1 ? loose[0] : null;
    }

    // ======================= Guardar =======================

    private sealed record KeyPart(TableColumn Column, string Text, object DbValue);

    private sealed record SetPart(TableColumn Column, string? Original, string? NewText, object DbValue);

    private sealed record PlannedRow(IReadOnlyList<KeyPart> Key, IReadOnlyList<SetPart> Sets)
    {
        public string KeyText => string.Join(", ", Key.Select(k => $"{k.Column.Name} = {k.Text}"));
    }

    public async Task<RowEditResult> ApplyAsync(DbConnectionInfo connection, RowEditRequest request, CancellationToken cancellationToken = default)
    {
        if (!connection.AllowWrite)
            throw new RowEditException(RowEditError.WriteNotAllowed, $"La conexión \"{connection.Alias}\" no tiene habilitada la edición de datos.");

        if (request.Changes.Count == 0)
            throw new RowEditException(RowEditError.Invalid, "No hay cambios para guardar.");

        if (request.Changes.Count > MaxRows)
            throw new RowEditException(RowEditError.Invalid, $"Se pueden guardar como máximo {MaxRows} filas por vez.");

        if (string.IsNullOrWhiteSpace(request.Table))
            throw new RowEditException(RowEditError.Invalid, "Falta el nombre de la tabla.");

        TableMetadata? meta;

        try
        {
            meta = await _describer.DescribeTableAsync(connection, NullIfEmpty(request.Database), NullIfEmpty(request.Schema), request.Table, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new RowEditException(RowEditError.Database, ex.Message, ex);
        }

        if (meta == null)
            throw new RowEditException(RowEditError.NotEditable, $"\"{request.Table}\" no es una tabla de esta base: solo se editan tablas.");

        var plan = Plan(meta, request);
        var table = $"{Quote(meta.Schema)}.{Quote(meta.Name)}";
        var changes = plan.Select(ToAudit).ToList();

        await using var db = CreateConnection(connection.ConnectionString);

        try
        {
            await db.OpenAsync(cancellationToken);
            await SelectDatabaseAsync(db, NullIfEmpty(request.Database), cancellationToken);
        }
        catch (DbException ex)
        {
            throw new RowEditException(RowEditError.Database, ex.Message, ex);
        }

        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var statements = new List<string>();

        try
        {
            if (SessionSetupSql != null)
                await ExecuteAsync(db, transaction, SessionSetupSql, [], cancellationToken);

            foreach (var row in plan)
                statements.Add(await ApplyRowAsync(db, transaction, table, row, cancellationToken));

            if (request.DryRun)
            {
                await RollbackQuietlyAsync(transaction);
                TryAudit(connection, request, meta, "prueba (revertida)", null, changes);

                return new RowEditResult(plan.Count, true, statements);
            }

            // Sin constancia no se guarda: si la bitácora falla, se revierte todo.
            try
            {
                Audit(connection, request, meta, "aplicando", null, changes);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new RowEditException(RowEditError.Audit, $"No se pudo escribir la bitácora de cambios ({ex.Message}); no se guardó nada.", ex);
            }

            await transaction.CommitAsync(cancellationToken);
            TryAudit(connection, request, meta, "confirmado", null, changes);

            return new RowEditResult(plan.Count, false, statements);
        }
        catch (RowEditException ex)
        {
            await RollbackQuietlyAsync(transaction);
            TryAudit(connection, request, meta, "rechazado", $"{ex.Code}: {ex.Message}", changes);

            throw;
        }
        catch (DbException ex)
        {
            await RollbackQuietlyAsync(transaction);
            TryAudit(connection, request, meta, "fallido", ex.Message, changes);

            throw new RowEditException(RowEditError.Database, ex.Message, ex);
        }
        catch (OperationCanceledException)
        {
            await RollbackQuietlyAsync(transaction);

            throw;
        }
    }

    private async Task<string> ApplyRowAsync(DbConnection db, DbTransaction transaction, string table, PlannedRow row, CancellationToken cancellationToken)
    {
        var where = string.Join(" AND ", row.Key.Select((k, i) => $"{Quote(k.Column.Name)} = {Marker($"k{i}")}"));
        var keyParameters = row.Key.Select((k, i) => (Name: $"k{i}", k.Column, k.DbValue)).ToList();

        // 1) Bloquear la fila y leer cómo está AHORA.
        var columns = string.Join(", ", row.Sets.Select(s => Quote(s.Column.Name)));
        var lockSql = $"SELECT {columns} FROM {table}{LockHint} WHERE {where}{LockSuffix}";

        List<object?> current;

        await using (var command = CreateCommand(db, transaction, lockSql, keyParameters))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
                throw new RowEditException(RowEditError.NotFound, $"La fila ({row.KeyText}) ya no existe: alguien la borró o cambió su clave. No se guardó nada.");

            current = Enumerable.Range(0, row.Sets.Count).Select(i => ReadCell(reader, i)).ToList();

            if (await reader.ReadAsync(cancellationToken))
                throw new RowEditException(RowEditError.RowCount, $"La clave ({row.KeyText}) identifica más de una fila: no se guardó nada.");
        }

        // 2) ¿Sigue valiendo lo que el usuario vio? Si no, alguien más la cambió: no se pisa.
        for (var i = 0; i < row.Sets.Count; i++)
        {
            var now = CellText.Format(current[i]);
            var seen = row.Sets[i].Original;

            if (now != seen)
                throw new RowEditException(RowEditError.Conflict,
                    $"La fila ({row.KeyText}) cambió desde que la consultaste: {row.Sets[i].Column.Name} ahora vale {Describe(now)} y vos viste {Describe(seen)}. " +
                    "Vuelve a ejecutar la consulta. No se guardó nada.");
        }

        // 3) UPDATE por clave primaria, solo de las columnas cambiadas.
        var sets = string.Join(", ", row.Sets.Select((s, i) => $"{Quote(s.Column.Name)} = {Marker($"v{i}")}"));
        var parameters = row.Sets.Select((s, i) => (Name: $"v{i}", s.Column, s.DbValue)).Concat(keyParameters).ToList();
        var updateSql = $"UPDATE {table} SET {sets} WHERE {where}";

        await using var update = CreateCommand(db, transaction, updateSql, parameters);
        var affected = await update.ExecuteNonQueryAsync(cancellationToken);

        if (affected != 1)
            throw new RowEditException(RowEditError.RowCount,
                $"El UPDATE de la fila ({row.KeyText}) afectó {affected} filas en vez de 1 (¿un trigger?). No se guardó nada.");

        return updateSql;
    }

    private async Task ExecuteAsync(DbConnection db, DbTransaction transaction, string sql, IReadOnlyList<(string Name, TableColumn Column, object DbValue)> parameters, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(db, transaction, sql, parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateCommand(DbConnection db, DbTransaction transaction, string sql, IReadOnlyList<(string Name, TableColumn Column, object DbValue)> parameters)
    {
        var command = db.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandTimeout = CommandTimeoutSeconds;
        ConfigureCommand(command);

        foreach (var (name, column, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.DbType = DbTypeFor(column.Category);
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        return command;
    }

    // ======================= Validar y convertir lo que llega del navegador =======================

    private List<PlannedRow> Plan(TableMetadata meta, RowEditRequest request)
    {
        var key = meta.PrimaryKey;

        if (key.Count == 0)
            throw new RowEditException(RowEditError.NotEditable, $"La tabla {meta.Name} no tiene clave primaria.");

        if (key.Any(k => k.Category == ColumnCategory.Unsupported))
            throw new RowEditException(RowEditError.NotEditable, $"La clave primaria de {meta.Name} usa un tipo que no se puede editar.");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<PlannedRow>();

        foreach (var change in request.Changes)
        {
            var keyParts = PlanKey(meta, key, change);
            var sets = new List<SetPart>();
            var setNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (name, edit) in change.Set)
            {
                var column = FindColumn(meta, name)
                    ?? throw new RowEditException(RowEditError.Invalid, $"La columna \"{name}\" no existe en {meta.Name}.");

                if (!setNames.Add(column.Name))
                    throw new RowEditException(RowEditError.Invalid, $"La columna {column.Name} viene repetida.");

                if (!column.IsEditable)
                    throw new RowEditException(RowEditError.Invalid, $"La columna {column.Name} no se puede editar (clave, automática, calculada o de tipo no soportado).");

                if (edit.Original == edit.Value)
                    continue;

                if (CellText.IsTruncated(edit.Original))
                    throw new RowEditException(RowEditError.Invalid, $"El valor de {column.Name} es demasiado largo y la grilla lo muestra recortado: editarlo borraría el resto.");

                if (edit.Value == null && !column.Nullable)
                    throw new RowEditException(RowEditError.Invalid, $"La columna {column.Name} no admite NULL.");

                sets.Add(new SetPart(column, edit.Original, edit.Value, ParseValue(column, edit.Value)));
            }

            if (sets.Count == 0)
                continue;

            var planned = new PlannedRow(keyParts, sets);

            if (!seen.Add(string.Join("|", keyParts.Select(k => k.Text))))
                throw new RowEditException(RowEditError.Invalid, $"La fila ({planned.KeyText}) viene repetida.");

            rows.Add(planned);
        }

        if (rows.Count == 0)
            throw new RowEditException(RowEditError.Invalid, "No hay cambios: los valores nuevos son iguales a los originales.");

        return rows;
    }

    private static List<KeyPart> PlanKey(TableMetadata meta, IReadOnlyList<TableColumn> key, RowChange change)
    {
        var parts = new List<KeyPart>();

        foreach (var column in key)
        {
            var given = change.PrimaryKey.FirstOrDefault(kv => kv.Key.Equals(column.Name, StringComparison.OrdinalIgnoreCase));

            if (given.Key == null || given.Value == null)
                throw new RowEditException(RowEditError.Invalid, $"Falta el valor de la clave primaria {column.Name} de una fila.");

            parts.Add(new KeyPart(column, given.Value, ParseValue(column, given.Value)));
        }

        var extra = change.PrimaryKey.Keys.FirstOrDefault(k => key.All(c => !c.Name.Equals(k, StringComparison.OrdinalIgnoreCase)));

        if (extra != null)
            throw new RowEditException(RowEditError.Invalid, $"\"{extra}\" no es parte de la clave primaria de {meta.Name}.");

        return parts;
    }

    /// <summary>Texto → valor tipado para el parámetro. Falla con un mensaje claro si no encaja en el tipo de la columna.</summary>
    internal static object ParseValue(TableColumn column, string? text)
    {
        if (text == null)
            return DBNull.Value;

        object Bad() => throw new RowEditException(RowEditError.Invalid, $"El valor \"{Shorten(text)}\" no es válido para la columna {column.Name} ({column.DataType}).");

        var value = text.Trim();

        switch (column.Category)
        {
            case ColumnCategory.String:
                return text;

            case ColumnCategory.Integer:
                return long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var whole) ? whole : Bad();

            case ColumnCategory.Decimal:
                const NumberStyles numeric = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

                if (decimal.TryParse(value, numeric, CultureInfo.InvariantCulture, out var number))
                    return number;

                // float/real pueden valer más de lo que cabe en un decimal.
                return double.TryParse(value, numeric, CultureInfo.InvariantCulture, out var floating) && double.IsFinite(floating) ? floating : Bad();

            case ColumnCategory.DateTime:
                return DateTime.TryParseExact(value, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                    ? date
                    : throw new RowEditException(RowEditError.Invalid, $"La fecha \"{Shorten(text)}\" no es válida para {column.Name}: usa el formato 2026-10-26 14:30:00 (o solo 2026-10-26).");

            case ColumnCategory.Bool:
                return value.ToLowerInvariant() switch
                {
                    "true" or "1" => true,
                    "false" or "0" => false,
                    _ => Bad()
                };

            case ColumnCategory.Guid:
                return Guid.TryParse(value, out var guid) ? guid : Bad();

            default:
                return Bad();
        }
    }

    // ======================= Bitácora =======================

    private static AuditChange ToAudit(PlannedRow row) => new(
        row.Key.ToDictionary(k => k.Column.Name, k => (string?)Shorten(k.Text)),
        row.Sets.ToDictionary(s => s.Column.Name, s => s.Original == null ? null : Shorten(s.Original)),
        row.Sets.ToDictionary(s => s.Column.Name, s => s.NewText == null ? null : Shorten(s.NewText)));

    private void Audit(DbConnectionInfo connection, RowEditRequest request, TableMetadata meta, string outcome, string? error, IReadOnlyList<AuditChange> changes) =>
        _audit.Write(new EditAuditEntry(DateTime.UtcNow, connection.Alias, NullIfEmpty(request.Database), meta.Schema, meta.Name, outcome, request.DryRun, error, changes));

    // Las anotaciones posteriores al hecho no pueden cambiar el resultado: un fallo de la bitácora acá no deshace nada.
    private void TryAudit(DbConnectionInfo connection, RowEditRequest request, TableMetadata meta, string outcome, string? error, IReadOnlyList<AuditChange> changes)
    {
        try
        {
            Audit(connection, request, meta, outcome, error, changes);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    // ======================= Utilidades =======================

    private static async Task RollbackQuietlyAsync(DbTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException)
        {
            // La conexión ya se cayó: la base descarta la transacción sola.
        }
    }

    private static string? NullIfEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;

    private static string Describe(string? text) => text == null ? "NULL" : $"\"{Shorten(text)}\"";

    private static string Shorten(string text) => text.Length > 200 ? text[..200] + "…" : text;
}

public class SqlServerRowEditor : RowEditorBase
{
    public SqlServerRowEditor(ITableDescriber describer, IEditAuditLog audit) : base(describer, audit)
    {
    }

    public override DatabaseKind Kind => DatabaseKind.SqlServer;

    protected override DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    protected override async Task SelectDatabaseAsync(DbConnection connection, string? database, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(database))
            await connection.ChangeDatabaseAsync(database, cancellationToken);
    }

    // Si otra sesión tiene la fila bloqueada, esperar 5 s y avisar, no quedarse colgado.
    protected override string? SessionSetupSql => "SET LOCK_TIMEOUT 5000";

    protected override string Quote(string identifier) => "[" + identifier.Replace("]", "]]") + "]";

    protected override string Marker(string name) => "@" + name;

    protected override string LockHint => " WITH (UPDLOCK, ROWLOCK)";

    protected override string LockSuffix => string.Empty;

    protected override DbType DbTypeFor(ColumnCategory category) => category == ColumnCategory.DateTime ? DbType.DateTime2 : base.DbTypeFor(category);

    protected override (string? Database, string? Schema, string Table, string? Problem) ResolveName(SingleTable table, QueryOptions context)
    {
        var parts = table.Parts;

        return parts.Count switch
        {
            1 => (Blank(context.Database), null, parts[0], null),
            2 => (Blank(context.Database), parts[0], parts[1], null),
            3 => (parts[0], parts[1], parts[2], null),
            _ => (null, null, string.Empty, "El nombre de la tabla tiene demasiadas partes (¿servidor vinculado?): solo se editan tablas de la base conectada.")
        };
    }

    private static string? Blank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;
}

public class OracleRowEditor : RowEditorBase
{
    public OracleRowEditor(ITableDescriber describer, IEditAuditLog audit) : base(describer, audit)
    {
    }

    public override DatabaseKind Kind => DatabaseKind.Oracle;

    protected override DbConnection CreateConnection(string connectionString) => new OracleConnection(connectionString);

    protected override void ConfigureCommand(DbCommand command)
    {
        if (command is OracleCommand oracle)
            oracle.BindByName = true;
    }

    protected override string Quote(string identifier)
    {
        // Los nombres vienen del catálogo; si alguno trae comillas o caracteres de control, mejor no armar el SQL.
        if (identifier.Length == 0 || identifier.Any(c => c == '"' || char.IsControl(c)))
            throw new RowEditException(RowEditError.Invalid, $"El nombre \"{identifier}\" no se puede usar en una edición.");

        return "\"" + identifier + "\"";
    }

    protected override string Marker(string name) => ":" + name;

    protected override string LockHint => string.Empty;

    // Esperar 5 s por una fila bloqueada por otra sesión y avisar (ORA-30006), en vez de quedarse colgado.
    protected override string LockSuffix => " FOR UPDATE WAIT 5";

    protected override object? ReadCell(DbDataReader reader, int ordinal)
    {
        try
        {
            return base.ReadCell(reader, ordinal);
        }
        catch (OverflowException) when (reader is OracleDataReader oracle)
        {
            // Mismo caso que la grilla: NUMBER con más precisión que un decimal de .NET se compara como texto exacto.
            return oracle.GetOracleDecimal(ordinal).ToString();
        }
    }

    protected override (string? Database, string? Schema, string Table, string? Problem) ResolveName(SingleTable table, QueryOptions context)
    {
        // Oracle guarda en mayúsculas lo que se escribió sin comillas.
        string Name(int index) => table.Quoted[index] ? table.Parts[index] : table.Parts[index].ToUpperInvariant();

        return table.Parts.Count switch
        {
            1 => (null, string.IsNullOrWhiteSpace(context.Schema) ? null : context.Schema, Name(0), null),
            2 => (null, Name(0), Name(1), null),
            _ => (null, null, string.Empty, "El nombre de la tabla tiene demasiadas partes: solo se editan tablas como ESQUEMA.TABLA.")
        };
    }
}
