using Hotel_Reservations_Manager.Models;
using Hotel_Reservations_Manager.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Hotel_Reservations_Manager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRoomService _roomService;
        private readonly IReservationService _reservationService;
        private readonly IClientService _clientService;

        public HomeController(
            ILogger<HomeController> logger,
            IRoomService roomService,
            IReservationService reservationService,
            IClientService clientService)
        {
            _logger = logger;
            _roomService = roomService;
            _reservationService = reservationService;
            _clientService = clientService;
        }

        public async Task<IActionResult> Index()
        {
            // Get basic statistics for the dashboard
            ViewBag.TotalRooms = await _roomService.GetTotalRoomsCountAsync();
            ViewBag.AvailableRooms = await _roomService.GetFilteredRoomsCountAsync(null, null, true);
            ViewBag.TotalReservations = await _reservationService.GetTotalReservationsCountAsync();
            
            // Get number of upcoming check-ins (next 48 hours)
            var tomorrow = DateTime.Now.Date.AddDays(2);
            ViewBag.UpcomingCheckIns = await _reservationService.GetUpcomingCheckInsCountAsync(tomorrow);
            
            // Get upcoming reservations for the next 7 days
            var nextWeek = DateTime.Now.Date.AddDays(7);
            ViewBag.UpcomingReservations = await _reservationService.GetUpcomingReservationsAsync(nextWeek, 5);

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
