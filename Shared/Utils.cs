using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using NLog;

namespace LangTools.Shared
{
    public static class Log
    {
        public static Logger Logger = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Simple class to handle common IO operations.
    /// </summary>
    public static class IOTools
    {
        /// <summary>
        /// Provides IEnumarable of directory names in the given directory.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="foldersInDir"></param>
        /// <returns></returns>
        public static bool ListDirectories(string dir, out IEnumerable<string> foldersInDir)
        {
            Log.Logger.Debug(string.Format("Going to check {0} for directories.", dir));
            try
            {
                foldersInDir = Directory.GetDirectories(dir).Select(Path.GetFileName);
            }
            catch (Exception err)
            {
                // Do nothing but log and return
                Log.Logger.Error(string.Format("Something is wrong during directory access: {0}", err.Message));
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
            Log.Logger.Debug(string.Format("Going to check {0} for files.", dir));
            try
            {
                filesInDir = Directory.GetFiles(dir, filter).Select(Path.GetFileName);
            }
            catch (Exception err)
            {
                // Do nothing but log and return
                Log.Logger.Error(string.Format("Something is wrong during directory access: {0}", err.Message));
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
            Log.Logger.Debug(string.Format("Reading from {0}", filePath));
            try
            {
                content = File.ReadAllText(filePath, Encoding.UTF8);
                return true;
            }
            catch (Exception e)
            {
                Log.Logger.Error(string.Format("Can't read file: {0}", e.Message));
                content = null;
                return false;
            }
        }

        /// <summary>
        /// Provides the text contents of the file by line.
        /// </summary>
        public static bool ReadAllLines(string filePath, out string[] content)
        {
            Log.Logger.Debug(string.Format("Reading from {0}", filePath));
            try
            {
                content = File.ReadAllLines(filePath, Encoding.UTF8);
                return true;
            }
            catch (Exception e)
            {
                Log.Logger.Error(string.Format("Can't read file: {0}", e.Message));
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
            Log.Logger.Debug(string.Format("Saving to {0}", filePath));
            try
            {
                File.WriteAllText(filePath, content);
                return true;
            }
            catch (Exception e)
            {
                Log.Logger.Error(string.Format("Can't write file: {0}", e.Message));
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
            Log.Logger.Debug(string.Format("Appending {0} to {1}", content, filePath));
            try
            {
                File.AppendAllText(filePath, content);
                return true;
            }
            catch (Exception e)
            {
                Log.Logger.Error(string.Format("Can't append to file: {0}", e.Message));
                return false;
            }
        }

        /// <summary>
        /// Opens the file in the associated application.
        /// </summary>
        /// <param name="fileName"></param>
        public static bool OpenWithDefault(string fileName)
        {
            try
            {
                System.Diagnostics.Process.Start(fileName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="fileName"></param>
        public static bool DeleteFile(string fileName)
        {
            try
            {
                File.Delete(fileName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Combines an array of string into a path or null;
        /// </summary>
        /// <param name="pathes"></param>
        /// <returns></returns>
        public static string CombinePath(params string[] pathes)
        {
            string path = null;
            try
            {
                path = Path.Combine(pathes);
            }
            catch (Exception e)
            {
                Log.Logger.Error(string.Format("Trying to create path of non existent parts?: {0}", e.Message));
            }
            return path;
        }

        /// <summary>
        /// Creates directory unless it already exists.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool CreateDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception e)
            {
                string msg = string.Format("Something is wrong during directory creation: {0}", e.ToString());
                Log.Logger.Error(msg);
                return false;
            }
        }
    }
}
