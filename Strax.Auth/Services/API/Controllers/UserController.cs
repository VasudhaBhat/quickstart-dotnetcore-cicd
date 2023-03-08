using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Api.Dtos.User.BindingModels;
using API.Dtos.Users.BindingModels;
using API.Models;
using Core.Common.AppEnumaration;
using Databases.Identity.Models;
using DataServices.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Utilities.ApiResponse;
using Utilities.Email.EmailSender.Events;

namespace API.Controllers
{
    [Produces("application/json")]
    [Route("api/User")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IMediator _mediator;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IOptions<AutoLockoutOptions> _autoLockoutOptions;
        private readonly IConfiguration _configuration;


        public UserController(IUserService userService, 
            IMediator mediator,
            IConfiguration configuration,
            SignInManager<ApplicationUser> signInManager,
            IOptions<AutoLockoutOptions> autoLockoutOptions)
        {
            _userService = userService;
            _mediator = mediator;
            _signInManager = signInManager;
            _autoLockoutOptions = autoLockoutOptions;
            _configuration = configuration;
        }

        [Route("AddRoot")]
        [HttpPost]
        //[Authorize(Policy = DomainPolicies.SuperAdmin)]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> AddRootUser([FromBody]OrganisationBindingModel orgModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(SiApiResponse.Error(ErrorCode.InvalidFlow, "Model validation failed. Please provide correct values."));
            }
            var user = await _userService.AddRootUser(orgModel);

            if (user != null)
            {
                return Ok(SiApiResponse.FromData(user));
            }
            else
            {
                return BadRequest(SiApiResponse.Error(ErrorCode.InvalidFlow, "User insertion failed. Please try again later."));
            }
        }

        [Route("Add/{organisationId}")]
        [HttpPost]        
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> AddOrgUser(string organisationId, [FromBody]NewUserBindingModel userModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(SiApiResponse.Error(ErrorCode.InvalidFlow, "Model validation failed. Please provide correct values."));
            }
            var user = await _userService.AddOrgUser(organisationId, userModel);            
            if (user != null)
            {
                return Ok(SiApiResponse.FromData(user));
            }
            else
            {
                return BadRequest(SiApiResponse.Error(ErrorCode.InvalidFlow, "User insertion failed. Please try again later."));
            }
        }

        [HttpPut("modify/{userId}")]        
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> Modify(string userId, ChangeUserProfileDto model)
        {
            var user = await _userService.Get(userId);

            if (user == null)
            {
                return NotFound(SiApiResponse.Error(ErrorCode.DataResolutionFailed, "User not found."));
            }

            var updateUser = await _userService.Modify(user, model);

            return Ok(updateUser);
        }


        [HttpPut("modifyRole/{userId}")]        
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> ModifyRole(string userId, [FromBody]List<string> userRoles)
        {
            userRoles.ForEach(x => x = Role.FromName(x).ToString());

            var user = await _userService.Get(userId);

            if (user == null)
            {
                return NotFound(SiApiResponse.Error(ErrorCode.DataResolutionFailed, "User not found."));
            }

            var result = await _userService.ModifyRoles(user, userRoles);

            return Ok(result);
        }

        [HttpPut("delete/{updateBy}/{userId}")]        
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> Delete(string updateBy, string userId)
        {
            var user = await _userService.Get(userId);

            if (user == null)
            {
                return NotFound(SiApiResponse.Error(ErrorCode.DataResolutionFailed, "User not found."));
            }

            var deletedUser = await _userService.Delete(updateBy, user);

            return Ok(SiApiResponse.FromData(user));
        }

        [HttpPut("deactivateOrganisation/{updateBy}/{organisationId}")]        
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> DeactivateOrganisation(string updateBy, string organisationId)
        {
            var user = await _userService.GetUsersByOrgId(organisationId);

            if (user == null)
            {
                return NotFound(SiApiResponse.Error(ErrorCode.DataResolutionFailed, "Users not found."));
            }

            var result = await _userService.DeactivateOrganisation(updateBy, user);

            return Ok(SiApiResponse.FromData(result));
        }

        [HttpPut("activateUser/{updateBy}/{userId}")]        
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> ActivateUser(string updateBy, string userId)
        {
            var user = await _userService.GetActiveDeactiveUsers(userId);

            if (user == null)
            {
                return NotFound(SiApiResponse.Error(ErrorCode.DataResolutionFailed, "User not found."));
            }

            var result = await _userService.ActivateUser(updateBy, user);

            return Ok(SiApiResponse.FromData(result));
        }


        [HttpPut("activateOrganisation/{updateBy}/{organisationId}")]        
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> ActivateOrganisation(string updateBy, string organisationId)
        {
            var user = await _userService.GetUsersByOrgId(organisationId);

            if (user == null)
            {
                return NotFound(SiApiResponse.Error(ErrorCode.DataResolutionFailed, "Users not found."));
            }

            var result = await _userService.ActivateOrganisation(updateBy, user);

            return Ok(SiApiResponse.FromData(result));
        }

        [HttpGet("list")]        
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> List()
        {
            var user = await _userService.List();
            return Ok(user);
        }

        [HttpGet("get/{userId}")]        
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> Get(string userId)

        {
            var user = await _userService.Get(userId);
            return Ok(user);
        }

        [HttpPut("changePassword/{userId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> ChangePassword(string userId, [FromBody]ChangePasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(SiApiResponse.Error(ErrorCode.InvalidFlow, "Model validation failed. Please provide correct values."));
            }

            var user = await _userService.Get(userId);
            
            if (user == null)
            {
                return NotFound(SiApiResponse.Error(ErrorCode.DataResolutionFailed, "User not found."));
            }

            var changePasswordResult = await _userService.ChangePassword(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                var error = changePasswordResult.Errors.FirstOrDefault();
                if (error != null && error.Code == "PasswordMismatch")
                {
                    return BadRequest(SiApiResponse.Error(ErrorCode.InvalidFlow, "Password change failed, current password is incorrect"));                    
                }

                return BadRequest(SiApiResponse.Error(ErrorCode.InvalidFlow, "Password change failed"));                
            }
            var websiteUrlLink = _configuration["Email:WebsiteUrlLink"];  
            await _mediator.Send(new ResetPasswordDoneEvent(user.Email,websiteUrlLink));           

            return Ok(SiApiResponse.FromData("Password changed successfully"));
        }

        [HttpGet("getByUsername/{userName}")]        
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> GetByUsername(string userName)
        {
            var user = await _userService.GetByUsername(userName);
            return Ok(user);
        }

        [HttpPost, Route("lockout")]
        [AllowAnonymous]
        public async Task<IActionResult> Lockout([FromBody]LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if the user is active then only allow the user to login else return an error view
                //var userToVerify = _userManager.Users.Where(u => u.Email == model.Username).FirstOrDefault();
                var userToVerify = await _signInManager.UserManager.FindByEmailAsync(model.Username);
                if (userToVerify == null)
                {
                    //// - User account not found. Username is wrong.                    
                    return Ok(SiApiResponse.FromData("Wrong username or password"));
                }
                else if (userToVerify != null && userToVerify.IsActive == false)
                {
                    // - User is found but is not active account 
                    return Ok(SiApiResponse.FromData("Deactivated user cannot be logged in"));
                }
                else
                {
                    TimeSpan ts = userToVerify.LockoutEnd != null ? userToVerify.LockoutEnd.Value - DateTime.UtcNow : TimeSpan.Zero;
                    var warningMessage = "";

                    if (ts.TotalMinutes > 0)
                    {
                        var timeDifference = Math.Round(ts.TotalMinutes, 0, MidpointRounding.AwayFromZero);
                        warningMessage = timeDifference > 1 ? "User account locked for " + timeDifference + " mins"
                                                : timeDifference <= 1 ? "User account locked for 1 min"
                                                : "";

                        return Ok(SiApiResponse.FromData(warningMessage));
                    }

                    var attemptsLeft = _autoLockoutOptions.Value.MaxFailedAccessAttempts - userToVerify.AccessFailedCount;
                    warningMessage = attemptsLeft > 1 ? attemptsLeft + " login attempts left before account being locked for " + _autoLockoutOptions.Value.DefaultLockoutTimeSpan + " mins"
                                          : attemptsLeft == 1 ? "Last login attempt before account being locked for " + _autoLockoutOptions.Value.DefaultLockoutTimeSpan + " mins"
                                          : "";

                    return Ok(SiApiResponse.FromData("Wrong username or password. " + warningMessage));
                }
            }
            // If we got this far, something failed, redisplay form
            return BadRequest(SiApiResponse.FromError(ErrorCode.InvalidFlow, "Login attempt failed. Please try again."));
        }

        [HttpPost, Route("retrievePassword")]
        [AllowAnonymous]
        public async Task<ActionResult> RetrievePassword([FromBody]RetrievePasswordModel model)
        {
            var user = await _signInManager.UserManager.FindByEmailAsync(model.Username);
            if (user == null || !(await _signInManager.UserManager.IsEmailConfirmedAsync(user)))
            {
                return Ok(null);
            }
            var code = await _signInManager.UserManager.GeneratePasswordResetTokenAsync(user);
            code = HttpUtility.UrlEncode(code);
            code = code.Replace("%2", "+");
            var callbackUrl = model.CallBackURL + user.Id + "/" + code;
            var websiteUrlLink = _configuration["Email:WebsiteUrlLink"];  
            await _mediator.Send(new ResetPasswordRequestEvent(user.Email, callbackUrl,websiteUrlLink));

            return Ok(null);
        }

        [HttpPost, Route("resetPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody]ResetPasswordViewModel model)
        {
            var user = await _signInManager.UserManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest(SiApiResponse.Error(ErrorCode.InvalidFlow, "Could not reset your password. Please try again later."));
            }

            model.Code = HttpUtility.UrlDecode(model.Code.Replace("+", "%2"));

            var result = await _signInManager.UserManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                var websiteUrlLink = _configuration["Email:WebsiteUrlLink"];  
                await _mediator.Send(new ResetPasswordDoneEvent(user.Email,websiteUrlLink));
                return Ok(result);
            }
            else
            {
                return BadRequest(SiApiResponse.Error(ErrorCode.InvalidFlow, "Could not reset your password due to " + result.Errors.FirstOrDefault().Description + " Please contact tech support."));
            }
        }

        [HttpPost, Route("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody]LoginViewModel model)
        {
            if (ModelState.IsValid)
            {

                // Check if the user is active then only allow the user to login else return an error view
                //var userToVerify = _userManager.Users.Where(u => u.Email == model.Username).FirstOrDefault();
                //var userToVerify = await _userManager.FindByNameAsync(model.Username);
                var userToVerify = await _signInManager.UserManager.FindByEmailAsync(model.Username);
                if (userToVerify == null)
                {
                    //// - User account not found. Username is wrong.                    
                    return Ok(SiApiResponse.FromData("Wrong username or password"));
                }
                else if (userToVerify != null && userToVerify.IsActive == false)
                {
                    //userToVerify.AccessFailedCount = 0;
                    //userToVerify.LockoutEnd = null;

                    // - User is found but is not active account 
                    return Ok(SiApiResponse.FromData("Deactivated user cannot be logged in"));
                }
                else
                {
                    // - User is found and active
                    // This doesn't count login failures towards account lockout
                    // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                    //var result = await _signInManager.PasswordSignInAsync(userToVerify, model.Password, false, lockoutOnFailure: true);

                    var result = await _signInManager.PasswordSignInAsync(userToVerify, model.Password, false, lockoutOnFailure: true);

                    if (result.Succeeded)
                    {
                        return Ok(SiApiResponse.FromData(userToVerify));
                    }
                    if (result.IsLockedOut)
                    {
                        TimeSpan ts = userToVerify.LockoutEnd.Value - DateTime.UtcNow;
                        var timeDifference = Math.Round(ts.TotalMinutes, 0, MidpointRounding.AwayFromZero);
                        var warningMessage = timeDifference > 1 ? "User account locked for " + timeDifference + " mins"
                                              : timeDifference <= 1 ? "User account locked for 1 min"
                                              : "";

                        return Ok(SiApiResponse.FromError(ErrorCode.InvalidFlow, warningMessage, userToVerify));

                    }
                    else
                    {
                        var attemptsLeft = _autoLockoutOptions.Value.MaxFailedAccessAttempts - userToVerify.AccessFailedCount;
                        var warningMessage = attemptsLeft > 1 ? attemptsLeft + " login attempts left before account being locked for " + _autoLockoutOptions.Value.DefaultLockoutTimeSpan + " mins"
                                              : attemptsLeft == 1 ? "Last login attempt before account being locked for " + _autoLockoutOptions.Value.DefaultLockoutTimeSpan + " mins"
                                              : "";

                        return Ok(SiApiResponse.FromError(ErrorCode.InvalidFlow, warningMessage, userToVerify));
                    }
                }
            }
            // If we got this far, something failed, redisplay form
            return BadRequest(SiApiResponse.FromError(ErrorCode.InvalidFlow, "Login attempt failed. Please try again."));
        }

        [AllowAnonymous]
        [HttpGet("UniqueEmail/{email}")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<ActionResult> UniqueEmail(string email)
        {
            var user = await _userService.GetByUsername(email);
            return Ok(SiApiResponse.FromData(user == null));
        }
    }
}

