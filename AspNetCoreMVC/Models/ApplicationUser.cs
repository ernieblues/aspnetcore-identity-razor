#nullable disable
using Microsoft.AspNetCore.Identity;

namespace AspNetCoreMVC.Models
{
    public class ApplicationUser : IdentityUser
    {
        public virtual ICollection<Schedule> Schedules { get; set; }
        public virtual ICollection<ApplicationUserRole> UserRoles { get; set; }
    }
}
