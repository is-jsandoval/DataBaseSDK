using System.Globalization;
using System.Text;

namespace DataBaseSDK.Core;

public enum SqlFileError
{
    NotFound,
    AccessDenied,
    TooLarge,
    NotSql,
    InvalidPath,
    /// <summary>El archivo cambió en disco desde que se abrió (otro programa lo modificó).</summary>
    Conflict,
    AlreadyExists,
    Io
}

/// <summary>Un fallo de archivo que la UI sabe explicar (<see cref="Code"/>) en vez de mostrar una excepción cruda.</summary>
public class SqlFileException : Exception
{
    public SqlFileException(SqlFileError code, string message, Exception? inner = null) : base(message, inner)
    {
        Code = code;
    }

    public SqlFileError Code { get; }
}

/// <param name="Text">Con saltos de línea normalizados a <c>\n</c> (el editor trabaja así); <paramref name="Eol"/> recuerda el original para guardar igual.</param>
/// <param name="Version">Marca de la última modificación en disco: al guardar sirve para detectar que otro programa lo tocó.</param>
public record SqlFileContent(string Path, string Name, string Text, string Eol, string Encoding, string Version, long Size);

public record SqlFileStat(bool Exists, string? Version, long Size);

public record FolderEntry(string Name, string Path, bool IsFolder, long? Size);

/// <param name="Parent">Carpeta superior; cadena vacía = "Este equipo" (lista de unidades); null si ya se está en esa lista.</param>
public record FolderListing(string Path, string? Parent, IReadOnlyList<FolderEntry> Entries, bool Truncated);

public interface ISqlFileService
{
    /// <summary>Lista subcarpetas y archivos .sql de una carpeta (vacío o null = las unidades de disco).</summary>
    FolderListing Browse(string? path);

    /// <summary>Accesos rápidos del selector de rutas: Documentos, Escritorio, Descargas, el proyecto.</summary>
    IReadOnlyList<FolderEntry> Places();

    SqlFileContent Read(string path);

    SqlFileStat Stat(string path);

    /// <summary>
    /// Encuentra la ruta real de un archivo que el navegador entregó SIN ruta (al arrastrarlo a la página):
    /// busca <paramref name="name"/> con exactamente <paramref name="size"/> bytes dentro de las carpetas dadas
    /// (carpetas de trabajo, recientes). Devuelve hasta 5 coincidencias; la búsqueda tiene tope de profundidad,
    /// de carpetas y de tiempo, así que en un disco enorme puede no encontrarlo (no es un error).
    /// </summary>
    IReadOnlyList<string> Locate(string name, long size, IEnumerable<string> folders);

    /// <summary>
    /// Guarda un .sql. <paramref name="expectedVersion"/> es la versión que el editor tenía al abrir: si el
    /// archivo cambió en disco desde entonces lanza <see cref="SqlFileError.Conflict"/>. Sin versión esperada,
    /// un archivo que ya existe exige <paramref name="overwrite"/> (si no, <see cref="SqlFileError.AlreadyExists"/>).
    /// </summary>
    SqlFileStat Save(string path, string text, string? eol, string? encoding, string? expectedVersion, bool overwrite);
}

/// <summary>
/// Abrir, guardar y navegar archivos .sql del disco local — lo que hacen SSMS / SQL Developer con
/// "Abrir archivo" y "Guardar". Solo toca archivos con extensión <c>.sql</c> y de hasta 5 MB. La
/// codificación (UTF-8 con o sin BOM, UTF-16, Latin-1) y los saltos de línea (CRLF/LF) se detectan al
/// leer y se respetan al guardar, para no reescribir un archivo ajeno con otro formato.
/// </summary>
public class SqlFileService : ISqlFileService
{
    public const long MaxFileBytes = 5 * 1024 * 1024;
    private const int MaxEntries = 2000;

    private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public FolderListing Browse(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => { try { return d.IsReady; } catch (IOException) { return false; } })
                .Select(d => new FolderEntry(d.Name, d.Name, true, null))
                .ToList();

            return new FolderListing(string.Empty, null, drives, false);
        }

        var folder = NormalizePath(path);

        if (File.Exists(folder))
            folder = Path.GetDirectoryName(folder)!;

        if (!Directory.Exists(folder))
            throw new SqlFileException(SqlFileError.NotFound, $"La carpeta \"{folder}\" no existe.");

        var entries = new List<FolderEntry>();
        var truncated = false;

        try
        {
            foreach (var directory in new DirectoryInfo(folder).EnumerateDirectories().OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
            {
                // Carpetas ocultas o del sistema ($Recycle.Bin, System Volume Information...) solo estorban.
                if ((directory.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                    continue;

                entries.Add(new FolderEntry(directory.Name, directory.FullName, true, null));
            }

            foreach (var file in new DirectoryInfo(folder).EnumerateFiles("*.sql").OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                // EnumerateFiles("*.sql") en Windows también acepta ".sqlx"; se exige la extensión exacta.
                if (!file.Extension.Equals(".sql", StringComparison.OrdinalIgnoreCase))
                    continue;

                entries.Add(new FolderEntry(file.Name, file.FullName, false, file.Length));
            }

            if (entries.Count > MaxEntries)
            {
                entries = entries.Take(MaxEntries).ToList();
                truncated = true;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new SqlFileException(SqlFileError.AccessDenied, $"No tienes permiso para ver la carpeta \"{folder}\".", ex);
        }
        catch (IOException ex)
        {
            throw new SqlFileException(SqlFileError.Io, $"No se pudo leer la carpeta \"{folder}\": {ex.Message}", ex);
        }

        var parent = Directory.GetParent(folder)?.FullName ?? string.Empty;

        return new FolderListing(folder, parent, entries, truncated);
    }

    public IReadOnlyList<FolderEntry> Places()
    {
        var candidates = new List<(string Name, string? Path)>
        {
            ("Proyecto", ProjectPaths.Root),
            ("Documentos", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
            ("Escritorio", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)),
            ("Descargas", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"))
        };

        return candidates
            .Where(c => !string.IsNullOrWhiteSpace(c.Path) && Directory.Exists(c.Path))
            .Select(c => new FolderEntry(c.Name, c.Path!, true, null))
            .ToList();
    }

    public SqlFileContent Read(string path)
    {
        var full = NormalizeSqlPath(path);

        if (!File.Exists(full))
            throw new SqlFileException(SqlFileError.NotFound, $"No se encontró el archivo \"{full}\" (se movió, se renombró o se borró).");

        try
        {
            var info = new FileInfo(full);

            if (info.Length > MaxFileBytes)
                throw new SqlFileException(SqlFileError.TooLarge, $"El archivo pesa {info.Length / 1024 / 1024} MB; el máximo para abrirlo aquí es {MaxFileBytes / 1024 / 1024} MB.");

            var bytes = File.ReadAllBytes(full);
            var (text, encoding) = Decode(bytes);

            // El editor trabaja con \n; se recuerda el salto de línea dominante del archivo para devolverlo igual.
            var crlf = CountOccurrences(text, "\r\n");
            var lf = CountOccurrences(text, "\n") - crlf;
            var eol = lf > crlf ? "\n" : "\r\n";

            text = text.Replace("\r\n", "\n").Replace('\r', '\n');

            return new SqlFileContent(full, Path.GetFileName(full), text, eol, encoding, VersionOf(full), bytes.LongLength);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new SqlFileException(SqlFileError.AccessDenied, $"No tienes permiso para leer \"{full}\".", ex);
        }
        catch (IOException ex)
        {
            throw new SqlFileException(SqlFileError.Io, $"No se pudo leer \"{full}\" (¿lo tiene abierto otro programa?): {ex.Message}", ex);
        }
    }

    public SqlFileStat Stat(string path)
    {
        var full = NormalizeSqlPath(path);

        try
        {
            return File.Exists(full)
                ? new SqlFileStat(true, VersionOf(full), new FileInfo(full).Length)
                : new SqlFileStat(false, null, 0);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new SqlFileException(SqlFileError.AccessDenied, $"No tienes permiso para ver \"{full}\".", ex);
        }
    }

    private const int LocateMaxDepth = 5;
    private const int LocateMaxFolders = 4000;
    private const int LocateMaxMatches = 5;
    private static readonly TimeSpan LocateTimeLimit = TimeSpan.FromSeconds(2);

    public IReadOnlyList<string> Locate(string name, long size, IEnumerable<string> folders)
    {
        // El nombre viene del navegador: solo un nombre de archivo .sql, nunca una ruta.
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(['\\', '/', ':', '*', '?']) >= 0 ||
            !name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            return [];

        var matches = new List<string>();
        var visited = 0;
        var clock = System.Diagnostics.Stopwatch.StartNew();

        void Walk(string folder, int depth)
        {
            if (matches.Count >= LocateMaxMatches || visited >= LocateMaxFolders || clock.Elapsed > LocateTimeLimit)
                return;

            visited++;

            try
            {
                var directory = new DirectoryInfo(folder);

                foreach (var file in directory.EnumerateFiles(name))
                {
                    if (file.Length == size && file.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                        !matches.Contains(file.FullName, StringComparer.OrdinalIgnoreCase))
                        matches.Add(file.FullName);
                }

                if (depth >= LocateMaxDepth)
                    return;

                foreach (var child in directory.EnumerateDirectories())
                {
                    if ((child.Attributes & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint)) != 0)
                        continue;

                    Walk(child.FullName, depth + 1);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                // Una carpeta que no se puede leer no frena la búsqueda en las demás.
            }
        }

        foreach (var raw in folders.Where(f => !string.IsNullOrWhiteSpace(f)).Distinct(StringComparer.OrdinalIgnoreCase).Take(30))
        {
            try
            {
                var folder = NormalizePath(raw);

                if (Directory.Exists(folder))
                    Walk(folder, 0);
            }
            catch (SqlFileException)
            {
                // Ruta guardada que ya no es válida: se ignora.
            }
        }

        return matches;
    }

    public SqlFileStat Save(string path, string text, string? eol, string? encoding, string? expectedVersion, bool overwrite)
    {
        var full = NormalizeSqlPath(path);
        var folder = Path.GetDirectoryName(full)!;

        if (!Directory.Exists(folder))
            throw new SqlFileException(SqlFileError.NotFound, $"La carpeta \"{folder}\" no existe.");

        var exists = File.Exists(full);

        if (exists)
        {
            if (expectedVersion == null)
            {
                if (!overwrite)
                    throw new SqlFileException(SqlFileError.AlreadyExists, $"Ya existe un archivo \"{Path.GetFileName(full)}\" en esa carpeta.");
            }
            else if (!overwrite && VersionOf(full) != expectedVersion)
            {
                throw new SqlFileException(SqlFileError.Conflict, $"\"{Path.GetFileName(full)}\" cambió en disco desde que lo abriste (otro programa lo modificó).");
            }
        }
        else if (expectedVersion != null && !overwrite)
        {
            // Se abrió desde disco y ahora ya no está: guardar lo recrearía en silencio. Se avisa.
            throw new SqlFileException(SqlFileError.NotFound, $"\"{Path.GetFileName(full)}\" ya no existe en \"{folder}\" (se movió o se borró).");
        }

        var lineEnding = eol is "\n" or "\r\n" ? eol : "\r\n";
        var content = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", lineEnding);
        var bytes = Encode(content, encoding);

        if (bytes.LongLength > MaxFileBytes)
            throw new SqlFileException(SqlFileError.TooLarge, $"El contenido pesa más de {MaxFileBytes / 1024 / 1024} MB.");

        try
        {
            File.WriteAllBytes(full, bytes);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new SqlFileException(SqlFileError.AccessDenied, $"No tienes permiso para escribir \"{full}\" (¿es de solo lectura?).", ex);
        }
        catch (IOException ex)
        {
            throw new SqlFileException(SqlFileError.Io, $"No se pudo guardar \"{full}\": {ex.Message}", ex);
        }

        return new SqlFileStat(true, VersionOf(full), bytes.LongLength);
    }

    // --- Ruta ---

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new SqlFileException(SqlFileError.InvalidPath, "Falta la ruta.");

        var trimmed = path.Trim().Trim('"');

        if (trimmed.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || trimmed.IndexOfAny(['*', '?']) >= 0)
            throw new SqlFileException(SqlFileError.InvalidPath, "La ruta tiene caracteres no válidos.");

        try
        {
            var full = Path.GetFullPath(trimmed);

            if (!Path.IsPathRooted(full))
                throw new SqlFileException(SqlFileError.InvalidPath, "Escribe la ruta completa (con la unidad, ej. C:\\carpeta\\archivo.sql).");

            return full;
        }
        catch (ArgumentException ex)
        {
            throw new SqlFileException(SqlFileError.InvalidPath, "La ruta no es válida.", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new SqlFileException(SqlFileError.InvalidPath, "La ruta no es válida.", ex);
        }
        catch (PathTooLongException ex)
        {
            throw new SqlFileException(SqlFileError.InvalidPath, "La ruta es demasiado larga.", ex);
        }
    }

    private static string NormalizeSqlPath(string? path)
    {
        var full = NormalizePath(path);

        if (!Path.GetExtension(full).Equals(".sql", StringComparison.OrdinalIgnoreCase))
            throw new SqlFileException(SqlFileError.NotSql, "Solo se pueden abrir y guardar archivos con extensión .sql.");

        if (Directory.Exists(full))
            throw new SqlFileException(SqlFileError.InvalidPath, "Esa ruta es una carpeta, no un archivo.");

        return full;
    }

    private static string VersionOf(string fullPath) =>
        File.GetLastWriteTimeUtc(fullPath).Ticks.ToString(CultureInfo.InvariantCulture);

    // --- Codificación ---

    internal static (string Text, string Encoding) Decode(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return (Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3), "utf8-bom");

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return (Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2), "utf16le");

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return (Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2), "utf16be");

        try
        {
            return (StrictUtf8.GetString(bytes), "utf8");
        }
        catch (DecoderFallbackException)
        {
            // No es UTF-8 válido: un script viejo en Windows-1252 / Latin-1.
            return (Encoding.Latin1.GetString(bytes), "latin1");
        }
    }

    internal static byte[] Encode(string text, string? encoding) => encoding switch
    {
        "utf8-bom" => Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray(),
        "utf16le" => Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(text)).ToArray(),
        "utf16be" => Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes(text)).ToArray(),
        "latin1" => Encoding.Latin1.GetBytes(text),
        _ => new UTF8Encoding(false).GetBytes(text)
    };

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;

        for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
            count++;

        return count;
    }
}
