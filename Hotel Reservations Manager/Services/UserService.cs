using Hotel_Reservations_Manager.Data;
using Hotel_Reservations_Manager.Data.Models;
using Hotel_Reservations_Manager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hotel_Reservations_Manager.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Users
                .OrderBy(u => u.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<ApplicationUser> GetUserByIdAsync(string id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<IEnumerable<ApplicationUser>> SearchUsersAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllUsersAsync(page, pageSize);

            return await _context.Users
                .Where(u => u.UserName.Contains(searchTerm) || 
                            u.FirstName.Contains(searchTerm) || 
                            u.MiddleName.Contains(searchTerm) || 
                            u.LastName.Contains(searchTerm) || 
                            u.Email.Contains(searchTerm))
                .OrderBy(u => u.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<int> GetFilteredUsersCountAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetTotalUsersCountAsync();

            return await _context.Users
                .Where(u => u.UserName.Contains(searchTerm) || 
                            u.FirstName.Contains(searchTerm) || 
                            u.MiddleName.Contains(searchTerm) || 
                            u.LastName.Contains(searchTerm) || 
                            u.Email.Contains(searchTerm))
                .CountAsync();
        }

        public async Task UpdateUserAsync(ApplicationUser user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeactivateUserAsync(string id, DateTime releaseDate)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = false;
                user.ReleaseDate = releaseDate;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsUserActiveAsync(string id)
        {
            var user = await _context.Users.FindAsync(id);
            return user?.IsActive ?? false;
        }
    }
} 