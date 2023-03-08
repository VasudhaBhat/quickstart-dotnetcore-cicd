using Domain.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Databases.Configuration.SeedWork
{
    public interface IConfigurationSeedWork
    {
        Task SeedConfigurationData(DomainSettings domainSettings);
    }
}
