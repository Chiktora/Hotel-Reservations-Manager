using Hotel_Reservations_Manager.Data.Models;
using Hotel_Reservations_Manager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hotel_Reservations_Manager.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly IReservationService _reservationService;
        private readonly IRoomService _roomService;
        private readonly IClientService _clientService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReservationsController(
            IReservationService reservationService,
            IRoomService roomService,
            IClientService clientService,
            UserManager<ApplicationUser> userManager)
        {
            _reservationService = reservationService;
            _roomService = roomService;
            _clientService = clientService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            
            var reservations = await _reservationService.GetAllReservationsAsync(page, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(await _reservationService.GetTotalReservationsCountAsync() / (double)pageSize);

            return View(reservations);
        }

        public async Task<IActionResult> Create()
        {
            try 
            {
                // Get available rooms
                var rooms = await _roomService.GetAvailableRoomsAsync(DateTime.Today, DateTime.Today.AddDays(1));
                
                // Get all clients
                var clients = await _clientService.GetAllClientsAsync(1, 100);
                
                // Prepare SelectLists for the form
                ViewBag.RoomId = new SelectList(rooms, "Id", "RoomNumber");
                ViewBag.ClientIds = new MultiSelectList(clients, "Id", "FirstName");
                
                // Pass full objects for enhanced UI
                ViewBag.Rooms = rooms;
                ViewBag.Clients = clients;
                
                // Initialize default dates
                var reservation = new Reservation
                {
                    CheckInDate = DateTime.Today,
                    CheckOutDate = DateTime.Today.AddDays(1)
                };
                
                return View(reservation);
            }
            catch (Exception ex)
            {
                // Handle any errors
                ModelState.AddModelError("", $"Error loading form: {ex.Message}");
                return View(new Reservation
                {
                    CheckInDate = DateTime.Today,
                    CheckOutDate = DateTime.Today.AddDays(1)
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation reservation, List<int> clientIds)
        {
            // Initialize diagnostic and form data
            var diagnosticInfo = new List<string>();
            var roomsForForm = await _roomService.GetAvailableRoomsAsync(DateTime.Today, DateTime.Today.AddDays(1));
            var clientsForForm = await _clientService.GetAllClientsAsync(1, 100);
            
            try
            {
                // 1. Ensure reservation is not null
                if (reservation == null)
                {
                    ModelState.AddModelError("", "Reservation data is missing");
                    diagnosticInfo.Add("Reservation object is null");
                    throw new ArgumentException("Reservation data is missing");
                }
                
                // 2. Ensure clients are selected
                if (clientIds == null || !clientIds.Any())
                {
                    ModelState.AddModelError("", "Please select at least one client");
                    diagnosticInfo.Add("No clients selected");
                    throw new ArgumentException("No clients selected");
                }
                
                // 3. Clean up client IDs (remove duplicates and invalid entries)
                var originalClientIdsCount = clientIds.Count;
                clientIds = clientIds.Where(id => id > 0).Distinct().ToList();
                diagnosticInfo.Add($"Received client IDs: {string.Join(", ", clientIds)}");
                
                if (originalClientIdsCount != clientIds.Count)
                {
                    diagnosticInfo.Add($"Fixed client selection: removed {originalClientIdsCount - clientIds.Count} invalid or duplicate entries");
                }
                
                // 4. Set the UserId from the current user
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    ModelState.AddModelError("", "You must be logged in to create a reservation");
                    diagnosticInfo.Add("User is not authenticated properly");
                    throw new ArgumentException("User is not authenticated");
                }
                
                // 5. Set the UserID and log
                reservation.UserId = userId;
                diagnosticInfo.Add($"User ID set to: {userId}");
                
                // 6. Clear any model errors for fields we manually set
                ModelState.Remove("UserId");
                
                // 7. Validate the room selection
                if (reservation.RoomId <= 0)
                {
                    ModelState.AddModelError("RoomId", "Please select a valid room");
                    diagnosticInfo.Add("Room not selected properly");
                    throw new ArgumentException("Invalid room selection");
                }
                
                // 8. Get room details
                var room = await _roomService.GetRoomByIdAsync(reservation.RoomId);
                if (room == null)
                {
                    ModelState.AddModelError("RoomId", "Selected room does not exist");
                    diagnosticInfo.Add("Selected room does not exist");
                    throw new ArgumentException("Selected room does not exist");
                }
                
                diagnosticInfo.Add($"Room ID: {reservation.RoomId} ({room.RoomNumber}, capacity: {room.Capacity})");
                diagnosticInfo.Add($"Check-in: {reservation.CheckInDate}");
                diagnosticInfo.Add($"Check-out: {reservation.CheckOutDate}");
                diagnosticInfo.Add($"Selected clients: {string.Join(", ", clientIds)}");
                diagnosticInfo.Add($"Total client count: {clientIds.Count}");
                
                // 9. Validate check-in/check-out dates
                if (reservation.CheckInDate >= reservation.CheckOutDate)
                {
                    ModelState.AddModelError("CheckOutDate", "Check-out date must be after check-in date");
                    diagnosticInfo.Add("Check-out date must be after check-in date");
                    throw new ArgumentException("Invalid date range");
                }
                
                // 10. Validate capacity
                if (clientIds.Count > room.Capacity)
                {
                    ModelState.AddModelError("", $"Room capacity exceeded. Room {room.RoomNumber} has a maximum capacity of {room.Capacity}, but you selected {clientIds.Count} clients");
                    diagnosticInfo.Add($"Room capacity exceeded. Room {room.RoomNumber} has a maximum capacity of {room.Capacity}, but you selected {clientIds.Count} clients");
                    throw new ArgumentException("Room capacity exceeded");
                }
                
                // 11. Check room availability
                bool isRoomAvailable = await _reservationService.IsRoomAvailableAsync(
                    reservation.RoomId, 
                    reservation.CheckInDate, 
                    reservation.CheckOutDate);
                    
                if (!isRoomAvailable)
                {
                    ModelState.AddModelError("", "Room is not available for the selected dates");
                    diagnosticInfo.Add("Room is not available for the selected dates");
                    throw new InvalidOperationException("Room is not available");
                }
                
                // 12. Calculate total amount
                reservation.TotalAmount = await _reservationService.CalculateTotalAmountAsync(
                    reservation.RoomId,
                    reservation.CheckInDate,
                    reservation.CheckOutDate,
                    clientIds,
                    reservation.BreakfastIncluded,
                    reservation.AllInclusive);
                
                diagnosticInfo.Add($"Calculated total amount: {reservation.TotalAmount}");
                
                // 13. Create the reservation
                await _reservationService.CreateReservationAsync(reservation, clientIds);
                diagnosticInfo.Add("Reservation created successfully");
                
                // 14. Redirect to Index on success
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // 15. Log the specific exception
                diagnosticInfo.Add($"Error: {ex.GetType().Name} - {ex.Message}");
                if (ex.InnerException != null)
                {
                    diagnosticInfo.Add($"Inner exception: {ex.InnerException.Message}");
                }
                
                // 16. Don't add another error message if we already added one specific to the issue
                if (!ModelState.Values.SelectMany(v => v.Errors).Any())
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            
            // 17. If we get here, there was an error - repopulate the form and return
            ViewBag.RoomId = new SelectList(roomsForForm, "Id", "RoomNumber", reservation?.RoomId);
            ViewBag.ClientIds = new MultiSelectList(clientsForForm, "Id", "FirstName", clientIds);
            ViewBag.Rooms = roomsForForm;
            ViewBag.Clients = clientsForForm;
            ViewBag.ErrorDetails = diagnosticInfo;
            
            return View(reservation);
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var reservation = await _reservationService.GetReservationByIdAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            try
            {
                // Get all clients and rooms for the form
                var rooms = await _roomService.GetAllRoomsAsync();
                var clients = await _clientService.GetAllClientsAsync();
                
                // Get clients for this reservation
                var reservationClients = await _reservationService.GetClientsForReservationAsync(id);
                
                // Ensure we have a valid list of client IDs, even if empty
                var selectedClientIds = reservationClients?.Select(c => c.Id).ToList() ?? new List<int>();

                // Store data for the view
                ViewBag.RoomId = new SelectList(rooms, "Id", "RoomNumber", reservation.RoomId);
                ViewBag.SelectedClients = selectedClientIds;
                ViewBag.Rooms = rooms;
                ViewBag.Clients = clients;

                return View(reservation);
            }
            catch (Exception ex)
            {
                // Initialize empty lists to avoid null reference exceptions
                ViewBag.RoomId = new SelectList(new List<Room>(), "Id", "RoomNumber");
                ViewBag.SelectedClients = new List<int>();
                ViewBag.Rooms = new List<Room>();
                ViewBag.Clients = new List<Client>();
                ViewBag.ErrorDetails = $"Error loading data: {ex.Message}";
                
                return View(reservation);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Reservation reservation, List<int> clientIds)
        {
            ViewBag.ErrorDetails = null;
            var diagnosticInfo = new List<string>();
            
            // Ensure clientIds is never null
            clientIds = clientIds ?? new List<int>();
            
            // Retrieve rooms and clients at the beginning - we'll need these regardless of the outcome
            var rooms = await _roomService.GetAllRoomsAsync();
            var clients = await _clientService.GetAllClientsAsync();
            
            // Always set these ViewBag properties to avoid null reference exceptions
            ViewBag.Rooms = rooms;
            ViewBag.Clients = clients;
            ViewBag.SelectedClients = clientIds;
            
            if (id != reservation.Id)
            {
                diagnosticInfo.Add($"ID mismatch: URL ID {id} vs Form ID {reservation.Id}");
                ModelState.AddModelError("", "ID mismatch between the request and the form.");
                ViewBag.ErrorDetails = string.Join("; ", diagnosticInfo);
                return View(reservation);
            }

            // Set the UserId before validation
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                diagnosticInfo.Add("User not authenticated");
                ModelState.AddModelError("", "You must be logged in to update a reservation");
                ViewBag.ErrorDetails = string.Join("; ", diagnosticInfo);
                return View(reservation);
            }

            // Set the UserID
            reservation.UserId = userId;
            
            // Clear some ModelState errors since we'll handle them manually
            ModelState.Remove("UserId");

            // Validate client selection
            if (clientIds == null || !clientIds.Any())
            {
                ModelState.AddModelError("", "Please select at least one client");
                diagnosticInfo.Add("No clients selected");
                ViewBag.ErrorDetails = string.Join("; ", diagnosticInfo);
                return View(reservation);
            }

            // Clean up client IDs (remove duplicates and invalid entries)
            var originalClientIdsCount = clientIds.Count;
            clientIds = clientIds.Where(id => id > 0).Distinct().ToList();
            ViewBag.SelectedClients = clientIds; // Update after cleanup
            
            if (originalClientIdsCount != clientIds.Count)
            {
                diagnosticInfo.Add($"Fixed client selection: removed {originalClientIdsCount - clientIds.Count} invalid or duplicate entries");
            }

            // Validate the room selection
            if (reservation.RoomId <= 0)
            {
                ModelState.AddModelError("RoomId", "Please select a valid room");
                diagnosticInfo.Add("Room not selected properly");
                ViewBag.ErrorDetails = string.Join("; ", diagnosticInfo);
                return View(reservation);
            }

            // Get room details
            var room = await _roomService.GetRoomByIdAsync(reservation.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "Selected room does not exist");
                diagnosticInfo.Add("Selected room does not exist");
                ViewBag.ErrorDetails = string.Join("; ", diagnosticInfo);
                return View(reservation);
            }

            // Validate room capacity
            if (clientIds.Count > room.Capacity)
            {
                ModelState.AddModelError("", $"The selected room has a capacity of {room.Capacity}, but you selected {clientIds.Count} clients");
                diagnosticInfo.Add($"Room capacity exceeded: {clientIds.Count} clients for room with capacity {room.Capacity}");
                ViewBag.ErrorDetails = string.Join("; ", diagnosticInfo);
                return View(reservation);
            }

            // Validate dates
            if (reservation.CheckInDate >= reservation.CheckOutDate)
            {
                ModelState.AddModelError("CheckOutDate", "Check-out date must be after check-in date");
                diagnosticInfo.Add("Invalid date range");
                ViewBag.ErrorDetails = string.Join("; ", diagnosticInfo);
                return View(reservation);
            }

            // Check if the room is available for the selected dates (excluding this reservation)
            var isRoomAvailable = await _roomService.IsRoomAvailableAsync(
                reservation.RoomId, 
                reservation.CheckInDate, 
                reservation.CheckOutDate,
                reservation.Id);  // Exclude this reservation from availability check

            if (!isRoomAvailable)
            {
                ModelState.AddModelError("", "The selected room is not available for the chosen dates");
                diagnosticInfo.Add("Room not available for selected dates");
                ViewBag.ErrorDetails = string.Join("; ", diagnosticInfo);
                return View(reservation);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ErrorDetails = string.Join("; ", diagnosticInfo);
                return View(reservation);
            }

            try
            {
                // Calculate total amount
                try 
                {
                    // Calculate the number of nights
                    var nights = (reservation.CheckOutDate - reservation.CheckInDate).Days;
                    if (nights <= 0)
                    {
                        throw new ArgumentException("Check-out date must be after check-in date");
                    }

                    // Count adults and children
                    var selectedClients = await _clientService.GetClientsByIdsAsync(clientIds);
                    var adultCount = selectedClients.Count(c => c.IsAdult);
                    var childCount = selectedClients.Count(c => !c.IsAdult);

                    // Calculate base price
                    decimal basePrice = (adultCount * room.AdultBedPrice + childCount * room.ChildBedPrice) * nights;
                    
                    // Add extras
                    if (reservation.BreakfastIncluded)
                    {
                        basePrice += 10 * (adultCount + childCount) * nights; // $10 per person per night for breakfast
                    }
                    
                    if (reservation.AllInclusive)
                    {
                        basePrice += 25 * (adultCount + childCount) * nights; // $25 per person per night for all-inclusive
                    }
                    
                    reservation.TotalAmount = basePrice;
                    diagnosticInfo.Add($"Calculated total amount: ${basePrice}");
                }
                catch (Exception ex)
                {
                    diagnosticInfo.Add($"Error calculating total amount: {ex.Message}");
                    ModelState.AddModelError("", $"Error calculating total amount: {ex.Message}");
                    ViewBag.ErrorDetails = string.Join("; ", diagnosticInfo);
                    return View(reservation);
                }

                // Update the reservation
                try
                {
                    await _reservationService.UpdateReservationAsync(reservation, clientIds);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    diagnosticInfo.Add($"Error updating reservation: {ex.Message}");
                    ModelState.AddModelError("", $"Error updating reservation: {ex.Message}");
                    
                    if (ex.InnerException != null)
                    {
                        diagnosticInfo.Add($"Inner exception: {ex.InnerException.Message}");
                    }
                    
                    ViewBag.ErrorDetails = string.Join("; ", diagnosticInfo);
                    return View(reservation);
                }
            }
            catch (Exception ex)
            {
                diagnosticInfo.Add($"Unexpected error: {ex.Message}");
                ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    diagnosticInfo.Add($"Inner exception: {ex.InnerException.Message}");
                }
                
                ViewBag.ErrorDetails = string.Join("; ", diagnosticInfo);
                return View(reservation);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var reservation = await _reservationService.GetReservationByIdAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            return View(reservation);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var reservation = await _reservationService.GetReservationByIdAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            return View(reservation);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _reservationService.DeleteReservationAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            // Set up date ranges
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var weekEnd = weekStart.AddDays(6);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            // Get occupancy rates
            ViewBag.TodayOccupancy = await _reservationService.GetOccupancyRateAsync(today);
            ViewBag.WeekOccupancy = await _reservationService.GetOccupancyRateAsync(weekStart.AddDays(3)); // Mid-week
            ViewBag.MonthOccupancy = await _reservationService.GetOccupancyRateAsync(monthStart.AddDays(15)); // Mid-month

            // Get revenue data
            ViewBag.WeeklyRevenue = await _reservationService.GetRevenueForPeriodAsync(weekStart, weekEnd);
            ViewBag.MonthlyRevenue = await _reservationService.GetRevenueForPeriodAsync(monthStart, monthEnd);

            // Get upcoming check-ins
            ViewBag.UpcomingCheckIns = await _reservationService.GetUpcomingCheckInsAsync(today.AddDays(7));

            // Format date ranges for display
            ViewBag.WeekRange = $"{weekStart:MMM dd} - {weekEnd:MMM dd, yyyy}";
            ViewBag.MonthRange = $"{monthStart:MMM dd} - {monthEnd:MMM dd, yyyy}";

            return View();
        }

        [Authorize]
        public async Task<IActionResult> UpcomingCheckIns(DateTime? fromDate = null, DateTime? toDate = null, string searchTerm = "", int page = 1)
        {
            const int pageSize = 10;
            
            // Default date range is from today to a week later
            var startDate = fromDate ?? DateTime.Today;
            var endDate = toDate ?? DateTime.Today.AddDays(7);
            
            // Get upcoming check-ins for the specified date range
            var checkIns = await _reservationService.GetUpcomingCheckInsAsync(startDate, endDate, page, pageSize);
            
            // Calculate total pages
            var totalCount = await _reservationService.GetUpcomingCheckInsCountAsync(endDate);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            // Set view data
            ViewBag.UpcomingCheckIns = checkIns;
            ViewBag.FromDate = startDate;
            ViewBag.ToDate = endDate;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Analytics(int year = 0, int month = 0)
        {
            // Default to current year and month if not specified
            if (year == 0) year = DateTime.Today.Year;
            if (month == 0) month = DateTime.Today.Month;
            
            // Get date ranges for analysis
            var currentMonth = new DateTime(year, month, 1);
            var monthStart = currentMonth;
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var yearStart = new DateTime(year, 1, 1);
            var yearEnd = new DateTime(year, 12, 31);
            
            // Get room type distribution for current month
            ViewBag.RoomTypeDistribution = await _reservationService.GetReservationsByRoomTypeAsync(monthStart, monthEnd);
            
            // Get monthly revenue for selected year
            ViewBag.MonthlyRevenue = await _reservationService.GetRevenueByMonthAsync(year);
            
            // Get occupancy trend for current month
            ViewBag.OccupancyTrend = await _reservationService.GetOccupancyTrendAsync(monthStart, monthEnd);
            
            // Get cancellation rate
            var canceledCount = await _reservationService.GetCanceledReservationsCountAsync(yearStart, yearEnd);
            var totalCount = await _reservationService.GetTotalReservationsCountAsync();
            ViewBag.CancellationRate = totalCount > 0 ? (canceledCount * 100 / totalCount) : 0;
            
            // Get average stay duration
            ViewBag.AverageStayDuration = await _reservationService.GetAverageStayDurationAsync(yearStart, yearEnd);
            
            // Set date information for the view
            ViewBag.SelectedYear = year;
            ViewBag.SelectedMonth = month;
            ViewBag.MonthName = currentMonth.ToString("MMMM");
            ViewBag.AvailableYears = Enumerable.Range(DateTime.Today.Year - 5, 6).ToList(); // Last 5 years + current year
            
            return View();
        }
    }
} 