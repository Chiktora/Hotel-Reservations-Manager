using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hotel_Reservations_Manager.Data.Models
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Room")]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public virtual Room Room { get; set; }

        [Required]
        [Display(Name = "Created By")]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required]
        [Display(Name = "Check-in Date")]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Required]
        [Display(Name = "Check-out Date")]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [Display(Name = "Breakfast Included")]
        public bool BreakfastIncluded { get; set; }

        [Required]
        [Display(Name = "All Inclusive")]
        public bool AllInclusive { get; set; }

        [Required]
        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Is Canceled")]
        public bool IsCanceled { get; set; } = false;

        // Navigation property for clients in this reservation
        public virtual ICollection<ReservationClient> ReservationClients { get; set; } = new List<ReservationClient>();
    }
} 