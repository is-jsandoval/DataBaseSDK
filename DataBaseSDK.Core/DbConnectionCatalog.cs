using System.Collections;
using System.Data.Common;

namespace DataBaseSDK.Core;

public interface IDbConnectionCatalog
{
    IReadOnlyList<DbConnectionInfo> List(DatabaseKind kind);

    DbConnectionInfo? Find(DatabaseKind kind, string? alias);
}

/// <summary>
/// Las conexiones disponibles de cada motor: las del entorno (<c>ORACLE_CONN_&lt;ALIAS&gt;</c> /
/// <c>SQLSERVER_CONN_&lt;ALIAS&gt;</c> en el .env, solo lectura) más las guardadas desde la UI
/// (<see cref="IDbConnectionStore"/>, editables). Se leen en cada consulta —no se cachean— y la
/// contraseña nunca sale en <see cref="DbConnectionInfo.Description"/>. Si un alias está en los dos
/// lados gana el del .env.
/// </summary>
public class DbConnectionCatalog : IDbConnectionCatalog
{
    public const string OraclePrefix = "ORACLE_CONN_";
    public const string SqlServerPrefix = "SQLSERVER_CONN_";

    /// <summary>Aliases (separados por coma) de conexiones del .env que admiten editar filas; <c>*</c> = todas. Ausente = ninguna.</summary>
    public const string WriteEnvVariable = "DBSDK_WRITE";

    private readonly IDbConnectionStore? _store;

    public DbConnectionCatalog(IDbConnectionStore? store = null)
    {
        _store = store;
    }

    public IReadOnlyList<DbConnectionInfo> List(DatabaseKind kind)
    {
        var prefix = kind == DatabaseKind.Oracle ? OraclePrefix : SqlServerPrefix;

        var fromEnv = Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .Where(e => e.Key is string key && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && key.Length > prefix.Length)
            .Where(e => !string.IsNullOrWhiteSpace(e.Value as string))
            .Select(e => Create(kind, ((string)e.Key)[prefix.Length..], (string)e.Value!))
            .Select(c => c with { AllowWrite = WriteAllowedByEnv(c.Alias) })
            .ToList();

        var saved = (_store?.List(kind) ?? [])
            .Where(s => fromEnv.All(e => !e.Alias.Equals(s.Alias, StringComparison.OrdinalIgnoreCase)))
            .Select(s => new DbConnectionInfo(s.Alias, kind, ConnectionStringFactory.Build(s), ConnectionStringFactory.Describe(s))
            {
                Origin = ConnectionOrigin.Saved,
                AllowWrite = s.AllowWrite
            });

        return fromEnv.Concat(saved).OrderBy(c => c.Alias, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public DbConnectionInfo? Find(DatabaseKind kind, string? alias) =>
        string.IsNullOrWhiteSpace(alias)
            ? null
            : List(kind).FirstOrDefault(c => c.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));

    private static bool WriteAllowedByEnv(string alias)
    {
        var aliases = (Environment.GetEnvironmentVariable(WriteEnvVariable) ?? string.Empty)
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return aliases.Any(a => a == "*" || a.Equals(alias, StringComparison.OrdinalIgnoreCase));
    }

    public static DbConnectionInfo Create(DatabaseKind kind, string alias, string connectionString) =>
        new(alias, kind, connectionString, Describe(kind, connectionString));

    // "host:1521/SERVICIO · usuario X" / "servidor · base · Windows auth" — sin contraseña.
    private static string Describe(DatabaseKind kind, string connectionString)
    {
        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

            string? Get(params string[] keys) =>
                keys.Select(k => builder.TryGetValue(k, out var v) ? v?.ToString() : null)
                    .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            if (kind == DatabaseKind.Oracle)
            {
                var source = Get("Data Source", "DataSource");
                var user = Get("User Id", "UserId", "User ID", "Uid");

                return string.Join(" · ", new[] { source, user == null ? null : $"usuario {user}" }.Where(p => p != null));
            }

            var server = Get("Server", "Data Source", "Address", "Addr", "Network Address");
            var database = Get("Database", "Initial Catalog");
            var sqlUser = Get("User Id", "UID", "User ID");
            var integrated = Get("Integrated Security", "Trusted_Connection");
            var windowsAuth = integrated != null && (integrated.Equals("true", StringComparison.OrdinalIgnoreCase)
                || integrated.Equals("sspi", StringComparison.OrdinalIgnoreCase)
                || integrated.Equals("yes", StringComparison.OrdinalIgnoreCase));

            return string.Join(" · ", new[]
            {
                server,
                database,
                windowsAuth ? "autenticación de Windows" : sqlUser == null ? null : $"usuario {sqlUser}"
            }.Where(p => p != null));
        }
        catch (ArgumentException)
        {
            return "(connection string inválida)";
        }
    }
}
