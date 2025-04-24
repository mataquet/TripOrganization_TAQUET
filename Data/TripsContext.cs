using Microsoft.EntityFrameworkCore;
using TripOrganization_TAQUET.Models;

namespace TripOrganization_TAQUET.Data;

public class TripsContext(DbContextOptions<TripsContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Trips> Trips { get; set; }
    
}
