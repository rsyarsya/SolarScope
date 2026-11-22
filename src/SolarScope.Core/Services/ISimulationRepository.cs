using SolarScope.Core.Models;

namespace SolarScope.Core.Services;

public interface ISimulationRepository
{
    Task<ServiceResult<int>> InsertSimulationAsync(Simulation simulation);
    Task<ServiceResult<List<Simulation>>> GetAllSimulationsAsync();
    Task<ServiceResult<Simulation>> GetSimulationByIdAsync(int id);
    Task<ServiceResult<bool>> DeleteSimulationAsync(int id);
}
