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
    public class RoomServiceTests
    {
        private ApplicationDbContext _context;
        private IRoomService _roomService;
        private List<Room> _testRooms;
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
            _roomService = new RoomService(_context);
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
                },
                new Room 
                { 
                    Id = 3, 
                    RoomNumber = "103", 
                    Type = RoomType.DoubleRoom, 
                    Capacity = 2, 
                    AdultBedPrice = 120.00m, 
                    ChildBedPrice = 60.00m,
                    IsAvailable = false // Room under maintenance
                },
                new Room 
                { 
                    Id = 4, 
                    RoomNumber = "104", 
                    Type = RoomType.Penthouse, 
                    Capacity = 6, 
                    AdultBedPrice = 300.00m, 
                    ChildBedPrice = 150.00m,
                    IsAvailable = false // Room not in service
                }
            };
            _context.Rooms.AddRange(_testRooms);

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

            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetAllRoomsAsync_ReturnsAllActiveRooms()
        {
            // Act
            var rooms = await _roomService.GetAllRoomsAsync();

            // Assert
            Assert.That(rooms, Is.Not.Null);
            Assert.That(rooms.Count(), Is.EqualTo(4)); // All rooms regardless of availability
        }

        [Test]
        public async Task GetRoomByIdAsync_ExistingId_ReturnsRoom()
        {
            // Act
            var room = await _roomService.GetRoomByIdAsync(1);

            // Assert
            Assert.That(room, Is.Not.Null);
            Assert.That(room.RoomNumber, Is.EqualTo("101"));
            Assert.That(room.Type, Is.EqualTo(RoomType.DoubleRoom));
        }

        [Test]
        public async Task GetRoomByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var room = await _roomService.GetRoomByIdAsync(999);

            // Assert
            Assert.That(room, Is.Null);
        }

        [Test]
        public async Task GetAvailableRoomsAsync_ReturnsOnlyAvailableRooms()
        {
            // Arrange
            var checkInDate = DateTime.Now.AddDays(5);
            var checkOutDate = DateTime.Now.AddDays(7);

            // Act
            var rooms = await _roomService.GetAvailableRoomsAsync(checkInDate, checkOutDate);

            // Assert
            Assert.That(rooms, Is.Not.Null);
            Assert.That(rooms.Count(), Is.EqualTo(2)); // Room 3 and 4 are unavailable
            Assert.That(rooms.All(r => r.IsAvailable), Is.True);
        }

        [Test]
        public async Task GetAvailableRoomsAsync_WithOverlappingReservation_ExcludesBookedRoom()
        {
            // Arrange
            var checkInDate = DateTime.Now.AddDays(1); // Overlaps with existing reservation
            var checkOutDate = DateTime.Now.AddDays(2);

            // Act
            var rooms = await _roomService.GetAvailableRoomsAsync(checkInDate, checkOutDate);

            // Assert
            Assert.That(rooms, Is.Not.Null);
            Assert.That(rooms.Count(), Is.EqualTo(1)); // Room 1 is booked, Room 3 and 4 are unavailable
            Assert.That(rooms.Any(r => r.Id == 1), Is.False); // Room 1 should not be included
        }

        [Test]
        public async Task CreateRoomAsync_ValidRoom_AddsToDatabase()
        {
            // Arrange
            var newRoom = new Room
            {
                RoomNumber = "201",
                Type = RoomType.Penthouse,
                Capacity = 8,
                AdultBedPrice = 500.00m,
                ChildBedPrice = 250.00m,
                IsAvailable = true
            };

            // Act
            await _roomService.CreateRoomAsync(newRoom);

            // Assert
            var savedRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == "201");
            Assert.That(savedRoom, Is.Not.Null);
            Assert.That(savedRoom.Type, Is.EqualTo(RoomType.Penthouse));
            Assert.That(savedRoom.Capacity, Is.EqualTo(8));
        }

        [Test]
        public async Task UpdateRoomAsync_ValidRoom_UpdatesInDatabase()
        {
            // Arrange
            var room = await _context.Rooms.FindAsync(1);
            room.Type = RoomType.Maisonette;
            room.AdultBedPrice = 150.00m;

            // Act
            await _roomService.UpdateRoomAsync(room);

            // Assert
            var updatedRoom = await _context.Rooms.FindAsync(1);
            Assert.That(updatedRoom.Type, Is.EqualTo(RoomType.Maisonette));
            Assert.That(updatedRoom.AdultBedPrice, Is.EqualTo(150.00m));
        }

        [Test]
        public async Task GetRoomByIdAsync_ReturnsNull_WhenRoomNotFound()
        {
            // Act
            var result = await _roomService.GetRoomByIdAsync(999);

            // Assert
            Assert.That(result, Is.Null);
        }
    }
} 