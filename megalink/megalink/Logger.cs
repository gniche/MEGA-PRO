using System;
using System.Runtime.CompilerServices;

namespace megalink
{
    public enum LogLevel
    {
        ERROR,
        WARN,
        INFO,
        DEBUG
    }

    public class Logger
    {
        private static LogLevel logLevel = LogLevel.DEBUG;

        public static void setLogLevel(string logLvlStr)
        {
           LogLevel.TryParse(logLvlStr, true, out Logger.logLevel);
        }

        public static void err(string msg)
        {
            Console.Out.WriteLine($"{DateTime.Now:H:mm:ss.fff} |{LogLevel.ERROR}> {msg}");
        }

        public static void wrn(string msg)
        {
            if (logLevel > LogLevel.ERROR) Console.Out.WriteLine($"{DateTime.Now:H:mm:ss.fff} |{LogLevel.WARN}> {msg}");
        }

        public static void inf(string msg)
        {
            if (logLevel > LogLevel.WARN) Console.Out.WriteLine($"{DateTime.Now:H:mm:ss.fff} |{LogLevel.INFO}> {msg}");
        }

        public static void dbg(string msg)
        {
            if (logLevel > LogLevel.INFO) Console.Out.WriteLine($"{DateTime.Now:H:mm:ss.fff} |{LogLevel.DEBUG}> {msg}");
        }

        public static void nl()
        {
            Console.WriteLine("");
        }

        
    }
}