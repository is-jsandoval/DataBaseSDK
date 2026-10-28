using System.Text;
using DataBaseSDK.Core;

namespace DataBaseSDK.Tests;

public class SqlFileServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "azdo-sqlfiles-" + Guid.NewGuid().ToString("N"));
    private readonly SqlFileService _service = new();

    public SqlFileServiceTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private string P(string name) => Path.Combine(_directory, name);

    private static SqlFileError CodeOf(Action action) => Assert.Throws<SqlFileException>(action).Code;

    [Fact]
    public void Read_normalizes_line_endings_for_the_editor_and_remembers_the_original()
    {
        File.WriteAllBytes(P("crlf.sql"), Encoding.UTF8.GetBytes("SELECT 1\r\nFROM dual\r\n"));
        File.WriteAllBytes(P("lf.sql"), Encoding.UTF8.GetBytes("SELECT 1\nFROM dual\n"));

        var crlf = _service.Read(P("crlf.sql"));
        var lf = _service.Read(P("lf.sql"));

        Assert.Equal("SELECT 1\nFROM dual\n", crlf.Text);
        Assert.Equal("\r\n", crlf.Eol);
        Assert.Equal("SELECT 1\nFROM dual\n", lf.Text);
        Assert.Equal("\n", lf.Eol);
    }

    [Theory]
    [InlineData("utf8", false)]
    [InlineData("utf8-bom", true)]
    [InlineData("utf16le", true)]
    [InlineData("latin1", false)]
    public void A_save_keeps_the_encoding_and_line_endings_of_the_file(string encoding, bool hasBom)
    {
        var text = "-- Validación de saldo\nSELECT 'ñandú' FROM dual\n";

        _service.Save(P("a.sql"), text, "\r\n", encoding, null, overwrite: true);
        var bytes = File.ReadAllBytes(P("a.sql"));

        Assert.Equal(hasBom, bytes.Length > 2 && (bytes[0] == 0xEF || bytes[0] == 0xFF));

        var read = _service.Read(P("a.sql"));
        Assert.Equal(encoding, read.Encoding);
        Assert.Equal(text, read.Text);          // las tildes sobreviven al viaje de ida y vuelta
        Assert.Equal("\r\n", read.Eol);
    }

    [Fact]
    public void A_file_that_is_not_valid_utf8_is_read_as_latin1_instead_of_failing()
    {
        File.WriteAllBytes(P("viejo.sql"), Encoding.Latin1.GetBytes("SELECT 'camión'"));

        var read = _service.Read(P("viejo.sql"));

        Assert.Equal("latin1", read.Encoding);
        Assert.Equal("SELECT 'camión'", read.Text);
    }

    [Fact]
    public void Missing_file_is_a_typed_not_found_error_with_a_helpful_message()
    {
        var ex = Assert.Throws<SqlFileException>(() => _service.Read(P("no-existe.sql")));

        Assert.Equal(SqlFileError.NotFound, ex.Code);
        Assert.Contains("no-existe.sql", ex.Message);
        Assert.False(_service.Stat(P("no-existe.sql")).Exists);
    }

    [Fact]
    public void Only_sql_files_are_allowed()
    {
        File.WriteAllText(P("notas.txt"), "x");

        Assert.Equal(SqlFileError.NotSql, CodeOf(() => _service.Read(P("notas.txt"))));
        Assert.Equal(SqlFileError.NotSql, CodeOf(() => _service.Save(P("script.ps1"), "x", null, null, null, true)));
        Assert.Equal(SqlFileError.NotSql, CodeOf(() => _service.Save(P("sin-extension"), "x", null, null, null, true)));
        Assert.False(File.Exists(P("script.ps1")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("C:\\carpeta\\*.sql")]
    [InlineData("C:\\carpeta\\a?.sql")]
    public void Invalid_paths_are_rejected_before_touching_the_disk(string path)
    {
        Assert.Equal(SqlFileError.InvalidPath, CodeOf(() => _service.Read(path)));
    }

    [Fact]
    public void A_folder_named_like_a_sql_file_is_not_treated_as_a_file()
    {
        Directory.CreateDirectory(P("carpeta.sql"));

        Assert.Equal(SqlFileError.InvalidPath, CodeOf(() => _service.Stat(P("carpeta.sql"))));
    }

    [Fact]
    public void Save_detects_that_the_file_changed_on_disk_since_it_was_opened()
    {
        File.WriteAllText(P("a.sql"), "SELECT 1");
        var opened = _service.Read(P("a.sql"));

        // Otro programa lo modifica (la marca de tiempo cambia).
        File.WriteAllText(P("a.sql"), "SELECT 2");
        File.SetLastWriteTimeUtc(P("a.sql"), DateTime.UtcNow.AddMinutes(5));

        Assert.Equal(SqlFileError.Conflict, CodeOf(() => _service.Save(P("a.sql"), "SELECT 3", opened.Eol, opened.Encoding, opened.Version, overwrite: false)));
        Assert.Equal("SELECT 2", File.ReadAllText(P("a.sql")));            // no se pisó

        // Sobrescribir explícitamente sí funciona.
        _service.Save(P("a.sql"), "SELECT 3", opened.Eol, opened.Encoding, opened.Version, overwrite: true);
        Assert.Equal("SELECT 3", File.ReadAllText(P("a.sql")));
    }

    [Fact]
    public void Save_without_a_version_refuses_to_replace_an_existing_file_unless_told_to()
    {
        File.WriteAllText(P("a.sql"), "original");

        Assert.Equal(SqlFileError.AlreadyExists, CodeOf(() => _service.Save(P("a.sql"), "nuevo", null, null, null, overwrite: false)));
        Assert.Equal("original", File.ReadAllText(P("a.sql")));

        _service.Save(P("a.sql"), "nuevo", null, null, null, overwrite: true);
        Assert.Equal("nuevo", File.ReadAllText(P("a.sql")));
    }

    [Fact]
    public void Saving_a_file_that_vanished_since_it_was_opened_warns_instead_of_silently_recreating_it()
    {
        File.WriteAllText(P("a.sql"), "SELECT 1");
        var opened = _service.Read(P("a.sql"));
        File.Delete(P("a.sql"));

        Assert.Equal(SqlFileError.NotFound, CodeOf(() => _service.Save(P("a.sql"), "SELECT 2", opened.Eol, opened.Encoding, opened.Version, overwrite: false)));
        Assert.False(File.Exists(P("a.sql")));
    }

    [Fact]
    public void Saving_into_a_missing_folder_is_a_not_found_error()
    {
        Assert.Equal(SqlFileError.NotFound, CodeOf(() => _service.Save(Path.Combine(_directory, "no", "a.sql"), "x", null, null, null, true)));
    }

    [Fact]
    public void A_successful_save_returns_the_new_version_so_the_next_save_is_not_a_conflict()
    {
        File.WriteAllText(P("a.sql"), "v1");
        var opened = _service.Read(P("a.sql"));

        var first = _service.Save(P("a.sql"), "v2", opened.Eol, opened.Encoding, opened.Version, overwrite: false);
        var second = _service.Save(P("a.sql"), "v3", opened.Eol, opened.Encoding, first.Version, overwrite: false);

        Assert.Equal("v3", File.ReadAllText(P("a.sql")));
        Assert.True(second.Exists);
    }

    [Fact]
    public void Files_over_the_size_limit_are_not_opened()
    {
        using (var stream = File.Create(P("enorme.sql")))
            stream.SetLength(SqlFileService.MaxFileBytes + 1);

        Assert.Equal(SqlFileError.TooLarge, CodeOf(() => _service.Read(P("enorme.sql"))));
    }

    [Fact]
    public void Browse_lists_folders_first_then_only_sql_files_and_skips_hidden_folders()
    {
        Directory.CreateDirectory(P("zeta"));
        Directory.CreateDirectory(P("alfa"));
        var hidden = Directory.CreateDirectory(P("oculta"));
        hidden.Attributes |= FileAttributes.Hidden;
        File.WriteAllText(P("b.sql"), "x");
        File.WriteAllText(P("A.SQL"), "x");
        File.WriteAllText(P("notas.txt"), "x");
        File.WriteAllText(P("tabla.sqlx"), "x");

        var listing = _service.Browse(_directory);

        Assert.Equal(["alfa", "zeta", "A.SQL", "b.sql"], listing.Entries.Select(e => e.Name));
        Assert.Equal([true, true, false, false], listing.Entries.Select(e => e.IsFolder));
        Assert.Equal(Path.GetFullPath(_directory), listing.Path);
        Assert.False(string.IsNullOrEmpty(listing.Parent));
    }

    [Fact]
    public void Browse_of_a_file_path_lists_its_folder_and_of_a_missing_folder_fails_cleanly()
    {
        File.WriteAllText(P("a.sql"), "x");

        Assert.Single(_service.Browse(P("a.sql")).Entries);
        Assert.Equal(SqlFileError.NotFound, CodeOf(() => _service.Browse(P("no-existe"))));
    }

    [Fact]
    public void Browse_without_a_path_lists_the_drives()
    {
        var listing = _service.Browse(null);

        Assert.NotEmpty(listing.Entries);
        Assert.All(listing.Entries, e => Assert.True(e.IsFolder));
        Assert.Equal(string.Empty, listing.Path);
    }

    [Fact]
    public void Locate_finds_a_dropped_file_by_name_and_size_inside_the_given_folders()
    {
        Directory.CreateDirectory(P("proyecto\\scripts\\2026"));
        File.WriteAllText(P("proyecto\\scripts\\2026\\reporte.sql"), "SELECT 1");      // 8 bytes
        File.WriteAllText(P("proyecto\\otro.sql"), "SELECT 1");

        var found = _service.Locate("REPORTE.sql", 8, [_directory]);   // el nombre no distingue mayúsculas

        Assert.Equal([P("proyecto\\scripts\\2026\\reporte.sql")], found);
    }

    [Fact]
    public void Locate_ignores_files_with_the_same_name_but_a_different_size()
    {
        File.WriteAllText(P("reporte.sql"), "SELECT 1 FROM dual");

        Assert.Empty(_service.Locate("reporte.sql", 3, [_directory]));
    }

    [Fact]
    public void Locate_returns_every_candidate_so_the_caller_can_compare_contents()
    {
        Directory.CreateDirectory(P("a"));
        Directory.CreateDirectory(P("b"));
        File.WriteAllText(P("a\\x.sql"), "AAAA");
        File.WriteAllText(P("b\\x.sql"), "BBBB");   // mismo tamaño, distinto contenido

        Assert.Equal(2, _service.Locate("x.sql", 4, [_directory]).Count);
    }

    [Fact]
    public void Locate_skips_hidden_folders_and_stops_at_the_depth_limit()
    {
        var hidden = Directory.CreateDirectory(P("oculta"));
        File.WriteAllText(P("oculta\\a.sql"), "x");
        hidden.Attributes |= FileAttributes.Hidden;

        var deep = Path.Combine(_directory, "1", "2", "3", "4", "5", "6", "7");
        Directory.CreateDirectory(deep);
        File.WriteAllText(Path.Combine(deep, "profundo.sql"), "x");

        Assert.Empty(_service.Locate("a.sql", 1, [_directory]));
        Assert.Empty(_service.Locate("profundo.sql", 1, [_directory]));   // más allá de 5 niveles
    }

    [Theory]
    [InlineData("..\\secreto.sql")]
    [InlineData("C:\\x\\a.sql")]
    [InlineData("a*.sql")]
    [InlineData("notas.txt")]
    [InlineData("")]
    public void Locate_only_accepts_a_plain_sql_file_name(string name)
    {
        File.WriteAllText(P("notas.txt"), "x");

        Assert.Empty(_service.Locate(name, 1, [_directory]));
    }

    [Fact]
    public void Locate_ignores_folders_that_do_not_exist_or_are_invalid()
    {
        File.WriteAllText(P("a.sql"), "x");

        var found = _service.Locate("a.sql", 1, [P("no-existe"), "C:\\carpeta\\*", "", _directory]);

        Assert.Equal([P("a.sql")], found);
    }

    [Fact]
    public void ProjectPaths_finds_the_repo_root_and_keeps_local_data_inside_it()
    {
        Assert.True(File.Exists(Path.Combine(ProjectPaths.Root, "DataBaseSDK.sln")));
        Assert.Equal(Path.Combine(ProjectPaths.Root, ".local"), ProjectPaths.DataDirectory);
        Assert.Equal(Path.Combine(ProjectPaths.DataDirectory, "db-connections.json"), DbConnectionStore.DefaultPath);
        Assert.Contains(".local", File.ReadAllText(Path.Combine(ProjectPaths.Root, ".gitignore")));
    }
}
