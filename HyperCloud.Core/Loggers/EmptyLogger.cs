using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HyperCloud.Loggers
{
    public class EmptyLogger : ILogger
    {
        public void Debug(string format, params object[] args)
        {
        }

        public void Info(string format, params object[] args)
        {
        }

        public void Error(string format, params object[] args)
        {
        }

        public void Error(Exception exception)
        {
        }
    }
}
