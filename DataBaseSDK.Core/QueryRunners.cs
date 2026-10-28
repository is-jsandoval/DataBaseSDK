using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace DataBaseSDK.Core;

public interface ISqlQueryRunner
{
    DatabaseKind Kind { get; }

    /// <summary>Ejecuta una consulta de solo lectura. Lanza <see cref="QueryRejectedException"/> si no pasa el guard, o la excepción del driver si falla en la base.</summary>
    Task<QueryResult> ExecuteAsync(DbConnectionInfo connection, string sql, QueryOptions options, CancellationToken cancellationToken = default);
}

/// <summary>Lógica común: guard, contexto (base/esquema), transacción que siempre se revierte, lectura con tope de filas y de tiempo.</summary>
public abstract class QueryRunnerBase : ISqlQueryRunner
{

    public abstract DatabaseKind Kind { get; }

    protected abstract DbConnection CreateConnection(string connectionString);

    /// <summary>Aplica la base (SQL Server) o el esquema (Oracle) elegidos en la UI, antes de abrir la transacción.</summary>
    protected virtual Task ApplyContextAsync(DbConnection connection, QueryOptions options, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Abre la transacción en la que corre la consulta; nunca se hace commit.</summary>
    protected abstract Task<DbTransaction> BeginReadOnlyAsync(DbConnection connection, CancellationToken cancellationToken);

    protected virtual void ConfigureCommand(DbCommand command)
    {
    }

    /// <summary>Convierte un valor del driver a algo mostrable (null, texto, número, bool, DateTime).</summary>
    protected virtual object? ReadCell(DbDataReader reader, int ordinal) => Normalize(reader.GetValue(ordinal));

    public async Task<QueryResult> ExecuteAsync(DbConnectionInfo connection, string sql, QueryOptions options, CancellationToken cancellationToken = default)
    {
        var verdict = ReadOnlySqlGuard.Validate(Kind, sql);

        if (!verdict.Allowed)
            throw new QueryRejectedException(verdict.Reason!);

        var stopwatch = Stopwatch.StartNew();

        await using var dbConnection = CreateConnection(connection.ConnectionString);
        await dbConnection.OpenAsync(cancellationToken);
        await ApplyContextAsync(dbConnection, options, cancellationToken);

        await using var transaction = await BeginReadOnlyAsync(dbConnection, cancellationToken);

        await using var command = dbConnection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = verdict.Sql;
        command.CommandTimeout = options.TimeoutSeconds;
        ConfigureCommand(command);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var columns = new List<QueryColumn>();

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var clrType = SafeFieldType(reader, i);
            columns.Add(new QueryColumn(reader.GetName(i), SafeTypeName(reader, i), IsNumeric(clrType)));
        }

        var rows = new List<object?[]>();
        var truncated = false;

        while (await reader.ReadAsync(cancellationToken))
        {
            if (rows.Count >= options.MaxRows)
            {
                truncated = true;

                // No se sigue leyendo: se cancela para que la base deje de producir filas.
                try { command.Cancel(); } catch (Exception) { /* el reader ya se está cerrando */ }

                break;
            }

            var row = new object?[reader.FieldCount];

            for (var i = 0; i < row.Length; i++)
                row[i] = ReadCell(reader, i);

            rows.Add(row);
        }

        stopwatch.Stop();

        return new QueryResult
        {
            Columns = columns,
            Rows = rows,
            Truncated = truncated,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
        };
    }

    private static Type? SafeFieldType(DbDataReader reader, int ordinal)
    {
        try { return reader.GetFieldType(ordinal); }
        catch (Exception) { return null; }
    }

    private static string SafeTypeName(DbDataReader reader, int ordinal)
    {
        try { return reader.GetDataTypeName(ordinal); }
        catch (Exception) { return string.Empty; }
    }

    private static bool IsNumeric(Type? type) =>
        type != null && Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal;

    protected static object? Normalize(object? value) => CellText.Normalize(value);

    protected static string Truncate(string text) => CellText.Truncate(text);
}

public class OracleQueryRunner : QueryRunnerBase
{
    private static readonly Regex SchemaName = new(@"^[A-Za-z0-9_$#]{1,128}$", RegexOptions.Compiled);

    public override DatabaseKind Kind => DatabaseKind.Oracle;

    protected override DbConnection CreateConnection(string connectionString) => new OracleConnection(connectionString);

    // ALTER SESSION SET CURRENT_SCHEMA solo cambia cómo se resuelven los nombres sin prefijo (no da permisos
    // ni escribe nada). Va ANTES de la transacción de solo lectura, que exige ser la primera sentencia.
    protected override async Task ApplyContextAsync(DbConnection connection, QueryOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Schema))
            return;

        // El nombre se interpola (Oracle no admite un bind ahí), así que se valida estricto: nada de comillas ni espacios.
        if (!SchemaName.IsMatch(options.Schema))
            throw new QueryRejectedException($"El esquema \"{options.Schema}\" no es un nombre válido.");

        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER SESSION SET CURRENT_SCHEMA = \"{options.Schema}\"";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    protected override async Task<DbTransaction> BeginReadOnlyAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Primera sentencia de la transacción: a partir de acá cualquier DML en esta sesión falla con ORA-01456.
        await using var readOnly = connection.CreateCommand();
        readOnly.Transaction = transaction;
        readOnly.CommandText = "SET TRANSACTION READ ONLY";
        await readOnly.ExecuteNonQueryAsync(cancellationToken);

        return transaction;
    }

    protected override void ConfigureCommand(DbCommand command)
    {
        if (command is OracleCommand oracle)
            oracle.InitialLOBFetchSize = 4001; // los CLOB/BLOB chicos llegan inline
    }

    protected override object? ReadCell(DbDataReader reader, int ordinal)
    {
        object value;

        try
        {
            value = reader.GetValue(ordinal);
        }
        catch (OverflowException) when (reader is OracleDataReader oracle)
        {
            // NUMBER de Oracle con más precisión que un decimal de .NET: se muestra como texto exacto.
            return oracle.GetOracleDecimal(ordinal).ToString();
        }

        return value switch
        {
            OracleClob clob => Truncate(clob.Value ?? string.Empty),
            OracleBlob blob => $"[BLOB {blob.Length} bytes]",
            OracleBFile => "[BFILE]",
            OracleXmlType xml => Truncate(xml.Value),
            _ => Normalize(value)
        };
    }
}

public class SqlServerQueryRunner : QueryRunnerBase
{
    public override DatabaseKind Kind => DatabaseKind.SqlServer;

    protected override DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    // Como el selector "Available Databases" de SSMS: ChangeDatabase cita el nombre por su cuenta.
    protected override async Task ApplyContextAsync(DbConnection connection, QueryOptions options, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(options.Database))
            await connection.ChangeDatabaseAsync(options.Database, cancellationToken);
    }

    // SQL Server no tiene "transacción de solo lectura": el guard impide escribir y, pase lo que
    // pase, la transacción se revierte al cerrarse sin commit.
    protected override async Task<DbTransaction> BeginReadOnlyAsync(DbConnection connection, CancellationToken cancellationToken) =>
        await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
}

/// <summary>Exportación de un resultado a CSV (separador coma, UTF-8; fechas ISO y números con punto decimal).</summary>
public static class QueryResultCsv
{
    public static string Write(QueryResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine(string.Join(",", result.Columns.Select(c => Escape(c.Name))));

        foreach (var row in result.Rows)
            builder.AppendLine(string.Join(",", row.Select(Format)));

        return builder.ToString();
    }

    public static string Format(object? value) => value switch
    {
        null => string.Empty,
        DateTime date => Escape(date.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)),
        bool flag => flag ? "true" : "false",
        IFormattable number => Escape(number.ToString(null, System.Globalization.CultureInfo.InvariantCulture)),
        _ => Escape(value.ToString() ?? string.Empty)
    };

    private static string Escape(string text) =>
        text.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
}
