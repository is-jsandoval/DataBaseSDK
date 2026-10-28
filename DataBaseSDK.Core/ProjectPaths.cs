namespace DataBaseSDK.Core;

/// <summary>
/// Dónde vive el proyecto en disco, para guardar sus datos locales (conexiones de las consolas SQL)
/// dentro de la carpeta del propio repo y no escondidos en el perfil de Windows.
/// </summary>
public static class ProjectPaths
{
    private static readonly Lazy<string> RootLazy = new(FindRoot);

    /// <summary>Carpeta raíz del repo (la que tiene <c>DataBaseSDK.sln</c>); si no se encuentra, la carpeta actual.</summary>
    public static string Root => RootLazy.Value;

    /// <summary>
    /// Datos locales del proyecto: <c>&lt;repo&gt;\.local\</c> (en el <c>.gitignore</c>). Las contraseñas
    /// guardadas ahí van cifradas con DPAPI del usuario de Windows, así que ese archivo solo sirve en
    /// esta máquina y para este usuario.
    /// </summary>
    public static string DataDirectory => Path.Combine(Root, ".local");

    private static string FindRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var directory = new DirectoryInfo(start); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "DataBaseSDK.sln")))
                    return directory.FullName;
            }
        }

        return Directory.GetCurrentDirectory();
    }
}
