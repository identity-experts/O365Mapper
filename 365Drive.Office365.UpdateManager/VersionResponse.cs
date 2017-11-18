using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _365Drive.Office365.UpdateManager
{
    public class VersionResponse
    {
        public VersionResponseData data { get; set; }
    }
    public class VersionResponseData
    {
        public string version { get; set; }
        public string x86 { get; set; }
        public string x64 { get; set; }
    }
}
