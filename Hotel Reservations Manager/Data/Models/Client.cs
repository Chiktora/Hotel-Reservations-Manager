using System.ComponentModel.DataAnnotations;

namespace Hotel_Reservations_Manager.Data.Models
{
    public class Client
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Is Adult")]
        public bool IsAdult { get; set; }

        // Navigation property for reservations this client is part of
        public virtual ICollection<ReservationClient> ReservationClients { get; set; } = new List<ReservationClient>();
    }
} 