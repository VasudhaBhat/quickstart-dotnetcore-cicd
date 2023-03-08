using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Auth.Server.Extensions
{
    public static class KestrelServerOptionsExtensions
    {
        public static void ConfigureEndpoints(this KestrelServerOptions options)
        {
            var appSettingsPath = Path.GetFullPath(Path.Combine(@"appsettings.json"));

            var appConfiguration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(appSettingsPath))
                .AddJsonFile("appsettings.json")
                .Build();

            var port = appConfiguration.GetSection("Kestrel").GetValue<int>("Port");

            var certificate = appConfiguration.GetSection("Kestrel").GetValue<string>("Certificate");// localhost.crt thumbprint

            options.Listen(IPAddress.Any, port, listenOptions =>
            {
                listenOptions.UseHttps(GetCertificate(certificate)); 
            });
        }

        public static X509Certificate2 GetCertificate(string thumbprint)
        {
            using (X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                certStore.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (certCollection.Count > 0) return certCollection[0];
            }
            return null;
        }
    }
}
