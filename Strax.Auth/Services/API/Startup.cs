using API.Middlewares;
using API.Models;
using Cloud.S3;
using Cloud.SES;
using Core.Common.AppEnumaration;
using Databases.Identity;
using Databases.Identity.Models;
using DataServices.Contracts;
using DataServices.Services;
using Domain.Authentication;
using Domain.Configuration;
using IdentityModel;
using MediatR;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using Utilities.Email;
using Utilities.Email.EmailSender.Events;

namespace API
{
    public class Startup
    {
        private int _sslPort = 0;
        private string _activeEnv = string.Empty;

        private readonly ILoggerFactory _loggerFactory;
        private ILogger<Startup> logger;

        public Startup(IHostingEnvironment env, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            TelemetryConfiguration.Active.DisableTelemetry = true;

            if (env.IsDevelopment())
            {
                var launchConfiguration = new ConfigurationBuilder()
                    .SetBasePath(env.ContentRootPath)
                    .AddJsonFile("launchSettings.json")
                    .Build();
            }

            Configuration = configuration;

            var appSettingsPath = Path.GetFullPath(Path.Combine(@"appsettings.json"));

            var appConfiguration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(appSettingsPath))
                .AddJsonFile("appsettings.json")
                .Build();

            _activeEnv = appConfiguration.GetValue<string>("ActiveEnvironment");
            _sslPort = appConfiguration.GetSection("Kestrel").GetValue<int>("Port");

            _loggerFactory = loggerFactory;
            //AWS logging provider initialization required before runtime.
            _loggerFactory.AddAWSProvider(this.Configuration.GetAWSLoggingConfigSection(), formatter: (logLevel, message, exception) => $"[{DateTime.UtcNow}] {logLevel}: {message}");
            logger = _loggerFactory.CreateLogger<Startup>();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var autoLockoutOptions = new AutoLockoutOptions();
            Configuration.GetSection(nameof(AutoLockoutOptions)).Bind(autoLockoutOptions);
            services.Configure<AutoLockoutOptions>(Configuration.GetSection("AutoLockoutOptions"));

            // Logging in configure services
            logger.LogInformation("Auth API configuration");
            logger.LogInformation("Logging started in Foot AI StraxImages. Environment: " + _activeEnv);
            logger.LogInformation("Logging started in Startup");

            string connectionString = Configuration.GetConnectionString("DefaultConnection");
            var domainSettings = new DomainSettings();
            Configuration.GetSection(nameof(DomainSettings)).Bind(domainSettings);
            services.Configure<DomainSettings>(options => Configuration.GetSection(nameof(DomainSettings)).Bind(options));


            // Because we want to handle SSL redirection at the ALB level, suspiscion is with below redirect rule we were getting ERR_TOO_MANY_REDIRECTS 
            //services.AddMvcCore(options =>
            //{
            //    options.SslPort = _sslPort;
            //    options.Filters.Add(new RequireHttpsAttribute());
            //})

            services.AddMvcCore()
               .AddAuthorization()
               .AddJsonFormatters()
               .AddJsonOptions(options =>
               {
                   options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                   options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
               });

            services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "_af";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.HeaderName = "X-XSRF-TOKEN";
            });

            // Because we want to handle SSL redirection at the ALB level, suspiscion is with below redirect rule we were getting ERR_TOO_MANY_REDIRECTS 
            //services.Configure<MvcOptions>(options =>
            //{
            //    options.Filters.Add(new RequireHttpsAttribute());
            //});

            services.AddAuthentication("Bearer")
               .AddIdentityServerAuthentication(options =>
               {
                   options.Authority = domainSettings.Auth.Url;
                   options.RequireHttpsMetadata = false;
                   options.ApiName = domainSettings.Api.Id;
                   options.ApiSecret = domainSettings.Api.Secret;
               });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllHeaders",
                      builder =>
                      {
                          builder.AllowAnyOrigin()
                                 .AllowAnyHeader()
                                 .AllowAnyMethod()
                                 .SetIsOriginAllowedToAllowWildcardSubdomains();
                      });
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(DomainPolicies.NormalUser,
                     policy => policy.RequireClaim(JwtClaimTypes.Scope, DomainScopes.MvcClientUser));
                options.AddPolicy(DomainPolicies.SuperAdmin, policy => policy.RequireClaim(JwtClaimTypes.Role, Role.SuperAdmin.Name));
                options.AddPolicy(DomainPolicies.Admin, policy => policy.RequireClaim(JwtClaimTypes.Role, Role.Admin.Name));
            });


            services.AddDbContext<IdentityContext>(options =>
            {
                options.UseSqlServer(connectionString,
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                });

            });

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<IdentityContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(autoLockoutOptions.DefaultLockoutTimeSpan);
                options.Lockout.MaxFailedAccessAttempts = autoLockoutOptions.MaxFailedAccessAttempts;
                options.Lockout.AllowedForNewUsers = autoLockoutOptions.AllowedForNewUsers;
            });

            services.AddTransient<IUserService, UserService>();

            services.AddTransient<IEmail, Email>();
            services.AddTransient<IS3Service, S3Service>();
            if (_activeEnv != "Production" && _activeEnv != "UAT")
            {
                services.AddTransient<IEmailService, MockEmailService>();
                logger.LogInformation("Initialized mock email service");
            }
            else
            {
                services.AddTransient<IEmailService, EmailService>();
                logger.LogInformation("Initialized actual email service");
            }

            // To add the events that will be used by MediatR
            var assembly = typeof(ResetPasswordRequestEvent).Assembly;
            services.AddMediatR(assembly);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            logger.LogInformation("Logging in Configure method");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }


            // Because we want to handle SSL redirection at the ALB level, suspiscion is with below redirect rule we were getting ERR_TOO_MANY_REDIRECTS 
            //app.UseRewriter(new RewriteOptions().AddRedirectToHttps());
            app.UseCors("AllowAllHeaders");
            app.UseAuthentication();
            app.UseAuthApiExceptionHandler();
            app.UseMvc();

            // End Logging
            logger.LogInformation("Logging completed in start method");
        }
    }
}
