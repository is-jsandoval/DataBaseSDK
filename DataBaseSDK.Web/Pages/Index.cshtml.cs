using DataBaseSDK.Core;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataBaseSDK.Web.Pages;

/// <summary>Un módulo (motor) en la portada: cuántas conexiones tiene y cuántas permiten editar filas.</summary>
public record ModuleSummary(string Title, string Icon, string Page, string EnvPrefix, int Connections, int WriteEnabled);

public class IndexModel : PageModel
{
    private readonly IDbConnectionCatalog _catalog;

    public IndexModel(IDbConnectionCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<ModuleSummary> Modules { get; private set; } = [];

    public int TotalConnections => Modules.Sum(m => m.Connections);

    public void OnGet()
    {
        Modules =
        [
            Summarize(DatabaseKind.SqlServer, "SQL Server", "🗄️", "/SqlServer", DbConnectionCatalog.SqlServerPrefix),
            Summarize(DatabaseKind.Oracle, "Oracle", "🛢️", "/Oracle", DbConnectionCatalog.OraclePrefix)
        ];
    }

    private ModuleSummary Summarize(DatabaseKind kind, string title, string icon, string page, string envPrefix)
    {
        var connections = _catalog.List(kind);

        return new ModuleSummary(title, icon, page, envPrefix, connections.Count, connections.Count(c => c.AllowWrite));
    }
}
