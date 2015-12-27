using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Data;
using System.Windows;

namespace LangTools
{
    static class IOTools
    {
        internal static bool ListDirectories(string dir, out IEnumerable<string> foldersInDir)
        {
            // Get every directory from directory
            Logger.Write(string.Format("Going to check {0} for directories.", dir), Severity.DEBUG);
            try
            {
                foldersInDir = Directory.GetDirectories(dir).Select(Path.GetFileName);
            }
            catch (Exception err)
            {
                // Do nothing but log and return
                Logger.Write(string.Format("Something is wrong during directory access: {0}", err.Message));
                foldersInDir = new List<string>();
                return false;
            }
            return true;
        }

        internal static bool ListFiles(string dir, out IEnumerable<string> filesInDir, string filter="*.txt")
        {
            // Get every file name from directory
            Logger.Write(string.Format("Going to check {0} for files.", dir), Severity.DEBUG);
            try
            {
                filesInDir = Directory.GetFiles(dir, filter).Select(Path.GetFileName);
            }
            catch (Exception err)
            {
                // Do nothing but log and return
                Logger.Write(string.Format("Something is wrong during directory access: {0}", err.Message));
                filesInDir = new List<string>();
                return false;
            }
            return true;
        }

        internal static bool ReadAllText(string filePath, out string content)
        {
            try
            {
                content = File.ReadAllText(filePath, Encoding.UTF8);
                return true;
            }
            catch (Exception e)
            {
                Logger.Write(string.Format("Can't read file: {0}", e.Message));
                content = null;
                return false;
            }
        }

        internal static bool SaveFile(string filePath, string contents)
        {
            try
            {
                File.WriteAllText(filePath, contents);
                return true;
            }
            catch (Exception e)
            {
                Logger.Write(string.Format("Can't write file: {0}", e.Message));
                return false;
            }
        }

        internal static void OpenWithDefault(string fileName)
        {
            try
            {
                System.Diagnostics.Process.Start(fileName);
            }
            catch (Exception)
            {
                MessageBox.Show(string.Format("Can't open {0}.", fileName));
            }
        }

        internal static void DeleteFile(string fileName)
        {
            MessageBoxResult result = MessageBox.Show(
                string.Format("Do you want to delete\n {0} ?", fileName),
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(fileName);
                }
                catch (Exception)
                {
                    MessageBox.Show(string.Format("Can't delete {0}.", fileName));
                }
            }
        }
    }

    enum Severity
    {
        DEBUG,
        RELEASE
    }

    static class Logger
    {
        internal static string ConfigFile { get; set; }

        private static void Write(string text)
        {
            using(StreamWriter sw = File.AppendText(ConfigFile))
            {
                DateTime dt = DateTime.Now;
                sw.WriteLine(string.Format("{0}: {1}", dt, text));
            }
        }

        internal static void Write(string text, Severity severity = Severity.RELEASE)
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

    /// <summary>
    /// A sample assembly reference class that would exist in the `Core` project. 
    /// </summary>
    public static class CoreAssembly
    {
        public static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
        public static readonly Version Version = Reference.GetName().Version;
    }
}
