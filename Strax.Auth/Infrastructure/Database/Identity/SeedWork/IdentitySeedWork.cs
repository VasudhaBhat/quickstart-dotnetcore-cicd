using Core.Common.AppEnumaration;
using Databases.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Databases.Identity.SeedWork
{
    public class IdentitySeedWork : IIdentitySeedWork
    {
        private const string organisationId = "d68c5333-8132-4f78-a086-ccecb1e32b01";
        private const string superAdminId = "1071dccb-cb80-4ee3-ab7b-fb44ca916b40";
        private const string testOrgId = "ce0bee3a-25e2-435c-b163-6559b4e43777";
        private const string testOrgId2 = "f92842d3-e91a-4d26-a136-03dd2d0919a8";

        private readonly ILogger<IdentitySeedWork> _logger;
        private readonly IdentityContext _identityContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IdentitySeedWork(IdentityContext identityContext, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<IdentitySeedWork> logger)
        {
            _identityContext = identityContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }


        public async Task SeedIdentityData(string environment)
        {
            await AddRoles();
            await AddOrganisation();
            await AddSuperAdmin(environment);
            await AddStraxServiceUser();

        }

        public async Task SeedTestingData()
        {
            await AddAdmin();
            await AddAnnotator();
            await AddOrganisationForTesting();
            await AddRootUserForTesting();
            await AddDoctorUserForTesting();
            await AddCentreUserForTesting();
        }

        public async Task SeedAdminTestingData()
        {
            await AddAdmin();            
        }
        private async Task AddRoles()
        {
            try
            {
                foreach (var role in Role.List())
                {
                    var roleExists = _roleManager.Roles.Where(ur => ur.Name == role.Name).FirstOrDefault();
                    if (roleExists == null)
                    {
                        IdentityRole userRole = new IdentityRole();
                        userRole.Name = role.Name;
                        await _roleManager.CreateAsync(userRole);
                    }                   
                }
                await _identityContext.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to seed authentication database \n " +
                    $"Message: {ex.Message} \n " +
                    $"Inner Exception: {ex.InnerException} \n " +
                    $"Stack trace: {ex.StackTrace} \n ");
            }
        }

        private async Task AddOrganisation()
        {

            var organisation = new Organisation()
            {
                Id = organisationId,
                Name = "Curvebeam, LLC",
                Region = "USA",
                Data = "",
                CreatedDate = DateTime.UtcNow
            };

            var orgExist = _identityContext.Organisation.Where(u => u.Id == organisation.Id).AsNoTracking().FirstOrDefault();
            if (orgExist==null)
            {
                try
                {
                    var result = await _identityContext.AddAsync<Organisation>(organisation);
                    await _identityContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogCritical($"Failed to seed authentication database \n " +
                                        $"Message: {ex.Message} \n " +
                                        $"Inner Exception: {ex.InnerException} \n " +
                                        $"Stack trace: {ex.StackTrace} \n ");
                }
            }
            else
            {                
                await Task.FromResult(_identityContext.Update<Organisation>(organisation));
                await _identityContext.SaveChangesAsync();
            }
        }

        private async Task AddSuperAdmin(string environment)
        {
            try
            {
                //var orgnisation = _identityContext.Organisation.Where(o => o.Id == organisationId).FirstOrDefault();

                var adminUser = new ApplicationUser
                {
                    Id = superAdminId,                    
                    OrganisationId = organisationId,
                    IsActive = true,
                    LastLoggedOn = System.DateTime.UtcNow,
                    DateAdded = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow,
                    Email = "super.admin@curvebeam.com",//(environment == "Production" || environment == "UAT") ? "straxweb.super@straxcorp.com" : "straximages.dev.team.au@gmail.com",
                    NormalizedEmail = "super.admin@curvebeam.com",//(environment == "Production" || environment == "UAT") ? "straxweb.super@straxcorp.com" : "straximages.dev.team.au@gmail.com",
                    UserName = "super.admin@curvebeam.com", //(environment == "Production" || environment == "UAT") ? "straxweb.super@straxcorp.com" : "straximages.dev.team.au@gmail.com",
                    NormalizedUserName = "super.admin@curvebeam.com", //(environment == "Production" || environment == "UAT") ? "straxweb.super@straxcorp.com" : "straximages.dev.team.au@gmail.com",
                    PhoneNumber = "+61396200250",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString("D"),
                    IsPasswordTemporary = false
                };

                if (await _userManager.FindByIdAsync(superAdminId) == null)
                {
                    var password = new PasswordHasher<ApplicationUser>();
                    var hashed = password.HashPassword(adminUser, "Straximages#2017");
                    adminUser.PasswordHash = hashed;
                    var user = await _userManager.CreateAsync(adminUser);
                    if (user.Succeeded)
                    {
                        var addToRole = await _userManager.AddToRoleAsync(adminUser, Role.SuperAdmin.Name);
                    }
                }
                else {
                    await Task.FromResult(_userManager.UpdateAsync(adminUser));
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to seed authentication database \n " +
                                        $"Message: {ex.Message} \n " +
                                        $"Inner Exception: {ex.InnerException} \n " +
                                        $"Stack trace: {ex.StackTrace} \n ");
            }
        }

        private async Task AddAdmin()
        {
            try
            {
                //var orgnisation = _identityContext.Organisation.Where(o => o.Id == organisationId).FirstOrDefault();

                var adminUser = new ApplicationUser
                {
                    Id = "fafe50f0-f2d8-41eb-aec6-1ae3ae6b390b",                    
                    OrganisationId = organisationId,
                    IsActive = true,
                    LastLoggedOn = DateTime.UtcNow,
                    DateAdded = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow,
                    Email = "admin@curvebeam.com",
                    NormalizedEmail = "admin@curvebeam.com",
                    UserName = "admin@curvebeam.com",
                    NormalizedUserName = "admin@curvebeam.com",
                    PhoneNumber = "+61396200250",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString("D"),
                    IsPasswordTemporary = false
                };

                if (await _userManager.FindByEmailAsync(adminUser.Email) == null)
                {
                    var password = new PasswordHasher<ApplicationUser>();
                    var hashed = password.HashPassword(adminUser, "Straximages#2017");
                    adminUser.PasswordHash = hashed;
                    var user = await _userManager.CreateAsync(adminUser);
                    if (user.Succeeded)
                    {
                        var addToRole = await _userManager.AddToRoleAsync(adminUser, Role.Admin.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to seed authentication database \n " +
                                        $"Message: {ex.Message} \n " +
                                        $"Inner Exception: {ex.InnerException} \n " +
                                        $"Stack trace: {ex.StackTrace} \n ");
            }
        }

        private async Task AddAnnotator()
        {
            try
            {
                var annotatorUser = new ApplicationUser
                {
                    Id = "fafe50f0-f2d8-41eb-aec6-1ae3ae6b4000",
                    OrganisationId = organisationId,
                    IsActive = true,
                    LastLoggedOn = DateTime.UtcNow,
                    DateAdded = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow,
                    Email = "annotator@curvebeam.com",
                    NormalizedEmail = "annotator@curvebeam.com",
                    UserName = "annotator@curvebeam.com",
                    NormalizedUserName = "annotator@curvebeam.com",
                    PhoneNumber = "+61396200250",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString("D"),
                    IsPasswordTemporary = false
                };

                if (await _userManager.FindByEmailAsync(annotatorUser.Email) == null)
                {
                    var password = new PasswordHasher<ApplicationUser>();
                    var hashed = password.HashPassword(annotatorUser, "Straximages#2017");
                    annotatorUser.PasswordHash = hashed;
                    var user = await _userManager.CreateAsync(annotatorUser);
                    if (user.Succeeded)
                    {
                        var addToRole = await _userManager.AddToRoleAsync(annotatorUser, Role.Annotator.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to seed authentication database \n " +
                                        $"Message: {ex.Message} \n " +
                                        $"Inner Exception: {ex.InnerException} \n " +
                                        $"Stack trace: {ex.StackTrace} \n ");
            }
        }

        private async Task AddStraxServiceUser()
        {
            try
            {
                //var orgnisation = _identityContext.Organisation.FirstOrDefault();

                var straxServiceUser = new ApplicationUser
                {
                    Id = "1071dccb-cb80-4ee3-ab7b-fb44ca456c78",
                    OrganisationId = organisationId,
                    IsActive = true,
                    LastLoggedOn = DateTime.UtcNow,
                    DateAdded = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow,
                    Email = "strax.service.user@straxcorp.com",
                    NormalizedEmail = "strax.service.user@straxcorp.com",
                    UserName = "strax.service.user@straxcorp.com",
                    NormalizedUserName = "strax.service.user@straxcorp.com",
                    PhoneNumber = "+61396200250",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString("D"),
                    IsPasswordTemporary = false
                };

                if (await _userManager.FindByEmailAsync(straxServiceUser.Email) == null)
                {
                    var password = new PasswordHasher<ApplicationUser>();
                    var hashed = password.HashPassword(straxServiceUser, "Straximages#2017");
                    straxServiceUser.PasswordHash = hashed;
                    var user = await _userManager.CreateAsync(straxServiceUser);
                    if (user.Succeeded)
                    {
                        var addToRole = await _userManager.AddToRoleAsync(straxServiceUser, Role.StraxService.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to seed authentication database \n " +
                                        $"Message: {ex.Message} \n " +
                                        $"Inner Exception: {ex.InnerException} \n " +
                                        $"Stack trace: {ex.StackTrace} \n ");
            }
        }

        //Testing Data Start
        private async Task AddOrganisationForTesting()
        {
            var orgList = new List<Organisation> {
                new Organisation()
                {
                    Id = testOrgId,
                    Name = "Test Org",
                    Region = "AU",
                    Data = "",
                    CreatedDate = DateTime.UtcNow
                },
                new Organisation()
                {
                    Id = testOrgId2,
                    Name = "Test Org 2",
                    Region = "AU",
                    Data = "",
                    CreatedDate = DateTime.UtcNow
                }
            };


            foreach (var organisation in orgList)
            {
                if (!_identityContext.Organisation.Any(u => u.Name == organisation.Name))
                {
                    try
                    {
                        var result = await _identityContext.AddAsync<Organisation>(organisation);
                        await _identityContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical($"Failed to seed authentication database \n " +
                                            $"Message: {ex.Message} \n " +
                                            $"Inner Exception: {ex.InnerException} \n " +
                                            $"Stack trace: {ex.StackTrace} \n ");
                    }
                }
            }
        }
        private async Task AddRootUserForTesting()
        {            
            try
            {
                //var orgnisation = _identityContext.Organisation.Where(o => o.Id == testOrgId).AsNoTracking().FirstOrDefault();
                var userList = new List<ApplicationUser>
                {
                    new ApplicationUser
                    {
                        Id = testOrgId,
                        OrganisationId = testOrgId,
                        IsActive = true,
                        LastLoggedOn = DateTime.UtcNow,
                        DateAdded = DateTime.UtcNow,
                        DateModified = DateTime.UtcNow,
                        Email = "org@test.com",
                        NormalizedEmail = "org@test.com",
                        UserName = "org@test.com",
                        NormalizedUserName = "org@test.com",
                        PhoneNumber = "1234567890",
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString("D"),
                        IsPasswordTemporary = false
                    },
                    new ApplicationUser
                    {
                        Id = testOrgId2,
                        OrganisationId = testOrgId2,
                        IsActive = true,
                        LastLoggedOn = DateTime.UtcNow,
                        DateAdded = DateTime.UtcNow,
                        DateModified = DateTime.UtcNow,
                        Email = "org2@test.com",
                        NormalizedEmail = "org2@test.com",
                        UserName = "org2@test.com",
                        NormalizedUserName = "org2@test.com",
                        PhoneNumber = "1234567890",
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString("D"),
                        IsPasswordTemporary = false
                    }
                };

                foreach (var orgRootUser in userList)
                {
                    if (await _userManager.FindByEmailAsync(orgRootUser.Email) == null)
                    {
                        var password = new PasswordHasher<ApplicationUser>();
                        var hashed = password.HashPassword(orgRootUser, "Straximages#2017");
                        orgRootUser.PasswordHash = hashed;
                        var user = await _userManager.CreateAsync(orgRootUser);
                        if (user.Succeeded)
                        {
                            var addToRole = await _userManager.AddToRoleAsync(orgRootUser, Role.OrgRootUser.Name);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to seed authentication database \n " +
                                        $"Message: {ex.Message} \n " +
                                        $"Inner Exception: {ex.InnerException} \n " +
                                        $"Stack trace: {ex.StackTrace} \n ");
            }
        }
        private async Task AddDoctorUserForTesting()
        {
            try
            {
                //var orgnisation = _identityContext.Organisation.Where(o => o.Id == "ce0bee3a-25e2-435c-b163-6559b4e43777").AsNoTracking().FirstOrDefault();

                var userList = new List<ApplicationUser>
                {
                    new ApplicationUser
                    {
                        Id = "af6a466f-a1d8-4a5b-a209-75d7d7aae21e",
                        OrganisationId = testOrgId,
                        IsActive = true,
                        LastLoggedOn = DateTime.UtcNow,
                        DateAdded = DateTime.UtcNow,
                        DateModified = DateTime.UtcNow,
                        Email = "doctor@test.com",
                        NormalizedEmail = "doctor@test.com",
                        UserName = "doctor@test.com",
                        NormalizedUserName = "doctor@test.com",
                        PhoneNumber = "1234567890",
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString("D"),
                        IsPasswordTemporary = false
                    },
                    new ApplicationUser
                    {
                        Id = "af6a466f-a1d8-4a5b-a209-75d7d7aae22m",
                        OrganisationId = testOrgId2,
                        IsActive = true,
                        LastLoggedOn = DateTime.UtcNow,
                        DateAdded = DateTime.UtcNow,
                        DateModified = DateTime.UtcNow,
                        Email = "doctor2@test.com",
                        NormalizedEmail = "doctor2@test.com",
                        UserName = "doctor2@test.com",
                        NormalizedUserName = "doctor2@test.com",
                        PhoneNumber = "1234567890",
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString("D"),
                        IsPasswordTemporary = false
                    }
                };

                foreach (var orgDoctorUser in userList)
                {
                    if (await _userManager.FindByEmailAsync(orgDoctorUser.Email) == null)
                    {
                        var password = new PasswordHasher<ApplicationUser>();
                        var hashed = password.HashPassword(orgDoctorUser, "Straximages#2017");
                        orgDoctorUser.PasswordHash = hashed;
                        var user = await _userManager.CreateAsync(orgDoctorUser);
                        if (user.Succeeded)
                        {
                            var addToRole = await _userManager.AddToRoleAsync(orgDoctorUser, Role.Doctor.Name);
                            //var addToResearchRole = await _userManager.AddToRoleAsync(orgDoctorUser, DomainRoles.ResearchUser);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to seed authentication database \n " +
                                        $"Message: {ex.Message} \n " +
                                        $"Inner Exception: {ex.InnerException} \n " +
                                        $"Stack trace: {ex.StackTrace} \n ");
            }
        }

        private async Task AddCentreUserForTesting()
        {
            try
            {
                //var orgnisation = _identityContext.Organisation.Where(o => o.Id == "ce0bee3a-25e2-435c-b163-6559b4e43777").AsNoTracking().FirstOrDefault();

                var userlist = new List<ApplicationUser>
                {
                    new ApplicationUser
                    {
                        Id = "c2d80780-bf7c-4993-b966-6a747c135de4",
                        OrganisationId = testOrgId,
                        IsActive = true,
                        LastLoggedOn = DateTime.UtcNow,
                        DateAdded = DateTime.UtcNow,
                        DateModified = DateTime.UtcNow,
                        Email = "centre@test.com",
                        NormalizedEmail = "centre@test.com",
                        UserName = "centre@test.com",
                        NormalizedUserName = "centre@test.com",
                        PhoneNumber = "1234567890",
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString("D"),
                        IsPasswordTemporary = false
                    },
                    new ApplicationUser
                    {
                        Id = "c2d80780-bf7c-4993-b966-6a747c135df7",
                        OrganisationId = testOrgId2,
                        IsActive = true,
                        LastLoggedOn = DateTime.UtcNow,
                        DateAdded = DateTime.UtcNow,
                        DateModified = DateTime.UtcNow,
                        Email = "centre2@test.com",
                        NormalizedEmail = "centre2@test.com",
                        UserName = "centre2@test.com",
                        NormalizedUserName = "centre2@test.com",
                        PhoneNumber = "1234567890",
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString("D"),
                        IsPasswordTemporary = false
                    }
                };

                foreach (var orgCentreUser in userlist)
                {
                    if (await _userManager.FindByEmailAsync(orgCentreUser.Email) == null)
                    {
                        var password = new PasswordHasher<ApplicationUser>();
                        var hashed = password.HashPassword(orgCentreUser, "Straximages#2017");
                        orgCentreUser.PasswordHash = hashed;
                        var user = await _userManager.CreateAsync(orgCentreUser);
                        if (user.Succeeded)
                        {
                            var addToRole = await _userManager.AddToRoleAsync(orgCentreUser, Role.Centre.Name);
                            //var addToResearchRole = await _userManager.AddToRoleAsync(orgCentreUser, DomainRoles.ResearchUser);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to seed authentication database \n " +
                                        $"Message: {ex.Message} \n " +
                                        $"Inner Exception: {ex.InnerException} \n " +
                                        $"Stack trace: {ex.StackTrace} \n ");
            }
        }

        //Testing Data End

    }
}
