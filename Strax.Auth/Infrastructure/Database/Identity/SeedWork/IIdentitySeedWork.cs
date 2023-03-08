using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Databases.Identity.SeedWork
{
    public interface IIdentitySeedWork
    {
        Task SeedIdentityData(string environment);
        Task SeedTestingData();
        Task SeedAdminTestingData();
    }
}
