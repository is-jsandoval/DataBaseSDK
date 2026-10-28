namespace DataBaseSDK.Core;

public enum DbObjectKind
{
    Table,
    View,
    Procedure,
    Function,
    Package,
    Sequence,
    Synonym,
    Type
}

/// <summary>Lo que se puede desplegar dentro de un objeto del árbol (columnas, índices...).</summary>
public enum ObjectGroup
{
    Columns,
    Indexes,
    ForeignKeys,
    Triggers,
    Parameters,
    Members
}

public record ExplorerDatabase(string Name, bool IsSystem);

public record ExplorerSchema(string Name, bool IsSystem, bool IsCurrent);

/// <summary>Una lista que puede haberse cortado en el tope (para avisar "mostrando los primeros N").</summary>
public record ExplorerPage<T>(IReadOnlyList<T> Items, bool Truncated);

/// <summary>Columna, índice, clave foránea, trigger, parámetro o miembro: <see cref="Detail"/> es el texto gris al lado del nombre.</summary>
public record ExplorerItem(string Name, string? Detail);

/// <summary>Resultado de buscar un objeto por nombre en toda la base.</summary>
public record ExplorerObject(string Schema, string Name, DbObjectKind Kind);

public record ConnectionTestResult(bool Ok, string Message);

public static class ExplorerRules
{
    public static IReadOnlyList<ObjectGroup> GroupsFor(DbObjectKind kind) => kind switch
    {
        DbObjectKind.Table => [ObjectGroup.Columns, ObjectGroup.Indexes, ObjectGroup.ForeignKeys, ObjectGroup.Triggers],
        DbObjectKind.View => [ObjectGroup.Columns],
        DbObjectKind.Procedure or DbObjectKind.Function => [ObjectGroup.Parameters],
        DbObjectKind.Package => [ObjectGroup.Members],
        _ => []
    };

    /// <summary>Tablas y vistas se pueden consultar con un SELECT; procedimientos, funciones y paquetes solo se pueden leer (definición).</summary>
    public static bool IsQueryable(DbObjectKind kind) => kind is DbObjectKind.Table or DbObjectKind.View or DbObjectKind.Synonym;
}

/// <summary>
/// El "explorador de objetos" de SSMS / SQL Developer: servidores (conexiones) → bases de datos →
/// esquemas → tablas, vistas, procedimientos, funciones, paquetes, secuencias, sinónimos y tipos,
/// y dentro de cada objeto sus columnas, índices, claves, triggers o parámetros. Solo lee el
/// catálogo del motor; todos los nombres que llegan del cliente viajan como parámetros.
/// </summary>
public interface IDbExplorer : ITableDescriber
{
    DatabaseKind Kind { get; }

    /// <summary>SQL Server tiene varias bases por servidor; Oracle tiene una sola y el árbol arranca en los esquemas.</summary>
    bool HasDatabases { get; }

    IReadOnlyList<DbObjectKind> Kinds { get; }

    /// <summary>Abre la conexión y devuelve un resumen (versión, usuario) — nunca lanza: un fallo vuelve como <c>Ok = false</c> con el mensaje del driver.</summary>
    Task<ConnectionTestResult> TestAsync(DbConnectionInfo connection, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExplorerDatabase>> ListDatabasesAsync(DbConnectionInfo connection, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExplorerSchema>> ListSchemasAsync(DbConnectionInfo connection, string? database, bool includeSystem, CancellationToken cancellationToken = default);

    Task<ExplorerPage<string>> ListObjectsAsync(DbConnectionInfo connection, string? database, string schema, DbObjectKind kind, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExplorerItem>> ListGroupAsync(DbConnectionInfo connection, string? database, string schema, DbObjectKind kind, string name, ObjectGroup group, CancellationToken cancellationToken = default);

    /// <summary>Código fuente del objeto (vistas, procedimientos, funciones, paquetes...); null si el motor no lo expone (ej. tablas de SQL Server).</summary>
    Task<string?> GetDefinitionAsync(DbConnectionInfo connection, string? database, string schema, DbObjectKind kind, string name, CancellationToken cancellationToken = default);

    /// <summary>Busca objetos por nombre (contiene, sin distinguir mayúsculas) en todos los esquemas de la base.</summary>
    Task<ExplorerPage<ExplorerObject>> SearchAsync(DbConnectionInfo connection, string? database, string filter, CancellationToken cancellationToken = default);
}
