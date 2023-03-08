using System;
using System.IO;
using API.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "Auth Internal API";

            var host = BuildWebHost(args);

            // Get appsettings file
            var settingPath = Path.GetFullPath(Path.Combine(@"appsettings.json"));

            var lastFolder = Path.GetDirectoryName(settingPath);

            IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(lastFolder)
            .AddJsonFile("appsettings.json")
            .Build();

            // Apply application wide logging during runtime by configuring through AWS cloudwatch
            var _loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            // Do not remove. AWS logging initialization for APIs, middleware,etc.
            _loggerFactory.AddAWSProvider(configuration.GetAWSLoggingConfigSection(), formatter: (logLevel, message, exception) => $"[{DateTime.UtcNow}] {logLevel}: {message}");

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                //.UseKestrel(options => options.ConfigureEndpoints())
                //.UseUrls("http://localhost:5051")
                .UseKestrel()
                .Build();
    }
}
