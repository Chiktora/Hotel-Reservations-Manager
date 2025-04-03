using Hotel_Reservations_Manager.Data.Models;

namespace Hotel_Reservations_Manager.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync(int page = 1, int pageSize = 10);
        Task<ApplicationUser> GetUserByIdAsync(string id);
        Task<IEnumerable<ApplicationUser>> SearchUsersAsync(string searchTerm, int page = 1, int pageSize = 10);
        Task<int> GetTotalUsersCountAsync();
        Task<int> GetFilteredUsersCountAsync(string searchTerm);
        Task UpdateUserAsync(ApplicationUser user);
        Task DeactivateUserAsync(string id, DateTime releaseDate);
        Task<bool> IsUserActiveAsync(string id);
        Task ReactivateUserAsync(string id);
    }
} 