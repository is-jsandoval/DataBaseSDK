using DataBaseSDK.Core;

namespace DataBaseSDK.Tests;

public class WorkspaceStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "dbsdk-workspace-" + Guid.NewGuid().ToString("N"));
    private string Repo => Path.Combine(_directory, "repo");
    private string FilePath => Path.Combine(Repo, "db-workspace.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private WorkspaceStore Store(string? root = null) => new(root == null ? FilePath : Path.Combine(root, "db-workspace.json"), root ?? Repo);

    private static List<WorkspaceFile?> Tabs(params string?[] paths) => paths.Select(p => p == null ? null : new WorkspaceFile(p)).ToList();

    private static List<string> Paths(WorkspaceState state) => state.Files.Select(f => f.Path).ToList();

    [Fact]
    public void Each_engine_keeps_its_own_connection_and_tabs()
    {
        var store = Store();
        store.SaveContext(DatabaseKind.Oracle, "Bg_Core_DESA", null, "BG_TP");
        store.SaveContext(DatabaseKind.SqlServer, "SQL-DEV", "Comercios", null);
        store.SaveTabs(DatabaseKind.Oracle, Tabs(Path.Combine(Repo, "q", "a.sql")), null);

        var oracle = Store().Get(DatabaseKind.Oracle);
        var sql = Store().Get(DatabaseKind.SqlServer);

        Assert.Equal(("Bg_Core_DESA", "BG_TP"), (oracle.Alias, oracle.Schema));
        Assert.Single(oracle.Files);
        Assert.Equal(("SQL-DEV", "Comercios"), (sql.Alias, sql.Database));
        Assert.Empty(sql.Files);
    }

    [Fact]
    public void Each_file_keeps_its_own_connection_like_an_SSMS_query_window()
    {
        var a = Path.Combine(Repo, "a.sql");
        var b = Path.Combine(Repo, "b.sql");

        Store().SaveTabs(DatabaseKind.SqlServer,
            [new WorkspaceFile(a, "SQL-DEV", "Comercios"), new WorkspaceFile(b, "SQL-QA", null), new WorkspaceFile(Path.Combine(Repo, "c.sql"), "X\u0001")],
            b);

        var files = Store().Get(DatabaseKind.SqlServer).Files;

        Assert.Equal(new WorkspaceFile(a, "SQL-DEV", "Comercios"), files[0]);
        Assert.Equal(new WorkspaceFile(b, "SQL-QA"), files[1]);
        Assert.Null(files[2].Alias);                                   // nombre inválido: el archivo queda, sin conexión
        Assert.DoesNotContain("password", File.ReadAllText(FilePath), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Paths_inside_the_repo_are_stored_relative_and_come_back_full_on_another_machine()
    {
        var file = Path.Combine(Repo, "QuerysJean", "Link Pagos", "consulta links.sql");
        Store().SaveTabs(DatabaseKind.Oracle, [new WorkspaceFile(file, "ORA")], file);

        var json = File.ReadAllText(FilePath);
        Assert.Contains("\"QuerysJean/Link Pagos/consulta links.sql\"", json);
        Assert.DoesNotContain(_directory.Replace("\\", "\\\\"), json);

        // Otra computadora: el mismo archivo en otra carpeta.
        var otherRepo = Path.Combine(_directory, "otra-maquina");
        Directory.CreateDirectory(otherRepo);
        File.Copy(FilePath, Path.Combine(otherRepo, "db-workspace.json"));

        var there = Store(otherRepo).Get(DatabaseKind.Oracle);
        var expected = Path.Combine(otherRepo, "QuerysJean", "Link Pagos", "consulta links.sql");

        Assert.Equal([new WorkspaceFile(expected, "ORA")], there.Files);
        Assert.Equal(expected, there.ActiveFile);
        Assert.Equal(Store().Get(DatabaseKind.Oracle).Stamp, there.Stamp);   // mismo contenido, mismo sello en las dos máquinas
    }

    [Fact]
    public void Files_outside_the_repo_keep_their_full_path()
    {
        var outside = Path.Combine(_directory, "fuera", "x.sql");
        Store().SaveTabs(DatabaseKind.SqlServer, Tabs(outside), null);

        Assert.Equal([outside], Paths(Store().Get(DatabaseKind.SqlServer)));
    }

    [Theory]
    [InlineData("relativa.sql")]
    [InlineData("C:\\algo\\no-es-sql.txt")]
    [InlineData("")]
    [InlineData(null)]
    public void Anything_that_is_not_a_full_path_to_a_sql_file_is_dropped(string? path)
    {
        Assert.Empty(Store().SaveTabs(DatabaseKind.Oracle, Tabs(path), path).Files);
    }

    [Fact]
    public void Duplicates_are_dropped_the_list_is_capped_and_the_active_must_be_in_it()
    {
        var a = Path.Combine(Repo, "a.sql");
        var many = Enumerable.Range(0, WorkspaceStore.MaxFiles + 5).Select(i => Path.Combine(Repo, $"f{i}.sql"));

        var state = Store().SaveTabs(DatabaseKind.Oracle, Tabs([a, a.ToUpperInvariant(), .. many]), Path.Combine(Repo, "otro.sql"));

        Assert.Equal(WorkspaceStore.MaxFiles, state.Files.Count);
        Assert.Equal(a, state.Files[0].Path);
        Assert.Null(state.ActiveFile);
    }

    [Fact]
    public void The_stamp_changes_only_when_the_content_changes()
    {
        var a = Path.Combine(Repo, "a.sql");
        var first = Store().SaveTabs(DatabaseKind.Oracle, Tabs(a), a);

        var same = Store().SaveTabs(DatabaseKind.Oracle, Tabs(a), a);
        var changed = Store().SaveTabs(DatabaseKind.Oracle, Tabs(a), null);
        var otherConnection = Store().SaveTabs(DatabaseKind.Oracle, [new WorkspaceFile(a, "ORA")], null);

        Assert.Equal(first.Stamp, same.Stamp);
        Assert.NotEqual(first.Stamp, changed.Stamp);
        Assert.NotEqual(changed.Stamp, otherConnection.Stamp);
    }

    [Fact]
    public void Unchanged_saves_do_not_touch_the_file()
    {
        var a = Path.Combine(Repo, "a.sql");
        Store().SaveTabs(DatabaseKind.Oracle, Tabs(a), a);
        var old = DateTime.UtcNow.AddDays(-1);
        File.SetLastWriteTimeUtc(FilePath, old);

        Store().SaveTabs(DatabaseKind.Oracle, Tabs(a), a);

        Assert.Equal(old, File.GetLastWriteTimeUtc(FilePath));
    }

    [Fact]
    public void A_relative_path_that_escapes_the_repo_or_a_broken_file_is_ignored()
    {
        Directory.CreateDirectory(Repo);
        File.WriteAllText(FilePath, """{ "Oracle": { "files": [ { "path": "../fuera.sql" }, { "path": "ok.sql" }, { "path": "C:\\x\\malo.txt" }, null ] } }""");

        Assert.Equal([Path.Combine(Repo, "ok.sql")], Paths(Store().Get(DatabaseKind.Oracle)));

        File.WriteAllText(FilePath, "<<<<<<< HEAD");   // conflicto de merge sin resolver
        Store().SaveContext(DatabaseKind.Oracle, "X", null, null);

        Assert.Equal("<<<<<<< HEAD", File.ReadAllText(FilePath));   // no se pisa
        Assert.Null(Store().Get(DatabaseKind.Oracle).Alias);
    }

    [Fact]
    public void Names_with_control_characters_or_too_long_are_not_stored()
    {
        var state = Store().SaveContext(DatabaseKind.SqlServer, "DE\nV", new string('x', 200), "  ");

        Assert.Null(state.Alias);
        Assert.Null(state.Database);
        Assert.Null(state.Schema);
    }
}
