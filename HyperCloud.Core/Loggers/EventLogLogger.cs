using System;
using System.Collections;
using System.Text;
using System.Diagnostics;

namespace HyperCloud.Loggers
{
    public class EventLogLogger : ILogger
    {
        private EventLog log = null;
        private bool noDebug = true;

        public EventLogLogger(EventLog log, bool noDebug = true)
        {
            this.log = log;
            this.noDebug = noDebug;
        }

        public void Debug(string format, params object[] args)
        {
            if (!noDebug)
            {
                SafeWriteEntry(EventLogEntryType.Information, format, args);
            }
        }

        public void Info(string format, params object[] args)
        {
            SafeWriteEntry(EventLogEntryType.Information, format, args);
        }

        public void Error(string format, params object[] args)
        {
            SafeWriteEntry(EventLogEntryType.Error, format, args);
        }

        public void Error(Exception exception)
        {
            SafeWriteEntry(EventLogEntryType.Error, exception.Message);
        }

        private void SafeWriteEntry(EventLogEntryType type, string format, params object[] args)
        {
            if (args.Length == 0)
            {
                log.WriteEntry(format, type);
            }
            else
            {
                log.WriteEntry(String.Format(format, args), type);
            }
        }
    }
}
