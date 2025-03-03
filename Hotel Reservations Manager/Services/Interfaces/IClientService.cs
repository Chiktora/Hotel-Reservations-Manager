using Hotel_Reservations_Manager.Data.Models;

namespace Hotel_Reservations_Manager.Services.Interfaces
{
    public interface IClientService
    {
        Task<IEnumerable<Client>> GetAllClientsAsync(int page = 1, int pageSize = 10);
        Task<Client> GetClientByIdAsync(int id);
        Task<IEnumerable<Client>> SearchClientsAsync(string searchTerm, int page = 1, int pageSize = 10);
        Task<int> GetTotalClientsCountAsync();
        Task<int> GetFilteredClientsCountAsync(string searchTerm);
        Task CreateClientAsync(Client client);
        Task UpdateClientAsync(Client client);
        Task DeleteClientAsync(int id);
        Task<IEnumerable<Reservation>> GetClientReservationsAsync(int clientId);
        Task<IEnumerable<Client>> GetClientsByIdsAsync(IEnumerable<int> clientIds);
    }
} 