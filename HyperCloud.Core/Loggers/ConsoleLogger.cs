using System;

namespace HyperCloud.Loggers
{
    public class ConsoleLogger : ILogger
    {
        private bool noDebug = false;

        public ConsoleLogger(bool noDebug = false)
        {
            this.noDebug = noDebug;
        }

        public void Debug(string format, params object[] args)
        {
            if (!noDebug)
            {
                SafeConsoleWrite("DEBUG: " + format, args);
            }
        }

        public void Info(string format, params object[] args)
        {
            SafeConsoleWrite("INFO: " + format, args);
        }

        public void Error(string format, params object[] args)
        {
            SafeConsoleWrite("ERROR: " + format, args);
        }

        public void Error(Exception exception)
        {
            Console.WriteLine(exception.ToString());
        }

        private void SafeConsoleWrite(string format, params object[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(format);
            }
            else
            {
                Console.WriteLine(format, args);
            }
        }
    }
}