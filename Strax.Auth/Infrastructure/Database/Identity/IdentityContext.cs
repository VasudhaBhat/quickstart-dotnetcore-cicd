using Databases.Identity.Mappings;
using Databases.Identity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Databases.Identity
{
    public class IdentityContext : IdentityDbContext<ApplicationUser>
    {
        public IdentityContext(DbContextOptions<IdentityContext> options) : base(options)
        {

        }

        public DbSet<Organisation> Organisation { get; set; }        

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);    


            builder.ApplyConfiguration(new ApplicationUserMap());
            builder.ApplyConfiguration(new OrganisationMap());
        }

        public class IdentityContextFactory : IDesignTimeDbContextFactory<IdentityContext>
        {
            public IdentityContext CreateDbContext(string[] args)
            {

                var settingPath = Path.GetFullPath(Path.Combine(@"../../Server/Auth.Server/appsettings.json"));

                var lastFolder = Path.GetDirectoryName(settingPath);

                IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(lastFolder)
                .AddJsonFile("appsettings.json")
                .Build();

                var builder = new DbContextOptionsBuilder<IdentityContext>();
                builder.UseSqlServer(configuration.GetConnectionString("AuthContext"));
                return new IdentityContext(builder.Options);


                // Old code

                //var settingPath = Path.GetFullPath(Path.Combine(@"../../Server/Auth.Server/appsettings.json"));

                //var lastFolder = Path.GetDirectoryName(settingPath);
                ////var pathWithoutLastFolder = Path.GetDirectoryName(lastFolder);

                //IConfigurationRoot configuration = new ConfigurationBuilder()
                //.SetBasePath(lastFolder)
                //.AddJsonFile("appsettings.json")
                //.Build();

                //var appSettings = new AppSettings();
                //configuration.GetSection(nameof(AppSettings)).Bind(appSettings);

                //var builder = new DbContextOptionsBuilder<IdentityContext>();
                ////builder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=AuthServer;Trusted_Connection=False;MultipleActiveResultSets=true");
                ////builder.UseSqlServer("Server=(local);Database=AuthServer;Trusted_Connection=True;MultipleActiveResultSets=true;user id=sa;password=123456789");
                //builder.UseSqlServer("Server=strax.cp2rfyrtksav.ap-southeast-2.rds.amazonaws.com,1433;Database=AuthServer;MultipleActiveResultSets=true; User ID=sa; password=bd889c6b-936c-4cfa-8438-421b0d07f3a7;");

                //return new IdentityContext(builder.Options);
            }
        }
    }
}
