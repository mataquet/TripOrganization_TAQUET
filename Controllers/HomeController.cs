using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripOrganization_TAQUET.Models;
using TripOrganization_TAQUET.Data;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace TripOrganization_TAQUET.Controllers
{
    public class HomeController : Controller
    {
        private readonly TripsContext _context;

        public HomeController(TripsContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> Hub()
        {
            var username = User.Identity?.Name;
            if (username == null)
            {
                return RedirectToAction("Login", "User");
            }
            var user = await _context.Users
               .FirstOrDefaultAsync(u => u.Login == username);

            var trips = await _context.Trips
                .Where(t => t.participants.Contains(user.Login))
                .ToListAsync();

            return View(trips);
        }

    }
}
