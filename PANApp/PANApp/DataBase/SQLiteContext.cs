using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PANApp.DataBase;

public class SQLiteContext : IDisposable
{
    private readonly string _connectionString = string.Empty;
    private SqliteConnection? _connection = null;

    public SQLiteContext()
    {
        var dbPath = GetDatabasePath();
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        Console.WriteLine($"[DB] Database path: {dbPath}");
    }

    public SqliteConnection GetConnection()
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
            InitializeDatabase();
        }
        else if (_connection.State != System.Data.ConnectionState.Open)
            _connection.Open();

        return _connection;
    }

    private void InitializeDatabase()
    {
        var sql = """
            CREATE TABLE IF NOT EXISTS Notes (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Description TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            """;

        using var command = GetConnection().CreateCommand();

        command.CommandText = sql;
        command.ExecuteNonQuery();

        Console.WriteLine("[DB] Database initialized ✓");
    }

    private static string GetDatabasePath()
    {
        string configDir = string.Empty;

        try
        {
            var appData = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.Create);

            if (!string.IsNullOrEmpty(appData))
            {
                configDir = Path.Combine(appData, "PANApp");
                return Path.Combine(configDir, "panapp.db");
            }
        }
        catch { /* Ignored */ }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrEmpty(xdgConfig))
                configDir = Path.Combine(xdgConfig, "PANApp");
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                configDir = Path.Combine(home ?? ".", ".config", "PANApp");
            }

            return Path.Combine(configDir, "panapp.db");
        }

        return Path.Combine(AppContext.BaseDirectory, "panapp.db");
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
    }
}