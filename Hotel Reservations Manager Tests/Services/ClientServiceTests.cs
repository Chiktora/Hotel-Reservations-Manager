using Microsoft.EntityFrameworkCore;
using Hotel_Reservations_Manager.Data;
using Hotel_Reservations_Manager.Data.Models;
using Hotel_Reservations_Manager.Services;
using Hotel_Reservations_Manager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;

namespace Hotel_Reservations_Manager_Tests.Services
{
    [TestFixture]
    public class ClientServiceTests
    {
        private ApplicationDbContext _context;
        private IClientService _clientService;
        private List<Client> _testClients;

        [SetUp]
        public void Setup()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"HotelReservationsDb_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            
            // Seed test data
            SeedTestData();
            
            // Initialize service with test data
            _clientService = new ClientService(_context);
        }

        private void SeedTestData()
        {
            // Add test clients
            _testClients = new List<Client>
            {
                new Client 
                { 
                    Id = 1, 
                    FirstName = "John", 
                    LastName = "Doe", 
                    Email = "john.doe@example.com", 
                    PhoneNumber = "123456789", 
                    IsAdult = true
                },
                new Client 
                { 
                    Id = 2, 
                    FirstName = "Jane", 
                    LastName = "Doe", 
                    Email = "jane.doe@example.com", 
                    PhoneNumber = "987654321", 
                    IsAdult = true
                },
                new Client 
                { 
                    Id = 3, 
                    FirstName = "Kid", 
                    LastName = "Doe", 
                    Email = "kid.doe@example.com", 
                    PhoneNumber = "555555555", 
                    IsAdult = false
                },
                new Client 
                { 
                    Id = 4, 
                    FirstName = "Inactive", 
                    LastName = "Client", 
                    Email = "inactive@example.com", 
                    PhoneNumber = "111111111", 
                    IsAdult = true
                }
            };
            _context.Clients.AddRange(_testClients);
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetAllClientsAsync_ReturnsAllClients()
        {
            // Act
            var clients = await _clientService.GetAllClientsAsync();

            // Assert
            Assert.That(clients, Is.Not.Null);
            Assert.That(clients.Count(), Is.EqualTo(4)); // All clients
        }

        [Test]
        public async Task GetAllClientsAsync_WithPagination_ReturnsCorrectPage()
        {
            // Act
            var clients = await _clientService.GetAllClientsAsync(page: 1, pageSize: 2);

            // Assert
            Assert.That(clients, Is.Not.Null);
            Assert.That(clients.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GetClientByIdAsync_ExistingId_ReturnsClient()
        {
            // Act
            var client = await _clientService.GetClientByIdAsync(1);

            // Assert
            Assert.That(client, Is.Not.Null);
            Assert.That(client.FirstName, Is.EqualTo("John"));
            Assert.That(client.LastName, Is.EqualTo("Doe"));
        }

        [Test]
        public async Task GetClientByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var client = await _clientService.GetClientByIdAsync(999);

            // Assert
            Assert.That(client, Is.Null);
        }

        [Test]
        public async Task CreateClientAsync_ValidClient_AddsToDatabase()
        {
            // Arrange
            var newClient = new Client
            {
                FirstName = "New",
                LastName = "Client",
                Email = "new.client@example.com",
                PhoneNumber = "999999999",
                IsAdult = true
            };

            // Act
            await _clientService.CreateClientAsync(newClient);

            // Assert
            var savedClient = await _context.Clients.FirstOrDefaultAsync(c => c.Email == "new.client@example.com");
            Assert.That(savedClient, Is.Not.Null);
            Assert.That(savedClient.FirstName, Is.EqualTo("New"));
            Assert.That(savedClient.LastName, Is.EqualTo("Client"));
        }

        [Test]
        public async Task UpdateClientAsync_ValidClient_UpdatesInDatabase()
        {
            // Arrange
            var client = await _context.Clients.FindAsync(1);
            client.FirstName = "Updated";
            client.PhoneNumber = "000000000";

            // Act
            await _clientService.UpdateClientAsync(client);

            // Assert
            var updatedClient = await _context.Clients.FindAsync(1);
            Assert.That(updatedClient.FirstName, Is.EqualTo("Updated"));
            Assert.That(updatedClient.PhoneNumber, Is.EqualTo("000000000"));
        }

        [Test]
        public async Task SearchClientsAsync_ReturnsMatchingClients()
        {
            // Act
            var clients = await _clientService.SearchClientsAsync("Doe");

            // Assert
            Assert.That(clients, Is.Not.Null);
            Assert.That(clients.Count(), Is.EqualTo(3)); // All clients with "Doe" in their name
        }

        [Test]
        public async Task SearchClientsAsync_WithEmail_ReturnsMatchingClients()
        {
            // Act
            var clients = await _clientService.SearchClientsAsync("john.doe");

            // Assert
            Assert.That(clients, Is.Not.Null);
            Assert.That(clients.Count(), Is.EqualTo(1));
            Assert.That(clients.First().FirstName, Is.EqualTo("John"));
        }

        [Test]
        public async Task GetTotalClientsCountAsync_ReturnsCorrectCount()
        {
            // Act
            var count = await _clientService.GetTotalClientsCountAsync();

            // Assert
            Assert.That(count, Is.EqualTo(4)); // All clients
        }
    }
} 