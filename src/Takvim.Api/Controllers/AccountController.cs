
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Takvim.Api.Models;

namespace Takvim.Api.Controllers;

public record RegisterRequest(
    [Required, StringLength(100)] string Name,
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record UserResponse(
    int Id,
    string Name,
    string Email,
    string Role
);

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;

    public AccountController(
        UserManager<AppUser> users,
        SignInManager<AppUser> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request)
    {
        var user = new AppUser
        {
            Name = request.Name.Trim(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim()
        };

        var result = await _users.CreateAsync(
            user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(x => x.Description)
            });
        }

        var roleResult = await _users.AddToRoleAsync(
            user, "User");

        if (!roleResult.Succeeded)
        {
            return StatusCode(500, new
            {
                message = "Kullanıcı rolü atanamadı."
            });
        }

        return Ok(new { message = "Kayıt başarılı." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        var user = await _users.FindByEmailAsync(
            request.Email);

        if (user is null)
        {
            return Unauthorized(new
            {
                message = "E-posta veya parola hatalı."
            });
        }

        var result = await _signIn.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Giriş başarısız. Bilgilerinizi kontrol edin."
            });
        }

        var roles = await _users.GetRolesAsync(user);

        return Ok(new UserResponse(
            user.Id,
            user.Name,
            user.Email ?? "",
            roles.FirstOrDefault() ?? "User"
        ));
    }
}
