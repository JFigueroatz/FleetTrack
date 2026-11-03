using FleetTrack.Mobile.Models;
using SQLite;

namespace FleetTrack.Mobile.Services;

/// <summary>Base SQLite local: ultima ruta consultada y cola de operaciones pendientes de sincronizar.</summary>
public class LocalDatabase
{
    private readonly SQLiteAsyncConnection _connection;

    public LocalDatabase(string databasePath)
    {
        _connection = new SQLiteAsyncConnection(databasePath);
        _connection.CreateTableAsync<CachedRoute>().Wait();
        _connection.CreateTableAsync<PendingOperation>().Wait();
    }

    public Task SaveRouteAsync(CachedRoute route) => _connection.InsertOrReplaceAsync(route);

    public async Task<CachedRoute?> GetCachedRouteAsync() =>
        await _connection.Table<CachedRoute>().FirstOrDefaultAsync();

    public Task EnqueueAsync(PendingOperation operation) => _connection.InsertAsync(operation);

    public Task<List<PendingOperation>> GetPendingAsync() =>
        _connection.Table<PendingOperation>().OrderBy(o => o.Id).ToListAsync();

    public Task<int> CountPendingAsync() => _connection.Table<PendingOperation>().CountAsync();

    public Task UpdatePendingAsync(PendingOperation operation) => _connection.UpdateAsync(operation);

    public Task RemovePendingAsync(PendingOperation operation) => _connection.DeleteAsync(operation);
}
