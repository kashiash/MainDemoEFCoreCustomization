#nullable enable
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using DevExpress.ExpressApp.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;

namespace MainDemo.WebApi.Jwt;

[ApiController]
[Route("api/[controller]")]
// This is a JWT authentication service sample.
public class AuthenticationController : ControllerBase {
    readonly SignInManager signInManager;
    readonly IConfiguration configuration;

    public AuthenticationController(SignInManager signInManager, IConfiguration configuration) {
        this.signInManager = signInManager;
        this.configuration = configuration;
    }
    [HttpPost("Authenticate")]
    [SwaggerOperation("Checks if the user with the specified logon parameters exists in the database. If it does, authenticates this user.", "Refer to the following help topic for more information on authentication methods in the XAF Security System: <a href='https://docs.devexpress.com/eXpressAppFramework/119064/data-security-and-safety/security-system/authentication'>Authentication</a>.")]
    public IActionResult Authenticate(
        [FromBody]
            [SwaggerRequestBody(@"For example: <br /> { ""userName"": ""Sam"", ""password"": """" }")]
            AuthenticationStandardLogonParameters logonParameters
    ) {
        var authenticationResult = signInManager.AuthenticateByLogonParameters(logonParameters);

        if(authenticationResult.Succeeded) {
            var issuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Authentication:Jwt:IssuerSigningKey"]!));
            var token = new JwtSecurityToken(
                issuer: configuration["Authentication:Jwt:ValidIssuer"],
                audience: configuration["Authentication:Jwt:ValidAudience"],
                claims: authenticationResult.Principal.Claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: new SigningCredentials(issuerSigningKey, SecurityAlgorithms.HmacSha256)
                );
            return Ok(new JwtSecurityTokenHandler().WriteToken(token));
        }
        return Unauthorized("User name or password is incorrect.");
    }
}
#nullable disable
