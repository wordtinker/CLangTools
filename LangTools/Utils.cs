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
    /// <summary>
    /// Simple class to handle common IO operations.
    /// </summary>
    static class IOTools
    {
        /// <summary>
        /// Provides IEnumarable of directory names in the given directory.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="foldersInDir"></param>
        /// <returns></returns>
        public static bool ListDirectories(string dir, out IEnumerable<string> foldersInDir)
        {
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

        /// <summary>
        /// Provides IEnumerable of file names in the given directory.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="filesInDir"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static bool ListFiles(string dir, out IEnumerable<string> filesInDir, string filter="*.txt")
        {
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

        /// <summary>
        /// Provides the text contents of the file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static bool ReadAllText(string filePath, out string content)
        {
            Logger.Write(string.Format("Reading from {0}", filePath), Severity.DEBUG);
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

        /// <summary>
        /// Saves the content to the file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static bool SaveFile(string filePath, string content)
        {
            Logger.Write(string.Format("Saving to {0}", filePath), Severity.DEBUG);
            try
            {
                File.WriteAllText(filePath, content);
                return true;
            }
            catch (Exception e)
            {
                Logger.Write(string.Format("Can't write file: {0}", e.Message));
                return false;
            }
        }

        /// <summary>
        /// Appends the string to the file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static bool AppendToFile(string filePath, string content)
        {
            Logger.Write(string.Format("Appending {0} to {1}", content, filePath), Severity.DEBUG);
            try
            {
                File.AppendAllText(filePath, content);
                return true;
            }
            catch (Exception e)
            {
                Logger.Write(string.Format("Can't append to file: {0}", e.Message));
                return false;
            }
        }

        /// <summary>
        /// Opens the file in the associated application.
        /// </summary>
        /// <param name="fileName"></param>
        public static void OpenWithDefault(string fileName)
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

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="fileName"></param>
        public static void DeleteFile(string fileName)
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

    /// <summary>
    /// Level of logging severity.
    /// </summary>
    enum Severity
    {
        DEBUG,
        RELEASE
    }

    /// <summary>
    /// Simple Logger class.
    /// </summary>
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

    /// <summary>
    /// A sample assembly reference class that would exist in the `Core` project. 
    /// </summary>
    public static class CoreAssembly
    {
        public static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
        public static readonly Version Version = Reference.GetName().Version;
    }
}
