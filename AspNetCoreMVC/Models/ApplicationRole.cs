#nullable disable
using Microsoft.AspNetCore.Identity;

namespace AspNetCoreMVC.Models
{
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() : base() { }
        public ApplicationRole(string role) : base(role) { }
        public virtual ICollection<ApplicationUserRole> UserRoles { get; set; }
    }
}
