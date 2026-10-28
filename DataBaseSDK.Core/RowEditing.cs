using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DataBaseSDK.Core;

// ======================= Modelos =======================

public enum ColumnCategory
{
    Integer,
    Decimal,
    String,
    DateTime,
    Bool,
    Guid,
    /// <summary>Binarios, LOB, XML, rowversion, intervalos...: se muestran pero no se editan desde la grilla.</summary>
    Unsupported
}

public record TableColumn(string Name, string DataType, ColumnCategory Category, bool Nullable, bool IsPrimaryKey, bool IsIdentity, bool IsComputed)
{
    /// <summary>Una clave primaria, una columna identity o calculada, o un tipo no soportado nunca se editan.</summary>
    public bool IsEditable => !IsPrimaryKey && !IsIdentity && !IsComputed && Category != ColumnCategory.Unsupported;
}

/// <summary>La tabla tal como la ve el catálogo: nombres exactos (con sus mayúsculas) y columnas.</summary>
public record TableMetadata(string Schema, string Name, IReadOnlyList<TableColumn> Columns)
{
    public IReadOnlyList<TableColumn> PrimaryKey => Columns.Where(c => c.IsPrimaryKey).ToList();
}

/// <summary>Una columna del RESULTADO de la consulta: si se puede editar, y si no, por qué.</summary>
public record EditColumn(string Name, bool Editable, bool Nullable, string Category, string? Reason);

/// <summary>Si el resultado se puede editar en la grilla. <see cref="Reason"/> explica por qué no (null = no hay nada que decir).</summary>
public record EditInfo(
    bool Editable, string? Reason, string? Database, string? Schema, string? Table,
    IReadOnlyList<string> PrimaryKey, IReadOnlyList<EditColumn> Columns)
{
    public static EditInfo No(string? reason) => new(false, reason, null, null, null, [], []);
}

public class CellEdit
{
    /// <summary>Lo que se veía en la grilla al consultar (texto de <see cref="CellText.Format"/>); null = NULL.</summary>
    public string? Original { get; set; }

    /// <summary>Lo que el usuario quiere dejar; null = NULL.</summary>
    public string? Value { get; set; }
}

public class RowChange
{
    /// <summary>Columna de clave primaria → su valor (texto). Identifica la fila.</summary>
    public Dictionary<string, string?> PrimaryKey { get; set; } = [];

    public Dictionary<string, CellEdit> Set { get; set; } = [];
}

public class RowEditRequest
{
    public string? Database { get; set; }

    public string Schema { get; set; } = string.Empty;

    public string Table { get; set; } = string.Empty;

    /// <summary>Hace todo (incluida la verificación) dentro de la transacción y la revierte: prueba que se podría guardar, sin guardar.</summary>
    public bool DryRun { get; set; }

    public List<RowChange> Changes { get; set; } = [];
}

public record RowEditResult(int RowsAffected, bool DryRun, IReadOnlyList<string> Statements);

public enum RowEditError
{
    /// <summary>La conexión no tiene habilitada la edición de datos.</summary>
    WriteNotAllowed,
    NotEditable,
    Invalid,
    NotFound,
    /// <summary>El valor actual en la base ya no es el que se vio al consultar: alguien lo cambió.</summary>
    Conflict,
    /// <summary>El UPDATE no afectó exactamente 1 fila.</summary>
    RowCount,
    /// <summary>No se pudo dejar constancia en la bitácora: sin bitácora no se guarda nada.</summary>
    Audit,
    Database
}

public class RowEditException : Exception
{
    public RowEditException(RowEditError code, string message, Exception? inner = null) : base(message, inner)
    {
        Code = code;
    }

    public RowEditError Code { get; }
}

public static class ColumnCategories
{
    public static ColumnCategory From(DatabaseKind kind, string dataType, int? scale = null)
    {
        var type = dataType.Trim().ToLowerInvariant();

        if (kind == DatabaseKind.SqlServer)
        {
            return type switch
            {
                "tinyint" or "smallint" or "int" or "bigint" => ColumnCategory.Integer,
                "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => ColumnCategory.Decimal,
                "char" or "varchar" or "nchar" or "nvarchar" => ColumnCategory.String,
                "date" or "datetime" or "datetime2" or "smalldatetime" => ColumnCategory.DateTime,
                "bit" => ColumnCategory.Bool,
                "uniqueidentifier" => ColumnCategory.Guid,
                _ => ColumnCategory.Unsupported
            };
        }

        if (type == "number")
            return scale == 0 ? ColumnCategory.Integer : ColumnCategory.Decimal;

        if (type is "float" or "binary_float" or "binary_double")
            return ColumnCategory.Decimal;

        if (type is "varchar2" or "nvarchar2" or "char" or "nchar")
            return ColumnCategory.String;

        if (type == "date" || (type.StartsWith("timestamp", StringComparison.Ordinal) && !type.Contains("with", StringComparison.Ordinal)))
            return ColumnCategory.DateTime;

        return ColumnCategory.Unsupported;
    }
}

// ======================= ¿El SELECT es de una sola tabla? =======================

/// <summary>Lee la estructura de una tabla del catálogo del motor (columnas, tipos, clave primaria).</summary>
public interface ITableDescriber
{
    /// <summary>
    /// Describe la TABLA (no vistas ni sinónimos) con ese nombre; null si no existe o no es una tabla. Con <paramref name="schema"/> null
    /// se usa el esquema por defecto de la sesión. Los nombres que devuelve son los exactos del catálogo.
    /// </summary>
    Task<TableMetadata?> DescribeTableAsync(DbConnectionInfo connection, string? database, string? schema, string table, CancellationToken cancellationToken = default);
}

public record SingleTable(IReadOnlyList<string> Parts, IReadOnlyList<bool> Quoted);

public record SingleTableResult(SingleTable? Table, string? Reason);

/// <summary>
/// Decide si un SELECT lee de UNA sola tabla, sin agrupar ni combinar: solo entonces cada fila del resultado es una
/// fila de esa tabla y se puede editar. Es un análisis de texto conservador — ante la duda dice que no. Lo que no
/// detecta (un alias que se llama como otra columna, por ejemplo) lo frena después la verificación de valores
/// originales al guardar.
/// </summary>
public static partial class SingleTableAnalyzer
{
    private const string Ident = @"(?:\[[^\]]*\]|""[^""]*""|[A-Za-z_@#][\w$#]*)";

    [GeneratedRegex(Ident + @"(?:\." + Ident + @")*|[(),;]|\S")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(Ident)]
    private static partial Regex IdentRegex();

    private static readonly HashSet<string> NotAnAlias = new(StringComparer.OrdinalIgnoreCase)
    {
        "WHERE", "GROUP", "ORDER", "HAVING", "UNION", "INTERSECT", "EXCEPT", "MINUS", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "CROSS",
        "OUTER", "NATURAL", "ON", "USING", "FETCH", "OFFSET", "FOR", "LIMIT", "WITH", "OPTION", "WINDOW", "AS"
    };

    private static readonly HashSet<string> Forbidden = new(StringComparer.OrdinalIgnoreCase)
    {
        "JOIN", "UNION", "INTERSECT", "EXCEPT", "MINUS", "GROUP", "HAVING", "DISTINCT", "PIVOT", "UNPIVOT", "APPLY", "INTO"
    };

    // Lo único que puede venir después de la tabla (y su alias): el resto de las cláusulas de un SELECT simple.
    private static readonly HashSet<string> Trailing = new(StringComparer.OrdinalIgnoreCase)
    {
        "WHERE", "ORDER", "FETCH", "OFFSET", "OPTION", "LIMIT", ";", ")"
    };

    private static readonly HashSet<string> Aggregates = new(StringComparer.OrdinalIgnoreCase) { "COUNT", "SUM", "AVG", "MIN", "MAX" };

    public static SingleTableResult Analyze(string sql)
    {
        var tokens = TokenRegex().Matches(MaskLiterals(sql)).Select(m => m.Value).ToList();

        static bool IsWord(string token) => token.Length > 0 && (char.IsLetter(token[0]) || token[0] is '_' or '@' or '#' or '[' or '"');
        static SingleTableResult No(string reason) => new(null, reason);

        if (tokens.Count == 0 || !tokens[0].Equals("SELECT", StringComparison.OrdinalIgnoreCase))
            return No("Solo se pueden editar los resultados de un SELECT simple de una tabla (no de un WITH ni de otra sentencia).");

        var depth = 0;
        var fromIndex = -1;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (token == "(") { depth++; continue; }
            if (token == ")") { depth--; continue; }
            if (depth != 0 || !IsWord(token)) continue;

            if (Forbidden.Contains(token))
                return No($"El resultado no es de una sola tabla sin transformar (usa {token.ToUpperInvariant()}): no se puede editar.");

            if (fromIndex < 0 && Aggregates.Contains(token) && i + 1 < tokens.Count && tokens[i + 1] == "(")
                return No("El SELECT agrupa o calcula totales (COUNT, SUM...): las filas no corresponden a filas de la tabla.");

            if (fromIndex < 0 && token.Equals("FROM", StringComparison.OrdinalIgnoreCase))
                fromIndex = i;
        }

        if (fromIndex < 0)
            return No("El SELECT no lee de ninguna tabla.");

        var next = fromIndex + 1;

        if (next >= tokens.Count || tokens[next] == "(")
            return No("El FROM es una subconsulta: solo se editan resultados que vienen directo de una tabla.");

        if (!IsWord(tokens[next]) || tokens[next][0] == '@')
            return No("No se reconoce la tabla del FROM (¿es una variable?).");

        if (next + 1 < tokens.Count && tokens[next + 1][0] == '@')
            return No("La tabla viene de otro servidor (DB link): solo se editan tablas de la base conectada.");

        var chain = tokens[next];
        var parts = IdentRegex().Matches(chain).Select(m => m.Value).ToList();
        var table = new SingleTable(parts.Select(Unquote).ToList(), parts.Select(p => p.StartsWith('[') || p.StartsWith('"')).ToList());

        // Después de la tabla: alias opcional (con o sin AS) y pista WITH (NOLOCK); luego nada que sume tablas.
        var after = next + 1;

        if (after < tokens.Count && tokens[after].Equals("AS", StringComparison.OrdinalIgnoreCase)) after += 2;
        else if (after < tokens.Count && IsWord(tokens[after]) && !NotAnAlias.Contains(tokens[after])) after++;

        if (after < tokens.Count && tokens[after].Equals("WITH", StringComparison.OrdinalIgnoreCase) && after + 1 < tokens.Count && tokens[after + 1] == "(")
        {
            var level = 0;

            for (after++; after < tokens.Count; after++)
            {
                if (tokens[after] == "(") level++;
                else if (tokens[after] == ")" && --level == 0) { after++; break; }
            }
        }

        if (after < tokens.Count && tokens[after] == ",")
            return No("El FROM tiene varias tablas: solo se editan resultados de una sola tabla.");

        // Si lo que sigue no es una cláusula conocida, el análisis no entendió el texto: ante la duda, no se edita.
        if (after < tokens.Count && !Trailing.Contains(tokens[after]))
            return No($"No se pudo interpretar el FROM (\"{tokens[after]}\"): solo se editan SELECT simples de una tabla.");

        return new SingleTableResult(table, null);
    }

    private static string Unquote(string part)
    {
        if (part.Length >= 2 && part[0] == '[' && part[^1] == ']') return part[1..^1].Replace("]]", "]");
        if (part.Length >= 2 && part[0] == '"' && part[^1] == '"') return part[1..^1].Replace("\"\"", "\"");
        return part;
    }

    // Misma longitud que el texto: comentarios y textos entre comillas simples quedan en blanco.
    private static string MaskLiterals(string text)
    {
        var chars = text.ToCharArray();
        var i = 0;

        while (i < chars.Length)
        {
            var c = chars[i];
            var d = i + 1 < chars.Length ? chars[i + 1] : '\0';

            if (c == '-' && d == '-')
            {
                while (i < chars.Length && chars[i] != '\n') chars[i++] = ' ';
            }
            else if (c == '/' && d == '*')
            {
                var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? chars.Length : end + 2;
                while (i < end) chars[i++] = ' ';
            }
            else if (c == '\'')
            {
                chars[i++] = ' ';

                while (i < chars.Length)
                {
                    var closing = chars[i] == '\'';
                    chars[i++] = ' ';

                    if (closing)
                    {
                        if (i < chars.Length && chars[i] == '\'') { chars[i++] = ' '; continue; }
                        break;
                    }
                }
            }
            else
            {
                i++;
            }
        }

        return new string(chars);
    }
}

// ======================= Bitácora de cambios =======================

public record AuditChange(IReadOnlyDictionary<string, string?> PrimaryKey, IReadOnlyDictionary<string, string?> From, IReadOnlyDictionary<string, string?> To);

public record EditAuditEntry(
    DateTime AtUtc, string Connection, string? Database, string Schema, string Table, string Outcome, bool DryRun, string? Error,
    IReadOnlyList<AuditChange> Changes);

public interface IEditAuditLog
{
    /// <summary>Agrega una línea a la bitácora; si no se puede escribir, lanza (y entonces no se guarda ningún cambio).</summary>
    void Write(EditAuditEntry entry);
}

/// <summary>Una línea JSON por evento, en <c>&lt;repo&gt;\.local\data-edits.log</c>: quién, cuándo, qué tabla, qué fila, antes y después.</summary>
public class FileEditAuditLog : IEditAuditLog
{
    private static readonly JsonSerializerOptions Options = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    private readonly string _path;
    private readonly object _lock = new();

    public FileEditAuditLog(string? path = null)
    {
        _path = path ?? DefaultPath;
    }

    public static string DefaultPath => Path.Combine(ProjectPaths.DataDirectory, "data-edits.log");

    public void Write(EditAuditEntry entry)
    {
        var line = JsonSerializer.Serialize(entry, Options);

        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.AppendAllText(_path, line + Environment.NewLine, new System.Text.UTF8Encoding(false));
        }
    }
}

public interface IRowEditor
{
    DatabaseKind Kind { get; }

    /// <summary>¿Se puede editar este resultado en la grilla? Nunca lanza por "no se puede": lo explica en <see cref="EditInfo.Reason"/>.</summary>
    Task<EditInfo> PrepareAsync(DbConnectionInfo connection, QueryOptions context, string sql, IReadOnlyList<QueryColumn> resultColumns, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aplica los cambios en UNA transacción: por cada fila bloquea y verifica que sus valores actuales sigan siendo los que
    /// se vieron, hace el UPDATE por clave primaria y exige que afecte exactamente 1 fila. Cualquier fallo revierte todo.
    /// </summary>
    Task<RowEditResult> ApplyAsync(DbConnectionInfo connection, RowEditRequest request, CancellationToken cancellationToken = default);
}
