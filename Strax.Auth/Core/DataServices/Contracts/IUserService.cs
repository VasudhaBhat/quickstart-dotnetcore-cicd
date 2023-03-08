
using Api.Dtos.User.BindingModels;
using API.Dtos.Users.BindingModels;
using Databases.Identity.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataServices.Contracts
{
    public interface IUserService
    {
        Task<ApplicationUser> AddRootUser(OrganisationBindingModel orgModel);

        Task<ApplicationUser> AddOrgUser(string organisationId, NewUserBindingModel userModel);

        Task<ApplicationUser> Modify(ApplicationUser entity, ChangeUserProfileDto model);
        Task<IdentityResult> ModifyRoles(ApplicationUser entity, List<string> userRoles);

        Task<ApplicationUser> Delete(string updateBy, ApplicationUser entity);

        Task<bool> DeactivateOrganisation(string updateBy, IEnumerable<ApplicationUser> organisationUsers);

        Task<ApplicationUser> ActivateUser(string updateBy, ApplicationUser entity);

        Task<bool> ActivateOrganisation(string updateBy, IEnumerable<ApplicationUser> organisationUsers);

        Task<IEnumerable<ApplicationUser>> List();

        Task<ApplicationUser> Get(string userId);

        Task<ApplicationUser> GetByUsername(string userName);

        Task<IEnumerable<ApplicationUser>> GetUsersByOrgId(string organisationId);

        Task<ApplicationUser> GetActiveDeactiveUsers(string userId);

        Task<IdentityResult> ChangePassword(ApplicationUser user, string currentPassword, string newPassword);
    }
}
