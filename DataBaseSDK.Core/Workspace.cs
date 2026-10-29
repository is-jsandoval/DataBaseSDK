using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataBaseSDK.Core;

/// <summary>
/// Lo que estaba abierto en la consola de un motor: la última conexión elegida (con su base o esquema; la
/// usan las pestañas nuevas) y los archivos .sql de las pestañas, cada uno con SU conexión, como las
/// ventanas de consulta de SSMS. <see cref="Files"/> y <see cref="ActiveFile"/> van como rutas completas
/// de esta máquina. <see cref="Stamp"/> identifica el contenido: el navegador lo recuerda y, si al
/// volver es otro, el archivo cambió por fuera (un <c>git pull</c> desde otra computadora).
/// </summary>
public record WorkspaceState(
    string? Alias,
    string? Database,
    string? Schema,
    IReadOnlyList<WorkspaceFile> Files,
    string? ActiveFile,
    string Stamp);

/// <summary>Una pestaña de archivo y la conexión (base / esquema) con la que se consulta.</summary>
public record WorkspaceFile(string Path, string? Alias = null, string? Database = null, string? Schema = null);

public interface IWorkspaceStore
{
    WorkspaceState Get(DatabaseKind kind);

    /// <summary>Reemplaza la lista de archivos abiertos. Lo que no sea una ruta completa a un .sql se descarta.</summary>
    WorkspaceState SaveTabs(DatabaseKind kind, IEnumerable<WorkspaceFile?>? files, string? activeFile);

    /// <summary>Recuerda la conexión elegida y su base (SQL Server) o esquema (Oracle).</summary>
    WorkspaceState SaveContext(DatabaseKind kind, string? alias, string? database, string? schema);
}

/// <summary>
/// La sesión de trabajo en un archivo <b>versionado</b> (<c>&lt;repo&gt;\db-workspace.json</c>): así, al subirlo a
/// git y abrir el proyecto en otra computadora, se ven las mismas pestañas y la misma conexión. Cada motor tiene
/// su sección, por eso cambiar de Oracle a SQL Server y volver no pierde lo de la otra consola.
/// Solo guarda rutas y nombres, nunca el texto de las consultas ni contraseñas (el repo puede ser público): lo
/// que no está guardado en disco sigue en la caché del navegador de cada máquina.
/// Las rutas dentro del repo se guardan relativas (con <c>/</c>) para que sirvan en otra carpeta u otra máquina.
/// </summary>
public class WorkspaceStore : IWorkspaceStore
{
    public const string EnvVariable = "DBSDK_WORKSPACE_FILE";

    /// <summary>Tope de pestañas recordadas: el archivo lo lee una persona en el diff de git.</summary>
    public const int MaxFiles = 50;

    private const int MaxNameLength = 128;
    private const int MaxPathLength = 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string? _path;
    private readonly string _root;
    private readonly object _lock = new();

    /// <param name="path">Archivo (null = <c>DBSDK_WORKSPACE_FILE</c> o <c>&lt;repo&gt;\db-workspace.json</c>; <c>none</c> lo apaga).</param>
    /// <param name="root">Carpeta contra la que se guardan las rutas relativas (null = la raíz del repo).</param>
    public WorkspaceStore(string? path = null, string? root = null)
    {
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root ?? ProjectPaths.Root));

        if (path != null)
        {
            _path = path;
            return;
        }

        var fromEnv = Environment.GetEnvironmentVariable(EnvVariable);

        _path = string.IsNullOrWhiteSpace(fromEnv)
            ? Path.Combine(_root, "db-workspace.json")
            : fromEnv.Equals("none", StringComparison.OrdinalIgnoreCase) ? null : fromEnv;
    }

    public WorkspaceState Get(DatabaseKind kind)
    {
        lock (_lock)
        {
            return ToState(Section(ReadAll(), kind));
        }
    }

    public WorkspaceState SaveTabs(DatabaseKind kind, IEnumerable<WorkspaceFile?>? files, string? activeFile)
    {
        var clean = new List<FileModel>();

        foreach (var file in files ?? [])
        {
            var stored = ToStored(file?.Path);

            if (stored == null || clean.Count >= MaxFiles || clean.Any(f => f.Path.Equals(stored, StringComparison.OrdinalIgnoreCase)))
                continue;

            clean.Add(new FileModel
            {
                Path = stored,
                Alias = CleanName(file!.Alias),
                Database = CleanName(file.Database),
                Schema = CleanName(file.Schema)
            });
        }

        var active = ToStored(activeFile);

        return Update(kind, section =>
        {
            section.Files = clean;
            section.ActiveFile = active != null && clean.Any(f => f.Path.Equals(active, StringComparison.OrdinalIgnoreCase)) ? active : null;
        });
    }

    public WorkspaceState SaveContext(DatabaseKind kind, string? alias, string? database, string? schema) =>
        Update(kind, section =>
        {
            section.Alias = CleanName(alias);
            section.Database = CleanName(database);
            section.Schema = CleanName(schema);
        });

    private WorkspaceState Update(DatabaseKind kind, Action<SectionModel> change)
    {
        lock (_lock)
        {
            var all = ReadAll();

            // Archivo dañado (p. ej. un conflicto de merge a medio resolver): no se pisa, que lo arregle git.
            if (all == null)
                return ToState(new SectionModel());

            var section = Section(all, kind);
            var before = Serialize(section);
            change(section);

            if (Serialize(section) != before || !File.Exists(_path))
            {
                all[kind.ToString()] = section;
                Write(all);
            }

            return ToState(section);
        }
    }

    private static SectionModel Section(Dictionary<string, SectionModel>? all, DatabaseKind kind) =>
        all != null && all.TryGetValue(kind.ToString(), out var section) && section != null ? section : new SectionModel();

    private WorkspaceState ToState(SectionModel section)
    {
        var files = (section.Files ?? [])
            .Where(f => f != null)
            .Select(f => ToFull(f.Path) is { } full ? new WorkspaceFile(full, f.Alias, f.Database, f.Schema) : null)
            .OfType<WorkspaceFile>()
            .ToList();

        return new WorkspaceState(section.Alias, section.Database, section.Schema, files, ToFull(section.ActiveFile), Stamp(section));
    }

    // El sello sale del contenido guardado (rutas relativas): es el mismo en todas las máquinas para el mismo archivo.
    private static string Stamp(SectionModel section) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Serialize(section))))[..16].ToLowerInvariant();

    private static string Serialize(SectionModel section) => JsonSerializer.Serialize(section, JsonOptions);

    /// <summary>Ruta completa de esta máquina → como se guarda (relativa con <c>/</c> si está dentro del repo). null si no es válida.</summary>
    private string? ToStored(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > MaxPathLength || path.Any(char.IsControl))
            return null;

        if (!Path.IsPathFullyQualified(path) || !path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            return null;

        string full;

        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        var prefix = _root + Path.DirectorySeparatorChar;

        return full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? full[prefix.Length..].Replace('\\', '/')
            : full;
    }

    /// <summary>Lo guardado → ruta completa de esta máquina. Lo que venga mal escrito a mano en el archivo se ignora.</summary>
    private string? ToFull(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored) || stored.Any(char.IsControl) || !stored.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            if (Path.IsPathFullyQualified(stored))
                return Path.GetFullPath(stored);

            var full = Path.GetFullPath(Path.Combine(_root, stored.Replace('/', Path.DirectorySeparatorChar)));

            // Una ruta relativa con "..\" que sale del repo no se acepta: relativa significa "dentro del proyecto".
            return full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? full : null;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static string? CleanName(string? value)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxNameLength || trimmed.Any(char.IsControl) ? null : trimmed;
    }

    /// <summary>null = el archivo existe pero está dañado.</summary>
    private Dictionary<string, SectionModel>? ReadAll()
    {
        if (_path == null || !File.Exists(_path))
            return new Dictionary<string, SectionModel>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, SectionModel>>(File.ReadAllText(_path, Encoding.UTF8), JsonOptions)
                   ?? new Dictionary<string, SectionModel>();
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private void Write(Dictionary<string, SectionModel> all)
    {
        if (_path == null)
            return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);

            // Orden fijo de las secciones y escritura atómica: diff de git limpio y nunca un archivo a medias.
            var ordered = all.OrderBy(p => p.Key, StringComparer.Ordinal).ToDictionary(p => p.Key, p => p.Value);
            var temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(ordered, JsonOptions) + Environment.NewLine, new UTF8Encoding(false));
            File.Move(temp, _path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Recordar las pestañas es una comodidad: si no se puede escribir, la consola sigue funcionando.
        }
    }

    private class SectionModel
    {
        [JsonPropertyName("alias")] public string? Alias { get; set; }
        [JsonPropertyName("database")] public string? Database { get; set; }
        [JsonPropertyName("schema")] public string? Schema { get; set; }
        [JsonPropertyName("files")] public List<FileModel>? Files { get; set; }
        [JsonPropertyName("activeFile")] public string? ActiveFile { get; set; }
    }

    private class FileModel
    {
        [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
        [JsonPropertyName("alias")] public string? Alias { get; set; }
        [JsonPropertyName("database")] public string? Database { get; set; }
        [JsonPropertyName("schema")] public string? Schema { get; set; }
    }
}
