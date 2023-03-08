using System;
using System.IO;
using System.Reflection;
using Auth.Server.Configuration;
using Auth.Server.Extensions;
using Auth.Server.Services;
using Cloud.S3;
using Cloud.SES;
using Databases;
using Databases.Configuration.SeedWork;
using Databases.Identity;
using Databases.Identity.Models;
using Databases.Identity.SeedWork;
using Domain.Authentication;
using Domain.Configuration;
using FootAI.AuthServer.Extensions;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using MediatR;
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
using Utilities.Email;
using Utilities.Email.EmailSender.Events;

namespace Auth.Server
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }
        private string _activeEnv = string.Empty;
        private int _sslPort = 0;
        private string _tokenSigning = string.Empty;
        private string _tokenValidation = string.Empty;

        private readonly ILoggerFactory _loggerFactory;
        private ILogger<Startup> logger;

        public Startup(IHostingEnvironment env, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;

            var appSettingsPath = Path.GetFullPath(Path.Combine(@"appsettings.json"));

            var appConfiguration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(appSettingsPath))
                .AddJsonFile("appsettings.json")
                .Build();

            _activeEnv = appConfiguration.GetValue<string>("ActiveEnvironment");

            _sslPort = appConfiguration.GetSection("Kestrel").GetValue<int>("Port");
            _tokenSigning = appConfiguration.GetSection("Kestrel").GetValue<string>("Token-signing");
            _tokenValidation = appConfiguration.GetSection("Kestrel").GetValue<string>("Token-validation");

            _loggerFactory = loggerFactory;
            //AWS logging provider initialization required before runtime.
            _loggerFactory.AddAWSProvider(this.Configuration.GetAWSLoggingConfigSection(), formatter: (logLevel, message, exception) => $"[{DateTime.UtcNow}] {logLevel}: {message}");
            logger = _loggerFactory.CreateLogger<Startup>();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Logging in configure services
            logger.LogInformation("Auth Server configuration");
            logger.LogInformation("Logging started in Foot AI StraxImages. Environment: " + _activeEnv);
            logger.LogInformation("Logging started in Startup");

            var domainSettings = new DomainSettings();
            Configuration.GetSection(nameof(DomainSettings)).Bind(domainSettings);
            services.Configure<DomainSettings>(options => Configuration.GetSection(nameof(DomainSettings)).Bind(options));

            var connection = new ConnectionStrings();
            Configuration.GetSection(nameof(ConnectionStrings)).Bind(connection);

            var autoLockoutOptions = new AutoLockoutOptions();
            Configuration.GetSection(nameof(AutoLockoutOptions)).Bind(autoLockoutOptions);
            services.Configure<AutoLockoutOptions>(Configuration.GetSection("AutoLockoutOptions"));

            var connectionString = connection.AuthContext;
            var migrationsAssembly = typeof(DataModule).GetTypeInfo().Assembly.GetName().Name;

            services.AddDbContext<IdentityContext>(options =>
            {
                options.UseSqlServer(connectionString,
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(migrationsAssembly);

                    sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                });
            });

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<IdentityContext>()
            .AddDefaultTokenProviders();


            services.AddMvc();
            // Because we want to handle SSL redirection at the ALB level, suspiscion is with below redirect rule we were getting ERR_TOO_MANY_REDIRECTS 

            //services.AddMvc(options =>
            //{
            //    options.SslPort = _sslPort;
            //    options.Filters.Add(new RequireHttpsAttribute());
            //});

            //services.Configure<MvcOptions>(options =>
            //{
            //    options.Filters.Add(new RequireHttpsAttribute());
            //});

            services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "_af";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.HeaderName = "X-XSRF-TOKEN";
            });

            services.Configure<IdentityOptions>(options =>
            {
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(autoLockoutOptions.DefaultLockoutTimeSpan);
                options.Lockout.MaxFailedAccessAttempts = autoLockoutOptions.MaxFailedAccessAttempts;
                options.Lockout.AllowedForNewUsers = autoLockoutOptions.AllowedForNewUsers;
            });

            services.AddTransient<IIdentitySeedWork, IdentitySeedWork>();
            services.AddTransient<IConfigurationSeedWork, ConfigurationSeedWork>();
            services.AddTransient<IClientStore, ClientStore>();
            services.AddTransient<IResourceOwnerPasswordValidator, ResourceOwnerPasswordValidator>();

            services.AddIdentityServer(options =>
            {
                options.UserInteraction.LoginUrl = "/Account/login";
                options.UserInteraction.LogoutUrl = "/Account/logout";
            })
             .AddSigningCredential(KestrelServerOptionsExtensions.GetCertificate(_tokenSigning))   // signing.crt thumbprint
             .AddValidationKey(KestrelServerOptionsExtensions.GetCertificate(_tokenValidation))       // validation.crt thumbprint

             .AddCustomAuthorizeRequestValidator<CustomAuthorizeRequestValidator>()
             //.AddInMemoryClients(Resources.GetClients(domainSettings))
             //.AddInMemoryIdentityResources(Resources.GetIdentityResources())    

             .AddOperationalStore(options =>
             {
                 options.ConfigureDbContext = builder =>
                     builder.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));

                 // this enables automatic token cleanup. this is optional.
                 options.EnableTokenCleanup = true;
                 options.TokenCleanupInterval = 60; // interval in seconds               
             }).
            AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = builder =>
                    builder.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddAspNetIdentity<ApplicationUser>()
            .AddProfileService<ProfileService>();

            //Adding Application Services

            services.AddScoped<IClientStore, ClientStore>();
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


            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllHeaders", policy =>
                {
                    policy.WithOrigins("*")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            logger.LogInformation("Logging in Configure method");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }


            // Because we want to handle SSL redirection at the ALB level, suspiscion is with below redirect rule we were getting ERR_TOO_MANY_REDIRECTS 
            //var options = new RewriteOptions()
            //.AddRedirectToHttps(StatusCodes.Status301MovedPermanently, 62423);
            //app.UseRewriter(new RewriteOptions().AddRedirectToHttps());
            

            app.UseCors("AllowAllHeaders");

            app.UseIdentityServer();
            //app.UseAuthServerApiExceptionHandler();
            app.UseExceptionHandler("/error/500");

            app.UseStatusCodePagesWithReExecute("/error/404");

            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();

            // End Logging
            logger.LogInformation("Logging completed in start method");
        }
    }
}
