using Hotel_Reservations_Manager.Data.Models;

namespace Hotel_Reservations_Manager.Services.Interfaces
{
    public interface IReservationService
    {
        Task<IEnumerable<Reservation>> GetAllReservationsAsync(int page = 1, int pageSize = 10);
        Task<Reservation?> GetReservationByIdAsync(int id);
        Task<IEnumerable<Reservation>> GetReservationsByUserIdAsync(string userId, int page = 1, int pageSize = 10);
        Task<IEnumerable<Reservation>> GetReservationsByRoomIdAsync(int roomId, int page = 1, int pageSize = 10);
        Task<int> GetTotalReservationsCountAsync();
        Task<int> GetUserReservationsCountAsync(string userId);
        Task<int> GetRoomReservationsCountAsync(int roomId);
        Task<decimal> CalculateTotalAmountAsync(int roomId, DateTime checkInDate, DateTime checkOutDate, List<int> clientIds, bool breakfastIncluded, bool allInclusive);
        Task CreateReservationAsync(Reservation reservation, List<int> clientIds);
        Task UpdateReservationAsync(Reservation reservation, List<int> clientIds);
        Task DeleteReservationAsync(int id);
        Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkInDate, DateTime checkOutDate, int? excludeReservationId = null);
        Task UpdateRoomAvailabilityAsync();
        
        // New methods for dashboard features
        Task<IEnumerable<Reservation>> GetUpcomingReservationsAsync(DateTime endDate, int limit = 5);
        Task<int> GetUpcomingCheckInsCountAsync(DateTime endDate);
        Task<IEnumerable<Reservation>> GetUpcomingCheckInsAsync(DateTime endDate, int limit = 5);
        Task<IEnumerable<Reservation>> GetUpcomingCheckInsAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 10);
        Task<decimal> GetRevenueForPeriodAsync(DateTime startDate, DateTime endDate);
        Task<int> GetOccupancyRateAsync(DateTime date);
        
        // Additional analytics methods
        Task<Dictionary<string, int>> GetReservationsByRoomTypeAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, decimal>> GetRevenueByMonthAsync(int year);
        Task<Dictionary<int, int>> GetOccupancyTrendAsync(DateTime startDate, DateTime endDate);
        Task<int> GetCanceledReservationsCountAsync(DateTime startDate, DateTime endDate);
        Task<double> GetAverageStayDurationAsync(DateTime startDate, DateTime endDate);
        
        // Methods for retrieving clients and checking room availability
        Task<IEnumerable<Client>> GetClientsForReservationAsync(int reservationId);
        Task<bool> IsRoomAvailableForUpdateAsync(int reservationId, int roomId, DateTime checkInDate, DateTime checkOutDate);
    }
} 