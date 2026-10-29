using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;

namespace DataBaseSDK.Core;

/// <summary>
/// Una conexión armada desde el formulario de la UI (como el diálogo "Nueva conexión" de SSMS o
/// SQL Developer): se guarda por campos, no como connection string, y de ahí se construye la
/// connection string con el builder de cada driver (que escapa bien claves con caracteres raros).
/// </summary>
public class StoredConnection
{
    /// <summary>Nombre de la conexión en la lista.</summary>
    public string Alias { get; set; } = string.Empty;

    public DatabaseKind Kind { get; set; }

    /// <summary>Servidor (SQL Server: <c>host</c>, <c>host\INSTANCIA</c>) o host del listener de Oracle.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Puerto; vacío = el de siempre (1433 / 1521).</summary>
    public int? Port { get; set; }

    /// <summary>SQL Server: base de datos inicial (opcional).</summary>
    public string? Database { get; set; }

    /// <summary>Oracle: <c>ServiceName</c> (host:puerto/servicio), <c>Sid</c> o <c>Tns</c> (alias de tnsnames.ora).</summary>
    public string OracleMode { get; set; } = OracleModes.ServiceName;

    /// <summary>Oracle: el nombre del servicio, el SID o el alias TNS, según <see cref="OracleMode"/>.</summary>
    public string? OracleTarget { get; set; }

    /// <summary>SQL Server: autenticación integrada de Windows (sin usuario ni clave).</summary>
    public bool WindowsAuth { get; set; }

    public string? User { get; set; }

    /// <summary>En claro solo en memoria; en disco se guarda cifrada (ver <see cref="DbConnectionStore"/>).</summary>
    public string? Password { get; set; }

    /// <summary>SQL Server: aceptar el certificado del servidor aunque no sea de una CA conocida (típico de servidores internos).</summary>
    public bool TrustServerCertificate { get; set; } = true;

    /// <summary>Permite editar filas desde la grilla de resultados (apagado por defecto; ver <see cref="DbConnectionInfo.AllowWrite"/>).</summary>
    public bool AllowWrite { get; set; }
}

public static class OracleModes
{
    public const string ServiceName = "ServiceName";
    public const string Sid = "Sid";
    public const string Tns = "Tns";
}

public static class ConnectionStringFactory
{
    private const int ConnectTimeoutSeconds = 15;

    public static string Build(StoredConnection connection) =>
        connection.Kind == DatabaseKind.SqlServer ? BuildSqlServer(connection) : BuildOracle(connection);

    private static string BuildSqlServer(StoredConnection c)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = c.Port is > 0 ? $"{c.Host.Trim()},{c.Port}" : c.Host.Trim(),
            ApplicationName = "DataBaseSDK.Web",
            ConnectTimeout = ConnectTimeoutSeconds,
            TrustServerCertificate = c.TrustServerCertificate
        };

        if (!string.IsNullOrWhiteSpace(c.Database))
            builder.InitialCatalog = c.Database.Trim();

        if (c.WindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = c.User?.Trim() ?? string.Empty;
            builder.Password = c.Password ?? string.Empty;
        }

        return builder.ConnectionString;
    }

    private static string BuildOracle(StoredConnection c)
    {
        var host = c.Host.Trim();
        var port = c.Port is > 0 ? c.Port.Value : 1521;
        var target = c.OracleTarget?.Trim() ?? string.Empty;

        var dataSource = c.OracleMode switch
        {
            OracleModes.Tns => target,
            OracleModes.Sid => $"(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port}))(CONNECT_DATA=(SID={target})))",
            _ => $"{host}:{port}/{target}"
        };

        return new OracleConnectionStringBuilder
        {
            DataSource = dataSource,
            UserID = c.User?.Trim() ?? string.Empty,
            Password = c.Password ?? string.Empty,
            ConnectionTimeout = ConnectTimeoutSeconds
        }.ConnectionString;
    }

    /// <summary>Resumen sin secretos para mostrar en la UI.</summary>
    public static string Describe(StoredConnection c)
    {
        if (c.Kind == DatabaseKind.SqlServer)
        {
            var server = c.Port is > 0 ? $"{c.Host},{c.Port}" : c.Host;

            return string.Join(" · ", new[]
            {
                server,
                string.IsNullOrWhiteSpace(c.Database) ? null : c.Database,
                c.WindowsAuth ? "autenticación de Windows" : string.IsNullOrWhiteSpace(c.User) ? null : $"usuario {c.User}"
            }.Where(p => p != null));
        }

        var where = c.OracleMode == OracleModes.Tns
            ? c.OracleTarget
            : $"{c.Host}:{(c.Port is > 0 ? c.Port : 1521)}/{c.OracleTarget}";

        return string.Join(" · ", new[] { where, string.IsNullOrWhiteSpace(c.User) ? null : $"usuario {c.User}" }.Where(p => p != null));
    }
}

public static partial class ConnectionValidation
{
    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9_.\-]{0,39}$")]
    private static partial Regex AliasRegex();

    /// <summary>Devuelve el motivo si la conexión no es válida (null si está bien).</summary>
    public static string? Validate(StoredConnection c)
    {
        if (string.IsNullOrWhiteSpace(c.Alias) || !AliasRegex().IsMatch(c.Alias))
            return "El nombre debe tener de 1 a 40 caracteres: letras, números, punto, guion o guion bajo (sin espacios).";

        var tns = c.Kind == DatabaseKind.Oracle && c.OracleMode == OracleModes.Tns;

        if (!tns && string.IsNullOrWhiteSpace(c.Host))
            return "Falta el servidor.";

        if (c.Port is < 1 or > 65535)
            return "El puerto debe estar entre 1 y 65535.";

        if (c.Kind == DatabaseKind.Oracle)
        {
            if (string.IsNullOrWhiteSpace(c.OracleTarget))
                return c.OracleMode switch
                {
                    OracleModes.Sid => "Falta el SID.",
                    OracleModes.Tns => "Falta el alias de tnsnames.ora.",
                    _ => "Falta el nombre del servicio."
                };

            if (string.IsNullOrWhiteSpace(c.User))
                return "Falta el usuario.";
        }
        else if (!c.WindowsAuth && string.IsNullOrWhiteSpace(c.User))
        {
            return "Falta el usuario (o marca la autenticación de Windows).";
        }

        // Estos campos terminan dentro del descriptor TNS: sin paréntesis ni espacios no se puede inyectar otro bloque.
        if (c.Kind == DatabaseKind.Oracle && c.OracleMode != OracleModes.Tns &&
            (Regex.IsMatch(c.Host, @"[\s()=]") || Regex.IsMatch(c.OracleTarget ?? string.Empty, @"[\s()=]")))
        {
            return "El servidor y el servicio/SID no pueden tener espacios, paréntesis ni '='.";
        }

        return null;
    }
}

/// <summary>Cifra y descifra secretos para guardarlos en disco.</summary>
public interface ISecretProtector
{
    string Protect(string plainText);

    string Unprotect(string protectedText);
}

/// <summary>DPAPI de Windows con el alcance del usuario actual: solo tu usuario de Windows, en esta máquina, puede descifrar.</summary>
public class DpapiSecretProtector : ISecretProtector
{
    // OJO: este texto es parte de la clave de cifrado. Se conserva el de AzureDevOpsSdk.Database (de donde nació este
    // proyecto) a propósito: así las conexiones importadas de allí se siguen pudiendo descifrar. Cambiarlo deja
    // ilegibles las contraseñas ya guardadas (habría que volver a escribirlas).
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AzureDevOpsSdk.Database.v1");

    public string Protect(string plainText) =>
        Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), Entropy, DataProtectionScope.CurrentUser));

    public string Unprotect(string protectedText) =>
        Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(protectedText), Entropy, DataProtectionScope.CurrentUser));
}

public interface IDbConnectionStore
{
    IReadOnlyList<StoredConnection> List(DatabaseKind kind);

    StoredConnection? Get(DatabaseKind kind, string alias);

    /// <summary>Crea o reemplaza (por Kind + Alias). Con <see cref="StoredConnection.Password"/> null se conserva la clave guardada.</summary>
    void Save(StoredConnection connection);

    bool Delete(DatabaseKind kind, string alias);
}

/// <summary>
/// Conexiones guardadas desde la UI, dentro del proyecto: <c>&lt;repo&gt;\.local\db-connections.json</c>
/// (en el .gitignore; <c>DBSDK_CONNECTIONS_FILE</c> cambia la ruta). La contraseña se guarda
/// cifrada con <see cref="ISecretProtector"/>; el resto de los campos va en claro.
/// Además se mantiene una copia <b>sin contraseñas</b> en <c>&lt;repo&gt;\db-connections.shared.json</c>,
/// que sí se versiona: al clonar el repo en otra máquina aparecen los servidores y usuarios y solo
/// hay que volver a escribir la clave (la cifrada con DPAPI no serviría allí de todos modos).
/// </summary>
public class DbConnectionStore : IDbConnectionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;
    private readonly string? _sharedPath;
    private readonly ISecretProtector _protector;
    private readonly object _lock = new();

    /// <summary>Variable de entorno que fija desde dónde importar conexiones la primera vez; el valor <c>none</c> lo desactiva.</summary>
    public const string ImportEnvVariable = "DBSDK_IMPORT_CONNECTIONS_FROM";

    /// <summary>Variable de entorno con la ruta del archivo versionado sin contraseñas; el valor <c>none</c> lo desactiva.</summary>
    public const string SharedEnvVariable = "DBSDK_SHARED_CONNECTIONS_FILE";

    /// <param name="path">Archivo de conexiones (null = <c>DBSDK_CONNECTIONS_FILE</c> o <see cref="DefaultPath"/>).</param>
    /// <param name="importFrom">Archivo de otro proyecto del que traer las conexiones la PRIMERA vez (null = el de por defecto, solo si <paramref name="path"/> tampoco se indicó).</param>
    /// <param name="sharedPath">Archivo versionado sin contraseñas (null = el de por defecto, solo si <paramref name="path"/> tampoco se indicó: así las pruebas nunca tocan el del repo).</param>
    public DbConnectionStore(ISecretProtector protector, string? path = null, string? importFrom = null, string? sharedPath = null)
    {
        _protector = protector;

        var fromEnv = Environment.GetEnvironmentVariable("DBSDK_CONNECTIONS_FILE");
        _path = path ?? (string.IsNullOrWhiteSpace(fromEnv) ? DefaultPath : fromEnv);
        _sharedPath = sharedPath ?? (path == null ? DefaultSharedPath() : null);

        var source = importFrom ?? (path == null && string.IsNullOrWhiteSpace(fromEnv) ? DefaultImportPath() : null);

        if (source != null)
            ImportOnce(source);

        PublishMissingToShared();
    }

    /// <summary><c>&lt;repo&gt;\.local\db-connections.json</c> — las conexiones viven con el proyecto.</summary>
    public static string DefaultPath => Path.Combine(ProjectPaths.DataDirectory, "db-connections.json");

    private static string? DefaultSharedPath()
    {
        var fromEnv = Environment.GetEnvironmentVariable(SharedEnvVariable);

        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.Equals("none", StringComparison.OrdinalIgnoreCase) ? null : fromEnv;

        return Path.Combine(ProjectPaths.Root, "db-connections.shared.json");
    }

    // Este proyecto nació de AzureDevOpsSdk, que guardaba sus conexiones en <hermano>\AzureDevOpsSdk\.local\: si existe y
    // todavía no hay archivo propio, se traen una única vez. DBSDK_IMPORT_CONNECTIONS_FROM=<ruta> elige otro origen; =none lo apaga.
    private static string? DefaultImportPath()
    {
        var fromEnv = Environment.GetEnvironmentVariable(ImportEnvVariable);

        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.Equals("none", StringComparison.OrdinalIgnoreCase) ? null : fromEnv;

        var parent = Directory.GetParent(ProjectPaths.Root);

        return parent == null ? null : Path.Combine(parent.FullName, "AzureDevOpsSdk", ".local", "db-connections.json");
    }

    // La clave cifrada con DPAPI se descifra igual: es el mismo usuario de Windows en la misma máquina (y el mismo texto de entropía).
    // Importación única: si ya existe el archivo propio (aunque esté vacío) no se vuelve a importar nada.
    private void ImportOnce(string source)
    {
        try
        {
            if (File.Exists(_path) || !File.Exists(source))
                return;

            var imported = JsonSerializer.Deserialize<FileModel>(File.ReadAllText(source, Encoding.UTF8), JsonOptions)?.Connections;

            if (imported == null || imported.Count == 0)
                return;

            // La edición de datos NO se importa: se vuelve a habilitar a propósito, conexión por conexión.
            foreach (var row in imported)
                row.AllowWrite = false;

            WriteAll(imported);
        }
        catch (JsonException)
        {
            // Archivo de origen ilegible: se arranca sin conexiones, el original no se toca.
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public IReadOnlyList<StoredConnection> List(DatabaseKind kind)
    {
        lock (_lock)
        {
            var local = ReadAll();

            // Las del archivo versionado que no están en el local (repo recién clonado) se muestran sin clave.
            var fromShared = (ReadShared() ?? [])
                .Where(s => !local.Any(r => SameKey(r, s.Kind, s.Alias)))
                .Select(s => s.ToRow());

            return local.Concat(fromShared).Where(r => r.Kind == kind)
                .Select(ToConnection)
                .OrderBy(c => c.Alias, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public StoredConnection? Get(DatabaseKind kind, string alias) =>
        List(kind).FirstOrDefault(c => c.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));

    public void Save(StoredConnection connection)
    {
        lock (_lock)
        {
            var all = ReadAll();
            var index = all.FindIndex(r => r.Kind == connection.Kind && r.Alias.Equals(connection.Alias, StringComparison.OrdinalIgnoreCase));
            var previous = index >= 0 ? all[index] : null;

            var row = new Row
            {
                Alias = connection.Alias,
                Kind = connection.Kind,
                Host = connection.Host,
                Port = connection.Port,
                Database = connection.Database,
                OracleMode = connection.OracleMode,
                OracleTarget = connection.OracleTarget,
                WindowsAuth = connection.WindowsAuth,
                User = connection.User,
                TrustServerCertificate = connection.TrustServerCertificate,
                AllowWrite = connection.AllowWrite,
                PasswordProtected = connection.Password == null
                    ? previous?.PasswordProtected
                    : connection.Password.Length == 0 ? null : _protector.Protect(connection.Password)
            };

            if (index >= 0)
                all[index] = row;
            else
                all.Add(row);

            WriteAll(all);
            UpdateShared(shared =>
            {
                shared.RemoveAll(s => SameKey(s, row.Kind, row.Alias));
                shared.Add(SharedRow.From(row));
            });
        }
    }

    public bool Delete(DatabaseKind kind, string alias)
    {
        lock (_lock)
        {
            var all = ReadAll();
            var removed = all.RemoveAll(r => SameKey(r, kind, alias));

            if (removed > 0)
                WriteAll(all);

            // También se quita del versionado: si no, al volver a abrir reaparecería (sin clave).
            var removedShared = 0;
            UpdateShared(shared => removedShared = shared.RemoveAll(s => SameKey(s, kind, alias)));

            return removed + removedShared > 0;
        }
    }

    private static bool SameKey(Row r, DatabaseKind kind, string alias) =>
        r.Kind == kind && r.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase);

    private static bool SameKey(SharedRow s, DatabaseKind kind, string alias) =>
        s.Kind == kind && s.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase);

    // Al arrancar, las conexiones locales que todavía no estén en el archivo versionado se agregan (las de antes de que
    // existiera, o las importadas). No pisa lo que ya hay: lo del repo puede venir de otra máquina.
    private void PublishMissingToShared()
    {
        lock (_lock)
        {
            var local = ReadAll();

            if (local.Count == 0)
                return;

            UpdateShared(shared =>
            {
                foreach (var row in local.Where(r => !shared.Any(s => SameKey(s, r.Kind, r.Alias))))
                    shared.Add(SharedRow.From(row));
            });
        }
    }

    /// <summary>null = no hay archivo versionado configurado o está dañado (en ese caso no se toca: lo arregla git).</summary>
    private List<SharedRow>? ReadShared()
    {
        if (_sharedPath == null || !File.Exists(_sharedPath))
            return _sharedPath == null ? null : [];

        try
        {
            return JsonSerializer.Deserialize<SharedFileModel>(File.ReadAllText(_sharedPath, Encoding.UTF8), JsonOptions)?.Connections ?? [];
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void UpdateShared(Action<List<SharedRow>> change)
    {
        var shared = ReadShared();

        if (shared == null)
            return;

        var before = JsonSerializer.Serialize(shared, JsonOptions);
        change(shared);

        // Orden estable y sin reescribir si nada cambió: el diff de git muestra solo lo que de verdad cambió.
        shared = shared.OrderBy(s => s.Kind).ThenBy(s => s.Alias, StringComparer.OrdinalIgnoreCase).ToList();

        if (File.Exists(_sharedPath) && JsonSerializer.Serialize(shared, JsonOptions) == before)
            return;

        try
        {
            var temp = _sharedPath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(new SharedFileModel { Connections = shared }, JsonOptions) + Environment.NewLine, new UTF8Encoding(false));
            File.Move(temp, _sharedPath!, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // La copia versionada es una comodidad: si no se puede escribir, la conexión local ya quedó guardada.
        }
    }

    private StoredConnection ToConnection(Row row)
    {
        string? password = null;

        try
        {
            password = row.PasswordProtected == null ? null : _protector.Unprotect(row.PasswordProtected);
        }
        catch (CryptographicException)
        {
            // Cifrada con otro usuario/máquina: la conexión sigue en la lista, pero hay que volver a escribir la clave.
        }

        return new StoredConnection
        {
            Alias = row.Alias,
            Kind = row.Kind,
            Host = row.Host,
            Port = row.Port,
            Database = row.Database,
            OracleMode = row.OracleMode,
            OracleTarget = row.OracleTarget,
            WindowsAuth = row.WindowsAuth,
            User = row.User,
            Password = password,
            TrustServerCertificate = row.TrustServerCertificate,
            AllowWrite = row.AllowWrite
        };
    }

    private List<Row> ReadAll()
    {
        if (!File.Exists(_path))
            return [];

        try
        {
            return JsonSerializer.Deserialize<FileModel>(File.ReadAllText(_path, Encoding.UTF8), JsonOptions)?.Connections ?? [];
        }
        catch (JsonException)
        {
            // Archivo dañado: se conserva una copia para no perder las claves cifradas y se arranca limpio.
            File.Copy(_path, _path + ".corrupto", overwrite: true);

            return [];
        }
    }

    private void WriteAll(List<Row> rows)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        // Escritura atómica: si el proceso muere a mitad, queda el archivo anterior intacto.
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(new FileModel { Connections = rows }, JsonOptions), new UTF8Encoding(false));
        File.Move(temp, _path, overwrite: true);
    }

    private class FileModel
    {
        [JsonPropertyName("connections")]
        public List<Row> Connections { get; set; } = [];
    }

    private class Row
    {
        [JsonPropertyName("alias")] public string Alias { get; set; } = string.Empty;
        [JsonPropertyName("kind")] [JsonConverter(typeof(JsonStringEnumConverter))] public DatabaseKind Kind { get; set; }
        [JsonPropertyName("host")] public string Host { get; set; } = string.Empty;
        [JsonPropertyName("port")] public int? Port { get; set; }
        [JsonPropertyName("database")] public string? Database { get; set; }
        [JsonPropertyName("oracleMode")] public string OracleMode { get; set; } = OracleModes.ServiceName;
        [JsonPropertyName("oracleTarget")] public string? OracleTarget { get; set; }
        [JsonPropertyName("windowsAuth")] public bool WindowsAuth { get; set; }
        [JsonPropertyName("user")] public string? User { get; set; }
        [JsonPropertyName("trustServerCertificate")] public bool TrustServerCertificate { get; set; } = true;
        [JsonPropertyName("allowWrite")] public bool AllowWrite { get; set; }
        [JsonPropertyName("passwordProtected")] public string? PasswordProtected { get; set; }
    }

    private class SharedFileModel
    {
        [JsonPropertyName("connections")]
        public List<SharedRow> Connections { get; set; } = [];
    }

    // Sin campo de contraseña ni de edición de datos: así es imposible que lleguen al archivo versionado. La edición se
    // habilita a propósito en cada máquina.
    private class SharedRow
    {
        [JsonPropertyName("alias")] public string Alias { get; set; } = string.Empty;
        [JsonPropertyName("kind")] [JsonConverter(typeof(JsonStringEnumConverter))] public DatabaseKind Kind { get; set; }
        [JsonPropertyName("host")] public string Host { get; set; } = string.Empty;
        [JsonPropertyName("port")] public int? Port { get; set; }
        [JsonPropertyName("database")] public string? Database { get; set; }
        [JsonPropertyName("oracleMode")] public string OracleMode { get; set; } = OracleModes.ServiceName;
        [JsonPropertyName("oracleTarget")] public string? OracleTarget { get; set; }
        [JsonPropertyName("windowsAuth")] public bool WindowsAuth { get; set; }
        [JsonPropertyName("user")] public string? User { get; set; }
        [JsonPropertyName("trustServerCertificate")] public bool TrustServerCertificate { get; set; } = true;

        public static SharedRow From(Row r) => new()
        {
            Alias = r.Alias, Kind = r.Kind, Host = r.Host, Port = r.Port, Database = r.Database, OracleMode = r.OracleMode,
            OracleTarget = r.OracleTarget, WindowsAuth = r.WindowsAuth, User = r.User, TrustServerCertificate = r.TrustServerCertificate
        };

        public Row ToRow() => new()
        {
            Alias = Alias, Kind = Kind, Host = Host, Port = Port, Database = Database, OracleMode = OracleMode,
            OracleTarget = OracleTarget, WindowsAuth = WindowsAuth, User = User, TrustServerCertificate = TrustServerCertificate
        };
    }
}
