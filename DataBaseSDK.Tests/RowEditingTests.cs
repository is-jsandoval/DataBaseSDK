using System.Data;
using System.Data.Common;
using DataBaseSDK.Core;

namespace DataBaseSDK.Tests;

internal class FakeDbException : DbException
{
    public FakeDbException(string message) : base(message)
    {
    }
}

internal class MemoryAuditLog : IEditAuditLog
{
    public List<EditAuditEntry> Entries { get; } = [];

    /// <summary>Simula un disco que no deja escribir.</summary>
    public bool Fail { get; set; }

    public void Write(EditAuditEntry entry)
    {
        if (Fail)
            throw new IOException("disco lleno");

        Entries.Add(entry);
    }
}

internal class FakeDescriber : ITableDescriber
{
    public TableMetadata? Metadata { get; set; }

    public Exception? Failure { get; set; }

    public List<(string? Database, string? Schema, string Table)> Calls { get; } = [];

    public Task<TableMetadata?> DescribeTableAsync(DbConnectionInfo connection, string? database, string? schema, string table, CancellationToken cancellationToken = default)
    {
        Calls.Add((database, schema, table));

        if (Failure != null)
            throw Failure;

        return Task.FromResult(Metadata);
    }
}

internal class TestSqlServerEditor : SqlServerRowEditor
{
    public TestSqlServerEditor(ITableDescriber describer, IEditAuditLog audit) : base(describer, audit)
    {
    }

    public FakeConnection Connection { get; } = new();

    protected override DbConnection CreateConnection(string connectionString) => Connection;
}

internal class TestOracleEditor : OracleRowEditor
{
    public TestOracleEditor(ITableDescriber describer, IEditAuditLog audit) : base(describer, audit)
    {
    }

    public FakeConnection Connection { get; } = new();

    protected override DbConnection CreateConnection(string connectionString) => Connection;
}

public class SingleTableAnalyzerTests
{
    [Theory]
    [InlineData("SELECT * FROM dbo.Clientes", "dbo.Clientes")]
    [InlineData("select id, nombre from clientes c where id > 3 order by id", "clientes")]
    [InlineData("SELECT TOP 10 * FROM [dbo].[Mis Clientes] WITH (NOLOCK) WHERE x = 'a join b'", "dbo.Mis Clientes")]
    [InlineData("SELECT * FROM \"BGUSER\".\"Clientes\" t WHERE ROWNUM <= 100", "BGUSER.Clientes")]
    [InlineData("SELECT * FROM clientes AS c;", "clientes")]
    [InlineData("SELECT * FROM otra.base.dbo.t", "otra.base.dbo.t")]
    [InlineData("SELECT a FROM t WHERE a IN (SELECT MAX(b) FROM u)", "t")]
    [InlineData("-- comentario con FROM x JOIN y\nSELECT * FROM t /* GROUP BY */", "t")]
    public void A_simple_single_table_select_is_recognized(string sql, string expected)
    {
        var result = SingleTableAnalyzer.Analyze(sql);

        Assert.Null(result.Reason);
        Assert.NotNull(result.Table);
        Assert.Equal(expected, string.Join(".", result.Table!.Parts));
    }

    [Theory]
    [InlineData("SELECT * FROM a JOIN b ON a.id = b.id")]
    [InlineData("SELECT * FROM a INNER JOIN b ON 1 = 1")]
    [InlineData("SELECT id FROM a UNION SELECT id FROM b")]
    [InlineData("SELECT id FROM a MINUS SELECT id FROM b")]
    [InlineData("SELECT region, COUNT(*) FROM ventas GROUP BY region")]
    [InlineData("SELECT COUNT(*) FROM ventas")]
    [InlineData("SELECT DISTINCT region FROM ventas")]
    [InlineData("SELECT * FROM (SELECT * FROM a) x")]
    [InlineData("SELECT * FROM a, b")]
    [InlineData("SELECT * FROM a x, b y")]
    [InlineData("WITH c AS (SELECT 1 AS n) SELECT * FROM c")]
    [InlineData("SELECT 1")]
    [InlineData("SELECT * FROM t@remoto")]
    [InlineData("SELECT * FROM @variable")]
    [InlineData("SELECT * FROM db..t")]
    [InlineData("SELECT * FROM t SAMPLE (10)")]
    [InlineData("DELETE FROM t")]
    [InlineData("")]
    public void Anything_that_is_not_one_plain_table_is_refused_with_a_reason(string sql)
    {
        var result = SingleTableAnalyzer.Analyze(sql);

        Assert.Null(result.Table);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
    }

    [Fact]
    public void Quoting_is_remembered_so_oracle_can_tell_a_lowercase_name_from_an_unquoted_one()
    {
        var table = SingleTableAnalyzer.Analyze("SELECT * FROM \"app\".clientes").Table!;

        Assert.Equal(["app", "clientes"], table.Parts);
        Assert.Equal([true, false], table.Quoted);
    }
}

public class RowEditorTests
{
    private static readonly DbConnectionInfo Writable = new("dev", DatabaseKind.SqlServer, "x", "dev") { AllowWrite = true };
    private static readonly DbConnectionInfo ReadOnly = new("dev", DatabaseKind.SqlServer, "x", "dev");
    private static readonly DbConnectionInfo OracleWritable = new("dev", DatabaseKind.Oracle, "x", "dev") { AllowWrite = true };

    private static TableMetadata Clientes() => new("dbo", "Clientes",
    [
        new TableColumn("ID", "int", ColumnCategory.Integer, false, true, true, false),
        new TableColumn("NOMBRE", "nvarchar", ColumnCategory.String, true, false, false, false),
        new TableColumn("MONTO", "decimal", ColumnCategory.Decimal, false, false, false, false),
        new TableColumn("CREADO", "datetime2", ColumnCategory.DateTime, true, false, false, false),
        new TableColumn("FIRMA", "varbinary", ColumnCategory.Unsupported, true, false, false, false),
        new TableColumn("TOTAL", "int", ColumnCategory.Integer, true, false, false, true)
    ]);

    private static IReadOnlyList<QueryColumn> Columns(params string[] names) => names.Select(n => new QueryColumn(n, "x", false)).ToList();

    private static (TestSqlServerEditor Editor, FakeDescriber Describer, MemoryAuditLog Audit) NewEditor(TableMetadata? metadata = null)
    {
        var describer = new FakeDescriber { Metadata = metadata ?? Clientes() };
        var audit = new MemoryAuditLog();

        return (new TestSqlServerEditor(describer, audit), describer, audit);
    }

    // Lo que la base devuelve en el SELECT ... FOR UPDATE: una fila con las columnas pedidas, en orden.
    private static DataTable Current(params object?[] values)
    {
        var table = new DataTable();

        for (var i = 0; i < values.Length; i++)
            table.Columns.Add($"c{i}", typeof(object));

        table.Rows.Add(values.Select(v => v ?? DBNull.Value).ToArray());

        return table;
    }

    private static RowChange Change(string id, params (string Column, string? Original, string? Value)[] cells) => new()
    {
        PrimaryKey = new Dictionary<string, string?> { ["ID"] = id },
        Set = cells.ToDictionary(c => c.Column, c => new CellEdit { Original = c.Original, Value = c.Value })
    };

    private static RowEditRequest Request(params RowChange[] changes) => new() { Schema = "dbo", Table = "Clientes", Changes = [.. changes] };

    // ---------- ¿se puede editar? ----------

    [Fact]
    public async Task Prepare_says_nothing_when_the_connection_does_not_allow_writes()
    {
        var (editor, describer, _) = NewEditor();

        var info = await editor.PrepareAsync(ReadOnly, new QueryOptions(), "SELECT * FROM dbo.Clientes", Columns("ID", "NOMBRE"));

        Assert.False(info.Editable);
        Assert.Null(info.Reason);
        Assert.Empty(describer.Calls);   // ni siquiera mira la estructura de la tabla
    }

    [Fact]
    public async Task Prepare_marks_only_the_plain_columns_as_editable()
    {
        var (editor, _, _) = NewEditor();

        var info = await editor.PrepareAsync(Writable, new QueryOptions(), "SELECT ID, NOMBRE, MONTO, FIRMA, TOTAL, CREADO, EXTRA FROM dbo.Clientes",
            Columns("ID", "NOMBRE", "MONTO", "FIRMA", "TOTAL", "CREADO", "EXTRA"));

        Assert.True(info.Editable);
        Assert.Equal("dbo", info.Schema);
        Assert.Equal("Clientes", info.Table);
        Assert.Equal(["ID"], info.PrimaryKey);

        var byName = info.Columns.ToDictionary(c => c.Name);
        Assert.False(byName["ID"].Editable);       // clave primaria
        Assert.True(byName["NOMBRE"].Editable);
        Assert.True(byName["NOMBRE"].Nullable);
        Assert.True(byName["MONTO"].Editable);
        Assert.False(byName["MONTO"].Nullable);
        Assert.True(byName["CREADO"].Editable);
        Assert.False(byName["FIRMA"].Editable);    // varbinary
        Assert.False(byName["TOTAL"].Editable);    // calculada
        Assert.False(byName["EXTRA"].Editable);    // no es una columna de la tabla
        Assert.All(info.Columns.Where(c => !c.Editable), c => Assert.False(string.IsNullOrEmpty(c.Reason)));
    }

    [Fact]
    public async Task Prepare_refuses_when_the_primary_key_is_not_in_the_select()
    {
        var (editor, _, _) = NewEditor();

        var info = await editor.PrepareAsync(Writable, new QueryOptions(), "SELECT NOMBRE FROM dbo.Clientes", Columns("NOMBRE"));

        Assert.False(info.Editable);
        Assert.Contains("clave primaria", info.Reason);
        Assert.Contains("ID", info.Reason);
    }

    [Fact]
    public async Task Prepare_refuses_views_missing_tables_tables_without_a_key_and_repeated_columns()
    {
        var (editor, describer, _) = NewEditor();

        describer.Metadata = null;
        var view = await editor.PrepareAsync(Writable, new QueryOptions(), "SELECT * FROM dbo.vista", Columns("ID"));
        Assert.False(view.Editable);
        Assert.Contains("no es una tabla", view.Reason);

        describer.Metadata = new TableMetadata("dbo", "Sin", [new TableColumn("A", "int", ColumnCategory.Integer, true, false, false, false)]);
        var noKey = await editor.PrepareAsync(Writable, new QueryOptions(), "SELECT * FROM dbo.Sin", Columns("A"));
        Assert.False(noKey.Editable);
        Assert.Contains("no tiene clave primaria", noKey.Reason);

        describer.Metadata = Clientes();
        var repeated = await editor.PrepareAsync(Writable, new QueryOptions(), "SELECT * FROM dbo.Clientes", Columns("ID", "NOMBRE", "nombre"));
        Assert.False(repeated.Editable);
        Assert.Contains("repite", repeated.Reason);
    }

    [Fact]
    public async Task Prepare_reports_a_failing_catalog_instead_of_throwing()
    {
        var (editor, describer, _) = NewEditor();
        describer.Failure = new FakeDbException("sin permiso");

        var info = await editor.PrepareAsync(Writable, new QueryOptions(), "SELECT * FROM dbo.Clientes", Columns("ID", "NOMBRE"));

        Assert.False(info.Editable);
        Assert.Contains("sin permiso", info.Reason);
    }

    [Fact]
    public async Task Prepare_resolves_sql_server_names_from_the_from_clause_and_the_selected_database()
    {
        var (editor, describer, _) = NewEditor();
        var context = new QueryOptions { Database = "ventas" };

        await editor.PrepareAsync(Writable, context, "SELECT * FROM Clientes", Columns("ID", "NOMBRE"));
        await editor.PrepareAsync(Writable, context, "SELECT * FROM [sales].[Clientes]", Columns("ID", "NOMBRE"));
        await editor.PrepareAsync(Writable, context, "SELECT * FROM otra.sales.Clientes", Columns("ID", "NOMBRE"));

        Assert.Equal(
        [
            ("ventas", (string?)null, "Clientes"),
            ("ventas", "sales", "Clientes"),
            ("otra", "sales", "Clientes")
        ], describer.Calls);
    }

    [Fact]
    public async Task Prepare_resolves_oracle_names_unquoted_to_uppercase_and_quoted_as_written()
    {
        var describer = new FakeDescriber { Metadata = Clientes() };
        var editor = new TestOracleEditor(describer, new MemoryAuditLog());
        var context = new QueryOptions { Schema = "BGUSER" };

        await editor.PrepareAsync(OracleWritable, context, "SELECT * FROM clientes", Columns("ID", "NOMBRE"));
        await editor.PrepareAsync(OracleWritable, new QueryOptions(), "SELECT * FROM clientes", Columns("ID", "NOMBRE"));
        await editor.PrepareAsync(OracleWritable, context, "SELECT * FROM app.clientes", Columns("ID", "NOMBRE"));
        await editor.PrepareAsync(OracleWritable, context, "SELECT * FROM \"app\".\"Clientes\"", Columns("ID", "NOMBRE"));
        var tooLong = await editor.PrepareAsync(OracleWritable, context, "SELECT * FROM a.b.c", Columns("ID"));

        Assert.Equal(
        [
            ((string?)null, "BGUSER", "CLIENTES"),
            (null, null, "CLIENTES"),                 // sin esquema: lo resuelve el catálogo con el del usuario
            (null, "APP", "CLIENTES"),
            (null, "app", "Clientes")
        ], describer.Calls);
        Assert.False(tooLong.Editable);
    }

    // ---------- guardar ----------

    [Fact]
    public async Task Apply_locks_verifies_updates_by_primary_key_and_commits_with_audit_trail()
    {
        var (editor, _, audit) = NewEditor();
        editor.Connection.TableFor = _ => Current("Ana");

        var result = await editor.ApplyAsync(Writable, Request(Change("7", ("NOMBRE", "Ana", "Ana María"))));

        Assert.Equal(1, result.RowsAffected);
        Assert.False(result.DryRun);

        var connection = editor.Connection;
        Assert.True(connection.Transaction!.Committed);
        Assert.False(connection.Transaction.RolledBack);

        // 1) bloquea y lee la fila  2) UPDATE solo de la columna cambiada, por clave primaria, con parámetros.
        Assert.Contains("SELECT [NOMBRE] FROM [dbo].[Clientes] WITH (UPDLOCK, ROWLOCK) WHERE [ID] = @k0", connection.ExecutedSql);

        var update = Assert.Single(connection.NonQueries, n => n.Sql.StartsWith("UPDATE", StringComparison.Ordinal));
        Assert.Equal("UPDATE [dbo].[Clientes] SET [NOMBRE] = @v0 WHERE [ID] = @k0", update.Sql);
        Assert.Equal("Ana María", update.Parameters["v0"]);
        Assert.Equal(7L, update.Parameters["k0"]);

        // La bitácora se escribe ANTES del commit y se confirma después.
        Assert.Equal(["aplicando", "confirmado"], audit.Entries.Select(e => e.Outcome));
        var entry = audit.Entries[0];
        Assert.Equal("dev", entry.Connection);
        Assert.Equal("Clientes", entry.Table);
        var change = Assert.Single(entry.Changes);
        Assert.Equal("7", change.PrimaryKey["ID"]);
        Assert.Equal("Ana", change.From["NOMBRE"]);
        Assert.Equal("Ana María", change.To["NOMBRE"]);
    }

    [Fact]
    public async Task Apply_never_puts_client_text_in_the_sql_only_catalog_names()
    {
        var (editor, _, _) = NewEditor();
        editor.Connection.TableFor = _ => Current("x");

        // El nombre de tabla del cliente solo sirve para buscar en el catálogo; el SQL usa los nombres exactos del catálogo.
        var request = Request(Change("1", ("NOMBRE", "x", "'; DROP TABLE Clientes; --")));
        request.Table = "clientes]; DROP TABLE x; --";

        await editor.ApplyAsync(Writable, request);

        Assert.All(editor.Connection.ExecutedSql, sql => Assert.DoesNotContain("DROP", sql));
        Assert.Equal("'; DROP TABLE Clientes; --", editor.Connection.NonQueries.Single(n => n.Sql.StartsWith("UPDATE", StringComparison.Ordinal)).Parameters["v0"]);
    }

    [Fact]
    public async Task Apply_sets_a_lock_timeout_first_and_types_the_parameters()
    {
        var (editor, _, _) = NewEditor();
        editor.Connection.TableFor = _ => Current("Ana", 10.50m, new DateTime(2026, 1, 2));   // decimal(18,2): conserva la escala, como la grilla

        await editor.ApplyAsync(Writable, Request(Change("7",
            ("NOMBRE", "Ana", null),
            ("MONTO", "10.50", "99.90"),
            ("CREADO", "2026-01-02 00:00:00", "2026-10-26 14:30:00"))));

        Assert.Equal("SET LOCK_TIMEOUT 5000", editor.Connection.NonQueries[0].Sql);

        var update = editor.Connection.NonQueries.Single(n => n.Sql.StartsWith("UPDATE", StringComparison.Ordinal));
        Assert.Null(update.Parameters["v0"]);                                  // NULL
        Assert.Equal(99.90m, update.Parameters["v1"]);
        Assert.Equal(new DateTime(2026, 10, 26, 14, 30, 0), update.Parameters["v2"]);
        Assert.Equal([DbType.String, DbType.Decimal, DbType.DateTime2, DbType.Int64], update.Types);
    }

    [Fact]
    public async Task Apply_updates_several_rows_in_one_transaction()
    {
        var (editor, _, audit) = NewEditor();
        editor.Connection.TableFor = sql => Current(sql.Contains("[NOMBRE]") ? "Ana" : "?");

        var result = await editor.ApplyAsync(Writable, Request(
            Change("1", ("NOMBRE", "Ana", "A1")),
            Change("2", ("NOMBRE", "Ana", "A2"))));

        Assert.Equal(2, result.RowsAffected);
        Assert.Equal(2, editor.Connection.NonQueries.Count(n => n.Sql.StartsWith("UPDATE", StringComparison.Ordinal)));
        Assert.True(editor.Connection.Transaction!.Committed);
        Assert.Equal(2, audit.Entries[0].Changes.Count);
    }

    [Fact]
    public async Task A_dry_run_does_everything_and_rolls_back()
    {
        var (editor, _, audit) = NewEditor();
        editor.Connection.TableFor = _ => Current("Ana");

        var request = Request(Change("7", ("NOMBRE", "Ana", "Otra")));
        request.DryRun = true;

        var result = await editor.ApplyAsync(Writable, request);

        Assert.True(result.DryRun);
        Assert.Single(result.Statements);
        Assert.Contains(editor.Connection.NonQueries, n => n.Sql.StartsWith("UPDATE", StringComparison.Ordinal));
        Assert.False(editor.Connection.Transaction!.Committed);
        Assert.True(editor.Connection.Transaction.RolledBack);
        Assert.Equal(["prueba (revertida)"], audit.Entries.Select(e => e.Outcome));
    }

    [Fact]
    public async Task Apply_refuses_to_overwrite_a_row_that_changed_since_it_was_queried()
    {
        var (editor, _, audit) = NewEditor();
        editor.Connection.TableFor = _ => Current("Beto");   // otra persona la cambió

        var ex = await Assert.ThrowsAsync<RowEditException>(() =>
            editor.ApplyAsync(Writable, Request(Change("7", ("NOMBRE", "Ana", "Ana María")))));

        Assert.Equal(RowEditError.Conflict, ex.Code);
        Assert.Contains("Beto", ex.Message);
        Assert.DoesNotContain(editor.Connection.NonQueries, n => n.Sql.StartsWith("UPDATE", StringComparison.Ordinal));
        Assert.True(editor.Connection.Transaction!.RolledBack);
        Assert.False(editor.Connection.Transaction.Committed);
        Assert.Equal("rechazado", audit.Entries.Last().Outcome);
    }

    [Fact]
    public async Task A_conflict_in_a_later_row_rolls_back_the_earlier_ones_too()
    {
        var (editor, _, _) = NewEditor();
        var reads = 0;
        editor.Connection.TableFor = _ => Current(++reads == 1 ? "Ana" : "Cambiada");

        var ex = await Assert.ThrowsAsync<RowEditException>(() => editor.ApplyAsync(Writable, Request(
            Change("1", ("NOMBRE", "Ana", "A1")),
            Change("2", ("NOMBRE", "Ana", "A2")))));

        Assert.Equal(RowEditError.Conflict, ex.Code);
        Assert.Single(editor.Connection.NonQueries, n => n.Sql.StartsWith("UPDATE", StringComparison.Ordinal));   // la 1ª se aplicó dentro de la transacción...
        Assert.True(editor.Connection.Transaction!.RolledBack);                                                    // ...y se revierte
        Assert.False(editor.Connection.Transaction.Committed);
    }

    [Fact]
    public async Task Apply_compares_nulls_dates_and_numbers_with_the_same_text_the_grid_shows()
    {
        var (editor, _, _) = NewEditor();
        editor.Connection.TableFor = _ => Current(DBNull.Value, 12.5m, new DateTime(2026, 3, 4, 5, 6, 7));

        await editor.ApplyAsync(Writable, Request(Change("1",
            ("NOMBRE", null, "x"),
            ("MONTO", "12.5", "13"),
            ("CREADO", "2026-03-04 05:06:07", null))));

        Assert.True(editor.Connection.Transaction!.Committed);
    }

    [Fact]
    public async Task Apply_fails_when_the_row_no_longer_exists()
    {
        var (editor, _, _) = NewEditor();
        editor.Connection.TableFor = _ => new DataTable { Columns = { { "c0", typeof(object) } } };   // sin filas

        var ex = await Assert.ThrowsAsync<RowEditException>(() =>
            editor.ApplyAsync(Writable, Request(Change("7", ("NOMBRE", "Ana", "x")))));

        Assert.Equal(RowEditError.NotFound, ex.Code);
        Assert.True(editor.Connection.Transaction!.RolledBack);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task Apply_requires_the_update_to_affect_exactly_one_row(int affected)
    {
        var (editor, _, _) = NewEditor();
        editor.Connection.TableFor = _ => Current("Ana");
        editor.Connection.RowsAffected = affected;

        var ex = await Assert.ThrowsAsync<RowEditException>(() =>
            editor.ApplyAsync(Writable, Request(Change("7", ("NOMBRE", "Ana", "x")))));

        Assert.Equal(RowEditError.RowCount, ex.Code);
        Assert.True(editor.Connection.Transaction!.RolledBack);
        Assert.False(editor.Connection.Transaction.Committed);
    }

    [Fact]
    public async Task Apply_does_not_save_anything_when_the_audit_log_cannot_be_written()
    {
        var (editor, _, audit) = NewEditor();
        editor.Connection.TableFor = _ => Current("Ana");
        audit.Fail = true;

        var ex = await Assert.ThrowsAsync<RowEditException>(() =>
            editor.ApplyAsync(Writable, Request(Change("7", ("NOMBRE", "Ana", "x")))));

        Assert.Equal(RowEditError.Audit, ex.Code);
        Assert.Contains("disco lleno", ex.Message);
        Assert.False(editor.Connection.Transaction!.Committed);
        Assert.True(editor.Connection.Transaction.RolledBack);
    }

    [Fact]
    public async Task Database_failures_roll_back_and_come_back_as_a_row_edit_error()
    {
        var (editor, _, audit) = NewEditor();
        editor.Connection.TableFor = _ => Current("Ana");
        editor.Connection.FailOn = sql => sql.StartsWith("UPDATE", StringComparison.Ordinal) ? new FakeDbException("Violation of UNIQUE KEY constraint") : null;

        var ex = await Assert.ThrowsAsync<RowEditException>(() =>
            editor.ApplyAsync(Writable, Request(Change("7", ("NOMBRE", "Ana", "x")))));

        Assert.Equal(RowEditError.Database, ex.Code);
        Assert.Contains("UNIQUE KEY", ex.Message);
        Assert.True(editor.Connection.Transaction!.RolledBack);
        Assert.Equal("fallido", audit.Entries.Last().Outcome);
    }

    [Fact]
    public async Task Apply_needs_a_connection_that_allows_writes_and_never_opens_it_otherwise()
    {
        var (editor, describer, _) = NewEditor();

        var ex = await Assert.ThrowsAsync<RowEditException>(() =>
            editor.ApplyAsync(ReadOnly, Request(Change("7", ("NOMBRE", "Ana", "x")))));

        Assert.Equal(RowEditError.WriteNotAllowed, ex.Code);
        Assert.Equal(0, editor.Connection.Opened);
        Assert.Empty(describer.Calls);
    }

    [Fact]
    public async Task Apply_refuses_what_is_not_a_table()
    {
        var (editor, describer, _) = NewEditor();
        describer.Metadata = null;

        var ex = await Assert.ThrowsAsync<RowEditException>(() =>
            editor.ApplyAsync(Writable, Request(Change("7", ("NOMBRE", "Ana", "x")))));

        Assert.Equal(RowEditError.NotEditable, ex.Code);
        Assert.Equal(0, editor.Connection.Opened);
    }

    [Fact]
    public async Task Apply_switches_to_the_requested_database()
    {
        var (editor, describer, _) = NewEditor();
        editor.Connection.TableFor = _ => Current("Ana");
        var request = Request(Change("7", ("NOMBRE", "Ana", "x")));
        request.Database = "ventas";

        await editor.ApplyAsync(Writable, request);

        Assert.Equal(["ventas"], editor.Connection.ChangedDatabases);
        Assert.Equal("ventas", describer.Calls.Single().Database);
    }

    public static IEnumerable<object[]> InvalidRequests()
    {
        // (descripción, cambio, mensaje esperado)
        yield return ["clave primaria", Change("7", ("ID", "7", "8")), "no se puede editar"];
        yield return ["calculada", Change("7", ("TOTAL", "1", "2")), "no se puede editar"];
        yield return ["binaria", Change("7", ("FIRMA", "0xCA", "0xFE")), "no se puede editar"];
        yield return ["columna inexistente", Change("7", ("NOEXISTE", "a", "b")), "no existe"];
        yield return ["NULL en NOT NULL", Change("7", ("MONTO", "10", null)), "no admite NULL"];
        yield return ["texto en número", Change("7", ("MONTO", "10", "mucho")), "no es válido"];
        yield return ["fecha mal escrita", Change("7", ("CREADO", "2026-01-01", "26/10/2026")), "fecha"];
        yield return ["valor recortado", Change("7", ("NOMBRE", new string('x', 4000) + "…", "corto")), "recortado"];
        yield return ["clave no numérica", Change("siete", ("NOMBRE", "a", "b")), "no es válido"];
        yield return ["sin cambios", Change("7", ("NOMBRE", "Ana", "Ana")), "No hay cambios"];
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Apply_rejects_invalid_requests_before_touching_the_database(string _, RowChange change, string expectedMessage)
    {
        var (editor, _2, _3) = NewEditor();

        var ex = await Assert.ThrowsAsync<RowEditException>(() => editor.ApplyAsync(Writable, Request(change)));

        Assert.Equal(RowEditError.Invalid, ex.Code);
        Assert.Contains(expectedMessage, ex.Message);
        Assert.Equal(0, editor.Connection.Opened);
    }

    [Fact]
    public async Task Apply_rejects_a_missing_extra_or_repeated_primary_key_and_oversized_batches()
    {
        var (editor, _, _) = NewEditor();

        var missing = new RowChange { Set = { ["NOMBRE"] = new CellEdit { Original = "a", Value = "b" } } };
        var extra = Change("7", ("NOMBRE", "a", "b"));
        extra.PrimaryKey["NOMBRE"] = "a";

        foreach (var request in new[]
        {
            Request(missing),
            Request(extra),
            Request(Change("7", ("NOMBRE", "a", "b")), Change("7", ("NOMBRE", "a", "c"))),
            Request(Enumerable.Range(1, RowEditorBase.MaxRows + 1).Select(i => Change(i.ToString(), ("NOMBRE", "a", "b"))).ToArray()),
            Request()
        })
        {
            var ex = await Assert.ThrowsAsync<RowEditException>(() => editor.ApplyAsync(Writable, request));
            Assert.Equal(RowEditError.Invalid, ex.Code);
        }

        Assert.Equal(0, editor.Connection.Opened);
    }

    [Fact]
    public async Task Oracle_uses_quoted_catalog_names_bind_markers_and_a_bounded_row_lock()
    {
        var describer = new FakeDescriber
        {
            Metadata = new TableMetadata("BGUSER", "CLIENTES",
            [
                new TableColumn("ID", "NUMBER", ColumnCategory.Integer, false, true, false, false),
                new TableColumn("NOMBRE", "VARCHAR2", ColumnCategory.String, true, false, false, false)
            ])
        };
        var audit = new MemoryAuditLog();
        var editor = new TestOracleEditor(describer, audit);
        editor.Connection.TableFor = _ => Current("Ana");

        await editor.ApplyAsync(OracleWritable, new RowEditRequest
        {
            Schema = "BGUSER",
            Table = "CLIENTES",
            Changes = [Change("7", ("NOMBRE", "Ana", "Bea"))]
        });

        Assert.Contains("SELECT \"NOMBRE\" FROM \"BGUSER\".\"CLIENTES\" WHERE \"ID\" = :k0 FOR UPDATE WAIT 5", editor.Connection.ExecutedSql);
        var update = Assert.Single(editor.Connection.NonQueries);
        Assert.Equal("UPDATE \"BGUSER\".\"CLIENTES\" SET \"NOMBRE\" = :v0 WHERE \"ID\" = :k0", update.Sql);
        Assert.True(editor.Connection.Transaction!.Committed);
    }

    // ---------- valores ----------

    [Theory]
    [InlineData(ColumnCategory.Integer, "42", 42L)]
    [InlineData(ColumnCategory.Integer, " -7 ", -7L)]
    [InlineData(ColumnCategory.Decimal, "1250.50", 1250.50)]
    [InlineData(ColumnCategory.Decimal, "1e3", 1000.0)]
    [InlineData(ColumnCategory.Bool, "TRUE", true)]
    [InlineData(ColumnCategory.Bool, "0", false)]
    public void ParseValue_converts_text_to_the_column_type(ColumnCategory category, string text, object expected)
    {
        var value = RowEditorBase.ParseValue(new TableColumn("C", "t", category, true, false, false, false), text);

        Assert.Equal(Convert.ChangeType(expected, value.GetType(), System.Globalization.CultureInfo.InvariantCulture), value);
    }

    [Theory]
    [InlineData("2026-10-26", 2026, 10, 26, 0, 0, 0)]
    [InlineData("2026-10-26 14:30", 2026, 10, 26, 14, 30, 0)]
    [InlineData("2026-10-26T14:30:15", 2026, 10, 26, 14, 30, 15)]
    public void ParseValue_accepts_iso_dates_only(string text, int y, int mo, int d, int h, int mi, int s)
    {
        var value = RowEditorBase.ParseValue(new TableColumn("C", "datetime", ColumnCategory.DateTime, true, false, false, false), text);

        Assert.Equal(new DateTime(y, mo, d, h, mi, s), value);
    }

    [Theory]
    [InlineData(ColumnCategory.Integer, "12.5")]
    [InlineData(ColumnCategory.Integer, "1,000")]
    [InlineData(ColumnCategory.Decimal, "NaN")]
    [InlineData(ColumnCategory.Decimal, "1,5")]
    [InlineData(ColumnCategory.Bool, "yes")]
    [InlineData(ColumnCategory.Guid, "no-es-un-guid")]
    [InlineData(ColumnCategory.DateTime, "26/10/2026")]
    [InlineData(ColumnCategory.Unsupported, "x")]
    public void ParseValue_rejects_text_that_does_not_fit(ColumnCategory category, string text)
    {
        var ex = Assert.Throws<RowEditException>(() =>
            RowEditorBase.ParseValue(new TableColumn("C", "t", category, true, false, false, false), text));

        Assert.Equal(RowEditError.Invalid, ex.Code);
    }

    [Fact]
    public void ParseValue_keeps_strings_untouched_and_maps_null_to_dbnull()
    {
        var column = new TableColumn("C", "nvarchar", ColumnCategory.String, true, false, false, false);

        Assert.Equal("  con espacios  ", RowEditorBase.ParseValue(column, "  con espacios  "));
        Assert.Equal(DBNull.Value, RowEditorBase.ParseValue(column, null));
    }

    [Fact]
    public void Column_categories_follow_the_engine_types()
    {
        Assert.Equal(ColumnCategory.Integer, ColumnCategories.From(DatabaseKind.SqlServer, "BIGINT"));
        Assert.Equal(ColumnCategory.DateTime, ColumnCategories.From(DatabaseKind.SqlServer, "datetime2"));
        Assert.Equal(ColumnCategory.Unsupported, ColumnCategories.From(DatabaseKind.SqlServer, "varbinary"));
        Assert.Equal(ColumnCategory.Unsupported, ColumnCategories.From(DatabaseKind.SqlServer, "timestamp"));   // rowversion
        Assert.Equal(ColumnCategory.Integer, ColumnCategories.From(DatabaseKind.Oracle, "NUMBER", 0));
        Assert.Equal(ColumnCategory.Decimal, ColumnCategories.From(DatabaseKind.Oracle, "NUMBER", 2));
        Assert.Equal(ColumnCategory.Decimal, ColumnCategories.From(DatabaseKind.Oracle, "NUMBER"));
        Assert.Equal(ColumnCategory.String, ColumnCategories.From(DatabaseKind.Oracle, "VARCHAR2"));
        Assert.Equal(ColumnCategory.DateTime, ColumnCategories.From(DatabaseKind.Oracle, "DATE"));
        Assert.Equal(ColumnCategory.DateTime, ColumnCategories.From(DatabaseKind.Oracle, "TIMESTAMP(6)"));
        Assert.Equal(ColumnCategory.Unsupported, ColumnCategories.From(DatabaseKind.Oracle, "TIMESTAMP(6) WITH TIME ZONE"));
        Assert.Equal(ColumnCategory.Unsupported, ColumnCategories.From(DatabaseKind.Oracle, "CLOB"));
    }

    [Fact]
    public void A_truncated_grid_value_is_recognized()
    {
        Assert.True(CellText.IsTruncated(CellText.Truncate(new string('x', 5000))));
        Assert.False(CellText.IsTruncated(new string('x', 4000)));
        Assert.False(CellText.IsTruncated(null));
    }
}

public class FileEditAuditLogTests
{
    [Fact]
    public void Each_entry_is_one_json_line_appended_to_the_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"audit-{Guid.NewGuid():N}", "data-edits.log");

        try
        {
            var log = new FileEditAuditLog(path);
            var change = new AuditChange(
                new Dictionary<string, string?> { ["ID"] = "7" },
                new Dictionary<string, string?> { ["NOMBRE"] = "Ana" },
                new Dictionary<string, string?> { ["NOMBRE"] = null });

            log.Write(new EditAuditEntry(new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc), "dev", null, "dbo", "Clientes", "aplicando", false, null, [change]));
            log.Write(new EditAuditEntry(new DateTime(2026, 10, 26, 12, 0, 1, DateTimeKind.Utc), "dev", null, "dbo", "Clientes", "confirmado", false, null, [change]));

            var lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.All(lines, line => Assert.StartsWith("{", line));
            Assert.Contains("\"Outcome\":\"aplicando\"", lines[0]);
            Assert.Contains("\"Table\":\"Clientes\"", lines[0]);
            Assert.Contains("\"Ana\"", lines[0]);
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(path)!, recursive: true); } catch (IOException) { }
        }
    }
}
