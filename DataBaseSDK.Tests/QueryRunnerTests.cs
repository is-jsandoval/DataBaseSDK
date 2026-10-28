using System.Data;
using System.Data.Common;
using DataBaseSDK.Core;

namespace DataBaseSDK.Tests;

// Conexión en memoria sobre un DataTable (DataTableReader es un DbDataReader real): permite probar la
// lógica común de QueryRunnerBase —guard, columnas, tope de filas, conversión de celdas, rollback—
// sin una base de datos. Lo específico de cada driver (Oracle/SQL Server) no se puede probar así.
internal class FakeConnection : DbConnection
{
    public DataTable Table { get; set; } = new();

    /// <summary>Si se define, elige la tabla según el SQL ejecutado (para pruebas con varias consultas distintas).</summary>
    public Func<string, DataTable>? TableFor { get; set; }

    public int Opened { get; private set; }

    public FakeTransaction? Transaction { get; private set; }

    public List<string> ExecutedSql { get; } = [];

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;
    public override string Database => "fake";
    public override string DataSource => "fake";
    public override string ServerVersion => "1";
    public override ConnectionState State => ConnectionState.Open;
    public List<string> ChangedDatabases { get; } = [];

    /// <summary>Parámetros de cada comando que devolvió filas, en orden de ejecución.</summary>
    public List<Dictionary<string, object?>> ExecutedParameters { get; } = [];

    /// <summary>Filas que "afecta" cada sentencia de escritura (ExecuteNonQuery).</summary>
    public int RowsAffected { get; set; } = 1;

    /// <summary>Sentencias de escritura ejecutadas (texto y parámetros), en orden.</summary>
    public List<(string Sql, Dictionary<string, object?> Parameters, DbType?[] Types)> NonQueries { get; } = [];

    /// <summary>Si devuelve una excepción para ese SQL, se lanza al ejecutarlo (simula un fallo de la base).</summary>
    public Func<string, Exception?>? FailOn { get; set; }

    public override void ChangeDatabase(string databaseName) => ChangedDatabases.Add(databaseName);
    public override void Close() { }
    public override void Open() => Opened++;

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => Transaction = new FakeTransaction(this);
    protected override DbCommand CreateDbCommand() => new FakeCommand(this);
}

internal class FakeTransaction : DbTransaction
{
    private readonly FakeConnection _connection;

    public FakeTransaction(FakeConnection connection) => _connection = connection;

    public bool Committed { get; private set; }

    public bool RolledBack { get; private set; }

    public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
    protected override DbConnection DbConnection => _connection;
    public override void Commit() => Committed = true;
    public override void Rollback() => RolledBack = true;
}

internal class FakeCommand : DbCommand
{
    private readonly FakeConnection _connection;

    public FakeCommand(FakeConnection connection) => _connection = connection;

    public int Timeout { get; private set; }

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get => Timeout; set => Timeout = value; }
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get => _connection; set { } }
    private readonly FakeParameterCollection _parameters = new();

    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction? DbTransaction { get; set; }
    public override void Cancel() { }
    public override int ExecuteNonQuery()
    {
        _connection.ExecutedSql.Add(CommandText);

        if (_connection.FailOn?.Invoke(CommandText) is { } failure)
            throw failure;

        _connection.NonQueries.Add((
            CommandText,
            _parameters.Items.ToDictionary(p => p.ParameterName, p => p.Value is DBNull ? null : p.Value),
            _parameters.Items.Select(p => (DbType?)p.DbType).ToArray()));

        return _connection.RowsAffected;
    }

    public override object? ExecuteScalar() => null;
    public override void Prepare() { }
    protected override DbParameter CreateDbParameter() => new FakeParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        _connection.ExecutedSql.Add(CommandText);

        if (_connection.FailOn?.Invoke(CommandText) is { } failure)
            throw failure;

        _connection.ExecutedParameters.Add(_parameters.Items.ToDictionary(p => p.ParameterName, p => p.Value is DBNull ? null : p.Value));

        return new DataTableReader(_connection.TableFor?.Invoke(CommandText) ?? _connection.Table);
    }
}

internal class FakeRunner : QueryRunnerBase
{
    public FakeConnection Connection { get; } = new();

    public override DatabaseKind Kind => DatabaseKind.SqlServer;
    protected override DbConnection CreateConnection(string connectionString) => Connection;

    protected override async Task<DbTransaction> BeginReadOnlyAsync(DbConnection connection, CancellationToken cancellationToken) =>
        await connection.BeginTransactionAsync(cancellationToken);
}

public class QueryRunnerTests
{
    private static readonly DbConnectionInfo Info = new("fake", DatabaseKind.SqlServer, "x", "fake");

    private static DataTable Table(int rows)
    {
        var table = new DataTable();
        table.Columns.Add("ID", typeof(int));
        table.Columns.Add("NOMBRE", typeof(string));
        table.Columns.Add("MONTO", typeof(decimal));
        table.Columns.Add("CREADO", typeof(DateTime));
        table.Columns.Add("FIRMA", typeof(byte[]));
        table.Columns.Add("TOKEN", typeof(Guid));

        for (var i = 1; i <= rows; i++)
            table.Rows.Add(i, i % 2 == 0 ? DBNull.Value : $"cliente {i}", i * 10.5m, new DateTime(2026, 10, 26), new byte[] { 0xCA, 0xFE }, Guid.Empty);

        return table;
    }

    [Fact]
    public async Task ExecuteAsync_returns_typed_columns_and_display_ready_cells()
    {
        var runner = new FakeRunner { Connection = { Table = Table(2) } };

        var result = await runner.ExecuteAsync(Info, "SELECT * FROM clientes;", new QueryOptions());

        Assert.Equal(["ID", "NOMBRE", "MONTO", "CREADO", "FIRMA", "TOKEN"], result.Columns.Select(c => c.Name));
        Assert.Equal([true, false, true, false, false, false], result.Columns.Select(c => c.IsNumeric));

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("cliente 1", result.Rows[0][1]);
        Assert.Null(result.Rows[1][1]);                       // DBNull → null
        Assert.Equal(10.5m, result.Rows[0][2]);
        Assert.Equal("0xCAFE", result.Rows[0][4]);            // binario → hex legible
        Assert.Equal(Guid.Empty.ToString(), result.Rows[0][5]);
        Assert.False(result.Truncated);
    }

    [Fact]
    public async Task ExecuteAsync_strips_the_trailing_semicolon_and_applies_the_timeout()
    {
        var runner = new FakeRunner { Connection = { Table = Table(1) } };

        await runner.ExecuteAsync(Info, "SELECT 1;", new QueryOptions { TimeoutSeconds = 7 });

        Assert.Equal("SELECT 1", Assert.Single(runner.Connection.ExecutedSql));
    }

    [Theory]
    [InlineData(3, 5, true, 3)]    // hay más filas que el tope: corta y avisa
    [InlineData(5, 5, false, 5)]   // exactamente el tope: no es "cortado"
    [InlineData(10, 5, false, 5)]  // menos filas que el tope
    public async Task ExecuteAsync_caps_the_rows_and_flags_truncation(int maxRows, int available, bool truncated, int expected)
    {
        var runner = new FakeRunner { Connection = { Table = Table(available) } };

        var result = await runner.ExecuteAsync(Info, "SELECT * FROM t", new QueryOptions { MaxRows = maxRows });

        Assert.Equal(expected, result.Rows.Count);
        Assert.Equal(truncated, result.Truncated);
    }

    [Fact]
    public async Task ExecuteAsync_never_commits_the_transaction()
    {
        var runner = new FakeRunner { Connection = { Table = Table(1) } };

        await runner.ExecuteAsync(Info, "SELECT * FROM t", new QueryOptions());

        Assert.NotNull(runner.Connection.Transaction);
        Assert.False(runner.Connection.Transaction!.Committed);
    }

    [Fact]
    public async Task A_rejected_query_never_reaches_the_database()
    {
        var runner = new FakeRunner();

        var ex = await Assert.ThrowsAsync<QueryRejectedException>(() =>
            runner.ExecuteAsync(Info, "DELETE FROM clientes", new QueryOptions()));

        Assert.Contains("SELECT o WITH", ex.Message);
        Assert.Equal(0, runner.Connection.Opened);
        Assert.Empty(runner.Connection.ExecutedSql);
    }

    [Fact]
    public async Task Long_text_cells_are_truncated_so_a_huge_clob_cannot_flood_the_page()
    {
        var table = new DataTable();
        table.Columns.Add("DOC", typeof(string));
        table.Rows.Add(new string('x', 10_000));

        var runner = new FakeRunner { Connection = { Table = table } };

        var result = await runner.ExecuteAsync(Info, "SELECT doc FROM t", new QueryOptions());

        var cell = Assert.IsType<string>(result.Rows[0][0]);
        Assert.Equal(4001, cell.Length); // 4000 caracteres + "…"
        Assert.EndsWith("…", cell);
    }
}

internal class FakeParameter : DbParameter
{
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; }
    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string ParameterName { get; set; } = string.Empty;
    public override int Size { get; set; }
    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;
    public override bool SourceColumnNullMapping { get; set; }
    public override object? Value { get; set; }
    public override void ResetDbType() { }
}

internal class FakeParameterCollection : DbParameterCollection
{
    public List<DbParameter> Items { get; } = [];

    public override int Count => Items.Count;
    public override object SyncRoot => Items;
    public override int Add(object value) { Items.Add((DbParameter)value); return Items.Count - 1; }
    public override void AddRange(Array values) { foreach (var v in values) Add(v); }
    public override void Clear() => Items.Clear();
    public override bool Contains(object value) => Items.Contains((DbParameter)value);
    public override bool Contains(string value) => Items.Any(p => p.ParameterName == value);
    public override void CopyTo(Array array, int index) => ((System.Collections.ICollection)Items).CopyTo(array, index);
    public override System.Collections.IEnumerator GetEnumerator() => Items.GetEnumerator();
    public override int IndexOf(object value) => Items.IndexOf((DbParameter)value);
    public override int IndexOf(string parameterName) => Items.FindIndex(p => p.ParameterName == parameterName);
    public override void Insert(int index, object value) => Items.Insert(index, (DbParameter)value);
    public override void Remove(object value) => Items.Remove((DbParameter)value);
    public override void RemoveAt(int index) => Items.RemoveAt(index);
    public override void RemoveAt(string parameterName) => Items.RemoveAll(p => p.ParameterName == parameterName);
    protected override DbParameter GetParameter(int index) => Items[index];
    protected override DbParameter GetParameter(string parameterName) => Items.First(p => p.ParameterName == parameterName);
    protected override void SetParameter(int index, DbParameter value) => Items[index] = value;
    protected override void SetParameter(string parameterName, DbParameter value) => Items[IndexOf(parameterName)] = value;
}
