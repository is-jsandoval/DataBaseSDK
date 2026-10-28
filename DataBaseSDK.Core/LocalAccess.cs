using System.Net;

namespace DataBaseSDK.Core;

/// <summary>
/// ¿Esta petición viene de esta misma máquina? El sitio no tiene login y maneja credenciales de bases de datos, así que
/// solo se atiende a quien esté en el equipo. Se exigen DOS cosas: que la conexión TCP sea de loopback Y que la cabecera
/// <c>Host</c> también lo sea — lo segundo evita el "DNS rebinding" (una página web externa cuyo dominio resuelve a
/// 127.0.0.1 y que, sin este chequeo, podría hablarle al sitio desde tu navegador). Lógica pura (sin ASP.NET) para poder probarla.
/// </summary>
public static class LocalAccess
{
    /// <param name="remoteAddress"><c>HttpContext.Connection.RemoteIpAddress</c>.</param>
    /// <param name="host">Solo el nombre de la cabecera <c>Host</c> (sin puerto): <c>Request.Host.Host</c>.</param>
    public static bool IsLocal(IPAddress? remoteAddress, string? host)
    {
        if (remoteAddress == null || string.IsNullOrWhiteSpace(host))
            return false;

        if (remoteAddress.IsIPv4MappedToIPv6)
            remoteAddress = remoteAddress.MapToIPv4();

        if (!IPAddress.IsLoopback(remoteAddress))
            return false;

        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || (IPAddress.TryParse(host.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address));
    }
}
