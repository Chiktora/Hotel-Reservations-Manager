using Hotel_Reservations_Manager.Data;
using Hotel_Reservations_Manager.Data.Models;
using Hotel_Reservations_Manager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hotel_Reservations_Manager.Services
{
    public class RoomService : IRoomService
    {
        private readonly ApplicationDbContext _context;

        public RoomService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Room>> GetAllRoomsAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Rooms
                .OrderBy(r => r.RoomNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Room> GetRoomByIdAsync(int id)
        {
            // Include reservations and their related data for the Details view
            return await _context.Rooms
                .Include(r => r.Reservations)
                    .ThenInclude(res => res.ReservationClients)
                        .ThenInclude(rc => rc.Client)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkInDate, DateTime checkOutDate, int page = 1, int pageSize = 10)
        {
            // Get all rooms that are not reserved for the given date range
            var reservedRoomIds = await _context.Reservations
                .Where(r => (checkInDate < r.CheckOutDate && checkOutDate > r.CheckInDate))
                .Select(r => r.RoomId)
                .Distinct()
                .ToListAsync();

            return await _context.Rooms
                .Where(r => !reservedRoomIds.Contains(r.Id) && r.IsAvailable)
                .OrderBy(r => r.RoomNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Room>> FilterRoomsAsync(int? capacity, RoomType? type, bool? isAvailable, int page = 1, int pageSize = 10)
        {
            var query = _context.Rooms.AsQueryable();

            if (capacity.HasValue)
            {
                query = query.Where(r => r.Capacity == capacity.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(r => r.Type == type.Value);
            }

            if (isAvailable.HasValue)
            {
                query = query.Where(r => r.IsAvailable == isAvailable.Value);
            }

            return await query
                .OrderBy(r => r.RoomNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalRoomsCountAsync()
        {
            return await _context.Rooms.CountAsync();
        }

        public async Task<int> GetFilteredRoomsCountAsync(int? capacity, RoomType? type, bool? isAvailable)
        {
            var query = _context.Rooms.AsQueryable();

            if (capacity.HasValue)
            {
                query = query.Where(r => r.Capacity == capacity.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(r => r.Type == type.Value);
            }

            if (isAvailable.HasValue)
            {
                query = query.Where(r => r.IsAvailable == isAvailable.Value);
            }

            return await query.CountAsync();
        }

        public async Task CreateRoomAsync(Room room)
        {
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRoomAsync(Room room)
        {
            _context.Rooms.Update(room);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRoomAsync(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkInDate, DateTime checkOutDate, int? excludeReservationId = null)
        {
            var query = _context.Reservations
                .Where(r => r.RoomId == roomId)
                .Where(r => (checkInDate < r.CheckOutDate && checkOutDate > r.CheckInDate));
                
            // If we're updating an existing reservation, exclude it from the check
            if (excludeReservationId.HasValue)
            {
                query = query.Where(r => r.Id != excludeReservationId.Value);
            }
            
            // Room is available if no conflicting reservations exist
            return await query.CountAsync() == 0;
        }
    }
} 