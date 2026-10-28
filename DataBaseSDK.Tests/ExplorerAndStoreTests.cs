using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using DataBaseSDK.Core;

namespace DataBaseSDK.Tests;

// Subclases que reemplazan la conexión real por la falsa de QueryRunnerTests: se prueba el SQL y los
// parámetros que realmente se mandarían, sin base de datos. Lo que dice el catálogo real de Oracle o
// SQL Server no se puede comprobar así (eso solo se verifica contra un servidor de verdad).
internal class TestSqlServerExplorer : SqlServerExplorer
{
    public FakeConnection Connection { get; } = new();
    protected override DbConnection CreateConnection(string connectionString) => Connection;
}

internal class TestOracleExplorer : OracleExplorer
{
    public FakeConnection Connection { get; } = new();
    protected override DbConnection CreateConnection(string connectionString) => Connection;
}

internal class TestOracleRunner : OracleQueryRunner
{
    public FakeConnection Connection { get; } = new();
    protected override DbConnection CreateConnection(string connectionString) => Connection;
}

internal class TestSqlServerRunner : SqlServerQueryRunner
{
    public FakeConnection Connection { get; } = new();
    protected override DbConnection CreateConnection(string connectionString) => Connection;
}

public class SqlServerExplorerTests
{
    private static readonly DbConnectionInfo Info = new("x", DatabaseKind.SqlServer, "Server=x", "x");

    private static DataTable Names(params string[] names)
    {
        var table = new DataTable();
        table.Columns.Add("name", typeof(string));

        foreach (var name in names)
            table.Rows.Add(name);

        return table;
    }

    [Fact]
    public async Task User_supplied_names_travel_as_parameters_never_inside_the_sql()
    {
        var explorer = new TestSqlServerExplorer { Connection = { Table = Names("Clientes", "Pagos") } };
        const string evil = "dbo'; DROP TABLE Clientes; --";

        var page = await explorer.ListObjectsAsync(Info, null, evil, DbObjectKind.Table);

        Assert.Equal(["Clientes", "Pagos"], page.Items);
        Assert.False(page.Truncated);

        var sql = Assert.Single(explorer.Connection.ExecutedSql);
        Assert.DoesNotContain("DROP", sql);
        Assert.Equal(evil, explorer.Connection.ExecutedParameters[0]["p_schema"]);
    }

    [Fact]
    public async Task The_database_is_switched_with_ChangeDatabase_so_the_driver_quotes_it()
    {
        var table = new DataTable();
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("isDefault", typeof(int));
        table.Rows.Add("dbo", 1);

        var explorer = new TestSqlServerExplorer { Connection = { Table = table } };
        const string evil = "Pagos]; DROP DATABASE x; --";

        var schemas = await explorer.ListSchemasAsync(Info, evil, includeSystem: false);

        Assert.Equal(evil, Assert.Single(explorer.Connection.ChangedDatabases));
        Assert.DoesNotContain("DROP", Assert.Single(explorer.Connection.ExecutedSql));
        Assert.True(schemas.Single().IsCurrent);
    }

    [Fact]
    public async Task Search_escapes_like_wildcards_in_the_filter()
    {
        var table = new DataTable();
        table.Columns.Add("schema", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("type", typeof(string));
        table.Rows.Add("dbo", "Clientes", "U ");   // sys.objects.type viene con espacio al final
        table.Rows.Add("dbo", "Rara", "XX");       // tipo que no mostramos

        var explorer = new TestSqlServerExplorer { Connection = { Table = table } };

        var page = await explorer.SearchAsync(Info, null, @"50%_a\b");

        Assert.Equal(@"%50\%\_A\\B%", explorer.Connection.ExecutedParameters[0]["p_filter"]);
        Assert.Equal(@"50\%\_A\\B%", explorer.Connection.ExecutedParameters[0]["p_prefix"]);
        Assert.Contains("ORDER BY CASE WHEN UPPER(o.name) LIKE @p_prefix", Assert.Single(explorer.Connection.ExecutedSql));  // primero lo que empieza con el texto
        var found = Assert.Single(page.Items);
        Assert.Equal(("dbo", "Clientes", DbObjectKind.Table), (found.Schema, found.Name, found.Kind));
    }

    [Fact]
    public async Task Columns_show_type_pk_identity_and_nullability()
    {
        var table = new DataTable();
        foreach (var (name, type) in new[]
        {
            ("name", typeof(string)), ("type", typeof(string)), ("max", typeof(int)), ("precision", typeof(int)), ("scale", typeof(int)),
            ("nullable", typeof(bool)), ("identity", typeof(bool)), ("computed", typeof(bool)), ("pk", typeof(int))
        })
            table.Columns.Add(name, type);

        table.Rows.Add("Id", "int", 4, 10, 0, false, true, false, 1);
        table.Rows.Add("Nombre", "nvarchar", 200, 0, 0, true, false, false, 0);
        table.Rows.Add("Monto", "decimal", 9, 18, 2, true, false, false, 0);

        var explorer = new TestSqlServerExplorer { Connection = { Table = table } };

        var items = await explorer.ListGroupAsync(Info, null, "dbo", DbObjectKind.Table, "Clientes", ObjectGroup.Columns);

        Assert.Equal("int · PK · identity · NOT NULL", items[0].Detail);
        Assert.Equal("nvarchar(100) · NULL", items[1].Detail);      // nvarchar: max_length son bytes, se divide entre 2
        Assert.Equal("decimal(18,2) · NULL", items[2].Detail);
    }

    [Fact]
    public async Task A_list_longer_than_the_cap_is_flagged_as_truncated()
    {
        var explorer = new TestSqlServerExplorer { Connection = { Table = Names(Enumerable.Range(1, 2001).Select(i => $"T{i}").ToArray()) } };

        var page = await explorer.ListObjectsAsync(Info, null, "dbo", DbObjectKind.Table);

        Assert.Equal(2000, page.Items.Count);
        Assert.True(page.Truncated);
    }

    [Theory]
    [InlineData("nvarchar", -1, 0, 0, "nvarchar(max)")]
    [InlineData("nvarchar", 100, 0, 0, "nvarchar(50)")]
    [InlineData("varchar", 30, 0, 0, "varchar(30)")]
    [InlineData("decimal", 9, 18, 2, "decimal(18,2)")]
    [InlineData("datetime", 8, 23, 3, "datetime")]
    public void TypeText_formats_sql_server_types(string type, int length, int precision, int scale, string expected)
    {
        Assert.Equal(expected, SqlServerExplorer.TypeText(type, length, precision, scale));
    }

    [Fact]
    public async Task A_failed_connection_test_returns_the_message_instead_of_throwing()
    {
        var explorer = new FailingSqlServerExplorer();

        var result = await explorer.TestAsync(Info);

        Assert.False(result.Ok);
        Assert.Contains("servidor inalcanzable", result.Message);
    }

    private class FailingSqlServerExplorer : SqlServerExplorer
    {
        protected override DbConnection CreateConnection(string connectionString) => throw new InvalidOperationException("servidor inalcanzable");
    }
}

public class OracleExplorerTests
{
    private static readonly DbConnectionInfo Info = new("x", DatabaseKind.Oracle, "Data Source=x", "x");

    private static DataTable Column(params object[] values)
    {
        var table = new DataTable();
        table.Columns.Add("v", typeof(string));

        foreach (var value in values)
            table.Rows.Add(value);

        return table;
    }

    [Fact]
    public async Task Objects_are_listed_with_bind_variables()
    {
        var explorer = new TestOracleExplorer { Connection = { Table = Column("CLIENTES", "PAGOS") } };

        var page = await explorer.ListObjectsAsync(Info, null, "HR'; DROP USER x; --", DbObjectKind.Table);

        Assert.Equal(["CLIENTES", "PAGOS"], page.Items);

        var parameters = explorer.Connection.ExecutedParameters[0];
        Assert.Equal("HR'; DROP USER x; --", parameters["p_owner"]);
        Assert.Equal("TABLE", parameters["p_type"]);
        Assert.DoesNotContain("DROP", Assert.Single(explorer.Connection.ExecutedSql));
    }

    [Fact]
    public async Task Schemas_hide_the_system_ones_and_put_the_current_user_first()
    {
        var explorer = new TestOracleExplorer
        {
            Connection =
            {
                TableFor = sql => sql.Contains("SYS_CONTEXT") ? Column("HR") : Column("APEX_050000", "HR", "SCOTT", "SYS", "PUBLIC")
            }
        };

        var visible = await explorer.ListSchemasAsync(Info, null, includeSystem: false);
        var all = await explorer.ListSchemasAsync(Info, null, includeSystem: true);

        Assert.Equal(["HR", "SCOTT"], visible.Select(s => s.Name));
        Assert.True(visible[0].IsCurrent);
        Assert.Equal(["HR", "APEX_050000", "PUBLIC", "SCOTT", "SYS"], all.Select(s => s.Name));
    }

    [Fact]
    public async Task Search_ranks_prefix_matches_first_and_leaves_out_oracle_system_schemas_but_keeps_public_synonyms()
    {
        var table = new DataTable();
        table.Columns.Add("owner", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("type", typeof(string));
        table.Rows.Add("PUBLIC", "DUAL", "SYNONYM");

        var explorer = new TestOracleExplorer { Connection = { Table = table } };

        var page = await explorer.SearchAsync(Info, null, "du_l");

        var sql = Assert.Single(explorer.Connection.ExecutedSql);
        Assert.Contains("ORDER BY CASE WHEN UPPER(object_name) LIKE :p_prefix", sql);
        Assert.Contains("owner NOT IN (", sql);
        Assert.Contains("'SYS'", sql);
        Assert.DoesNotContain("'PUBLIC'", sql);          // PUBLIC no se excluye: ahí viven DUAL y otros sinónimos
        Assert.Equal(@"DU\_L%", explorer.Connection.ExecutedParameters[0]["p_prefix"]);

        var found = Assert.Single(page.Items);
        Assert.Equal(("PUBLIC", "DUAL", DbObjectKind.Synonym), (found.Schema, found.Name, found.Kind));
    }

    [Fact]
    public async Task A_package_definition_joins_spec_and_body_with_create_or_replace()
    {
        var table = new DataTable();
        table.Columns.Add("type", typeof(string));
        table.Columns.Add("text", typeof(string));
        table.Rows.Add("PACKAGE", "PACKAGE pkg IS\n");
        table.Rows.Add("PACKAGE", "END pkg;\n");
        table.Rows.Add("PACKAGE BODY", "PACKAGE BODY pkg IS\n");
        table.Rows.Add("PACKAGE BODY", "END pkg;\n");

        var explorer = new TestOracleExplorer { Connection = { Table = table } };

        var text = await explorer.GetDefinitionAsync(Info, null, "HR", DbObjectKind.Package, "PKG");

        Assert.StartsWith("CREATE OR REPLACE PACKAGE pkg IS", text);
        Assert.Contains("\n/\n\nCREATE OR REPLACE PACKAGE BODY pkg IS", text);
        Assert.EndsWith("/", text);
    }

    [Fact]
    public void Oracle_has_no_databases_level()
    {
        Assert.False(new OracleExplorer().HasDatabases);
        Assert.True(new SqlServerExplorer().HasDatabases);
    }

    [Theory]
    [InlineData("NUMBER", 10, 2, "NUMBER(10,2)")]
    [InlineData("NUMBER", 10, 0, "NUMBER(10)")]
    [InlineData("VARCHAR2", null, null, "VARCHAR2(100)")]   // la longitud de prueba siempre es 100
    public void TypeText_formats_oracle_types(string type, int? precision, int? scale, string expected)
    {
        Assert.Equal(expected, OracleExplorer.TypeText(type, 100, precision, scale));
    }
}

public class QueryContextTests
{
    private static readonly DbConnectionInfo OracleInfo = new("x", DatabaseKind.Oracle, "Data Source=x", "x");
    private static readonly DbConnectionInfo SqlInfo = new("x", DatabaseKind.SqlServer, "Server=x", "x");

    private static DataTable OneRow()
    {
        var table = new DataTable();
        table.Columns.Add("v", typeof(int));
        table.Rows.Add(1);

        return table;
    }

    [Fact]
    public async Task Oracle_sets_the_schema_before_the_read_only_transaction()
    {
        var runner = new TestOracleRunner { Connection = { Table = OneRow() } };

        await runner.ExecuteAsync(OracleInfo, "SELECT 1 FROM dual", new QueryOptions { Schema = "HR" });

        Assert.Equal(
            ["ALTER SESSION SET CURRENT_SCHEMA = \"HR\"", "SET TRANSACTION READ ONLY", "SELECT 1 FROM dual"],
            runner.Connection.ExecutedSql);
    }

    [Theory]
    [InlineData("HR\"; DROP USER x; --")]
    [InlineData("HR HR")]
    [InlineData("")]
    public async Task Oracle_rejects_a_schema_that_is_not_a_plain_identifier(string schema)
    {
        var runner = new TestOracleRunner { Connection = { Table = OneRow() } };

        if (schema.Length == 0)
        {
            await runner.ExecuteAsync(OracleInfo, "SELECT 1 FROM dual", new QueryOptions { Schema = schema });
            Assert.DoesNotContain(runner.Connection.ExecutedSql, s => s.StartsWith("ALTER SESSION"));
            return;
        }

        await Assert.ThrowsAsync<QueryRejectedException>(() =>
            runner.ExecuteAsync(OracleInfo, "SELECT 1 FROM dual", new QueryOptions { Schema = schema }));

        Assert.DoesNotContain(runner.Connection.ExecutedSql, s => s.Contains("DROP"));
    }

    [Fact]
    public async Task SqlServer_switches_to_the_chosen_database()
    {
        var runner = new TestSqlServerRunner { Connection = { Table = OneRow() } };

        await runner.ExecuteAsync(SqlInfo, "SELECT 1", new QueryOptions { Database = "Pagos" });

        Assert.Equal(["Pagos"], runner.Connection.ChangedDatabases);
    }
}

public class ConnectionStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "azdo-store-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_directory, "db-connections.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private DbConnectionStore NewStore() => new(new DpapiSecretProtector(), FilePath);

    private static StoredConnection Sample(string alias = "DEV", string? password = "S3cr3t;\"clave\"") => new()
    {
        Alias = alias,
        Kind = DatabaseKind.SqlServer,
        Host = "sql.banco.local",
        Port = 1433,
        Database = "Pagos",
        User = "lector",
        Password = password
    };

    [Fact]
    public void Save_encrypts_the_password_on_disk_and_Get_decrypts_it()
    {
        var store = NewStore();

        store.Save(Sample());

        var json = File.ReadAllText(FilePath);
        Assert.DoesNotContain("S3cr3t", json);
        Assert.Contains("passwordProtected", json);

        var loaded = NewStore().Get(DatabaseKind.SqlServer, "dev");   // alias sin distinguir mayúsculas, y desde un store nuevo
        Assert.Equal("S3cr3t;\"clave\"", loaded!.Password);
        Assert.Equal("Pagos", loaded.Database);
    }

    [Fact]
    public void Saving_with_a_null_password_keeps_the_stored_one_and_empty_clears_it()
    {
        var store = NewStore();
        store.Save(Sample());

        var edit = Sample(password: null);
        edit.Host = "otro.banco.local";
        store.Save(edit);

        var kept = store.Get(DatabaseKind.SqlServer, "DEV")!;
        Assert.Equal("otro.banco.local", kept.Host);
        Assert.Equal("S3cr3t;\"clave\"", kept.Password);

        store.Save(Sample(password: string.Empty));
        Assert.True(string.IsNullOrEmpty(store.Get(DatabaseKind.SqlServer, "DEV")!.Password));
    }

    [Fact]
    public void Connections_are_separated_by_engine_and_can_be_deleted()
    {
        var store = NewStore();
        store.Save(Sample("A"));
        store.Save(new StoredConnection { Alias = "A", Kind = DatabaseKind.Oracle, Host = "h", OracleTarget = "SVC", User = "u", Password = "p" });

        Assert.Single(store.List(DatabaseKind.SqlServer));
        Assert.Single(store.List(DatabaseKind.Oracle));

        Assert.True(store.Delete(DatabaseKind.SqlServer, "a"));
        Assert.False(store.Delete(DatabaseKind.SqlServer, "a"));
        Assert.Empty(store.List(DatabaseKind.SqlServer));
        Assert.Single(store.List(DatabaseKind.Oracle));
    }

    [Fact]
    public void A_corrupt_file_is_backed_up_and_the_store_starts_empty()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, "{ esto no es json");

        Assert.Empty(NewStore().List(DatabaseKind.SqlServer));
        Assert.True(File.Exists(FilePath + ".corrupto"));
    }

    [Fact]
    public void The_catalog_merges_env_and_saved_connections_and_env_wins_a_duplicate_alias()
    {
        var env = DbConnectionCatalog.SqlServerPrefix + "DUP";

        try
        {
            Environment.SetEnvironmentVariable(env, "Server=desde-env;Database=A;Integrated Security=True");

            var store = NewStore();
            store.Save(Sample("DUP"));
            store.Save(Sample("SOLO-GUARDADA"));

            var list = new DbConnectionCatalog(store).List(DatabaseKind.SqlServer);

            var dup = Assert.Single(list, c => c.Alias == "DUP");
            Assert.Equal(ConnectionOrigin.Env, dup.Origin);

            var saved = Assert.Single(list, c => c.Alias == "SOLO-GUARDADA");
            Assert.Equal(ConnectionOrigin.Saved, saved.Origin);
            Assert.Equal("sql.banco.local,1433 · Pagos · usuario lector", saved.Description);
            Assert.DoesNotContain("S3cr3t", saved.Description);
        }
        finally
        {
            Environment.SetEnvironmentVariable(env, null);
        }
    }
}

public class ConnectionStringFactoryTests
{
    [Fact]
    public void SqlServer_builds_a_connection_string_that_round_trips_awkward_passwords()
    {
        var connection = new StoredConnection
        {
            Alias = "x", Kind = DatabaseKind.SqlServer, Host = "srv\\SQLEXPRESS", Database = "Pagos",
            User = "lector", Password = "a;b=\"c'd", TrustServerCertificate = true
        };

        var parsed = new SqlConnectionStringBuilder(ConnectionStringFactory.Build(connection));

        Assert.Equal("srv\\SQLEXPRESS", parsed.DataSource);
        Assert.Equal("Pagos", parsed.InitialCatalog);
        Assert.Equal("lector", parsed.UserID);
        Assert.Equal("a;b=\"c'd", parsed.Password);
        Assert.True(parsed.TrustServerCertificate);
        Assert.False(parsed.IntegratedSecurity);
    }

    [Fact]
    public void SqlServer_with_a_port_and_windows_auth()
    {
        var parsed = new SqlConnectionStringBuilder(ConnectionStringFactory.Build(new StoredConnection
        {
            Alias = "x", Kind = DatabaseKind.SqlServer, Host = "srv", Port = 14330, WindowsAuth = true, User = "ignorado", Password = "ignorada"
        }));

        Assert.Equal("srv,14330", parsed.DataSource);
        Assert.True(parsed.IntegratedSecurity);
        Assert.True(string.IsNullOrEmpty(parsed.UserID));
    }

    [Theory]
    [InlineData(OracleModes.ServiceName, "db.banco.local:1521/PROD")]
    [InlineData(OracleModes.Sid, "(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=db.banco.local)(PORT=1521))(CONNECT_DATA=(SID=PROD)))")]
    public void Oracle_builds_the_data_source_for_each_mode(string mode, string expected)
    {
        var connection = new StoredConnection { Alias = "x", Kind = DatabaseKind.Oracle, Host = "db.banco.local", OracleMode = mode, OracleTarget = "PROD", User = "u", Password = "p" };

        var parsed = new Oracle.ManagedDataAccess.Client.OracleConnectionStringBuilder(ConnectionStringFactory.Build(connection));

        Assert.Equal(expected, parsed.DataSource);
        Assert.Equal("u", parsed.UserID);
    }

    [Fact]
    public void Oracle_tns_uses_the_alias_as_is()
    {
        var parsed = new Oracle.ManagedDataAccess.Client.OracleConnectionStringBuilder(ConnectionStringFactory.Build(new StoredConnection
        {
            Alias = "x", Kind = DatabaseKind.Oracle, OracleMode = OracleModes.Tns, OracleTarget = "PROD_TNS", User = "u", Password = "p"
        }));

        Assert.Equal("PROD_TNS", parsed.DataSource);
    }

    [Theory]
    [InlineData("", "nombre")]                                 // sin alias
    [InlineData("con espacio", "nombre")]
    [InlineData("a;b", "nombre")]
    public void Validation_rejects_bad_aliases(string alias, string expectedWord)
    {
        var result = ConnectionValidation.Validate(new StoredConnection { Alias = alias, Kind = DatabaseKind.SqlServer, Host = "h", WindowsAuth = true });

        Assert.Contains(expectedWord, result);
    }

    [Fact]
    public void Validation_checks_required_fields_per_engine()
    {
        Assert.Contains("servidor", ConnectionValidation.Validate(new StoredConnection { Alias = "a", Kind = DatabaseKind.SqlServer, User = "u" }));
        Assert.Contains("usuario", ConnectionValidation.Validate(new StoredConnection { Alias = "a", Kind = DatabaseKind.SqlServer, Host = "h" }));
        Assert.Null(ConnectionValidation.Validate(new StoredConnection { Alias = "a", Kind = DatabaseKind.SqlServer, Host = "h", WindowsAuth = true }));
        Assert.Contains("puerto", ConnectionValidation.Validate(new StoredConnection { Alias = "a", Kind = DatabaseKind.SqlServer, Host = "h", Port = 70000, WindowsAuth = true }));
        Assert.Contains("servicio", ConnectionValidation.Validate(new StoredConnection { Alias = "a", Kind = DatabaseKind.Oracle, Host = "h", User = "u" }));
        Assert.Null(ConnectionValidation.Validate(new StoredConnection { Alias = "a", Kind = DatabaseKind.Oracle, OracleMode = OracleModes.Tns, OracleTarget = "TNS", User = "u" }));
    }

    [Fact]
    public void Validation_blocks_tns_descriptor_injection_through_host_or_service()
    {
        var injected = new StoredConnection { Alias = "a", Kind = DatabaseKind.Oracle, Host = "h)(PORT=1", OracleTarget = "S", User = "u" };
        var injectedTarget = new StoredConnection { Alias = "a", Kind = DatabaseKind.Oracle, OracleMode = OracleModes.Sid, Host = "h", OracleTarget = "S)(X=1", User = "u" };

        Assert.NotNull(ConnectionValidation.Validate(injected));
        Assert.NotNull(ConnectionValidation.Validate(injectedTarget));
    }
}
