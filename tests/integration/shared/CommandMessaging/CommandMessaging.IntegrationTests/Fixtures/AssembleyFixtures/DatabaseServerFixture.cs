using Cqrs.IntegrationTests.Fixtures.AssembleyFixtures;
using Microsoft.Data.SqlClient;
using Respawn;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(DatabaseServerFixture))]
namespace Cqrs.IntegrationTests.Fixtures.AssembleyFixtures;

public sealed class DatabaseServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-CU10-ubuntu-22.04")
        .Build();

    public string GetConnectionString()
        => _dbContainer.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();  
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    public async Task<string> CreateDatabaseAsync(string databaseName)
    {
        using var connection = new SqlConnection(_dbContainer.GetConnectionString());
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{databaseName}];";
        await command.ExecuteNonQueryAsync();

        return GetDatabaseConnectionString(databaseName);
    }

    public async Task<Respawner> GetRespawnerAsync(string databaseName)
    {
        using var connection = new SqlConnection(GetDatabaseConnectionString(databaseName));
        await connection.OpenAsync();
        return await Respawner.CreateAsync(connection);
    }

    public async Task DropDatabaseAsync(string databaseName)
    {
        using var connection = new SqlConnection(GetDatabaseConnectionString(databaseName));
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE [{databaseName}];";
        await command.ExecuteNonQueryAsync();
    }

    public string GetDatabaseConnectionString(string databaseName)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(_dbContainer.GetConnectionString());
        connectionStringBuilder.InitialCatalog = databaseName;
        return connectionStringBuilder.ConnectionString;
    }
}