using AuthenticationService.Models;
using AuthenticationService.Models.Authentication.Login;
using AuthenticationService.Models.Authentication.SignUp;
using AuthenticationServiceImp.Model;
using AuthenticationServiceImp.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthenticationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AuthenticationController(UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager, 
            IConfiguration configuration,IEmailService emailService, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _emailService = emailService;
            _signInManager = signInManager;
        }

        [HttpPost]
        [Route("Create-User")]
        public async Task<IActionResult> Register([FromBody]RegisterUser registerUser, string role)
        {
            //check if user exist
            var userExist = await _userManager.FindByEmailAsync(registerUser.Email);
            if (userExist != null)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new Response { Status = "Error", Mesaage ="User already exist"});
            }

            //Add user to the database
            IdentityUser user = new()
            {
                Email = registerUser.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = registerUser.UserName,
                TwoFactorEnabled = true
            };
          

            if(await _roleManager.RoleExistsAsync(role))
            {
                var result = await _userManager.CreateAsync(user, registerUser.Password);
                if (!result.Succeeded)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                                    new Response { Status = "Error", Mesaage = "User Failed to Create" });
                }
                //Add role to the user
                await _userManager.AddToRolesAsync(user, new List<string> { role });

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(nameof(ConfirmEmail), "Authentication", new { token, email = user.Email }, Request.Scheme);
                var message = new Message(new List<string> { user.Email!} , "Confirm Email link", confirmationLink!);
                _emailService.SendEmail(message);

                     return StatusCode(StatusCodes.Status201Created,
                        new Response { Status = "Success", Mesaage = $"Email sent to {user.Email} Successfully" });
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                   new Response { Status = "Error", Mesaage = "this Role Does not Exist" });
            }
        }

        [HttpGet]
        [Route("Email-text")]
        public  IActionResult EmailText()
        {
            var message = new Message(new List<string>
            { "johanben330@gmail.com" }, "Text", "<h1>Testing this email service</h1>");

            _emailService.SendEmail(message);
            return StatusCode(StatusCodes.Status201Created,
                                  new Response { Status = "Success", Mesaage = "Email Sent Successfully" });
        }

        [HttpGet]
        [Route("Confirm-Email")]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);
                if(result.Succeeded)
                {
                    return StatusCode(StatusCodes.Status201Created,
                      new Response { Status = "Success", Mesaage = "Email verified Successfully" });
                }
            }
            return StatusCode(StatusCodes.Status500InternalServerError,
               new Response { Status = "Error", Mesaage = "User does not exist" });
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(LoginModel login)
        {
            var user = await _userManager.FindByNameAsync(login.UserName);
            if (user.TwoFactorEnabled)
            {
                await _signInManager.SignOutAsync();
                await _signInManager.PasswordSignInAsync(user, login.Password, false, true);
                var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                var message = new Message(new List<string> { user.Email! }, "Confirm Email link", token!);
                _emailService.SendEmail(message);

                return StatusCode(StatusCodes.Status201Created,
                new Response { Status = "Success", Mesaage = $"We have sent you an OTP to your email {user.Email}" });
            }
            if (user != null && await _userManager.CheckPasswordAsync(user, login.Password))
            {
                var authClaim = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };
                var userRoles = await _userManager.GetRolesAsync(user);
                foreach (var role in userRoles)
                {
                    authClaim.Add(new Claim(ClaimTypes.Role, role));
                }

               
                var jwtToken = GetToken(authClaim);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                    expiration = jwtToken.ValidTo
                });
            }

            return Unauthorized();
        }

        [HttpPost]
        [Route("login-2Fa")]
        public async Task<IActionResult> LoginWithOTP(string code,string userName)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(userName);
                var signIn = await _signInManager.TwoFactorSignInAsync("Email", code, false, false);

                if (signIn.Succeeded)
                {
                    if (user != null)
                    {
                        var authClaim = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };
                        var userRoles = await _userManager.GetRolesAsync(user);
                        foreach (var role in userRoles)
                        {
                            authClaim.Add(new Claim(ClaimTypes.Role, role));
                        }
                        var jwtToken = GetToken(authClaim);

                        return Ok(new
                        {
                            token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                            expiration = jwtToken.ValidTo
                        });
                    }
                }
                return StatusCode(StatusCodes.Status404NotFound,
                   new Response { Status = "Error", Mesaage = "Invalid code" });

            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
           
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("forget-password")]

        public async Task<IActionResult> ForgotPassword([Required] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if(user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var forgetPasswordLink = Url.Action(nameof(ResetPassword), "Authentication", new { token, email = user.Email }, Request.Scheme);
                var message = new Message(new List<string> { user.Email! }, "Reset Password link", forgetPasswordLink!);
                _emailService.SendEmail(message);

                return StatusCode(StatusCodes.Status200OK,
                       new Response { Status = "Success", Mesaage = $"Password change request has been sent to your email {user.Email}. please open your email and clink the link" });
            }
            return StatusCode(StatusCodes.Status404NotFound,
                       new Response { Status = "Error", Mesaage = $"Could not send link to email. please try again" });
        }

        [HttpGet("reset-password")]
        public IActionResult ResetPassword(string token, string email)
        {
            var model = new ResetPassword { Token = token, Email = email };

            return Ok(new { model });

        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPassword model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if(user != null)
            {
                var resetPassResult = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
                if(!resetPassResult.Succeeded)
                {
                    foreach (var error in resetPassResult.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                    return Ok(ModelState);
                }
                return StatusCode(StatusCodes.Status200OK,
                       new Response { Status = "Success", Mesaage = $"Password has been changed" });
            }
                return StatusCode(StatusCodes.Status400BadRequest, 
                       new Response { Status = "Error", Mesaage = "Could not send link to email, please try again" });
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));
            var jwtToken = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );
            return jwtToken;
        }
    }
}
