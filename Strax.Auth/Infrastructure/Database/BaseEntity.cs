using System;
using System.Collections.Generic;
using System.Text;

namespace Databases
{
    public abstract class BaseEntity
    {
        protected BaseEntity()
        {
        }
        public string Id { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedById { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedById { get; set; }
    }
}
