using FarmazonDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Data
{
    public class ApplicationDbContext : DbContext
    {
        //Construtor metod      
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }

        // Set of users 
        public DbSet<Users> Users { get; set; }

    }
}
