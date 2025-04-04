# 🏨 Hotel Reservations Manager

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-blue)
![C#](https://img.shields.io/badge/C%23-12.0-purple)
![Entity Framework Core](https://img.shields.io/badge/Entity%20Framework%20Core-8.0-orange)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5.0-blueviolet)

A comprehensive hotel management system designed to streamline reservation operations, manage room inventory, and provide analytics for hotel administrators.

## ✨ Features 

### 🔑 User Management
- Role-based access control (Admin, Manager, Staff)
- User registration and authentication
- Account management

### 🛏️ Room Management
- Add, edit, and delete room information
- Room capacity management
- Room type categorization
- Room status tracking (Available, Occupied, Maintenance)
- Real-time room availability calendar

### 👥 Client Management
- Client profiles with contact information
- Client history tracking
- Search functionality by various parameters

### 📅 Reservation Management
- Create, update, and cancel reservations
- Multi-day reservations
- Multiple guests per reservation
- Automatic price calculation
- Check-in and check-out processing
- Upcoming check-ins view

### 📊 Analytics and Reporting
- Occupancy rates visualization
- Revenue analytics
- Popular booking period identification
- Reservation statistics

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core 8.0, C# 12
- **ORM**: Entity Framework Core 8.0
- **Database**: SQL Server
- **Frontend**: HTML5, CSS3, JavaScript, Bootstrap 5
- **Authentication**: ASP.NET Core Identity
- **Validation**: Data Annotations, Fluent Validation
- **Testing**: xUnit, Moq

## 📋 Prerequisites

- .NET 8.0 SDK or later
- SQL Server/SQL Server Express
- Visual Studio 2022 or any compatible IDE

## 🚀 Getting Started

### Installation

1. Clone the repository
   ```
   git clone https://github.com/Chiktora/Hotel-Reservations-Manager.git
   ```

2. Navigate to the project directory
   ```
   cd Hotel-Reservations-Manager
   ```

3. Restore dependencies
   ```
   dotnet restore
   ```

4. Update the connection string in `appsettings.json` to point to your SQL Server instance

5. Apply migrations to create the database
   ```
   dotnet ef database update
   ```

6. Run the application
   ```
   dotnet run
   ```

7. Access the application at `https://localhost:5001` or `http://localhost:5000`

### Initial Setup

The application comes with a seed data feature that creates:
- Default admin account (Username: admin@hotel.com, Password: Admin123!)
- Sample room types and configurations
- Test client data

## 🧪 Running Tests

```
dotnet test
```

## 📱 Screenshots

<details>
<summary>Click to expand</summary>

### Dashboard
[Dashboard Screenshot]

### Room Management
[Room Management Screenshot]

### Reservation Creation
[Reservation Creation Screenshot]

### Client Management
[Client Management Screenshot]

### Analytics
[Analytics Screenshot]

</details>

## 🔒 Security Features

- Password hashing and salting
- HTTPS enforcement
- Anti-forgery protection
- Authorization policies
- Input validation
- Prevention of common vulnerabilities (XSS, CSRF, SQL Injection)

## 🗂️ Project Structure

```
Hotel-Reservations-Manager/
├── Controllers/             # MVC Controllers
├── Data/                    # Data access and models
│   ├── Models/              # Entity models
│   └── ApplicationDbContext # Database context
├── Services/                # Business logic
│   └── Interfaces/          # Service interfaces
├── Views/                   # Razor views
│   ├── Clients/             # Client-related views
│   ├── Home/                # Home and dashboard views
│   ├── Reservations/        # Reservation-related views
│   ├── Rooms/               # Room-related views
│   ├── Users/               # User management views
│   └── Shared/              # Layout and partial views
├── wwwroot/                 # Static files (CSS, JS, images)
└── Migrations/              # Database migrations
```

## 🤝 Contributing

Contributions, issues, and feature requests are welcome! Feel free to check the [issues page](https://github.com/Chiktora/Hotel-Reservations-Manager/issues).

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Contact

Project Link: [https://github.com/Chiktora/Hotel-Reservations-Manager](https://github.com/Chiktora/Hotel-Reservations-Manager)

---

⭐️ Star this repo if you find it useful! ⭐️ 
