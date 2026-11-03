using FleetTrack.Mobile.Models;
using SQLite;

namespace FleetTrack.Mobile.Services;

/// <summary>
/// Base SQLite local. D03 agregara aqui la cola de operaciones pendientes
/// (shortcut: por ahora solo cachea la ultima ruta consultada).
/// </summary>
public class LocalDatabase
{
    private readonly SQLiteAsyncConnection _connection;

    public LocalDatabase(string databasePath)
    {
        _connection = new SQLiteAsyncConnection(databasePath);
        _connection.CreateTableAsync<CachedRoute>().Wait();
    }

    public Task SaveRouteAsync(CachedRoute route) => _connection.InsertOrReplaceAsync(route);

    public async Task<CachedRoute?> GetCachedRouteAsync() =>
        await _connection.Table<CachedRoute>().FirstOrDefaultAsync();
}
