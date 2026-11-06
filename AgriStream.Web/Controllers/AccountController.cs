using System.Security.Claims;
using AgriStream.Web.Data;
using AgriStream.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriStream.Web.Controllers;

public class AccountController(AppDbContext db, IPasswordHasher<AppUser> hasher) : Controller
{
    private static readonly string[] SelfServiceRoles = [Roles.Farmer, Roles.Agronomist];

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!SelfServiceRoles.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Choose Farmer or Agronomist.");
        }

        if (ModelState.IsValid && await db.Users.AnyAsync(u => u.Username == model.Username))
        {
            ModelState.AddModelError(nameof(model.Username), "That username is already taken.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new AppUser { Username = model.Username, Role = model.Role };
        user.PasswordHash = hasher.HashPassword(user, model.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        TempData["Success"] = "Account created. You can sign in now.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
        var verified = user is not null &&
            hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) != PasswordVerificationResult.Failed;

        if (!verified)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, user!.Username), new Claim(ClaimTypes.Role, user.Role)],
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(new ClaimsPrincipal(identity));
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
