using IdentityService.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

// 1. Data Transfer Object (DTO) for incoming JSON requests
public record UserRegisterDto(string Email, string Password, string FirstName, string LastName);

// 2. Controller handling user registration
[ApiController]
[Route("api/auth")]
public class AccountController : ControllerBase
{
	private readonly UserManager<ApplicationUser> _userManager;

	public AccountController(UserManager<ApplicationUser> userManager)
	{
		_userManager = userManager;
	}

	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] UserRegisterDto model)
	{
		var user = new ApplicationUser
		{
			UserName = model.Email,
			Email = model.Email,
			FirstName = model.FirstName,
			LastName = model.LastName
		};

		// CreateAsync automatically runs security validations and applies PBKDF2 hashing
		var result = await _userManager.CreateAsync(user, model.Password);

		if (!result.Succeeded)
		{
			return BadRequest(result.Errors.Select(e => e.Description));
		}

		return Ok(new { Message = "User provisioned securely on Azure SQL." });
	}
}