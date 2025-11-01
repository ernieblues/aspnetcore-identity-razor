#nullable disable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspNetCoreMVC.Models
{
    public class Schedule
    {
        [Key]
        public int ScheduleId { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(450)")] // match with primary key
        [ForeignKey("User")]                 // primary key AspNetUsers.Id
        public string OwnerId { get; set; }

        // Navigation property to AspNetUsers.Id = OwnerId
        public virtual ApplicationUser User { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }

        [Required]
        public string Day { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:h:mm tt}")]
        public TimeOnly StartTime { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:h:mm tt}")]
        public TimeOnly EndTime { get; set; }

        [Required]
        public ScheduleStatus Status { get; set; }
    }

    public enum ScheduleStatus
    {
        Submitted,
        Approved,
        Rejected
    }
}