using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripOrganization_TAQUET.Data;
using TripOrganization_TAQUET.Models;


namespace TripOrganization_TAQUET.Controllers
{ 

public class TripsController(TripsContext _context) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Create(Trips trip)
    {
        if (ModelState.IsValid)
        {
            var username = User.Identity?.Name;
            if (username == null)
            {
                return Unauthorized();
            }

            // Définit l'utilisateur actuel comme propriétaire du voyage
            trip.owners = [username];
            // Initialise la liste des participants si null
            trip.participants ??= new List<string>();
                
            _context.Add(trip);
            await _context.SaveChangesAsync();
            return RedirectToAction("Hub", "Home");
        }
        return View(trip);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return View();
        }
        return RedirectToAction("Login", "User");
    }
    
 
    public async Task<IActionResult> Delete(int id)
    {
        var trip = await _context.Trips.FindAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        // Vérification que l'utilisateur est un propriétaire du voyage
        var username = User.Identity?.Name;
        if (username == null || !trip.owners.Contains(username))
        {
            return Forbid();
        }

        return View(trip);
    }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return NotFound();
            }

            // Vérification que l'utilisateur est un propriétaire du voyage
            var username = User.Identity?.Name;
            if (username == null || !trip.owners.Contains(username))
            {
                return Forbid();
            }

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
            return RedirectToAction("UpcomingTrips");
        }

        [HttpGet]
    public async Task<IActionResult> UpcomingTrips()
    {
        var currentDate = DateTime.Now;
        var trips =await _context.Trips
            .Where(t => t.DepartureDate > currentDate)
            .ToListAsync();

        return View(trips);
    }

    [HttpPost]
    public async Task<IActionResult> Join(int id)
    {
        // Utilisation d'une transaction avec niveau d'isolation élevé pour gérer la concurrence
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        try
        {
            // Récupère le voyage avec verrouillage de mise à jour pour éviter les inscriptions simultanées
            var trip = await _context.Trips.Where(t => t.Id == id).FirstOrDefaultAsync();

            if (trip == null)
            {
                await transaction.RollbackAsync();
                return NotFound();
            }

            // Vérification que le voyage n'est pas déjà complet
            if (trip.participants != null && trip.participants.Count >= trip.Capacity)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "This trip is full, you can't register.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Récupération de l'identifiant de l'utilisateur connecté
            var userIdClaim = User.FindFirst(ClaimTypes.Name);
            if (userIdClaim == null)
            {
                await transaction.RollbackAsync();
                return Unauthorized();
            }

            var userId = userIdClaim.Value;

            // Initialisation de la liste de participants si nécessaire
            if (trip.participants == null)
            {
                trip.participants = new List<string>();
            }

            // Vérification et ajout de l'utilisateur s'il n'est pas déjà inscrit
            if (!trip.participants.Contains(userId))
            {
                trip.participants.Add(userId);
                _context.Entry(trip).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "You are now registered to the trip.";
            }
            else
            {
                await transaction.CommitAsync();
            }

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateConcurrencyException)
        {
            // Gestion des erreurs de concurrence
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "You can't register, try again.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }


    public async Task<IActionResult> Details(int id)
    {
        var trip = await _context.Trips.FindAsync(id);
        if (trip == null)
        {
            return NotFound();
        }
        return View(trip);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Trips trip, string newOwner)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {

                if (id != trip.Id)
                {
                    return NotFound();
                }
                var existingTrip = await _context.Trips.Where(t => t.Id == id).FirstOrDefaultAsync();

                if (existingTrip == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                var username = User.Identity?.Name;
                if (username == null || !existingTrip.owners.Contains(username))
                {
                    await transaction.RollbackAsync();
                    return Forbid();
                }

                if (trip.Capacity < trip.participants.Count)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "You can't reduce the capacity below the number of participants.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                trip.owners = existingTrip.owners;
                trip.participants = existingTrip.participants;
                if (!string.IsNullOrWhiteSpace(newOwner) && !trip.owners.Contains(newOwner))
                {
                    var newOwnerExists = await _context.Users.AnyAsync(u => u.Login == newOwner);

                    if (newOwnerExists)
                    {
                        trip.owners.Add(newOwner);
                        TempData["SuccessMessage"] = $"{newOwner} has been add to the trip's owners.";
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = "New owner does not exist.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }
                _context.Entry(existingTrip).State = EntityState.Detached;
                _context.Update(trip);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return RedirectToAction("Hub", "Home");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "An error occurred while updating the trip.";
                return RedirectToAction(nameof(Details), new { id });
            }

        }



    [HttpGet]
    public async Task<IActionResult> Edit(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            var username = User.Identity?.Name;
            if (trip == null)
            {
                return NotFound();
            }
            if (username == null || !trip.owners.Contains(username))
            {
                return Forbid();
            }
            return View(trip);
        }

    [HttpPost]
        public async Task<IActionResult> Leave(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return NotFound();
            }

            var userName = User.Identity?.Name;
            if (userName == null)
            {
                return NotFound();
            }

            if (trip.participants != null && trip.participants.Contains(userName))
            {
                trip.participants = trip.participants.Where(p => p != userName).ToList();
                _context.Entry(trip).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id });
        }

    }
}