﻿using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Auth.Server.Extensions;
using Auth.Server.Models.AccountViewModels;
using Databases.Identity.Models;
using OpenSoftware.OidcTemplate.Auth.Models.AccountViewModels;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using MediatR;
using Utilities.Email.EmailSender.Events;
using IdentityServer4.Stores;
using IdentityServer4.Events;
using Auth.Server.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;


namespace Auth.Server.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger _logger;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IPersistedGrantService _persistedGrantService;
        private readonly IClientStore _clientStore;
        private readonly IMediator _mediator;
        private readonly IEventService _events;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IOptions<AutoLockoutOptions> _autoLockoutOptions;
        private readonly IConfiguration _configuration;


        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            IMediator mediator,
            IConfiguration configuration,
            IIdentityServerInteractionService interaction,
            IPersistedGrantService persistedGrantService,
            IClientStore clientStore,
            IEventService events,
            IAuthenticationSchemeProvider schemeProvider,
            IOptions<AutoLockoutOptions> autoLockoutOptions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _interaction = interaction;
            _persistedGrantService = persistedGrantService;
            _mediator = mediator;
            _clientStore = clientStore;
            _events = events;
            _schemeProvider = schemeProvider;
            _autoLockoutOptions = autoLockoutOptions;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View("StraxLogin");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // Check if the user is active then only allow the user to login else return an error view
                var userToVerify = _userManager.Users.Where(u => u.Email == model.Email).FirstOrDefault();
                if (userToVerify == null) 
                {
                    ModelState.AddModelError("LoginError", "Wrong username or password");

                    return View("StraxLogin", model);
                }
                else if (userToVerify != null && userToVerify.IsActive == false)
                {
                    ModelState.AddModelError("Login Error", "Deactivated user cannot be logged in");
                    return View("StraxLogin", model);
                }
                else
                {
                    // This doesn't count login failures towards account lockout
                    // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                    var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in.");
                        return RedirectToLocal(returnUrl);
                    }
                    if (result.IsLockedOut)
                    {
                        TimeSpan ts = userToVerify.LockoutEnd.Value - DateTime.UtcNow;
                        var timeDifference = Math.Round(ts.TotalMinutes, 0, MidpointRounding.AwayFromZero);
                        var warningMessage = timeDifference > 1 ? "User account locked for " + timeDifference + " mins"
                                              : timeDifference <= 1 ? "User account locked for 1 min"
                                              : "";

                        ModelState.AddModelError("LoginError", warningMessage);

                        return View("StraxLogin", model);
                    }
                    else
                    {
                        var attemptsLeft = _autoLockoutOptions.Value.MaxFailedAccessAttempts - userToVerify.AccessFailedCount;
                        var warningMessage = attemptsLeft > 1 ? attemptsLeft + " login attempts left before account being locked for " + _autoLockoutOptions.Value.DefaultLockoutTimeSpan + " mins"
                                              : attemptsLeft == 1 ? "Last login attempt before account being locked for " + _autoLockoutOptions.Value.DefaultLockoutTimeSpan + " mins"
                                              : "";

                        ModelState.AddModelError("LoginError", "Wrong username or password");
                        ModelState.AddModelError("LoginError", warningMessage);

                        return View("StraxLogin", model);
                    }
                }
            }
            // If we got this far, something failed, redisplay form
            return View("StraxLogin", model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var model = new LoginWith2faViewModel { RememberMe = rememberMe };
            ViewData["ReturnUrl"] = returnUrl;

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, bool rememberMe, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, model.RememberMachine);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                _logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return View();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with a recovery code.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                _logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
                return View();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]        
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
                    //await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("User created a new account with password.");
                    return RedirectToLocal(returnUrl);
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]        
        public async Task<IActionResult> Logout(string logoutId)
        {         
            // build a model so the logout page knows what to display
            var vm = await BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await Logout(vm);
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]        
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            //var idp = User?.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
            var subjectId = HttpContext.User.Identity.GetSubjectId();

            //// delete authentication cookie
            //await _signInManager.SignOutAsync();

            //// set this so UI rendering sees an anonymous user
            //HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            //// get context information (client name, post logout redirect URI and iframe for federated signout)
            //var logout = await _interaction.GetLogoutContextAsync(model.LogoutId);

            //var vm = new LoggedOutViewModel
            //{
            //    PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
            //    ClientName = logout?.ClientId,
            //    SignOutIframeUrl = logout?.SignOutIFrameUrl
            //};

            //await _persistedGrantService.RemoveAllGrantsAsync(subjectId, vm.ClientName);



            /////// START: Previous way WHEN MULTI TABS WERE NOT LOGGING OUT ///////////////
            
            //// build a model so the logged out page knows what to display
            var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

            //if (User?.Identity.IsAuthenticated == true)
            //{
                // delete authentication cookie
                await _signInManager.SignOutAsync();

                // set this so UI rendering sees an anonymous user
                HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

                await _persistedGrantService.RemoveAllGrantsAsync(subjectId, vm.ClientName);


            //    // delete local authentication cookie
            //    await HttpContext.SignOutAsync();

            //    // raise the logout event
            //    await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            //}

            //// check if we need to trigger sign-out at an upstream identity provider
            //if (vm.TriggerExternalSignout)
            //{
            //    // build a return URL so the upstream provider will redirect back
            //    // to us after the user has logged out. this allows us to then
            //    // complete our single sign-out processing.
            //    string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

            //    // this triggers a redirect to the external provider for sign-out
            //    return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            //}

            /////// END: Previous way WHEN MULTI TABS WERE NOT LOGGING OUT ///////////////

            return Redirect(vm.PostLogoutRedirectUri);
            //return View("LoggedOut", vm);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToAction(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                return View("ExternalLogin", new ExternalLoginViewModel { Email = email });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    throw new ApplicationException("Error loading external login information during confirmation.");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(nameof(ExternalLogin), model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View("StraxForgotPassword");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
                var websiteUrlLink = _configuration["Email:WebsiteUrlLink"]; 
                await _mediator.Send(new ResetPasswordRequestEvent(user.Email, callbackUrl,websiteUrlLink));                
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // If we got this far, something failed, redisplay form
            return View("StraxForgotPassword", model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string userId, string code = null)
        {
            if (code == null)
            {
                throw new ApplicationException("A code must be supplied for password reset.");
            }
            var model = new ResetPasswordViewModel { UserId = userId, Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                ModelState.AddModelError("Reset Password Error", "Could not reset your password. Please check the username and try again.");
                return View("StraxForgotPassword");
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                
                var websiteUrlLink = _configuration["Email:WebsiteUrlLink"]; 

                await _mediator.Send(new ResetPasswordDoneEvent(user.Email,websiteUrlLink));

                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            AddErrors(result);
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            //if (returnUrl.Contains("%2F"))
            //{
            //    returnUrl = HttpUtility.UrlDecode(returnUrl);
            //}

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm;
        }

        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);
            var clientId = logout.ClientId;
            if (clientId == null)
                clientId = logout.ClientIds.FirstOrDefault();

            var client = await _clientStore.FindClientByIdAsync(clientId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = client.PostLogoutRedirectUris.FirstOrDefault(),  // logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(client?.ClientName) ? client?.ClientId : client?.ClientName, //string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (vm.LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }

        #endregion
    }
}