using System.Text.Json;
using SolarScope.Core.Models;
using SolarScope.Core.Services;

internal sealed class TestSimulationRepository : ISimulationRepository
{
    internal const string Error = "Database uji tidak dapat diakses.";
    private readonly Dictionary<int, byte[]> rows = new();
    private int nextId = 1;
    private TaskCompletionSource<bool>? nextSave;

    internal int Count => rows.Count;
    internal int SaveRequests { get; private set; }
    internal int DeleteRequests { get; private set; }
    internal bool FailNextList { get; set; }
    internal bool FailNextOpen { get; set; }
    internal bool FailNextDelete { get; set; }

    internal TaskCompletionSource<bool> PauseNextSave() =>
        nextSave = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<ServiceResult<int>> InsertSimulationAsync(Simulation simulation)
    {
        SaveRequests++;
        if (nextSave is { } pending)
        {
            nextSave = null;
            if (!await pending.Task) return ServiceResult<int>.Fail(Error);
        }

        var snapshot = Deserialize(JsonSerializer.SerializeToUtf8Bytes(simulation));
        snapshot.SimulationId = nextId++;
        snapshot.CreatedAt = DateTime.UtcNow;
        rows.Add(snapshot.SimulationId, JsonSerializer.SerializeToUtf8Bytes(snapshot));
        return ServiceResult<int>.Ok(snapshot.SimulationId);
    }

    public Task<ServiceResult<List<Simulation>>> GetAllSimulationsAsync()
    {
        if (FailNextList)
        {
            FailNextList = false;
            return Task.FromResult(ServiceResult<List<Simulation>>.Fail(Error));
        }

        var list = rows.OrderByDescending(row => row.Key).Select(row => Deserialize(row.Value)).ToList();
        foreach (var simulation in list) simulation.Summary!.MonthlyProductions.Clear();
        return Task.FromResult(ServiceResult<List<Simulation>>.Ok(list));
    }

    public Task<ServiceResult<Simulation>> GetSimulationByIdAsync(int id)
    {
        if (FailNextOpen)
        {
            FailNextOpen = false;
            return Task.FromResult(ServiceResult<Simulation>.Fail(Error));
        }

        return Task.FromResult(rows.TryGetValue(id, out var snapshot)
            ? ServiceResult<Simulation>.Ok(Deserialize(snapshot))
            : ServiceResult<Simulation>.Fail(Error));
    }

    public Task<ServiceResult<bool>> DeleteSimulationAsync(int id)
    {
        DeleteRequests++;
        if (FailNextDelete)
        {
            FailNextDelete = false;
            return Task.FromResult(ServiceResult<bool>.Fail(Error));
        }

        return Task.FromResult(rows.Remove(id)
            ? ServiceResult<bool>.Ok(true)
            : ServiceResult<bool>.Fail(Error));
    }

    private static Simulation Deserialize(byte[] data) => JsonSerializer.Deserialize<Simulation>(data)!;
}
