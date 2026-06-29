using System.Security.Claims;
using IdentityService.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IdentityService.Controllers;

[ApiController]
public class AuthorizationController : ControllerBase
{
	private readonly SignInManager<ApplicationUser> _signInManager;
	private readonly UserManager<ApplicationUser> _userManager;

	public AuthorizationController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
	{
		_signInManager = signInManager;
		_userManager = userManager;
	}

	[HttpPost("~/connect/token")]
	[Produces("application/json")]
	public async Task<IActionResult> Exchange()
	{
		// Extracts the protocol request payload straight from the native feature collection.
		// This acts as a robust approach for OpenIddict 7.x running inside a containerized .NET 10 mesh.
		var feature = HttpContext.Features.Get<OpenIddictServerAspNetCoreFeature>();
		var request = feature?.Transaction?.Request;

		if (request == null)
		{
			return BadRequest(new { error = "invalid_request", description = "The OpenID Connect request payload cannot be retrieved from the current context context execution loop." });
		}

		if (request.IsPasswordGrantType())
		{
			var user = await _userManager.FindByNameAsync(request.Username!);
			if (user == null)
			{
				return Forbid(
					authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
					properties: new AuthenticationProperties(new Dictionary<string, string?>
					{
						[OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
						[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The credentials are invalid."
					}));
			}

			var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password!, lockoutOnFailure: true);
			if (!result.Succeeded)
			{
				return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
			}

			var identity = new ClaimsIdentity(
				authenticationType: TokenValidationParameters.DefaultAuthenticationType,
				nameType: Claims.Name,
				roleType: Claims.Role);

			identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
					.SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
					.SetClaim(Claims.Name, user.UserName);

			identity.SetDestinations(claim => [Destinations.AccessToken]);

			var principal = new ClaimsPrincipal(identity);
			principal.SetScopes(new[] { Scopes.OpenId, Scopes.Email, Scopes.Profile, "api_mesh" });

			return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
		}

		throw new NotImplementedException("The specified grant type is not implemented.");
	}
}
