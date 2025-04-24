using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TripOrganization_TAQUET.Models;
using TripOrganization_TAQUET.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace TripOrganization_TAQUET.Controllers
{
    public class UserController(TripsContext context) : Controller
    {
        // POST: User/register

        [HttpPost]
        public async Task<IActionResult> Register(UserPayload userPayload)
        {
            if (UserExists(userPayload.Login, userPayload.FirstName, userPayload.LastName))
            {
                return View(userPayload);
            }
            var user = new User
            {
                Login = userPayload.Login,
                FirstName = userPayload.FirstName,
                LastName = userPayload.LastName,
                Role = userPayload.Role.ToString(),
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(userPayload.Password),
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return RedirectToAction("Login");
        }



        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }



        [NonAction]
        public bool UserExists(string login, string firstname, string lastname)
        {
            bool test = context.Users.Any(e => e.Login == login || (e.FirstName == firstname && e.LastName == lastname));
            return test;
        }

        //POST: User/login
        [HttpPost]
        public async Task<IActionResult> Login(UserLogin userPayload)
        {
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Login == userPayload.Login);

            if (user != null && BCrypt.Net.BCrypt.Verify(userPayload.Password, user.HashedPassword))
            {
                // Création des revendications (claims) pour l'identité de l'utilisateur
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Login),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                // Configuration de l'identité et des propriétés d'authentification
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Cookie persistant
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Expiration après 7 jours
                };

                // Connexion de l'utilisateur avec ASP.NET Core Identity
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Stockage des informations utilisateur en session
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");

                return RedirectToAction("Hub", "Home");
            }

            // Message d'erreur en cas d'échec d'authentification
            ModelState.AddModelError("", "Incorrect Login or Password");
            return View(userPayload);
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Hub", "Home");
            }

            UserLogin userLogin = new UserLogin();
            return View(userLogin);
        }


        //DELETE: api/User/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<User>> DeleteUser(int id)
        {
            var userRole = User.FindFirst(ClaimTypes.Role).Value;
            var user = await context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }

            context.Users.Remove(user);
            await context.SaveChangesAsync();

            return user;
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

    }
}
