using Hotel_Reservations_Manager.Data.Models;
using Hotel_Reservations_Manager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Reservations_Manager.Controllers
{
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        public async Task<IActionResult> Index(string searchTerm, int page = 1)
        {
            const int pageSize = 10;
            
            var clients = string.IsNullOrEmpty(searchTerm) 
                ? await _clientService.GetAllClientsAsync(page, pageSize)
                : await _clientService.SearchClientsAsync(searchTerm, page, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.TotalPages = (int)Math.Ceiling(await _clientService.GetFilteredClientsCountAsync(searchTerm) / (double)pageSize);

            return View(clients);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Client client)
        {
            if (ModelState.IsValid)
            {
                await _clientService.CreateClientAsync(client);
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var client = await _clientService.GetClientByIdAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Client client)
        {
            if (id != client.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _clientService.UpdateClientAsync(client);
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        public async Task<IActionResult> Details(int id)
        {
            var client = await _clientService.GetClientByIdAsync(id);
            if (client == null)
            {
                return NotFound();
            }

            var reservations = await _clientService.GetClientReservationsAsync(id);
            ViewBag.Reservations = reservations;

            return View(client);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var client = await _clientService.GetClientByIdAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            return View(client);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _clientService.DeleteClientAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 3)
            {
                return Json(new List<object>());
            }

            var clients = await _clientService.SearchClientsAsync(term);
            var result = clients.Select(c => new
            {
                id = c.Id,
                firstName = c.FirstName,
                lastName = c.LastName,
                isAdult = c.IsAdult
            });

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] Client client)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid client data" });
            }

            try
            {
                await _clientService.CreateClientAsync(client);
                return Json(new { success = true, client = new
                {
                    id = client.Id,
                    firstName = client.FirstName,
                    lastName = client.LastName,
                    isAdult = client.IsAdult
                }});
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
} 