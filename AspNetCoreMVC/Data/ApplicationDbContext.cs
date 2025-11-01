using AspNetCoreMVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AspNetCoreMVC.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string, IdentityUserClaim<string>,
        ApplicationUserRole, IdentityUserLogin<string>, IdentityRoleClaim<string>, IdentityUserToken<string>>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<Schedule> Schedules { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ApplicationUserRole
            builder.Entity<ApplicationUserRole>(userRole =>
            {
                userRole.HasKey(ur => new { ur.UserId, ur.RoleId });

                userRole.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                userRole.HasOne(ur => ur.User)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            // Schedule
            builder.Entity<Schedule>(schedule =>
            {
                schedule.HasOne(s => s.User)
                    .WithMany(u => u.Schedules)
                    .HasForeignKey(s => s.OwnerId);
            });

            // Schedule: TimeOnly <-> TimeSpan converter
            var timeOnly = new ValueConverter<TimeOnly, TimeSpan>(
                t => t.ToTimeSpan(),
                ts => TimeOnly.FromTimeSpan(ts));

            builder.Entity<Schedule>()
                .Property(x => x.StartTime)
                .HasConversion(timeOnly)
                .HasColumnType("time");

            builder.Entity<Schedule>()
                .Property(x => x.EndTime)
                .HasConversion(timeOnly)
                .HasColumnType("time");
        }

        public DbSet<ApplicationUserRole> ApplicationUserRole { get; set; }
    }
}
