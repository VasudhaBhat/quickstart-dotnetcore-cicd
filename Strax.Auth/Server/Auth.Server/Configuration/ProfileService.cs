using Databases.Identity.Models;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Auth.Server.Configuration
{
    public class ProfileService : IProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;


        public ProfileService(UserManager<ApplicationUser> userManager) : base()
        {
            _userManager = userManager;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var subjectId = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(subjectId);
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Subject, user.Id.ToString()),
				//add as many claims as you want!new Claim(JwtClaimTypes.Email, user.Email),new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean)
                //new Claim(JwtClaimTypes.Role, userRoles.First()),                
                new Claim(JwtClaimTypes.Email, user.Email),
                new Claim("organisationId", user.OrganisationId),
            };
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(JwtClaimTypes.Role, role));
            }
            context.IssuedClaims = claims;
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var user = await _userManager.FindByIdAsync(context.Subject.GetSubjectId()); //_repository.GetUserById(context.Subject.GetSubjectId());
            context.IsActive = (user != null);
        }
    }
}
