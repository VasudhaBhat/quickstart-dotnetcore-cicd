using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Configuration
{
    public class AppSettings
    {
        public ConnectionStrings ConnectionStrings { get; set; }
    }

    public class ConnectionStrings
    {
        public string AuthContext { get; set; }
    }
}
