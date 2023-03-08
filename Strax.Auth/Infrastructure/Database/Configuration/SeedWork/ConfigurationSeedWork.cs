using Domain.Authentication;
using Domain.Configuration;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Databases.Configuration.SeedWork
{
    public class ConfigurationSeedWork : IConfigurationSeedWork
    {
        private ConfigurationDbContext _configurationContext;
        private readonly ILogger<ConfigurationSeedWork> _logger;

        public ConfigurationSeedWork(ConfigurationDbContext configurationContext, ILogger<ConfigurationSeedWork> logger)
        {
            _configurationContext = configurationContext;
            _logger = logger;
        }


        public async Task SeedConfigurationData(DomainSettings domainSettings)
        {
            await AddApiResources();
            await AddIdentityReseources();
            await AddClients(domainSettings);
        }


        private async Task AddApiResources()
        {
            try
            {
                foreach (var api in Resources.GetApis())
                {

                    var apiResourceExists = _configurationContext.ApiResources.Where(a => a.Name == api.Name).FirstOrDefault();


                    if (apiResourceExists == null)
                    {
                        _configurationContext.ApiResources.Add(api.ToEntity());
                    }
                    else
                    {
                        _configurationContext.Entry(apiResourceExists).CurrentValues.SetValues(api);
                    }
                }
                await _configurationContext.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to seed authentication database \n " +
                   $"Message: {ex.Message} \n " +
                   $"Inner Exception: {ex.InnerException} \n " +
                   $"Stack trace: {ex.StackTrace} \n ");
            }
        }

        private async Task AddIdentityReseources()
        {
            try
            {
                foreach (var resource in Resources.GetIdentityResources())
                {
                    var identityResourceExists = _configurationContext.IdentityResources.Where(a => a.Name == resource.Name).FirstOrDefault();

                    if (identityResourceExists == null)
                    {
                        _configurationContext.IdentityResources.Add(resource.ToEntity());
                    }
                    else
                    {
                        _configurationContext.Entry(identityResourceExists).CurrentValues.SetValues(resource);
                    }
                }
                await _configurationContext.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Failed to seed authentication database \n " +
                   $"Message: {ex.Message} \n " +
                   $"Inner Exception: {ex.InnerException} \n " +
                   $"Stack trace: {ex.StackTrace} \n ");
            }
        }

        private async Task AddClients(DomainSettings domainSettings)
        {
            try
            {
                foreach (var client in Resources.GetClients(domainSettings))
                {
                    var clientExists = _configurationContext.Clients
                                                            .Where(c => c.ClientId == client.ClientId)
                                                            .Include(cli => cli.AllowedCorsOrigins)
                                                            .Include(cli => cli.RedirectUris)
                                                            .Include(cli => cli.PostLogoutRedirectUris)
                                                            .Include(cli => cli.AllowedGrantTypes)
                                                            .Include(cli => cli.ClientSecrets)
                                                            .Include(cli => cli.AllowedScopes)
                                                            .FirstOrDefault();

                    if (clientExists == null)
                    {
                        _configurationContext.Clients.Add(client.ToEntity());
                    }
                    else
                    {
                        _configurationContext.Entry(clientExists).CurrentValues.SetValues(client);

                        if (clientExists.AllowedGrantTypes != null)
                            clientExists.AllowedGrantTypes.RemoveAll(u => u.ClientId == clientExists.Id);

                        foreach (var objNew in client.AllowedGrantTypes.ToList())
                        {
                            var obj = new ClientGrantType
                            {
                                ClientId = clientExists.Id,
                                GrantType = objNew
                            };
                            //clientExists.RedirectUris.Add(newRedirectUri);
                            _configurationContext.Add(obj);
                        }

                        if (clientExists.RedirectUris != null)
                            clientExists.RedirectUris.RemoveAll(u => u.ClientId == clientExists.Id);

                        foreach (var objNew in client.RedirectUris.ToList())
                        {

                            var newRedirectUri = new ClientRedirectUri
                            {
                                ClientId = clientExists.Id,
                                RedirectUri = objNew
                            };
                            //clientExists.RedirectUris.Add(newRedirectUri);
                            _configurationContext.Add(newRedirectUri);
                        }


                        if (clientExists.PostLogoutRedirectUris != null)
                            clientExists.PostLogoutRedirectUris.RemoveAll(u => u.ClientId == clientExists.Id);

                        foreach (var objNew in client.PostLogoutRedirectUris.ToList())
                        {
                            var newPostLogoutUri = new ClientPostLogoutRedirectUri
                            {
                                ClientId = clientExists.Id,
                                PostLogoutRedirectUri = objNew
                            };
                            //clientExists.PostLogoutRedirectUris.Add(newPostLogoutUri);

                            _configurationContext.Add(newPostLogoutUri);
                        }

                        if (clientExists.AllowedCorsOrigins != null)
                            clientExists.AllowedCorsOrigins.RemoveAll(u => u.ClientId == clientExists.Id);

                        foreach (var objNew in client.AllowedCorsOrigins.ToList())
                        {

                            var newCorOrigin = new ClientCorsOrigin
                            {
                                ClientId = clientExists.Id,
                                Origin = objNew
                            };
                            //clientExists.AllowedCorsOrigins.Add(newCorOrigin);

                            _configurationContext.Add(newCorOrigin);
                        }



                        if (clientExists.AllowedScopes != null)
                            clientExists.AllowedScopes.RemoveAll(u => u.ClientId == clientExists.Id);

                        foreach (var objNew in client.AllowedScopes.ToList())
                        {
                            var obj = new ClientScope
                            {
                                ClientId = clientExists.Id,
                                Scope = objNew
                            };
                            //clientExists.AllowedScopes.Add(obj);
                            _configurationContext.Add(obj);
                        }


                        if (clientExists.ClientSecrets != null)
                            clientExists.ClientSecrets.RemoveAll(u => u.ClientId == clientExists.Id);

                        foreach (var objNew in client.ClientSecrets.ToList())
                        {
                            var obj = new ClientSecret
                            {
                                ClientId = clientExists.Id,
                                Value = objNew.Value
                            };

                            _configurationContext.Add(obj);
                            //clientExists.ClientSecrets.Add(obj);
                        }
                    }
                    await _configurationContext.SaveChangesAsync();
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
    }
}
