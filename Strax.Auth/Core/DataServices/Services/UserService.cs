using API.Dtos.Users.BindingModels;
using Databases.Identity;
using Databases.Identity.Models;
using DataServices.Contracts;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Api.Dtos.User.BindingModels;
using Microsoft.Extensions.Logging;
using Core.Common.AppEnumaration;

namespace DataServices.Services
{
    public class UserService : IUserService
    {
        private readonly IdentityContext _identityContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserService> _logger;

        public UserService(IdentityContext identityContext, UserManager<ApplicationUser> userManager, ILogger<UserService> logger)
        {
            _identityContext = identityContext;
            _userManager = userManager;
            _logger = logger;
        }


        public async Task<ApplicationUser> AddRootUser(OrganisationBindingModel orgModel)
        {
            try
            {
                var organisation = new Organisation();

                if (orgModel != null)
                {
                    organisation = await CheckOrgnisation(orgModel);
                }

                var user = new ApplicationUser
                {
                    Id = organisation.Id,
                    UserName = orgModel.User.Email,
                    Email = orgModel.User.Email,
                    AddedBy = organisation.CreatedById,
                    IsActive = true,
                    OrganisationId = organisation.Id,
                    IsRootUser = true,
                    IsPasswordTemporary = false,
                    EmailConfirmed = true,
                    DateAdded = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, orgModel.User.Password);

                await _userManager.AddToRoleAsync(user, Role.OrgRootUser.Name);

                await _identityContext.SaveChangesAsync();

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to add root user in auth database \n " +
                    $"Message: {ex.Message} \n " +
                    $"Inner Exception: {ex.InnerException} \n " +
                    $"Stack trace: {ex.StackTrace} \n ");

                return null;
            }
        }

        public async Task<ApplicationUser> AddOrgUser(string organisationId, NewUserBindingModel userModel)
        {
            try
            {
                var organisation = await _identityContext.Organisation.Where(o => o.Id == organisationId).FirstOrDefaultAsync();

                var user = new ApplicationUser
                {
                    UserName = userModel.Email,
                    Email = userModel.Email,                                        
                    OrganisationId = organisation.Id,
                    IsRootUser = userModel.IsRootUser,
                    IsActive = true,
                    IsPasswordTemporary = false,
                    EmailConfirmed = true,
                    AddedBy = userModel.AddedBy,
                    DateAdded = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow
                };


                var result = await _userManager.CreateAsync(user, userModel.Password);

                foreach (var userRole in userModel.UserRoles)
                {
                    var roleTester = await _userManager.IsInRoleAsync(user, Role.FromName(userRole).ToString());
                    if (roleTester == false)
                    {
                        result = await _userManager.AddToRoleAsync(user, userRole);
                    }
                }

                await _identityContext.SaveChangesAsync();

                return user;

            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to add organization user in auth database \n " +
                   $"Message: {ex.Message} \n " +
                   $"Inner Exception: {ex.InnerException} \n " +
                   $"Stack trace: {ex.StackTrace} \n ");

                return null;
            }
        }


        public async Task<ApplicationUser> Modify(ApplicationUser entity, ChangeUserProfileDto model)
        {
            entity.Email = model.EmailAddress;            
            entity.PhoneNumber = model.PhoneNumber;
            entity.ModifiedBy = model.ModifiedBy;
            entity.DateModified = DateTime.UtcNow;

            var result = _identityContext.Users.Update(entity);
            await _identityContext.SaveChangesAsync();

            return result.Entity;
        }

        public async Task<IdentityResult> ModifyRoles(ApplicationUser entity, List<string> userRoles)
        {
            var currentRoles = await _userManager.GetRolesAsync(entity);

            if (currentRoles.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(entity, currentRoles);
            }

            var result = await _userManager.AddToRolesAsync(entity, userRoles);
            await _identityContext.SaveChangesAsync();

            return result;
        }


        public async Task<ApplicationUser> Delete(string updateBy, ApplicationUser entity)
        {
            entity.IsActive = false;
            entity.ModifiedBy = updateBy;
            entity.DateModified = DateTime.UtcNow;

            var result = _identityContext.Users.Update(entity);
            await _identityContext.SaveChangesAsync();

            return result.Entity;
        }

        public async Task<IEnumerable<ApplicationUser>> List()
        {
            var userEntity = await _userManager.Users
                                            .Include(u => u.Organisation)
                                            .Where(u => u.IsActive == true).ToListAsync();
            return userEntity;
        }


        public async Task<ApplicationUser> Get(string userId)
        {
            var userEntity = await _userManager.Users
                                            .Include(u => u.Organisation)
                                            .Where(u => u.Id == userId && u.IsActive == true).FirstOrDefaultAsync();
            return userEntity;
        }

        public async Task<ApplicationUser> GetByUsername(string userName)
        {
            var userEntity = await _userManager.Users
                                            .Include(u => u.Organisation)
                                            .Where(u => u.UserName == userName && u.IsActive == true).FirstOrDefaultAsync();
            return userEntity;
        }

        private async Task<Organisation> CheckOrgnisation(OrganisationBindingModel orgModel)
        {
            var orgInDB = _identityContext.Organisation.Where(o => o.Name == orgModel.Name).FirstOrDefault();
            if (orgInDB != null)
            {
                return orgInDB;
            }
            var organisationEntity = new Organisation
            {
                Id = orgModel.Id,
                Name = orgModel.Name,
                Region = orgModel.Region,
                Data = orgModel.Region,
                CreatedDate = DateTime.UtcNow,
                CreatedById = orgModel.CreatedBy
            };
            var orgnisation = await _identityContext.Organisation.AddAsync(organisationEntity);
            return orgnisation.Entity;
        }

        public async Task<IdentityResult> ChangePassword(ApplicationUser user, string currentPassword, string newPassword)
        {
            var changedPasswordResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return changedPasswordResult;
        }

        public async Task<ApplicationUser> ActivateUser(string updateBy, ApplicationUser entity)
        {
            entity.IsActive = true;
            entity.ModifiedBy = updateBy;
            entity.DateModified = DateTime.UtcNow;

            var result = _identityContext.Users.Update(entity);
            await _identityContext.SaveChangesAsync();

            return result.Entity;
        }

        public async Task<ApplicationUser> GetActiveDeactiveUsers(string userId)
        {
            var userEntity = await _userManager.Users
                                            .Include(u => u.Organisation)
                                            .Where(u => u.Id == userId).FirstOrDefaultAsync();
            return userEntity;
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersByOrgId(string organisationId)
        {
            var userEntity = await _userManager.Users
                                             .Include(u => u.Organisation)
                                             .Where(u => u.OrganisationId == organisationId).ToListAsync();
            return userEntity;
        }

        public async Task<bool> DeactivateOrganisation(string updateBy, IEnumerable<ApplicationUser> organisationUsers)
        {
            try
            {
                foreach (var user in organisationUsers)
                {
                    user.IsActive = false;
                    user.ModifiedBy = updateBy;
                    user.DateModified = DateTime.UtcNow;

                    _identityContext.Users.Update(user);
                }

                await _identityContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to deactivate organization in auth database \n " +
                   $"Message: {ex.Message} \n " +
                   $"Inner Exception: {ex.InnerException} \n " +
                   $"Stack trace: {ex.StackTrace} \n ");

                return false;
            }
        }

        public async Task<bool> ActivateOrganisation(string updateBy, IEnumerable<ApplicationUser> organisationUsers)
        {
            try
            {
                foreach (var user in organisationUsers)
                {
                    user.IsActive = true;
                    user.ModifiedBy = updateBy;
                    user.DateModified = DateTime.UtcNow;

                    _identityContext.Users.Update(user);
                }

                await _identityContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to activate organization in auth database \n " +
                   $"Message: {ex.Message} \n " +
                   $"Inner Exception: {ex.InnerException} \n " +
                   $"Stack trace: {ex.StackTrace} \n ");

                return false;
            }
        }
    }
}
