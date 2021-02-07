using Microsoft.EntityFrameworkCore;
using TekTox.DAL.Models;

namespace TekTox.DAL
{
    public class RPGContext : DbContext
    {
        public RPGContext(DbContextOptions<RPGContext> options) : base(options) { }
        public DbSet<EventList> EventLists { get; set; }
    }
}
