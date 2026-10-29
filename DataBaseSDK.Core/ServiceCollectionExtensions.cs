using Microsoft.Extensions.DependencyInjection;

namespace DataBaseSDK.Core;

public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Registra las consolas de Oracle y SQL Server: conexiones (.env + guardadas con DPAPI en
    /// <c>.local</c> del proyecto), un ejecutor de consultas y un explorador de objetos por motor.
    /// </summary>
    public static IServiceCollection AddDatabaseConsole(this IServiceCollection services)
    {
        services.AddSingleton<ISecretProtector, DpapiSecretProtector>();
        services.AddSingleton<IDbConnectionStore>(sp => new DbConnectionStore(sp.GetRequiredService<ISecretProtector>()));
        services.AddSingleton<IDbConnectionCatalog>(sp => new DbConnectionCatalog(sp.GetRequiredService<IDbConnectionStore>()));

        services.AddSingleton<ISqlQueryRunner, OracleQueryRunner>();
        services.AddSingleton<ISqlQueryRunner, SqlServerQueryRunner>();

        services.AddSingleton<ISqlFileService, SqlFileService>();

        // Pestañas abiertas y conexión elegida por motor, en <repo>\db-workspace.json (versionado; sin texto ni claves).
        services.AddSingleton<IWorkspaceStore>(_ => new WorkspaceStore());

        services.AddSingleton<IDbExplorer, OracleExplorer>();
        services.AddSingleton<IDbExplorer, SqlServerExplorer>();

        // Edición de filas desde la grilla: bitácora en .local\data-edits.log; solo actúa en conexiones con AllowWrite.
        services.AddSingleton<IEditAuditLog>(_ => new FileEditAuditLog());
        services.AddSingleton<IRowEditor>(sp => new OracleRowEditor(
            sp.GetServices<IDbExplorer>().First(e => e.Kind == DatabaseKind.Oracle), sp.GetRequiredService<IEditAuditLog>()));
        services.AddSingleton<IRowEditor>(sp => new SqlServerRowEditor(
            sp.GetServices<IDbExplorer>().First(e => e.Kind == DatabaseKind.SqlServer), sp.GetRequiredService<IEditAuditLog>()));

        return services;
    }
}
