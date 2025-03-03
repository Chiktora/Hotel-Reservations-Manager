using Hotel_Reservations_Manager.Data.Models;

namespace Hotel_Reservations_Manager.Services.Interfaces
{
    public interface IRoomService
    {
        Task<IEnumerable<Room>> GetAllRoomsAsync(int page = 1, int pageSize = 10);
        Task<Room> GetRoomByIdAsync(int id);
        Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkInDate, DateTime checkOutDate, int page = 1, int pageSize = 10);
        Task<IEnumerable<Room>> FilterRoomsAsync(int? capacity, RoomType? type, bool? isAvailable, int page = 1, int pageSize = 10);
        Task<int> GetTotalRoomsCountAsync();
        Task<int> GetFilteredRoomsCountAsync(int? capacity, RoomType? type, bool? isAvailable);
        Task CreateRoomAsync(Room room);
        Task UpdateRoomAsync(Room room);
        Task DeleteRoomAsync(int id);
        Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkInDate, DateTime checkOutDate, int? excludeReservationId = null);
    }
} 