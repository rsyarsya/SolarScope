using SolarScope.Core.Models;
using SolarScope.Core.Services;

namespace SolarScope;

internal sealed class UnavailableSimulationRepository(string message) : ISimulationRepository
{
    public Task<ServiceResult<int>> InsertSimulationAsync(Simulation simulation) => Fail<int>();
    public Task<ServiceResult<List<Simulation>>> GetAllSimulationsAsync() => Fail<List<Simulation>>();
    public Task<ServiceResult<Simulation>> GetSimulationByIdAsync(int id) => Fail<Simulation>();
    public Task<ServiceResult<bool>> DeleteSimulationAsync(int id) => Fail<bool>();

    private Task<ServiceResult<T>> Fail<T>() => Task.FromResult(ServiceResult<T>.Fail(message));
}
