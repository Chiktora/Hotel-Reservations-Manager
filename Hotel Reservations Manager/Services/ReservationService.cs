using Hotel_Reservations_Manager.Data;
using Hotel_Reservations_Manager.Data.Models;
using Hotel_Reservations_Manager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hotel_Reservations_Manager.Services
{
    public class ReservationService : IReservationService
    {
        private readonly ApplicationDbContext _context;

        public ReservationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Reservation>> GetAllReservationsAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .Include(r => r.ReservationClients)
                    .ThenInclude(rc => rc.Client)
                .OrderByDescending(r => r.CheckInDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Reservation?> GetReservationByIdAsync(int id)
        {
            return await _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .Include(r => r.ReservationClients)
                    .ThenInclude(rc => rc.Client)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByUserIdAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .Include(r => r.ReservationClients)
                    .ThenInclude(rc => rc.Client)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CheckInDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByRoomIdAsync(int roomId, int page = 1, int pageSize = 10)
        {
            return await _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .Include(r => r.ReservationClients)
                    .ThenInclude(rc => rc.Client)
                .Where(r => r.RoomId == roomId)
                .OrderByDescending(r => r.CheckInDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalReservationsCountAsync()
        {
            return await _context.Reservations.CountAsync();
        }

        public async Task<int> GetUserReservationsCountAsync(string userId)
        {
            return await _context.Reservations
                .Where(r => r.UserId == userId)
                .CountAsync();
        }

        public async Task<int> GetRoomReservationsCountAsync(int roomId)
        {
            return await _context.Reservations
                .Where(r => r.RoomId == roomId)
                .CountAsync();
        }

        public async Task<decimal> CalculateTotalAmountAsync(int roomId, DateTime checkInDate, DateTime checkOutDate, List<int> clientIds, bool breakfastIncluded, bool allInclusive)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
                throw new ArgumentException("Room not found");

            var clients = await _context.Clients
                .Where(c => clientIds.Contains(c.Id))
                .ToListAsync();

            if (clients.Count == 0)
                throw new ArgumentException("No clients selected");

            if (clients.Count > room.Capacity)
                throw new ArgumentException($"Room capacity exceeded. Maximum capacity is {room.Capacity}");

            int nights = (int)(checkOutDate - checkInDate).TotalDays;
            if (nights <= 0)
                throw new ArgumentException("Check-out date must be after check-in date");

            decimal totalAmount = 0;

            // Calculate base price based on adult/child rates
            foreach (var client in clients)
            {
                decimal bedPrice = client.IsAdult ? room.AdultBedPrice : room.ChildBedPrice;
                totalAmount += bedPrice * nights;
            }

            // Add extras
            if (breakfastIncluded)
            {
                // Add 10% for breakfast
                totalAmount += totalAmount * 0.1m;
            }

            if (allInclusive)
            {
                // Add 20% for all inclusive
                totalAmount += totalAmount * 0.2m;
            }

            return totalAmount;
        }

        public async Task CreateReservationAsync(Reservation reservation, List<int> clientIds)
        {
            if (reservation == null)
                throw new ArgumentNullException(nameof(reservation), "Reservation cannot be null");
            
            if (clientIds == null || !clientIds.Any())
                throw new ArgumentException("At least one client must be selected", nameof(clientIds));

            // Remove duplicates for safety
            clientIds = clientIds.Distinct().ToList();
            
            // Check if we're using an in-memory database (for tests)
            bool isInMemoryDb = _context.Database.ProviderName?.Contains("InMemory") ?? false;

            if (!isInMemoryDb)
            {
                // Use transaction for real database
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await CreateReservationInternalAsync(reservation, clientIds);
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException($"Failed to create reservation: {ex.Message}", ex);
                }
            }
            else
            {
                // For in-memory database (tests), don't use transactions
                await CreateReservationInternalAsync(reservation, clientIds);
            }
        }

        private async Task CreateReservationInternalAsync(Reservation reservation, List<int> clientIds)
        {
            try
            {
                // 1. Validate reservation
                if (reservation == null)
                    throw new ArgumentNullException(nameof(reservation), "Reservation cannot be null");
                
                // 2. Validate clients
                if (clientIds == null || !clientIds.Any())
                    throw new ArgumentException("At least one client must be selected", nameof(clientIds));
                
                // 3. Get the room
                var room = await _context.Rooms.FindAsync(reservation.RoomId);
                if (room == null)
                    throw new ArgumentException($"Room with ID {reservation.RoomId} not found");
                
                // 4. Check room capacity
                if (clientIds.Count > room.Capacity)
                    throw new ArgumentException($"Room capacity exceeded. Room {room.RoomNumber} maximum capacity is {room.Capacity}, but selected {clientIds.Count} clients");
                
                // 5. Check room availability
                bool isAvailable = await IsRoomAvailableAsync(reservation.RoomId, reservation.CheckInDate, reservation.CheckOutDate);
                if (!isAvailable)
                    throw new InvalidOperationException($"Room {room.RoomNumber} is not available for the selected dates");
                
                // 6. Verify client existence
                foreach (var clientId in clientIds)
                {
                    var client = await _context.Clients.FindAsync(clientId);
                    if (client == null)
                        throw new ArgumentException($"Client with ID {clientId} not found");
                }
                
                // 7. Set reservation date if not set
                if (reservation.CheckInDate == default)
                    throw new ArgumentException("Check-in date must be specified");
                    
                if (reservation.CheckOutDate == default)
                    throw new ArgumentException("Check-out date must be specified");
                
                if (reservation.CheckInDate >= reservation.CheckOutDate)
                    throw new ArgumentException("Check-out date must be after check-in date");
                    
                // 8. Add the reservation
                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();
                
                // 9. Add clients to reservation
                foreach (var clientId in clientIds)
                {
                    var reservationClient = new ReservationClient
                    {
                        ReservationId = reservation.Id,
                        ClientId = clientId
                    };
                    _context.ReservationClients.Add(reservationClient);
                }
                
                // 10. Update room availability if check-in is today
                if (reservation.CheckInDate.Date == DateTime.Today.Date)
                {
                    room.IsAvailable = false;
                    _context.Rooms.Update(room);
                }
                
                // 11. Save all changes
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Add additional context to the exception
                throw new InvalidOperationException($"Reservation creation failed: {ex.Message}", ex);
            }
        }

        public async Task UpdateReservationAsync(Reservation reservation, List<int> clientIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if room is available (excluding this reservation)
                bool isAvailable = await IsRoomAvailableAsync(reservation.RoomId, reservation.CheckInDate, reservation.CheckOutDate, reservation.Id);
                if (!isAvailable)
                    throw new InvalidOperationException("Room is not available for the selected dates");

                // Update reservation
                _context.Reservations.Update(reservation);

                // Remove existing clients
                var existingClients = await _context.ReservationClients
                    .Where(rc => rc.ReservationId == reservation.Id)
                    .ToListAsync();

                _context.ReservationClients.RemoveRange(existingClients);

                // Add new clients
                foreach (var clientId in clientIds)
                {
                    var reservationClient = new ReservationClient
                    {
                        ReservationId = reservation.Id,
                        ClientId = clientId
                    };
                    _context.ReservationClients.Add(reservationClient);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteReservationAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var reservation = await _context.Reservations.FindAsync(id);
                if (reservation == null)
                    return;

                // Remove clients from reservation
                var reservationClients = await _context.ReservationClients
                    .Where(rc => rc.ReservationId == id)
                    .ToListAsync();

                _context.ReservationClients.RemoveRange(reservationClients);

                // Remove reservation
                _context.Reservations.Remove(reservation);

                // Update room availability if needed
                if (reservation.CheckOutDate > DateTime.Today)
                {
                    var room = await _context.Rooms.FindAsync(reservation.RoomId);
                    if (room != null)
                    {
                        // Check if there are any other active reservations for this room
                        var activeReservations = await _context.Reservations
                            .Where(r => r.RoomId == room.Id && r.Id != id && r.CheckOutDate > DateTime.Today)
                            .AnyAsync();

                        if (!activeReservations)
                        {
                            room.IsAvailable = true;
                            _context.Rooms.Update(room);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkInDate, DateTime checkOutDate, int? excludeReservationId = null)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null || !room.IsAvailable)
                return false;

            var query = _context.Reservations
                .Where(r => r.RoomId == roomId && 
                           (checkInDate < r.CheckOutDate && checkOutDate > r.CheckInDate));

            if (excludeReservationId.HasValue)
            {
                query = query.Where(r => r.Id != excludeReservationId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task UpdateRoomAvailabilityAsync()
        {
            // Get all rooms
            var rooms = await _context.Rooms.ToListAsync();

            foreach (var room in rooms)
            {
                // Check if there are any active reservations for this room
                var hasActiveReservation = await _context.Reservations
                    .Where(r => r.RoomId == room.Id && 
                               r.CheckInDate <= DateTime.Today && 
                               r.CheckOutDate > DateTime.Today)
                    .AnyAsync();

                // Update room availability
                if (hasActiveReservation && room.IsAvailable)
                {
                    room.IsAvailable = false;
                    _context.Rooms.Update(room);
                }
                else if (!hasActiveReservation && !room.IsAvailable)
                {
                    room.IsAvailable = true;
                    _context.Rooms.Update(room);
                }
            }

            await _context.SaveChangesAsync();
        }

        // New methods for dashboard features
        public async Task<IEnumerable<Reservation>> GetUpcomingReservationsAsync(DateTime endDate, int limit = 5)
        {
            return await _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .Include(r => r.ReservationClients)
                    .ThenInclude(rc => rc.Client)
                .Where(r => r.CheckInDate >= DateTime.Now.Date && r.CheckInDate <= endDate)
                .OrderBy(r => r.CheckInDate)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> GetUpcomingCheckInsCountAsync(DateTime endDate)
        {
            return await _context.Reservations
                .Where(r => r.CheckInDate >= DateTime.Now.Date && r.CheckInDate <= endDate)
                .CountAsync();
        }

        public async Task<IEnumerable<Reservation>> GetUpcomingCheckInsAsync(DateTime endDate, int limit = 5)
        {
            return await _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .Include(r => r.ReservationClients)
                    .ThenInclude(rc => rc.Client)
                .Where(r => r.CheckInDate >= DateTime.Now.Date && r.CheckInDate <= endDate)
                .OrderBy(r => r.CheckInDate)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetUpcomingCheckInsAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 10)
        {
            return await _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .Include(r => r.ReservationClients)
                    .ThenInclude(rc => rc.Client)
                .Where(r => r.CheckInDate >= startDate && r.CheckInDate <= endDate)
                .OrderBy(r => r.CheckInDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<decimal> GetRevenueForPeriodAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Reservations
                .Where(r => r.CheckInDate >= startDate && r.CheckOutDate <= endDate)
                .SumAsync(r => r.TotalAmount);
        }

        public async Task<int> GetOccupancyRateAsync(DateTime date)
        {
            // Get total number of rooms
            int totalRooms = await _context.Rooms.CountAsync();
            
            if (totalRooms == 0)
                return 0;

            // Get number of occupied rooms on the given date
            int occupiedRooms = await _context.Reservations
                .Where(r => r.CheckInDate <= date && r.CheckOutDate >= date)
                .Select(r => r.RoomId)
                .Distinct()
                .CountAsync();

            // Calculate occupancy rate as a percentage
            return (int)Math.Round((double)occupiedRooms / totalRooms * 100);
        }
        
        // Additional analytics methods implementation
        public async Task<Dictionary<string, int>> GetReservationsByRoomTypeAsync(DateTime startDate, DateTime endDate)
        {
            var reservations = await _context.Reservations
                .Include(r => r.Room)
                .Where(r => r.CheckInDate >= startDate && r.CheckInDate <= endDate)
                .ToListAsync();

            return reservations
                .GroupBy(r => r.Room.Type.ToString())
                .ToDictionary(
                    group => group.Key,
                    group => group.Count()
                );
        }

        public async Task<Dictionary<string, decimal>> GetRevenueByMonthAsync(int year)
        {
            var reservations = await _context.Reservations
                .Where(r => r.CheckInDate.Year == year)
                .ToListAsync();

            return reservations
                .GroupBy(r => r.CheckInDate.ToString("MMMM"))
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(r => r.TotalAmount)
                );
        }

        public async Task<Dictionary<int, int>> GetOccupancyTrendAsync(DateTime startDate, DateTime endDate)
        {
            var result = new Dictionary<int, int>();
            int totalRooms = await _context.Rooms.CountAsync();
            
            if (totalRooms == 0)
                return result;

            // Generate dates between start and end
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var occupiedRooms = await _context.Reservations
                    .Where(r => r.CheckInDate <= date && r.CheckOutDate > date)
                    .Select(r => r.RoomId)
                    .Distinct()
                    .CountAsync();

                int dayOfYear = date.DayOfYear;
                int occupancyRate = (int)Math.Round((double)occupiedRooms / totalRooms * 100);
                
                result.Add(dayOfYear, occupancyRate);
            }

            return result;
        }

        public async Task<int> GetCanceledReservationsCountAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Reservations
                .Where(r => r.IsCanceled && r.CheckInDate >= startDate && r.CheckInDate <= endDate)
                .CountAsync();
        }

        public async Task<double> GetAverageStayDurationAsync(DateTime startDate, DateTime endDate)
        {
            var reservations = await _context.Reservations
                .Where(r => r.CheckInDate >= startDate && r.CheckInDate <= endDate && !r.IsCanceled)
                .ToListAsync();

            if (!reservations.Any())
                return 0;

            var totalStayDays = reservations.Sum(r => (r.CheckOutDate - r.CheckInDate).TotalDays);
            return Math.Round(totalStayDays / reservations.Count, 1);
        }

        public async Task<IEnumerable<Client>> GetClientsForReservationAsync(int reservationId)
        {
            var reservationClients = await _context.ReservationClients
                .Include(rc => rc.Client)
                .Where(rc => rc.ReservationId == reservationId)
                .ToListAsync();

            return reservationClients.Select(rc => rc.Client).ToList();
        }

        public async Task<bool> IsRoomAvailableForUpdateAsync(int reservationId, int roomId, DateTime checkInDate, DateTime checkOutDate)
        {
            // Check if there are any overlapping reservations for the same room, excluding the current reservation
            var overlappingReservations = await _context.Reservations
                .Where(r => r.Id != reservationId && r.RoomId == roomId && !r.IsCanceled)
                .Where(r =>
                    (checkInDate < r.CheckOutDate) && (checkOutDate > r.CheckInDate)
                )
                .AnyAsync();

            // Room is available if there are no overlapping reservations
            return !overlappingReservations;
        }
    }
} 