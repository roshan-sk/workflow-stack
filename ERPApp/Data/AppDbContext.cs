using Microsoft.EntityFrameworkCore;
using ERPApp.Models;

namespace ERPApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Request> Requests { get; set; }
        public DbSet<User> Users { get; set; }
    }
}