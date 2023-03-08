using Microsoft.AspNetCore.Identity;
using System;

namespace Databases.Identity.Models
{
    public class ApplicationUser : IdentityUser
    {
        //public string Civility { get; set; }
        //public string FirstName { get; set; }
        //public string LastName { get; set; }

        public Boolean IsActive { get; set; }
        public DateTime LastLoggedOn { get; set; }        
        public DateTime DateAdded { get; set; }
        public string AddedBy { get; set; }
        public DateTime DateModified { get; set; }
        public string ModifiedBy { get; set; }
        public Boolean IsRootUser { get; set; }
        public bool IsPasswordTemporary { get; set; }

        public string OrganisationId { get; set; }

        public virtual Organisation Organisation { get; set; }
    }
}
