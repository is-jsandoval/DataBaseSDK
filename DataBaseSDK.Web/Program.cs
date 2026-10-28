using DataBaseSDK.Core;

LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

// Solo escucha en localhost: el sitio no tiene login y maneja credenciales de bases de datos reales. No hay opción para
// abrirlo a la red. DBSDK_PORT cambia el puerto (por defecto 5130, para convivir con AzureDevOpsSdk.Web en el 5124).
var port = int.TryParse(Environment.GetEnvironmentVariable("DBSDK_PORT"), out var configured) && configured is > 0 and < 65536
    ? configured
    : 5130;

builder.WebHost.UseUrls($"http://localhost:{port}");

builder.Services.AddRazorPages();

// Conexiones (.env + guardadas con DPAPI en .local), ejecutores de consultas, exploradores, editor de filas y archivos .sql.
builder.Services.AddDatabaseConsole();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// Barrera para TODO el sitio (páginas, endpoints y archivos estáticos): solo peticiones de esta misma máquina — conexión de
// loopback Y cabecera Host de loopback (esto último contra DNS rebinding). Ver LocalAccess.
app.Use(async (context, next) =>
{
    if (!LocalAccess.IsLocal(context.Connection.RemoteIpAddress, context.Request.Host.Host))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("DataBaseSDK solo se puede usar desde esta misma máquina (http://localhost).");

        return;
    }

    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

// start.cmd pide abrir el navegador (DBSDK_OPEN_BROWSER=1) cuando el sitio ya está escuchando.
if (Environment.GetEnvironmentVariable("DBSDK_OPEN_BROWSER") == "1")
{
    app.Lifetime.ApplicationStarted.Register(() => DataBaseSDK.Web.BrowserLauncher.Open($"http://localhost:{port}/"));
}

app.Run();

// Busca un ".env" desde el directorio actual hacia arriba y carga sus pares KEY=VALUE sin pisar variables ya definidas
// (ORACLE_CONN_*, SQLSERVER_CONN_*, DBSDK_*, TNS_ADMIN...). Es el mismo cargador de AzureDevOpsSdk.
static void LoadDotEnv()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (directory != null)
    {
        var envPath = Path.Combine(directory.FullName, ".env");

        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();

                if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                    continue;

                var separatorIndex = trimmed.IndexOf('=');

                if (separatorIndex <= 0)
                    continue;

                var key = trimmed[..separatorIndex].Trim();
                var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');

                if (Environment.GetEnvironmentVariable(key) == null)
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }

            return;
        }

        directory = directory.Parent;
    }
}
