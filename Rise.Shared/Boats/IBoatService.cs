using System.Threading;

namespace Rise.Shared.Boats;

public interface IBoatService
{

    Task<IEnumerable<BoatDto.BoatIndex>?> GetAllBoatsAsync();
    Task<BoatDto.BoatIndex> CreateNewBoatAsync(BoatDto.CreateBoatDto createDto);

    Task<int> GetAvailableBoatsCountAsync();
    Task<BoatDto.BoatIndex?> GetBoatByIdAsync(int boatId);
    Task<BoatDto.BoatIndex?> UpdateBoatStatusAsync(int boatId, BoatDto.BoatStatus status);
    Task<bool> DeleteBoatAsync(int boatId);
}
