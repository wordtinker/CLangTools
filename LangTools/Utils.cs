using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace LangTools
{
    enum Severity
    {
        DEBUG,
        RELEASE
    }

    static class Logger
    {
        public static string ConfigFile { get; set; }

        private static void Write(string text)
        {
            using(StreamWriter sw = File.AppendText(ConfigFile))
            {
                DateTime dt = DateTime.Now;
                sw.WriteLine(string.Format("{0}: {1}", dt, text));
            }
        }

        public static void Write(string text, Severity severity = Severity.RELEASE)
        {
            if (ConfigFile != null)
            {
                if (severity == Severity.RELEASE)
                {
                    Write(text);
                    return;
                }
#if (DEBUG)
                Write(text);
#endif
            }
        }
    }

    // A sample assembly reference class that would exist in the `Core` project.
    public static class CoreAssembly
    {
        public static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
        public static readonly Version Version = Reference.GetName().Version;
    }

    static class Storage
    {

    }
}
