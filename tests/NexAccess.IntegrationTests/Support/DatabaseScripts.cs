using Npgsql;

namespace NexAccess.IntegrationTests.Support;

public static class DatabaseScripts
{
    public static async Task EnsureDatabaseAsync(string connectionString)
    {
        var target = new NpgsqlConnectionStringBuilder(connectionString);
        var admin = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();

        await using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        exists.Parameters.AddWithValue("name", target.Database!);
        if (await exists.ExecuteScalarAsync() is null)
        {
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{target.Database}\" ENCODING 'UTF8' TEMPLATE template0", connection);
            await create.ExecuteNonQueryAsync();
        }
    }

public static async Task ApplyAsync(string connectionString)
{
    var directory = Path.Combine(
        FindRepositoryRoot(),
        "src",
        "NexAccess.Infrastructure",
        "Persistence",
        "Scripts");

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    // Registrar las migraciones ejecutadas.
    await using (var command = new NpgsqlCommand("""
        CREATE TABLE IF NOT EXISTS migracion_test (
            archivo TEXT PRIMARY KEY,
            aplicada_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """, connection))
    {
        await command.ExecuteNonQueryAsync();
    }

    var files = Directory.GetFiles(directory, "*.sql")
        .OrderBy(path => path, StringComparer.Ordinal);

    foreach (var file in files)
    {
        var fileName = Path.GetFileName(file);

        await using var check = new NpgsqlCommand(
            "SELECT COUNT(*) FROM migracion_test WHERE archivo = @archivo",
            connection);

        check.Parameters.AddWithValue("archivo", fileName);

        var exists = (long)(await check.ExecuteScalarAsync())!;

        if (exists > 0)
            continue;

        // Incorporar el registro antes del COMMIT del propio script.
var sql = (await File.ReadAllTextAsync(file)).Trim();

var commitMatch = System.Text.RegularExpressions.Regex.Match(
    sql,
    @"COMMIT\s*;\s*$",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

if (!System.Text.RegularExpressions.Regex.IsMatch(
        sql,
        @"^\s*BEGIN\s*;",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase)
    || !commitMatch.Success)
{
    throw new InvalidOperationException(
        $"La migración {fileName} debe comenzar con BEGIN y terminar con COMMIT;");
}

var sqlWithRegistration = sql.Insert(
    commitMatch.Index,
    "INSERT INTO migracion_test (archivo) VALUES (@archivo);\n");

await using var command = new NpgsqlCommand(
    sqlWithRegistration,
    connection);

command.Parameters.AddWithValue("archivo", fileName);

await command.ExecuteNonQueryAsync();


    }
}



    public static async Task<T?> ScalarAsync<T>(string connectionString, string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "NexAccess.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("No se encontró NexAccess.sln.");
    }
}
