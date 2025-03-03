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
using Microsoft.AspNetCore.Identity;

namespace Hotel_Reservations_Manager_Tests.Services
{
    [TestFixture]
    public class ReservationServiceTests
    {
        private ApplicationDbContext _context;
        private IReservationService _reservationService;
        private List<Room> _testRooms;
        private List<Client> _testClients;
        private List<Reservation> _testReservations;

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
            _reservationService = new ReservationService(_context);
        }

        private void SeedTestData()
        {
            // Add test rooms
            _testRooms = new List<Room>
            {
                new Room 
                { 
                    Id = 1, 
                    RoomNumber = "101", 
                    Type = RoomType.DoubleRoom, 
                    Capacity = 2, 
                    AdultBedPrice = 100.00m, 
                    ChildBedPrice = 50.00m,
                    IsAvailable = true
                },
                new Room 
                { 
                    Id = 2, 
                    RoomNumber = "102", 
                    Type = RoomType.Apartment, 
                    Capacity = 4, 
                    AdultBedPrice = 200.00m, 
                    ChildBedPrice = 100.00m,
                    IsAvailable = true
                }
            };
            _context.Rooms.AddRange(_testRooms);

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
                }
            };
            _context.Clients.AddRange(_testClients);

            // Add test user
            var user = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "testuser@example.com",
                Email = "testuser@example.com",
                FirstName = "Test",
                MiddleName = "Middle",
                LastName = "User",
                PhoneNumber = "1234567890",
                PersonalId = "1234567890",
                IsActive = true,
                HireDate = DateTime.Now.AddYears(-1)
            };
            _context.Users.Add(user);

            // Add test reservations
            _testReservations = new List<Reservation>
            {
                new Reservation
                {
                    Id = 1,
                    RoomId = 1,
                    UserId = "test-user-id",
                    CheckInDate = DateTime.Now.AddDays(1),
                    CheckOutDate = DateTime.Now.AddDays(3),
                    TotalAmount = 300.00m,
                    BreakfastIncluded = true,
                    AllInclusive = false,
                    IsCanceled = false
                }
            };
            _context.Reservations.AddRange(_testReservations);

            // Add reservation-client relationships
            var reservationClient = new ReservationClient
            {
                ReservationId = 1,
                ClientId = 1
            };
            _context.ReservationClients.Add(reservationClient);

            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetReservationByIdAsync_ExistingId_ReturnsReservation()
        {
            // Act
            var result = await _reservationService.GetReservationByIdAsync(1);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(1));
            Assert.That(result.RoomId, Is.EqualTo(1));
        }

        [Test]
        public async Task GetReservationByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var result = await _reservationService.GetReservationByIdAsync(999);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task CalculateTotalAmountAsync_WithValidData_ReturnsCorrectAmount()
        {
            // Arrange
            int roomId = 1;
            DateTime checkIn = DateTime.Now.AddDays(5);
            DateTime checkOut = DateTime.Now.AddDays(7); // 2 nights
            List<int> clientIds = new List<int> { 1, 3 }; // Adult and child
            bool breakfastIncluded = true;
            bool allInclusive = false;

            // Act
            var result = await _reservationService.CalculateTotalAmountAsync(
                roomId, checkIn, checkOut, clientIds, breakfastIncluded, allInclusive);

            // Assert - Base price: (100 + 50) * 2 nights = 300, plus extras for breakfast
            Assert.That(result, Is.GreaterThan(300));
        }

        [Test]
        public async Task IsRoomAvailableAsync_AvailableRoom_ReturnsTrue()
        {
            // Arrange
            int roomId = 2; // Room with no reservations
            DateTime checkIn = DateTime.Now.AddDays(5);
            DateTime checkOut = DateTime.Now.AddDays(7);

            // Act
            var result = await _reservationService.IsRoomAvailableAsync(roomId, checkIn, checkOut);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsRoomAvailableAsync_UnavailableRoom_ReturnsFalse()
        {
            // Arrange
            int roomId = 1; // Room with existing reservation
            DateTime checkIn = DateTime.Now.AddDays(2); // Overlaps with existing reservation
            DateTime checkOut = DateTime.Now.AddDays(4);

            // Act
            var result = await _reservationService.IsRoomAvailableAsync(roomId, checkIn, checkOut);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CreateReservationAsync_ValidReservation_AddsToDatabase()
        {
            // Arrange
            var newReservation = new Reservation
            {
                RoomId = 2,
                UserId = "test-user-id",
                CheckInDate = DateTime.Now.AddDays(10),
                CheckOutDate = DateTime.Now.AddDays(12),
                TotalAmount = 400.00m,
                BreakfastIncluded = true,
                AllInclusive = true
            };
            
            var clientIds = new List<int> { 1, 2 };

            // Act
            await _reservationService.CreateReservationAsync(newReservation, clientIds);

            // Assert
            var savedReservation = await _context.Reservations
                .Include(r => r.ReservationClients)
                .FirstOrDefaultAsync(r => r.RoomId == 2 && r.CheckInDate == newReservation.CheckInDate);
            
            Assert.That(savedReservation, Is.Not.Null);
            Assert.That(savedReservation.TotalAmount, Is.EqualTo(400.00m));
            Assert.That(savedReservation.ReservationClients.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetClientsForReservationAsync_ExistingReservation_ReturnsClients()
        {
            // Act
            var clients = await _reservationService.GetClientsForReservationAsync(1);

            // Assert
            Assert.That(clients, Is.Not.Null);
            Assert.That(clients.Count(), Is.EqualTo(1));
            Assert.That(clients.First().Id, Is.EqualTo(1));
        }
    }
} 