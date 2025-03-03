using Hotel_Reservations_Manager.Data.Models;
using Hotel_Reservations_Manager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Reservations_Manager.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            IUserService userService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userService = userService;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(string searchTerm, int page = 1)
        {
            const int pageSize = 10;
            
            var users = string.IsNullOrEmpty(searchTerm) 
                ? await _userService.GetAllUsersAsync(page, pageSize)
                : await _userService.SearchUsersAsync(searchTerm, page, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.TotalPages = (int)Math.Ceiling(await _userService.GetFilteredUsersCountAsync(searchTerm) / (double)pageSize);

            return View(users);
        }

        public async Task<IActionResult> Details(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Get user roles
            ViewBag.UserRoles = await _userManager.GetRolesAsync(user);

            return View(user);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Get all available roles
            ViewBag.AllRoles = _roleManager.Roles.ToList();
            
            // Get user roles
            ViewBag.UserRoles = await _userManager.GetRolesAsync(user);

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser user, List<string> roles)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingUser = await _userService.GetUserByIdAsync(id);
                if (existingUser == null)
                {
                    return NotFound();
                }

                // Update user properties
                existingUser.FirstName = user.FirstName;
                existingUser.MiddleName = user.MiddleName;
                existingUser.LastName = user.LastName;
                existingUser.PersonalId = user.PersonalId;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.Email = user.Email;
                existingUser.UserName = user.Email; // Use email as username
                existingUser.IsActive = user.IsActive;

                if (!user.IsActive && !existingUser.ReleaseDate.HasValue)
                {
                    existingUser.ReleaseDate = DateTime.Now;
                }
                else if (user.IsActive)
                {
                    existingUser.ReleaseDate = null;
                }

                // Update user in the database
                await _userService.UpdateUserAsync(existingUser);

                // Update user roles
                var userRoles = await _userManager.GetRolesAsync(existingUser);
                
                // Remove roles not in the new list
                foreach (var role in userRoles)
                {
                    if (!roles.Contains(role))
                    {
                        await _userManager.RemoveFromRoleAsync(existingUser, role);
                    }
                }
                
                // Add new roles
                foreach (var role in roles)
                {
                    if (!userRoles.Contains(role))
                    {
                        await _userManager.AddToRoleAsync(existingUser, role);
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed, redisplay form
            ViewBag.AllRoles = _roleManager.Roles.ToList();
            ViewBag.UserRoles = await _userManager.GetRolesAsync(user);
            
            return View(user);
        }

        public IActionResult Create()
        {
            // Get all available roles
            ViewBag.AllRoles = _roleManager.Roles.ToList();
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser user, string password, List<string> roles)
        {
            if (ModelState.IsValid)
            {
                // Set username to email
                user.UserName = user.Email;
                user.HireDate = DateTime.Now;
                user.IsActive = true;

                // Create user
                var result = await _userManager.CreateAsync(user, password);
                
                if (result.Succeeded)
                {
                    // Add roles to user
                    foreach (var role in roles)
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                    
                    return RedirectToAction(nameof(Index));
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            
            // If we got this far, something failed, redisplay form
            ViewBag.AllRoles = _roleManager.Roles.ToList();
            
            return View(user);
        }

        public async Task<IActionResult> Deactivate(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateConfirmed(string id)
        {
            await _userService.DeactivateUserAsync(id, DateTime.Now);
            return RedirectToAction(nameof(Index));
        }
    }
} 