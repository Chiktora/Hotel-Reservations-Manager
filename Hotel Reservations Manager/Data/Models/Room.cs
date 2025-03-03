using System.ComponentModel.DataAnnotations;

namespace Hotel_Reservations_Manager.Data.Models
{
    public enum RoomType
    {
        [Display(Name = "Twin Room")]
        TwinRoom,
        [Display(Name = "Double Room")]
        DoubleRoom,
        [Display(Name = "Apartment")]
        Apartment,
        [Display(Name = "Penthouse")]
        Penthouse,
        [Display(Name = "Maisonette")]
        Maisonette
    }

    public class Room
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Capacity")]
        [Range(1, 10, ErrorMessage = "Capacity must be between 1 and 10")]
        public int Capacity { get; set; }

        [Required]
        [Display(Name = "Room Type")]
        public RoomType Type { get; set; }

        [Required]
        [Display(Name = "Is Available")]
        public bool IsAvailable { get; set; } = true;

        [Required]
        [Display(Name = "Adult Price Per Bed")]
        [DataType(DataType.Currency)]
        public decimal AdultBedPrice { get; set; }

        [Required]
        [Display(Name = "Child Price Per Bed")]
        [DataType(DataType.Currency)]
        public decimal ChildBedPrice { get; set; }

        [Required]
        [Display(Name = "Room Number")]
        public string RoomNumber { get; set; }

        // Navigation property for reservations of this room
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
} 