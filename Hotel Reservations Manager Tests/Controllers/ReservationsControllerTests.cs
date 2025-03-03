using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Hotel_Reservations_Manager.Controllers;
using Hotel_Reservations_Manager.Data.Models;
using Hotel_Reservations_Manager.Services.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.Principal;
using System.Threading;

namespace Hotel_Reservations_Manager_Tests.Controllers
{
    [TestFixture]
    public class ReservationsControllerTests
    {
        private Mock<IReservationService> _mockReservationService;
        private Mock<IRoomService> _mockRoomService;
        private Mock<IClientService> _mockClientService;
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private ReservationsController _controller;

        [SetUp]
        public void Setup()
        {
            _mockReservationService = new Mock<IReservationService>();
            _mockRoomService = new Mock<IRoomService>();
            _mockClientService = new Mock<IClientService>();
            
            // Setup mock UserManager
            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = MockUserManager<ApplicationUser>();
            
            // Create controller
            _controller = new ReservationsController(
                _mockReservationService.Object,
                _mockRoomService.Object,
                _mockClientService.Object,
                _mockUserManager.Object);
            
            // Setup controller context
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "test@example.com")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            
            // Setup TempData provider for controller
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        }

        private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var mgr = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
            mgr.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(default(TUser));
            return mgr;
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _mockReservationService = null;
            _mockRoomService = null;
            _mockClientService = null;
            _mockUserManager = null;
        }

        [Test]
        public async Task Index_ReturnsViewWithReservations()
        {
            // Arrange
            var testReservations = new List<Reservation>
            {
                new Reservation { Id = 1, RoomId = 1, CheckInDate = DateTime.Now },
                new Reservation { Id = 2, RoomId = 2, CheckInDate = DateTime.Now.AddDays(1) }
            };
            
            _mockReservationService.Setup(x => x.GetAllReservationsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(testReservations);
            _mockReservationService.Setup(x => x.GetTotalReservationsCountAsync())
                .ReturnsAsync(2);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            var model = viewResult.Model as IEnumerable<Reservation>;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task Create_Get_ReturnsViewWithData()
        {
            // Arrange
            var testRooms = new List<Room>
            {
                new Room { Id = 1, RoomNumber = "101", Type = RoomType.DoubleRoom },
                new Room { Id = 2, RoomNumber = "102", Type = RoomType.Apartment }
            };
            
            var testClients = new List<Client>
            {
                new Client { Id = 1, FirstName = "John", LastName = "Doe" },
                new Client { Id = 2, FirstName = "Jane", LastName = "Doe" }
            };
            
            _mockRoomService.Setup(x => x.GetAvailableRoomsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 10))
                .ReturnsAsync(testRooms);
            
            _mockClientService.Setup(x => x.GetAllClientsAsync(1, 10))
                .ReturnsAsync(testClients);

            // Act
            var result = await _controller.Create();

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            
            // Check ViewData
            Assert.That(viewResult.ViewData["RoomId"], Is.Not.Null);
            Assert.That(viewResult.ViewData["ClientIds"], Is.Not.Null);
        }

        [Test]
        public async Task Details_ExistingId_ReturnsViewWithReservation()
        {
            // Arrange
            var testReservation = new Reservation 
            { 
                Id = 1, 
                RoomId = 1, 
                Room = new Room { Id = 1, RoomNumber = "101" },
                CheckInDate = DateTime.Now,
                CheckOutDate = DateTime.Now.AddDays(2)
            };
            
            _mockReservationService.Setup(x => x.GetReservationByIdAsync(1))
                .ReturnsAsync(testReservation);

            // Act
            var result = await _controller.Details(1);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            
            var model = viewResult.Model as Reservation;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(1));
        }

        [Test]
        public async Task Details_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockReservationService.Setup(x => x.GetReservationByIdAsync(999))
                .ReturnsAsync((Reservation)null);

            // Act
            var result = await _controller.Details(999);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task Delete_ExistingId_ReturnsViewWithReservation()
        {
            // Arrange
            var testReservation = new Reservation 
            { 
                Id = 1, 
                RoomId = 1, 
                Room = new Room { Id = 1, RoomNumber = "101" },
                CheckInDate = DateTime.Now,
                CheckOutDate = DateTime.Now.AddDays(2)
            };
            
            _mockReservationService.Setup(x => x.GetReservationByIdAsync(1))
                .ReturnsAsync(testReservation);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            
            var model = viewResult.Model as Reservation;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(1));
        }

        [Test]
        public async Task DeleteConfirmed_ExistingId_RedirectsToIndex()
        {
            // Arrange
            _mockReservationService.Setup(x => x.DeleteReservationAsync(1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult.ActionName, Is.EqualTo("Index"));
        }

        [Test]
        public async Task Dashboard_ReturnsViewWithCorrectData()
        {
            // Arrange
            var upcomingReservations = new List<Reservation>
            {
                new Reservation { Id = 1, CheckInDate = DateTime.Now.AddDays(1) },
                new Reservation { Id = 2, CheckInDate = DateTime.Now.AddDays(2) }
            };
            
            _mockReservationService.Setup(x => x.GetUpcomingCheckInsAsync(It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(upcomingReservations);
            
            _mockReservationService.Setup(x => x.GetOccupancyRateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(75);
            
            _mockReservationService.Setup(x => x.GetRevenueForPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(1000m);

            // Act
            var result = await _controller.Dashboard();

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            
            // Check ViewData
            Assert.That(viewResult.ViewData["TodayOccupancy"], Is.EqualTo(75));
            Assert.That(viewResult.ViewData["WeeklyRevenue"], Is.EqualTo(1000m));
            Assert.That(viewResult.ViewData["MonthlyRevenue"], Is.EqualTo(1000m));
            Assert.That(viewResult.ViewData["UpcomingCheckIns"], Is.EqualTo(upcomingReservations));
        }
    }
} 