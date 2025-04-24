namespace TripOrganization_TAQUET.Models;

public class Trips
{
    public int Id { get; set; }
    
    public int Capacity { get; set; } 
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Departure { get; set; } = string.Empty;
    public string Arrival { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public DateTime ArrivalDate { get; set; }
    public int Price { get; set; }
    public string Image { get; set; } = string.Empty;
    public List<string> owners { get; set; } = new List<string>();
    public List<string> participants { get; set; } = new List<string>();
}