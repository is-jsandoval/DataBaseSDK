using System.ComponentModel;
using System.Diagnostics;

namespace DataBaseSDK.Web;

/// <summary>
/// Abre el sitio en una ventana "app" de Edge (sin pestañas ni barra de direcciones: se ve como un programa) o, si no hay
/// Edge, en el navegador predeterminado. Nunca lanza: si no puede abrirlo, el sitio sigue corriendo y se abre a mano.
/// Solo se usa cuando <c>start.cmd</c> pide <c>DBSDK_OPEN_BROWSER=1</c>.
/// </summary>
public static class BrowserLauncher
{
    public static void Open(string url)
    {
        if (TryStart("msedge", $"--app={url}"))
            return;

        TryStart(url, null);
    }

    private static bool TryStart(string fileName, string? arguments)
    {
        try
        {
            Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true, Arguments = arguments ?? string.Empty });

            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
