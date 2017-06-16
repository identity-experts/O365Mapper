using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _365Drive.Office365.UI.MFA
{
    /// <summary>
    /// when user decides to have MFA later, below could be possible options
    /// </summary>
    public enum RemindLater
    {

        oneHour,
        twoHour,
        fiveHour,
        twentyFourHour
    }
    public static class ReminderStates
    {
        /// <summary>
        /// the last saved state
        /// </summary>
        public static RemindLater? lastRemiderState = null;

        /// <summary>
        /// Last time the user has given input
        /// </summary>
        public static DateTime lastAsked;



        /// <summary>
        /// will compare time difference 
        /// </summary>
        public static bool mfaConfirmationTimeNow
        {
            get
            {
                bool needtoAskforMFA = true;
                if (lastRemiderState != null)
                {
                    //calcuate the hour difference 
                    TimeSpan diff = DateTime.Now - Convert.ToDateTime(lastAsked);
                    double hours = diff.TotalHours;

                    switch (lastRemiderState)
                    {
                        case RemindLater.oneHour:
                            if (hours < 1)
                            {
                                needtoAskforMFA = false;
                            }
                            break;
                        case RemindLater.twoHour:
                            if (hours < 2)
                            {
                                needtoAskforMFA = false;
                            }
                            break;
                        case RemindLater.fiveHour:
                            if (hours < 5)
                            {
                                needtoAskforMFA = false;
                            }
                            break;
                        case RemindLater.twentyFourHour:
                            if (hours < 24)
                            {
                                needtoAskforMFA = false;
                            }
                            break;
                    }
                }
                return needtoAskforMFA;
            }


        }


        
    }
}
