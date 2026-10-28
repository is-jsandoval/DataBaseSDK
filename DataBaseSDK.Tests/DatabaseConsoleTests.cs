using DataBaseSDK.Core;

namespace DataBaseSDK.Tests;

public class ReadOnlySqlGuardTests
{
    [Theory]
    [InlineData(DatabaseKind.Oracle, "SELECT * FROM dual")]
    [InlineData(DatabaseKind.Oracle, "select sysdate from dual;")]
    [InlineData(DatabaseKind.Oracle, "  \n WITH x AS (SELECT 1 a FROM dual) SELECT a FROM x")]
    [InlineData(DatabaseKind.SqlServer, "SELECT TOP 10 * FROM sys.tables")]
    [InlineData(DatabaseKind.SqlServer, "WITH x AS (SELECT 1 a) SELECT a FROM x;")]
    [InlineData(DatabaseKind.SqlServer, "SELECT * FROM dbo.Clientes WITH (NOLOCK) WHERE Estado = 'A'")]
    public void Plain_selects_are_allowed(DatabaseKind kind, string sql)
    {
        Assert.True(ReadOnlySqlGuard.Validate(kind, sql).Allowed);
    }

    [Theory]
    [InlineData("INSERT INTO t VALUES (1)")]
    [InlineData("UPDATE t SET a = 1")]
    [InlineData("DELETE FROM t")]
    [InlineData("MERGE INTO t USING s ON (1=1) WHEN MATCHED THEN UPDATE SET a=1")]
    [InlineData("DROP TABLE t")]
    [InlineData("TRUNCATE TABLE t")]
    [InlineData("ALTER TABLE t ADD c INT")]
    [InlineData("CREATE TABLE t (a INT)")]
    [InlineData("GRANT SELECT ON t TO u")]
    [InlineData("EXEC sp_who")]
    [InlineData("BEGIN NULL; END;")]
    [InlineData("DECLARE @x INT = 1")]
    [InlineData("COMMIT")]
    public void Write_ddl_and_execution_statements_are_rejected(string sql)
    {
        foreach (var kind in new[] { DatabaseKind.Oracle, DatabaseKind.SqlServer })
            Assert.False(ReadOnlySqlGuard.Validate(kind, sql).Allowed, $"{kind}: {sql}");
    }

    [Theory]
    [InlineData("WITH x AS (SELECT 1 a) DELETE FROM t")]                  // CTE que escribe (SQL Server)
    [InlineData("SELECT * FROM t FOR UPDATE")]                            // bloquea filas (Oracle)
    [InlineData("SELECT * INTO nueva_tabla FROM t")]                      // crea una tabla
    [InlineData("SELECT * FROM t; DROP TABLE t")]                         // segunda sentencia
    [InlineData("SELECT 1; SELECT 2")]
    [InlineData("SELECT * FROM OPENROWSET('SQLNCLI', 'x', 'SELECT 1')")]
    [InlineData("SELECT UTL_HTTP.REQUEST('http://x') FROM dual")]
    [InlineData("SELECT DBMS_LOCK.SLEEP(30) FROM dual")]
    [InlineData("SELECT * FROM t WHERE 1=1 /* ok */ ; DELETE FROM t")]
    public void Sneaky_writes_and_side_effects_are_rejected(string sql)
    {
        foreach (var kind in new[] { DatabaseKind.Oracle, DatabaseKind.SqlServer })
            Assert.False(ReadOnlySqlGuard.Validate(kind, sql).Allowed, $"{kind}: {sql}");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-- solo un comentario")]
    [InlineData("EXPLAIN PLAN FOR SELECT 1 FROM dual")]
    public void Empty_or_non_select_text_is_rejected(string sql)
    {
        Assert.False(ReadOnlySqlGuard.Validate(DatabaseKind.Oracle, sql).Allowed);
    }

    [Fact]
    public void Forbidden_words_inside_literals_comments_and_quoted_identifiers_do_not_count()
    {
        Assert.True(ReadOnlySqlGuard.Validate(DatabaseKind.Oracle, "SELECT 'DELETE FROM t; DROP TABLE x' AS msg FROM dual").Allowed);
        Assert.True(ReadOnlySqlGuard.Validate(DatabaseKind.Oracle, "SELECT 1 FROM dual -- DROP TABLE t").Allowed);
        Assert.True(ReadOnlySqlGuard.Validate(DatabaseKind.Oracle, "SELECT 1 /* DELETE; INSERT */ FROM dual").Allowed);
        Assert.True(ReadOnlySqlGuard.Validate(DatabaseKind.Oracle, "SELECT \"UPDATE\", \"DROP\" FROM t").Allowed);
        Assert.True(ReadOnlySqlGuard.Validate(DatabaseKind.SqlServer, "SELECT [Delete], [INSERT] FROM dbo.t").Allowed);
        Assert.True(ReadOnlySqlGuard.Validate(DatabaseKind.SqlServer, "SELECT 'it''s; DELETE' AS x").Allowed);
    }

    [Fact]
    public void Columns_that_merely_contain_a_forbidden_word_are_fine()
    {
        Assert.True(ReadOnlySqlGuard.Validate(DatabaseKind.Oracle, "SELECT update_date, created_by, delete_flag, insert_ts FROM t").Allowed);
    }

    [Fact]
    public void Oracle_alternative_quoting_is_understood()
    {
        Assert.True(ReadOnlySqlGuard.Validate(DatabaseKind.Oracle, "SELECT q'[it's a DELETE; test]' FROM dual").Allowed);
        Assert.False(ReadOnlySqlGuard.Validate(DatabaseKind.Oracle, "SELECT q'[abc]' FROM dual; DELETE FROM t").Allowed);
    }

    [Fact]
    public void Unterminated_literals_and_comments_are_rejected_instead_of_guessed()
    {
        Assert.False(ReadOnlySqlGuard.Validate(DatabaseKind.Oracle, "SELECT 'abc FROM dual").Allowed);
        Assert.False(ReadOnlySqlGuard.Validate(DatabaseKind.Oracle, "SELECT 1 /* sin cerrar FROM dual").Allowed);
        Assert.False(ReadOnlySqlGuard.Validate(DatabaseKind.SqlServer, "SELECT [col FROM t").Allowed);
    }

    [Fact]
    public void The_trailing_semicolon_is_removed_from_the_text_that_runs()
    {
        var result = ReadOnlySqlGuard.Validate(DatabaseKind.Oracle, "SELECT 1 FROM dual;  ");

        Assert.True(result.Allowed);
        Assert.Equal("SELECT 1 FROM dual", result.Sql.TrimEnd());
    }

    [Fact]
    public void A_quoted_identifier_lets_you_use_a_reserved_word_as_a_column()
    {
        var rejected = ReadOnlySqlGuard.Validate(DatabaseKind.SqlServer, "SELECT call FROM llamadas");
        var accepted = ReadOnlySqlGuard.Validate(DatabaseKind.SqlServer, "SELECT [call] FROM llamadas");

        Assert.False(rejected.Allowed);
        Assert.Contains("CALL", rejected.Reason);
        Assert.True(accepted.Allowed);
    }
}

public class DbConnectionCatalogTests
{
    [Fact]
    public void List_reads_prefixed_environment_variables_and_hides_the_password()
    {
        var oracle = DbConnectionCatalog.OraclePrefix + "UNITTEST";
        var sql = DbConnectionCatalog.SqlServerPrefix + "UNITTEST";

        try
        {
            Environment.SetEnvironmentVariable(oracle, "Data Source=db.banco.local:1521/PROD;User Id=lector;Password=SuperSecreta123");
            Environment.SetEnvironmentVariable(sql, "Server=sql.banco.local,1433;Database=Pagos;User Id=sa_ro;Password=OtraSecreta456;TrustServerCertificate=True");

            var catalog = new DbConnectionCatalog();

            var o = Assert.Single(catalog.List(DatabaseKind.Oracle), c => c.Alias == "UNITTEST");
            var s = Assert.Single(catalog.List(DatabaseKind.SqlServer), c => c.Alias == "UNITTEST");

            Assert.Equal("db.banco.local:1521/PROD · usuario lector", o.Description);
            Assert.Equal("sql.banco.local,1433 · Pagos · usuario sa_ro", s.Description);
            Assert.DoesNotContain("Secreta", o.Description + s.Description);

            // Find no distingue mayúsculas y no mezcla motores.
            Assert.NotNull(catalog.Find(DatabaseKind.Oracle, "unittest"));
            Assert.Equal(DatabaseKind.SqlServer, catalog.Find(DatabaseKind.SqlServer, "UnitTest")!.Kind);
        }
        finally
        {
            Environment.SetEnvironmentVariable(oracle, null);
            Environment.SetEnvironmentVariable(sql, null);
        }
    }

    [Fact]
    public void Windows_authentication_is_described_without_a_user()
    {
        var connection = DbConnectionCatalog.Create(DatabaseKind.SqlServer, "local", "Server=localhost;Database=Pagos;Integrated Security=True");

        Assert.Equal("localhost · Pagos · autenticación de Windows", connection.Description);
    }

    [Fact]
    public void An_invalid_connection_string_does_not_break_the_list()
    {
        var connection = DbConnectionCatalog.Create(DatabaseKind.Oracle, "mala", "esto no es una connection string");

        Assert.Equal("(connection string inválida)", connection.Description);
    }

    [Fact]
    public void Find_returns_null_for_unknown_or_blank_aliases()
    {
        var catalog = new DbConnectionCatalog();

        Assert.Null(catalog.Find(DatabaseKind.Oracle, "no-existe-xyz"));
        Assert.Null(catalog.Find(DatabaseKind.Oracle, null));
        Assert.Null(catalog.Find(DatabaseKind.Oracle, "  "));
    }
}

public class QueryResultCsvTests
{
    [Fact]
    public void Write_escapes_commas_quotes_and_newlines_and_uses_invariant_formats()
    {
        var result = new QueryResult
        {
            Columns = [new QueryColumn("ID", "NUMBER", true), new QueryColumn("TEXTO", "VARCHAR2", false), new QueryColumn("FECHA", "DATE", false)],
            Rows =
            [
                [1234.5m, "Hola, \"mundo\"", new DateTime(2026, 10, 26, 8, 30, 0)],
                [null, "línea1\nlínea2", null]
            ]
        };

        var lines = QueryResultCsv.Write(result).Split("\r\n", StringSplitOptions.None);

        Assert.Equal("ID,TEXTO,FECHA", lines[0]);
        Assert.Equal("1234.5,\"Hola, \"\"mundo\"\"\",2026-10-26 08:30:00", lines[1]);
        Assert.Equal(",\"línea1\nlínea2\",", lines[2]); // el salto de línea queda DENTRO de las comillas
    }

    [Fact]
    public void QueryOptions_clamps_out_of_range_values()
    {
        var options = new QueryOptions { MaxRows = 999_999, TimeoutSeconds = 0 };

        Assert.Equal(QueryOptions.HardMaxRows, options.MaxRows);
        Assert.Equal(1, options.TimeoutSeconds);
        Assert.Equal(1, new QueryOptions { MaxRows = -5 }.MaxRows);
    }
}
