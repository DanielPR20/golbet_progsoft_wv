// GolBet.Web/Controllers/AccountController.cs
using GolBet.Entities;
using GolBet.Repositories.Data;
using GolBet.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GolBet.Web.Controllers;

public class AccountController : Controller
{
    private const decimal WelcomeBalance = 100_000m;   // Constant of the Balance, business rule #7 states that the user will receive 100,000 FutCoins on sign-up.

    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager; //Injection of the UserManager and SignInManager dependencies to administrate users and log in sessions.

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    } //Constructor initialization

    // GET /Account/Register
    public IActionResult Register() => View(new RegisterViewModel()); //Returns the view of the Register

    // POST /Account/Register
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model) //Receives the RegisterViewModel
    {
        if (!ModelState.IsValid) return View(model); //Validates if the ModelState is valid, if not, returns the view.

        var user = new AppUser //Creates a new AppUser object with the data from the RegisterViewModel
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Balance = WelcomeBalance      // 100,000 FutCoins on sign-up
        };

        var result = await _userManager.CreateAsync(user, model.Password); //saves the result 

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description); 
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, DbSeeder.BettorRole); //Gives the user the role of Bettor after successful registration.
        await _signInManager.SignInAsync(user, isPersistent: false); //Signs in the user after successful registration.

        TempData["Success"] = $"¡Bienvenido a GolBet, {user.FullName}! " +
                              "Tienes 100.000 FutCoins para empezar.";
        return RedirectToAction("Index", "Matches"); //Welcome message and redirect to index of Matches Controller
    }

    // GET /Account/Login
    public IActionResult Login() => View(new LoginViewModel()); //Returns the view of the Login

    // POST /Account/Login
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model) //Receives the LoginViewModel
    {
        if (!ModelState.IsValid) return View(model); //Validates if the ModelState is valid, if not, returns the view.

        var result = await _signInManager.PasswordSignInAsync( //We are using the SignInManager to log in the user with the email, password and remember me parameters of the LoginViewModel
            model.Email, model.Password, model.RememberMe,
            lockoutOnFailure: false);

        if (!result.Succeeded) 
        {
            ModelState.AddModelError(string.Empty,
                "Correo o contraseña incorrectos.");
            return View(model);
        }

        return RedirectToAction("Index", "Matches"); //Redirect after succesful login
    }

    // POST /Account/Logout
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync(); //Logs out the user from the view
        return RedirectToAction("Index", "Home"); //redirects to index of home controller
    }

    // GET /Account/AccessDenied
    public IActionResult AccessDenied() => View(); //calls AccessDenied with the view and returns the url Account/AccessDenied 
}

