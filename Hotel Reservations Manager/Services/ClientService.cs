using Hotel_Reservations_Manager.Data;
using Hotel_Reservations_Manager.Data.Models;
using Hotel_Reservations_Manager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hotel_Reservations_Manager.Services
{
    public class ClientService : IClientService
    {
        private readonly ApplicationDbContext _context;

        public ClientService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Client>> GetAllClientsAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Clients
                .OrderBy(c => c.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Client> GetClientByIdAsync(int id)
        {
            return await _context.Clients
                .Include(c => c.ReservationClients)
                    .ThenInclude(rc => rc.Reservation)
                        .ThenInclude(r => r.Room)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Client>> SearchClientsAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllClientsAsync(page, pageSize);

            return await _context.Clients
                .Where(c => c.FirstName.Contains(searchTerm) || 
                            c.LastName.Contains(searchTerm) ||
                            c.Email.Contains(searchTerm))
                .OrderBy(c => c.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalClientsCountAsync()
        {
            return await _context.Clients.CountAsync();
        }

        public async Task<int> GetFilteredClientsCountAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetTotalClientsCountAsync();

            return await _context.Clients
                .Where(c => c.FirstName.Contains(searchTerm) || 
                            c.LastName.Contains(searchTerm))
                .CountAsync();
        }

        public async Task CreateClientAsync(Client client)
        {
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateClientAsync(Client client)
        {
            _context.Clients.Update(client);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteClientAsync(int id)
        {
            var client = await _context.Clients
                .Include(c => c.ReservationClients)
                .FirstOrDefaultAsync(c => c.Id == id);
                
            if (client != null)
            {
                // First, remove all associations in ReservationClients table
                if (client.ReservationClients != null && client.ReservationClients.Any())
                {
                    _context.ReservationClients.RemoveRange(client.ReservationClients);
                }
                
                // Then, remove the client
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Reservation>> GetClientReservationsAsync(int clientId)
        {
            return await _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .Where(r => r.ReservationClients.Any(rc => rc.ClientId == clientId))
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetClientsByIdsAsync(IEnumerable<int> clientIds)
        {
            return await _context.Clients
                .Where(c => clientIds.Contains(c.Id))
                .ToListAsync();
        }
    }
} 