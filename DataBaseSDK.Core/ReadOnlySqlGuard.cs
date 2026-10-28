using System.Text;
using System.Text.RegularExpressions;

namespace DataBaseSDK.Core;

public record GuardResult(bool Allowed, string? Reason, string Sql);

/// <summary>
/// Chequeo de "solo lectura" antes de mandar una consulta a la base. Acepta una sola sentencia
/// que empiece en SELECT o WITH y rechaza cualquier palabra de escritura/DDL/ejecución fuera de
/// comentarios, literales e identificadores entre comillas.
///
/// <b>Es una red de seguridad contra errores, no una frontera de seguridad</b>: una función de
/// base de datos con efectos secundarios invocada desde un SELECT no se puede detectar leyendo el
/// texto. La protección real es usar un usuario de base de datos de solo lectura; además cada
/// consulta corre en una transacción que siempre se revierte (y en Oracle, <c>SET TRANSACTION READ ONLY</c>).
/// </summary>
public static class ReadOnlySqlGuard
{
    private static readonly HashSet<string> ForbiddenWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "INSERT", "UPDATE", "DELETE", "MERGE", "DROP", "ALTER", "CREATE", "TRUNCATE", "GRANT", "REVOKE",
        "EXEC", "EXECUTE", "CALL", "BEGIN", "DECLARE", "COMMIT", "ROLLBACK", "SAVEPOINT", "LOCK", "RENAME",
        "INTO", "SHUTDOWN", "KILL", "BACKUP", "RESTORE", "DBCC", "WAITFOR", "OPENROWSET", "OPENQUERY",
        "OPENDATASOURCE", "BULK", "SP_EXECUTESQL",
        // Oracle: paquetes/tipos que escriben, duermen, abren sockets o leen/escriben archivos desde un SELECT.
        "DBMS_LOCK", "DBMS_SCHEDULER", "DBMS_JOB", "DBMS_SQL", "DBMS_PIPE", "DBMS_ALERT", "DBMS_FILE_TRANSFER",
        "UTL_HTTP", "UTL_TCP", "UTL_SMTP", "UTL_FILE", "UTL_INADDR", "HTTPURITYPE"
    };

    private static readonly Regex WordRegex = new(@"[A-Za-z_][A-Za-z0-9_$#]*", RegexOptions.Compiled);

    public static GuardResult Validate(DatabaseKind kind, string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return Reject("Escribe una consulta.");

        var scan = Scan(kind, sql);

        if (scan.Error != null)
            return Reject(scan.Error);

        if (scan.SemicolonCount > 1)
            return Reject("Hay más de una sentencia (varios ';'). Ejecuta una sola consulta a la vez.");

        var cleaned = scan.Cleaned.TrimEnd();

        if (scan.SemicolonCount == 1 && !cleaned.EndsWith(';'))
            return Reject("Hay texto después del ';'. Ejecuta una sola consulta a la vez.");

        var words = WordRegex.Matches(cleaned).Select(m => m.Value).ToList();

        if (words.Count == 0 || !(words[0].Equals("SELECT", StringComparison.OrdinalIgnoreCase) || words[0].Equals("WITH", StringComparison.OrdinalIgnoreCase)))
            return Reject("Solo se permiten consultas que empiecen con SELECT o WITH (esta consola es de solo lectura).");

        foreach (var word in words)
        {
            if (ForbiddenWords.Contains(word) || word.StartsWith("XP_", StringComparison.OrdinalIgnoreCase))
            {
                return Reject($"La palabra \"{word.ToUpperInvariant()}\" no está permitida (consola de solo lectura). " +
                              "Si es el nombre de una columna, enciérralo entre comillas (\"nombre\" o [nombre]).");
            }
        }

        // El ';' final no hace falta (Oracle incluso lo rechaza con ORA-00911): se quita del texto a ejecutar.
        var executable = scan.SemicolonIndex >= 0 ? sql.Remove(scan.SemicolonIndex, 1) : sql;

        return new GuardResult(true, null, executable.Trim());
    }

    private static GuardResult Reject(string reason) => new(false, reason, string.Empty);

    private record ScanResult(string Cleaned, int SemicolonCount, int SemicolonIndex, string? Error);

    // Recorre el texto y devuelve una copia sin comentarios, con los literales vaciados y los
    // identificadores entre comillas reemplazados por una letra — así las palabras reservadas
    // dentro de un texto ('delete me') o de un nombre ("UPDATE") no disparan el chequeo.
    private static ScanResult Scan(DatabaseKind kind, string sql)
    {
        var output = new StringBuilder(sql.Length);
        var semicolons = 0;
        var semicolonIndex = -1;
        var i = 0;

        while (i < sql.Length)
        {
            var c = sql[i];
            var next = i + 1 < sql.Length ? sql[i + 1] : '\0';

            if (c == '-' && next == '-')
            {
                while (i < sql.Length && sql[i] != '\n')
                    i++;

                output.Append(' ');
            }
            else if (c == '/' && next == '*')
            {
                var end = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);

                if (end < 0)
                    return new ScanResult(string.Empty, 0, -1, "Hay un comentario /* sin cerrar.");

                i = end + 2;
                output.Append(' ');
            }
            else if (kind == DatabaseKind.Oracle && (c == 'q' || c == 'Q') && next == '\'' && i + 2 < sql.Length && !IsIdentifierChar(i > 0 ? sql[i - 1] : ' '))
            {
                // Literal con delimitador propio de Oracle: q'[ ... ]', q'{ ... }', q'! ... !'
                var delimiter = sql[i + 2];
                var closing = delimiter switch { '[' => ']', '{' => '}', '(' => ')', '<' => '>', _ => delimiter };
                var end = sql.IndexOf($"{closing}'", i + 3, StringComparison.Ordinal);

                if (end < 0)
                    return new ScanResult(string.Empty, 0, -1, "Hay un literal q'...' sin cerrar.");

                i = end + 2;
                output.Append("''");
            }
            else if (c == '\'')
            {
                i++;

                while (true)
                {
                    if (i >= sql.Length)
                        return new ScanResult(string.Empty, 0, -1, "Hay un texto entre comillas simples (') sin cerrar.");

                    if (sql[i] == '\'')
                    {
                        if (i + 1 < sql.Length && sql[i + 1] == '\'')
                        {
                            i += 2;
                            continue;
                        }

                        i++;
                        break;
                    }

                    i++;
                }

                output.Append("''");
            }
            else if (c == '"' || (kind == DatabaseKind.SqlServer && c == '['))
            {
                var close = c == '"' ? '"' : ']';
                i++;

                while (true)
                {
                    if (i >= sql.Length)
                        return new ScanResult(string.Empty, 0, -1, $"Hay un identificador entre comillas ({c}) sin cerrar.");

                    if (sql[i] == close)
                    {
                        if (i + 1 < sql.Length && sql[i + 1] == close)
                        {
                            i += 2;
                            continue;
                        }

                        i++;
                        break;
                    }

                    i++;
                }

                output.Append(" x ");
            }
            else
            {
                if (c == ';')
                {
                    semicolons++;
                    semicolonIndex = i;
                }

                output.Append(c);
                i++;
            }
        }

        return new ScanResult(output.ToString(), semicolons, semicolonIndex, null);
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '$' or '#';
}
