using Hotel_Reservations_Manager.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace Hotel_Reservations_Manager.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Create roles if they don't exist
            string[] roleNames = { "Administrator", "Employee" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create admin user if it doesn't exist
            var adminEmail = "admin@hotel.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    MiddleName = "System",
                    LastName = "User",
                    PersonalId = "0000000000",
                    PhoneNumber = "0888888888",
                    HireDate = DateTime.Now,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                }
            }

            // Add sample room types if none exist
            if (!context.Rooms.Any())
            {
                var rooms = new List<Room>
                {
                    new Room
                    {
                        RoomNumber = "101",
                        Capacity = 2,
                        Type = RoomType.TwinRoom,
                        IsAvailable = true,
                        AdultBedPrice = 50.00m,
                        ChildBedPrice = 25.00m
                    },
                    new Room
                    {
                        RoomNumber = "102",
                        Capacity = 2,
                        Type = RoomType.DoubleRoom,
                        IsAvailable = true,
                        AdultBedPrice = 60.00m,
                        ChildBedPrice = 30.00m
                    },
                    new Room
                    {
                        RoomNumber = "201",
                        Capacity = 4,
                        Type = RoomType.Apartment,
                        IsAvailable = true,
                        AdultBedPrice = 80.00m,
                        ChildBedPrice = 40.00m
                    },
                    new Room
                    {
                        RoomNumber = "301",
                        Capacity = 6,
                        Type = RoomType.Maisonette,
                        IsAvailable = true,
                        AdultBedPrice = 100.00m,
                        ChildBedPrice = 50.00m
                    },
                    new Room
                    {
                        RoomNumber = "401",
                        Capacity = 4,
                        Type = RoomType.Penthouse,
                        IsAvailable = true,
                        AdultBedPrice = 150.00m,
                        ChildBedPrice = 75.00m
                    }
                };

                context.Rooms.AddRange(rooms);
                await context.SaveChangesAsync();
            }

            // Add sample clients if none exist
            if (!context.Clients.Any())
            {
                var clients = new List<Client>
                {
                    new Client
                    {
                        FirstName = "John",
                        LastName = "Doe",
                        PhoneNumber = "0888123456",
                        Email = "john.doe@example.com",
                        IsAdult = true
                    },
                    new Client
                    {
                        FirstName = "Jane",
                        LastName = "Smith",
                        PhoneNumber = "0888654321",
                        Email = "jane.smith@example.com",
                        IsAdult = true
                    },
                    new Client
                    {
                        FirstName = "Mike",
                        LastName = "Johnson",
                        PhoneNumber = "0888111222",
                        Email = "mike.johnson@example.com",
                        IsAdult = true
                    },
                    new Client
                    {
                        FirstName = "Emily",
                        LastName = "Brown",
                        PhoneNumber = "0888333444",
                        Email = "emily.brown@example.com",
                        IsAdult = true
                    },
                    new Client
                    {
                        FirstName = "Tom",
                        LastName = "Wilson",
                        PhoneNumber = "0888555666",
                        Email = "tom.wilson@example.com",
                        IsAdult = false
                    }
                };

                context.Clients.AddRange(clients);
                await context.SaveChangesAsync();
            }
        }
    }
} 