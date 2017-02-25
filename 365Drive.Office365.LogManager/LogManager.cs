using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Text;

namespace _365Drive.Office365
{
    public static class LogManager
    {


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
            eventLog = new System.Diagnostics.EventLog();
            eventLog.Source = Constants.lServiceName;
            eventLog.Log = Constants.lLogName;

            ((ISupportInitialize)(eventLog)).BeginInit();
            if (!EventLog.SourceExists(eventLog.Source))
            {
                EventLog.CreateEventSource(eventLog.Source, eventLog.Log);
            }
            ((ISupportInitialize)(eventLog)).EndInit();
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
                eventLog.WriteEntry("[" + System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EventLogEntryType.Information, Constants.lLogeventId);
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

            eventLog.WriteEntry("[" + System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EventLogEntryType.Information, Constants.lLogeventId);
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

            eventLog.WriteEntry("[" + System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EventLogEntryType.Error, Constants.lLogeventId);
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

            eventLog.WriteEntry("[" + System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK") + "] - " + strLogMessage, EventLogEntryType.Warning, Constants.lLogeventId);
        }

        public static void ReadLogs()
        {
            ///Make sure event log is initialized
            if (eventLog == null)
                Init();
        }


    }
}
