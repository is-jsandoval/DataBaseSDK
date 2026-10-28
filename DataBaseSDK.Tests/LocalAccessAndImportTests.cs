using System.Net;
using DataBaseSDK.Core;

namespace DataBaseSDK.Tests;

public class LocalAccessTests
{
    [Theory]
    [InlineData("127.0.0.1", "localhost")]
    [InlineData("127.0.0.1", "LOCALHOST")]
    [InlineData("::1", "localhost")]
    [InlineData("127.0.0.1", "127.0.0.1")]
    [InlineData("127.0.0.1", "[::1]")]                    // Request.Host.Host conserva los corchetes de IPv6
    [InlineData("::ffff:127.0.0.1", "localhost")]         // loopback IPv4 mapeado a IPv6
    public void A_loopback_connection_with_a_loopback_host_is_local(string remote, string host)
    {
        Assert.True(LocalAccess.IsLocal(IPAddress.Parse(remote), host));
    }

    [Theory]
    [InlineData("192.168.3.165", "localhost")]            // otro equipo de la red
    [InlineData("127.0.0.1", "evil.example.com")]         // DNS rebinding: conexión local pero Host ajeno
    [InlineData("127.0.0.1", "localhost.evil.com")]
    [InlineData("127.0.0.1", "192.168.3.165")]            // la IP de la red, no la de loopback
    [InlineData("127.0.0.1", "")]
    [InlineData("10.0.0.5", "10.0.0.5")]
    public void Anything_else_is_not(string remote, string host)
    {
        Assert.False(LocalAccess.IsLocal(IPAddress.Parse(remote), host));
    }

    [Fact]
    public void A_missing_address_or_host_is_not_local()
    {
        Assert.False(LocalAccess.IsLocal(null, "localhost"));
        Assert.False(LocalAccess.IsLocal(IPAddress.Loopback, null));
    }
}

public class ConnectionImportTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "dbsdk-import-" + Guid.NewGuid().ToString("N"));
    private string SourcePath => Path.Combine(_directory, "origen", "db-connections.json");
    private string TargetPath => Path.Combine(_directory, "destino", "db-connections.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private static DbConnectionStore Store(string path, string? importFrom = null) => new(new DpapiSecretProtector(), path, importFrom);

    private void SeedSource()
    {
        Store(SourcePath).Save(new StoredConnection
        {
            Alias = "DEV", Kind = DatabaseKind.SqlServer, Host = "sql.banco.local", User = "lector", Password = "S3cr3t;clave", AllowWrite = true
        });
    }

    [Fact]
    public void The_first_run_imports_the_connections_with_their_passwords_but_never_the_write_permission()
    {
        SeedSource();

        var imported = Store(TargetPath, SourcePath).Get(DatabaseKind.SqlServer, "DEV");

        Assert.NotNull(imported);
        Assert.Equal("sql.banco.local", imported!.Host);
        Assert.Equal("S3cr3t;clave", imported.Password);       // la clave cifrada con DPAPI se descifra: mismo usuario, misma entropía
        Assert.False(imported.AllowWrite);                      // la edición de datos hay que volver a habilitarla a propósito

        // El archivo de origen no se toca.
        Assert.True(Store(SourcePath).Get(DatabaseKind.SqlServer, "DEV")!.AllowWrite);
    }

    [Fact]
    public void It_imports_only_once_even_if_every_connection_is_later_deleted()
    {
        SeedSource();
        var first = Store(TargetPath, SourcePath);
        Assert.True(first.Delete(DatabaseKind.SqlServer, "DEV"));

        // El archivo propio existe (aunque esté vacío): no se vuelve a importar.
        Assert.Empty(Store(TargetPath, SourcePath).List(DatabaseKind.SqlServer));
    }

    [Fact]
    public void A_missing_or_corrupt_source_is_ignored_without_failing()
    {
        Assert.Empty(Store(TargetPath, Path.Combine(_directory, "no-existe.json")).List(DatabaseKind.SqlServer));

        Directory.CreateDirectory(Path.GetDirectoryName(SourcePath)!);
        File.WriteAllText(SourcePath, "{ esto no es json");

        Assert.Empty(Store(TargetPath, SourcePath).List(DatabaseKind.SqlServer));
        Assert.False(File.Exists(TargetPath));
        Assert.Equal("{ esto no es json", File.ReadAllText(SourcePath));   // el original queda intacto
    }

    [Fact]
    public void An_explicit_path_without_an_import_source_never_imports()
    {
        SeedSource();

        Assert.Empty(Store(TargetPath).List(DatabaseKind.SqlServer));
    }
}
