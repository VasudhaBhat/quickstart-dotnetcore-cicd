using System;
using System.Collections.Generic;
using System.Text;

namespace Databases.Identity.Models
{
    public class Organisation : BaseEntity
    {
        public string Name { get; set; }
        public string Region { get; set; }
        public string Data { get; set; }

        public Boolean IsDeleted { get; set; }
    }
}
