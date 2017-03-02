using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using System.Text;

namespace _365Drive.Office365
{
    public static class LogManager
    {

        #region unmanaged logging declarations
        [DllImport("advapi32.dll")]
        private static extern IntPtr RegisterEventSource(string lpUNCServerName, string lpSourceName);

        [DllImport("advapi32.dll")]
        private static extern bool DeregisterEventSource(IntPtr hEventLog);

        [DllImport("advapi32.dll", EntryPoint = "ReportEventW", CharSet = CharSet.Unicode)]

        private static extern bool ReportEvent(

                    IntPtr hEventLog,

                    ushort wType,

                    ushort wCategory,

                    int dwEventID,

                    IntPtr lpUserSid,

                    ushort wNumStrings,

                    uint dwDataSize,

                    string[] lpStrings,

                    byte[] lpRawData

                    );

        public const ushort EVENTLOG_INFORMATION_TYPE = 0x0004;

        public const ushort EVENTLOG_WARNING_TYPE = 0x0002;

        public const ushort EVENTLOG_ERROR_TYPE = 0x0001;

        public static void WriteEventLogTextEntryApi(string text, ushort logType, int logEventId, byte[] rawData)

        {

            //Temporary registry of eventsource
            IntPtr hEventLog = RegisterEventSource(null, Constants.lServiceName);
            uint dataSize = (uint)(rawData != null ? rawData.Length : 0);

            //Write event to eventlog
            ReportEvent(hEventLog, logType, 0, logEventId, IntPtr.Zero, 1, dataSize, new string[] { text }, rawData);

            //Remove temporary registration
            DeregisterEventSource(hEventLog);
        }

        #endregion



        static EventLog eventLog;

        /// <summary>
        /// Handle global level errors
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="unhandledExceptionEventArgs"></param>
        public static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            var exceptionObject = unhandledExceptionEventArgs.ExceptionObject as Exception;
            if (exceptionObject == null) return;
            var assembly = exceptionObject.TargetSite.DeclaringType.Assembly;
            Error(assembly.FullName + " " + (exceptionObject as Exception).Message);
            Error(assembly.FullName + " " + (exceptionObject as Exception).StackTrace);
        }
        /// <summary>
        /// Initialize Logging Event Source. Also if source is NOT created, create them
        /// </summary>
        public static void Init()
        {
            //Below code requires ADMIN rights, which cant be given to CDM user so need to write to Application logs only
            eventLog = new System.Diagnostics.EventLog();
            eventLog.Source = Constants.lServiceName;
            eventLog.Log = Constants.lLogName;

            //((ISupportInitialize)(eventLog)).BeginInit();
            //if (!EventLog.SourceExists(eventLog.Source))
            //{
            //    EventLog.CreateEventSource(eventLog.Source, eventLog.Log);
            //}
            //((ISupportInitialize)(eventLog)).EndInit();


        }


        /// <summary>
        /// Log information
        /// </summary>
        /// <param name="strLogMessage"></param>
        public static void Verbose(string strLogMessage)
        {
            ///Make sure event log is initialized
            if (eventLog == null)
                Init();
            if (RegistryManager.Get(RegistryKeys.Verbose) == "1")
            {
                //eventLog.WriteEntry("[" + System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EventLogEntryType.Information, Constants.lLogeventId);
                //EventLog.WriteEntry(Constants.lServiceName, System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EventLogEntryType.Information, Constants.lLogeventId);
                //EventLog.WriteEntry(Constants.lServiceName, System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EventLogEntryType.Information, Constants.lLogeventId);
                WriteEventLogTextEntryApi(System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EVENTLOG_INFORMATION_TYPE, Constants.lLogeventId, null);
            }
        }


        /// <summary>
        /// Log information
        /// </summary>
        /// <param name="strLogMessage"></param>
        public static void Info(string strLogMessage)
        {
            ///Make sure event log is initialized
            if (eventLog == null)
                Init();

            //EventLog.WriteEntry(Constants.lServiceName, "[" + System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EventLogEntryType.Information, Constants.lLogeventId);
            WriteEventLogTextEntryApi(System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EVENTLOG_INFORMATION_TYPE, Constants.lLogeventId, null);
        }


        /// <summary>
        /// Globally from wherever we get exception, we will handle here
        /// </summary>
        /// <param name="MethodName"></param>
        /// <param name="ex"></param>
        public static void Exception(string MethodName, Exception ex)
        {
            Error("Method: " + MethodName + Environment.NewLine + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace);
        }

        /// <summary>
        /// Log Error
        /// </summary>
        /// <param name="strLogMessage"></param>
        public static void Error(string strLogMessage)
        {
            ///Make sure event log is initialized
            if (eventLog == null)
                Init();

            //EventLog.WriteEntry(Constants.lServiceName, "[" + System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EventLogEntryType.Error, Constants.lLogeventId);
            WriteEventLogTextEntryApi(System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EVENTLOG_ERROR_TYPE, Constants.lLogeventId, null);
        }


        /// <summary>
        /// Log Warning
        /// </summary>
        /// <param name="strLogMessage"></param>
        public static void Warning(string strLogMessage)
        {
            ///Make sure event log is initialized
            if (eventLog == null)
                Init();

            //EventLog.WriteEntry(Constants.lServiceName, "[" + System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EventLogEntryType.Warning, Constants.lLogeventId);
            WriteEventLogTextEntryApi(System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EVENTLOG_WARNING_TYPE, Constants.lLogeventId, null);
        }

        public static void ReadLogs()
        {
            ///Make sure event log is initialized
            if (eventLog == null)
                Init();
        }


    }
}
