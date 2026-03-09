using System.Security.Claims;
using Kotoba.Shared.DTOs;
using Kotoba.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kotoba.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AuthController(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        return Created($"/api/users/{user.Id}", new { userId = user.Id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: false
        );

        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        return Ok(new { userId = user!.Id });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(new { userId });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok();
    }
}
