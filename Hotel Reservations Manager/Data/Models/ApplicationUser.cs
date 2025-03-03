using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Hotel_Reservations_Manager.Data.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Middle Name")]
        public string MiddleName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Personal ID")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Personal ID must be exactly 10 characters")]
        public string PersonalId { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public override string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public override string Email { get; set; }

        [Required]
        [Display(Name = "Hire Date")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; }

        [Required]
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Release Date")]
        [DataType(DataType.Date)]
        public DateTime? ReleaseDate { get; set; }

        // Navigation property for reservations made by this user
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
} 