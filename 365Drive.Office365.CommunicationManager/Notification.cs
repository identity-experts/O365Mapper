using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _365Drive.Office365.CommunicationManager
{

    /// <summary>
    /// Notification class which will be queued 
    /// </summary>
    public class Notification
    {
        public string Heading { get; set; }
        public string Message { get; set; }

        public bool Notified { get; set; }
    }
}
