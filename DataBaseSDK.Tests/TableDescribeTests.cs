using System.Data;
using DataBaseSDK.Core;

namespace DataBaseSDK.Tests;

public class TableDescribeTests
{
    private static readonly DbConnectionInfo SqlInfo = new("x", DatabaseKind.SqlServer, "Server=x", "x");
    private static readonly DbConnectionInfo OraInfo = new("x", DatabaseKind.Oracle, "Data Source=x", "x");

    private static DataTable Rows(int columns, params object?[][] rows)
    {
        var table = new DataTable();

        for (var i = 0; i < columns; i++)
            table.Columns.Add($"c{i}", typeof(object));

        foreach (var row in rows)
            table.Rows.Add(row.Select(v => v ?? DBNull.Value).ToArray());

        return table;
    }

    [Fact]
    public async Task SqlServer_describes_columns_flags_and_primary_key_with_parameters_only()
    {
        var explorer = new TestSqlServerExplorer
        {
            Connection =
            {
                Table = Rows(9,
                    ["dbo", "Clientes", "ID", "int", 0, 1, 0, 0, 1],
                    ["dbo", "Clientes", "NOMBRE", "nvarchar", 1, 0, 0, 0, 0],
                    ["dbo", "Clientes", "TOTAL", "int", 1, 0, 1, 0, 0],
                    ["dbo", "Clientes", "FIRMA", "varbinary", 1, 0, 0, 0, 0])
            }
        };

        var meta = await explorer.DescribeTableAsync(SqlInfo, "ventas", "dbo'; DROP TABLE x; --", "Clientes");

        Assert.NotNull(meta);
        Assert.Equal("dbo", meta!.Schema);
        Assert.Equal("Clientes", meta.Name);
        Assert.Equal(["ID"], meta.PrimaryKey.Select(c => c.Name));

        var byName = meta.Columns.ToDictionary(c => c.Name);
        Assert.True(byName["ID"].IsIdentity);
        Assert.False(byName["ID"].IsEditable);
        Assert.True(byName["NOMBRE"].IsEditable);
        Assert.True(byName["NOMBRE"].Nullable);
        Assert.False(byName["TOTAL"].IsEditable);     // calculada
        Assert.False(byName["FIRMA"].IsEditable);     // varbinary

        Assert.Equal(["ventas"], explorer.Connection.ChangedDatabases);
        var sql = Assert.Single(explorer.Connection.ExecutedSql);
        Assert.DoesNotContain("DROP", sql);
        Assert.Contains("'U'", sql);                  // solo tablas de usuario
        Assert.Equal("dbo'; DROP TABLE x; --", explorer.Connection.ExecutedParameters[0]["p_schema"]);
        Assert.Equal("Clientes", explorer.Connection.ExecutedParameters[0]["p_name"]);
    }

    [Fact]
    public async Task SqlServer_returns_null_when_the_name_is_not_a_table()
    {
        var explorer = new TestSqlServerExplorer { Connection = { Table = Rows(9) } };

        Assert.Null(await explorer.DescribeTableAsync(SqlInfo, null, null, "vista"));
        Assert.Null(explorer.Connection.ExecutedParameters[0]["p_schema"]);   // sin esquema: lo resuelve el motor como un SELECT
    }

    [Fact]
    public async Task Oracle_uses_the_session_schema_when_none_is_given_and_marks_identity_columns()
    {
        var explorer = new TestOracleExplorer();
        explorer.Connection.TableFor = sql =>
            sql.Contains("SYS_CONTEXT") ? Rows(1, ["BGUSER"]) :
            sql.Contains("all_tab_identity_cols") ? Rows(1, ["ID"]) :
            Rows(8,
                ["BGUSER", "CLIENTES", "ID", "NUMBER", "N", 0, "NO", 1],
                ["BGUSER", "CLIENTES", "NOMBRE", "VARCHAR2", "Y", null, "NO", 0],
                ["BGUSER", "CLIENTES", "MONTO", "NUMBER", "Y", 2, "NO", 0],
                ["BGUSER", "CLIENTES", "CALC", "NUMBER", "Y", null, "YES", 0],
                ["BGUSER", "CLIENTES", "DOC", "CLOB", "Y", null, "NO", 0]);

        var meta = await explorer.DescribeTableAsync(OraInfo, null, null, "CLIENTES");

        Assert.Equal("BGUSER", meta!.Schema);
        var byName = meta.Columns.ToDictionary(c => c.Name);
        Assert.True(byName["ID"].IsPrimaryKey);
        Assert.True(byName["ID"].IsIdentity);
        Assert.Equal(ColumnCategory.Integer, byName["ID"].Category);
        Assert.Equal(ColumnCategory.Decimal, byName["MONTO"].Category);
        Assert.True(byName["NOMBRE"].IsEditable);
        Assert.False(byName["CALC"].IsEditable);       // columna virtual
        Assert.False(byName["DOC"].IsEditable);        // CLOB

        var withOwner = explorer.Connection.ExecutedParameters.Where(p => p.ContainsKey("p_owner")).ToList();
        Assert.All(withOwner, p => Assert.Equal("BGUSER", p["p_owner"]));
        Assert.All(withOwner, p => Assert.Equal("CLIENTES", p["p_name"]));
    }

    [Fact]
    public async Task Oracle_11g_has_no_identity_view_and_that_is_not_an_error()
    {
        var explorer = new TestOracleExplorer();
        explorer.Connection.FailOn = sql => sql.Contains("all_tab_identity_cols") ? new FakeDbException("ORA-00942: table or view does not exist") : null;
        explorer.Connection.TableFor = _ => Rows(8, ["APP", "T", "ID", "NUMBER", "N", 0, "NO", 1]);

        var meta = await explorer.DescribeTableAsync(OraInfo, null, "APP", "T");

        Assert.NotNull(meta);
        Assert.False(meta!.Columns.Single().IsIdentity);
    }

    [Fact]
    public async Task Oracle_returns_null_when_the_table_does_not_exist()
    {
        var explorer = new TestOracleExplorer { Connection = { Table = Rows(8) } };

        Assert.Null(await explorer.DescribeTableAsync(OraInfo, null, "APP", "NO_EXISTE"));
    }
}

public class WritePermissionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "azdo-write-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private DbConnectionStore NewStore() => new(new DpapiSecretProtector(), Path.Combine(_directory, "db-connections.json"));

    private static StoredConnection Sample(string alias, bool allowWrite) => new()
    {
        Alias = alias, Kind = DatabaseKind.SqlServer, Host = "h", User = "u", Password = "p", AllowWrite = allowWrite
    };

    [Fact]
    public void Writing_is_off_by_default_and_the_flag_survives_a_save_and_a_password_keeping_edit()
    {
        var store = NewStore();
        store.Save(new StoredConnection { Alias = "A", Kind = DatabaseKind.SqlServer, Host = "h", User = "u", Password = "p" });
        Assert.False(store.Get(DatabaseKind.SqlServer, "A")!.AllowWrite);

        store.Save(Sample("A", allowWrite: true));
        Assert.True(NewStore().Get(DatabaseKind.SqlServer, "A")!.AllowWrite);     // persistida en disco

        var edit = Sample("A", allowWrite: false);
        edit.Password = null;                                                      // editar sin reescribir la clave
        store.Save(edit);
        Assert.False(store.Get(DatabaseKind.SqlServer, "A")!.AllowWrite);
        Assert.Equal("p", store.Get(DatabaseKind.SqlServer, "A")!.Password);
    }

    [Fact]
    public void The_catalog_exposes_the_flag_for_saved_connections()
    {
        var store = NewStore();
        store.Save(Sample("EDITABLE", allowWrite: true));
        store.Save(Sample("LECTURA", allowWrite: false));

        var list = new DbConnectionCatalog(store).List(DatabaseKind.SqlServer);

        Assert.True(list.Single(c => c.Alias == "EDITABLE").AllowWrite);
        Assert.False(list.Single(c => c.Alias == "LECTURA").AllowWrite);
    }

    [Theory]
    [InlineData("WTEST", true)]
    [InlineData("otra,wtest;x", true)]      // sin distinguir mayúsculas; coma o punto y coma
    [InlineData("*", true)]
    [InlineData("OTRA", false)]
    [InlineData("WTEST2", false)]           // no es una coincidencia parcial
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Env_connections_allow_writes_only_when_listed_in_DBSDK_WRITE(string? setting, bool expected)
    {
        var variable = DbConnectionCatalog.SqlServerPrefix + "WTEST";
        var previous = Environment.GetEnvironmentVariable(DbConnectionCatalog.WriteEnvVariable);

        try
        {
            Environment.SetEnvironmentVariable(variable, "Server=x;Integrated Security=True");
            Environment.SetEnvironmentVariable(DbConnectionCatalog.WriteEnvVariable, setting);

            var connection = new DbConnectionCatalog().List(DatabaseKind.SqlServer).Single(c => c.Alias == "WTEST");

            Assert.Equal(expected, connection.AllowWrite);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
            Environment.SetEnvironmentVariable(DbConnectionCatalog.WriteEnvVariable, previous);
        }
    }
}
