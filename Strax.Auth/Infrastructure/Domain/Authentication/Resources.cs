using IdentityServer4;
using Domain.Configuration;
using IdentityServer4.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Domain.Authentication
{
    public static class Resources
    {
        #region Configuration Resources
        public static IEnumerable<ApiResource> GetApis()
        {
            return new List<ApiResource>
            {
                new ApiResource
                {
                    Name = "internal_auth_api",
                    DisplayName = "Auth  server API",
                    Description = "Protected Auth Api",
                    ApiSecrets = new List<Secret> {new Secret("internal_auth_api".Sha256())},
                    UserClaims = new List<string>
                    {
                        DomainClaimTypes.Role
                    },
                    Scopes = new List<Scope>
                    {
                        new Scope(){
                            Name = "internal_auth_api", // Should match name of ApiResource.Name
                            DisplayName = "Auth  server API",
                            UserClaims = new List<string>{
                                DomainClaimTypes.Role
                            }
                        }
                    }
                },
                new ApiResource
                {
                    Name = "strax-api-name",
                    DisplayName = "strax-api-name",
                    Description = "Protected Strax Api",
                    ApiSecrets = new List<Secret> {new Secret("strax-api-secret".Sha256())},
                    UserClaims = new List<string>
                    {
                        DomainClaimTypes.Role,
                    },
                    Scopes = new List<Scope>
                    {
                        new Scope(){
                            Name = "strax-api-name", // Should match name of ApiResource.Name
                            DisplayName = "strax-api-name",
                            UserClaims = new List<string>{
                                DomainClaimTypes.Role
                            }
                        }
                    }
                }
            };
        }

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource
                {
                    Name = DomainScopes.MvcClientUser,
                    DisplayName = "MVC client user",
                    Description = "Basic resource, to be adjusted for your application",
                },
                 new IdentityResource(DomainScopes.Roles, new List<string> {DomainClaimTypes.Role})
            };
        }

        public static IEnumerable<Client> GetClients(DomainSettings domainSettings)
        {
            var angularJSWebClient = GetAngularJSWebClient(domainSettings);
            var swaggerClient = GetSwaggerClient(domainSettings);
            var straxServiceClient = GetROStraxServiceClient(domainSettings);
            var footAIClient = GetFootAIClient(domainSettings);
            var annotationAppClient = AnnotationAppClient(domainSettings);

            var clientList = new List<Client>()
            {
                angularJSWebClient,
                swaggerClient,
                straxServiceClient,
                footAIClient,
                annotationAppClient
            };

            return clientList;
        }

        private static Client GetAngularJSWebClientOld(DomainSettings domainSettings)
        {
            return new Client
            {
                Enabled = true,
                ClientId = domainSettings.AngularClient.Id,
                ClientName = domainSettings.AngularClient.Name,
                ClientUri = domainSettings.AngularClient.Url + "/",
                //RequirePkce = false,                    
                RequireConsent = false,
                AllowAccessTokensViaBrowser = true,
                AlwaysSendClientClaims = true,
                AlwaysIncludeUserClaimsInIdToken = true,
                AccessTokenLifetime = 86400, //access token is valid for 1 day,
                //IdentityTokenLifetime = 4800,
                AllowedGrantTypes = GrantTypes.Implicit,
                AccessTokenType = AccessTokenType.Reference,
                RedirectUris = new List<string>
                    {
                        domainSettings.AngularClient.Url + "/#/callback/?#",
                        domainSettings.AngularClient.Url + "/#/silentrenew",
                        domainSettings.AngularClient.Url + "/app/common/modules/auth/silentrenew.html",
                        "https://localhost:4000/#/callback/?#"

                    },
                PostLogoutRedirectUris = new List<string>
                    {
                        domainSettings.AngularClient.Url + "/", //"http://localhost:3000/"
                        "https://localhost:4000/"
                    },
                AllowedCorsOrigins = {
                    domainSettings.AngularClient.Url,
                    "https://localhost:4000/"
                },
                AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        DomainScopes.Roles,
                        DomainScopes.MvcClientUser,
                        domainSettings.BusinessAPI.Name,
                        domainSettings.Api.Name
                    }
            };
        }

        private static Client GetAngularJSWebClient(DomainSettings domainSettings)
        {
            return new Client
            {
                Enabled = true,
                ClientId = domainSettings.AngularClient.Id, //"strax_web_ng",
                ClientName = domainSettings.AngularClient.Name, //"StraxImages Web",
                //ClientUri = domainSettings.AngularClient.Url + "/", //"http://localhost:3000/",
                ClientSecrets = new List<Secret>
                {
                    new Secret("strax_web_ng".Sha256())
                },

                AllowOfflineAccess = true,
                RequireConsent = false,
                AllowAccessTokensViaBrowser = true,
                AlwaysSendClientClaims = true,
                AlwaysIncludeUserClaimsInIdToken = true,
                AccessTokenLifetime = 1200, // access token is valid for 20 mins            
                AbsoluteRefreshTokenLifetime = 2592000, // refresh token is valid for 30 days
                IdentityTokenLifetime = 1200, // identity token is valid for 20 mins  
                RefreshTokenExpiration = TokenExpiration.Sliding,
                RefreshTokenUsage = TokenUsage.ReUse,
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                AccessTokenType = AccessTokenType.Reference,
                AllowedCorsOrigins = {
                    domainSettings.AngularClient.Url
                },
                AllowedScopes = new List<string>
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    DomainScopes.Roles,
                    DomainScopes.MvcClientUser,
                    domainSettings.Api.Name, //"internal_auth_api",
                    domainSettings.BusinessAPI.Name //"strax-api-name"                        
                }
            };
        }

        private static Client GetSwaggerClient(DomainSettings domainSettings)
        {
            return new Client
            {
                ClientId = "demo_api_swagger",
                ClientName = "Swagger UI for business_api",
                AllowedGrantTypes = GrantTypes.Implicit,
                AllowAccessTokensViaBrowser = true,
                RedirectUris = { domainSettings.BusinessAPI.Url + "/devdoc/oauth2-redirect.html" },
                AllowedScopes = { domainSettings.BusinessAPI.Name },
                RequireConsent = false,
                AccessTokenType = AccessTokenType.Reference
            };
        }

        private static Client GetROStraxServiceClient(DomainSettings domainSettings)
        {
            return new Client
            {
                ClientId = "StraxServiceClient",
                ClientName = "Resource Owner Strax Service Client",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                //AccessTokenType = AccessTokenType.Jwt,
                AccessTokenLifetime = 28800, // access token is valid for 8 hours            
                AbsoluteRefreshTokenLifetime = 2592000, // refresh token is valid for 30 days
                IdentityTokenLifetime = 28800, // identity token is valid for 8 hours 
                UpdateAccessTokenClaimsOnRefresh = true,
                SlidingRefreshTokenLifetime = 2592000,
                AllowOfflineAccess = true,
                RefreshTokenExpiration = TokenExpiration.Absolute,
                RefreshTokenUsage = TokenUsage.ReUse,
                AlwaysSendClientClaims = true,
                Enabled = true,
                AccessTokenType = AccessTokenType.Reference,
                ClientSecrets = new List<Secret> { new Secret("Straximages#2017".Sha256()) },
                AllowedScopes = {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    DomainScopes.Roles,
                    DomainScopes.MvcClientUser,
                    domainSettings.BusinessAPI.Name,
                    domainSettings.Api.Name
                }
            };
        }

        private static Client GetFootAIClient(DomainSettings domainSettings)
        {
            return new Client
            {
                ClientId = "FootAIClient",
                ClientName = "FootAI Client",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                AccessTokenLifetime = 28800, // access token is valid for 8 hours            
                AbsoluteRefreshTokenLifetime = 2592000, // refresh token is valid for 30 days
                IdentityTokenLifetime = 28800, // identity token is valid for 8 hours 
                UpdateAccessTokenClaimsOnRefresh = true,
                SlidingRefreshTokenLifetime = 2592000,
                AllowOfflineAccess = true,
                RefreshTokenExpiration = TokenExpiration.Absolute,
                RefreshTokenUsage = TokenUsage.ReUse,
                AlwaysSendClientClaims = true,
                Enabled = true,
                AccessTokenType = AccessTokenType.Reference,
                ClientSecrets = new List<Secret> { new Secret("Straximages#2017".Sha256()) },
                AllowedScopes = {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    DomainScopes.Roles,
                    DomainScopes.MvcClientUser,
                    domainSettings.BusinessAPI.Name,
                    domainSettings.Api.Name
                }
            };
        }

        private static Client AnnotationAppClient(DomainSettings domainSettings)
        {
            return new Client
            {
                ClientId = "AnnotationAppClient",
                ClientName = "Annotation App Client",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                AccessTokenLifetime = 604800, //86400, The token is valid for 7 days
                IdentityTokenLifetime = 604800, //86400, The token is valid for 7 days
                UpdateAccessTokenClaimsOnRefresh = true,
                SlidingRefreshTokenLifetime = 1296000,
                AllowOfflineAccess = true,
                RefreshTokenExpiration = TokenExpiration.Absolute,
                RefreshTokenUsage = TokenUsage.ReUse,
                AlwaysSendClientClaims = true,
                Enabled = true,
                AccessTokenType = AccessTokenType.Reference,
                ClientSecrets = new List<Secret> { new Secret("Straximages#2017".Sha256()) },
                AllowedScopes = {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    DomainScopes.Roles,
                    DomainScopes.MvcClientUser,
                    domainSettings.BusinessAPI.Name,
                    domainSettings.Api.Name
                }
            };
        }

        #endregion


        #region Identity Resources

        public static IEnumerable<IdentityRole> GetRoles()
        {
            return new List<IdentityRole> {
                new IdentityRole{ Name = DomainRoles.SuperAdmin , NormalizedName = DomainRoles.SuperAdmin},
                new IdentityRole{ Name = DomainRoles.Admin , NormalizedName = DomainRoles.Admin},
                new IdentityRole{ Name = DomainRoles.RootUser , NormalizedName = DomainRoles.RootUser},
                new IdentityRole{ Name = DomainRoles.Doctor , NormalizedName = DomainRoles.Doctor},
                new IdentityRole{ Name = DomainRoles.Centre , NormalizedName = DomainRoles.Centre},
                new IdentityRole{ Name = DomainRoles.ResearchUser , NormalizedName = DomainRoles.ResearchUser},
                new IdentityRole{ Name = DomainRoles.StraxService , NormalizedName = DomainRoles.StraxService}
            };
        }
        #endregion
    }
}