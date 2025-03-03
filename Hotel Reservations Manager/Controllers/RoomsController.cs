using Hotel_Reservations_Manager.Data.Models;
using Hotel_Reservations_Manager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Reservations_Manager.Controllers
{
    [Authorize]
    public class RoomsController : Controller
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        public async Task<IActionResult> Index(int? capacity, RoomType? type, bool? isAvailable, int page = 1)
        {
            const int pageSize = 10;
            
            var rooms = await _roomService.FilterRoomsAsync(capacity, type, isAvailable, page, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.Capacity = capacity;
            ViewBag.Type = type;
            ViewBag.IsAvailable = isAvailable;
            ViewBag.TotalPages = (int)Math.Ceiling(await _roomService.GetFilteredRoomsCountAsync(capacity, type, isAvailable) / (double)pageSize);
            ViewBag.RoomTypes = Enum.GetValues(typeof(RoomType));

            return View(rooms);
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            ViewBag.RoomTypes = Enum.GetValues(typeof(RoomType));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(Room room)
        {
            if (ModelState.IsValid)
            {
                await _roomService.CreateRoomAsync(room);
                return RedirectToAction(nameof(Index));
            }
            
            ViewBag.RoomTypes = Enum.GetValues(typeof(RoomType));
            return View(room);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            
            ViewBag.RoomTypes = Enum.GetValues(typeof(RoomType));
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int id, Room room)
        {
            if (id != room.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _roomService.UpdateRoomAsync(room);
                return RedirectToAction(nameof(Index));
            }
            
            ViewBag.RoomTypes = Enum.GetValues(typeof(RoomType));
            return View(room);
        }

        public async Task<IActionResult> Details(int id)
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            return View(room);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _roomService.DeleteRoomAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult RoomAvailability()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RoomAvailability(DateTime checkInDate, DateTime checkOutDate)
        {
            if (checkInDate < DateTime.Now.Date)
            {
                ModelState.AddModelError("checkInDate", "Check-in date cannot be in the past.");
                return View();
            }

            if (checkOutDate <= checkInDate)
            {
                ModelState.AddModelError("checkOutDate", "Check-out date must be after check-in date.");
                return View();
            }

            var availableRooms = await _roomService.GetAvailableRoomsAsync(checkInDate, checkOutDate);
            
            ViewBag.CheckInDate = checkInDate;
            ViewBag.CheckOutDate = checkOutDate;
            
            return View(availableRooms);
        }
    }
} 