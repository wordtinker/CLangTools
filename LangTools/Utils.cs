using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Data.SQLite;
using System.Data.Common;
using System.Data;
using System.Windows.Threading;
using System.Windows;

namespace LangTools
{
    static class IOTools
    {
        internal static bool ListDirectories(string dir, out List<string> foldersInDir)
        {
            // Get every directory from directory
            Logger.Write(string.Format("Going to check {0} for directories.", dir), Severity.DEBUG);
            try
            {
                DirectoryInfo di = new DirectoryInfo(dir);
                foldersInDir = new List<string>(di.GetDirectories().Select(d => d.Name));
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

        internal static bool ListFiles(string dir, out List<string> filesInDir)
        {
            // Get every project from corpus directory
            Logger.Write(string.Format("Going to check {0} for files.", dir), Severity.DEBUG);
            try
            {
                DirectoryInfo di = new DirectoryInfo(dir);
                filesInDir = new List<string>(di.GetFiles("*.txt").Select(d => d.Name));
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

        internal static void OpenWithDefaul(string fileName)
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
                "Do you want to delete this file?", "Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
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

    class Lingva
    {
        public string Language { get; set; }
        public string Folder { get; set; }
    }

    enum DictType
    {
        Project,
        General
    }

    class Dict
    {
        public string FileName { get; set; }
        public string DictType { get; set; }
        public string FilePath { get;  set;}

        public override bool Equals(object obj)
        {
            var item = obj as Dict;

            if (item == null)
            {
                return false;
            }

            return this.FilePath == item.FilePath;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    class Storage
    {
        private SQLiteConnection dbConn;

        internal Storage(string dbFile)
        {
            string connString = string.Format("Data Source={0};Version=3;", dbFile);
            dbConn = new SQLiteConnection(connString);
            dbConn.Open();
            InitializeTables();
            Logger.Write("DB conn is open.", Severity.DEBUG);
        }

        internal void Close()
        {
            dbConn.Close();
            Logger.Write("DB conn is closed.", Severity.DEBUG);
        }

        private void InitializeTables()
        {
            string sql = "CREATE TABLE IF NOT EXISTS Languages(lang TEXT, directory TEXT)";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.ExecuteNonQuery();
            }

            sql = "CREATE TABLE IF NOT EXISTS Files(" +
                "name TEXT, lang TEXT, project TEXT, size INTEGER, known INTEGER," +
                "pknown REAL, maybe INTEGER, pmaybe REAL, unknown INTEGER," +
                "punknown REAL)";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.ExecuteNonQuery();
            }

            sql = "CREATE TABLE IF NOT EXISTS Words(" +
                "word TEXT, lang TEXT, project TEXT, file TEXT, quantity INTEGER)";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Checks if the language is used in Table Languages.
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        internal bool LanguageExists(string language)
        {
            string sql = "SELECT lang FROM Languages WHERE lang=@lang";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                SQLiteParameter param = new SQLiteParameter("@lang");
                param.Value = language;
                param.DbType = System.Data.DbType.String;
                cmd.Parameters.Add(param);

                if (cmd.ExecuteScalar() == null) // nothing is found
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if the folder is used in Table Languages.
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        internal bool FolderExists(string folder)
        {
            string sql = "SELECT lang FROM Languages WHERE directory=@dir";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                SQLiteParameter param = new SQLiteParameter("@dir");
                param.Value = folder;
                param.DbType = System.Data.DbType.String;
                cmd.Parameters.Add(param);

                if (cmd.ExecuteScalar() == null) // nothing is found
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Adds new language and it's folder to DB.
        /// </summary>
        /// <param name="language"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        internal Lingva AddLanguage(string language, string directory)
        {
            string sql = "INSERT INTO Languages VALUES(@lang, @directory)";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                SQLiteParameter param = new SQLiteParameter("@lang");
                param.Value = language;
                param.DbType = System.Data.DbType.String;
                cmd.Parameters.Add(param);

                param = new SQLiteParameter("@directory");
                param.Value = directory;
                param.DbType = System.Data.DbType.String;
                cmd.Parameters.Add(param);

                cmd.ExecuteNonQuery(); 
            }
            return new Lingva { Language = language, Folder = directory };
        }

        /// <summary>
        /// Provides list of pairs: language, project folder.
        /// </summary>
        /// <returns>DataTable with language, project folder.</returns>
        internal List<Lingva> GetLanguages()
        {
            List<Lingva> langs = new List<Lingva>();

            string sql = "SELECT lang, directory FROM Languages";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                SQLiteDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    langs.Add(new Lingva { Language = (string)dr[0], Folder = (string)dr[1] });
                }
                dr.Close();
            }
            return langs;
        }

        /// <summary>
        /// Removes the stats of the given language from DB.
        /// </summary>
        /// <param name="language"></param>
        internal void RemoveLanguage(Lingva language)
        {
            SQLiteParameter param = new SQLiteParameter("@lang");
            param.Value = language.Language;
            param.DbType = System.Data.DbType.String;

            string sql = "DELETE FROM Languages WHERE lang=@lang";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.Parameters.Add(param);
                cmd.ExecuteNonQuery();
            }

            sql = "DELETE FROM Files WHERE lang=@lang";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.Parameters.Add(param);
                cmd.ExecuteNonQuery();
            }

            sql = "DELETE FROM Words WHERE lang=@lang";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.Parameters.Add(param);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Return projects for chosen language.
        /// </summary>
        /// <param name="selectedLang"></param>
        /// <returns></returns>
        internal List<string> GetProjects(Lingva selectedLang)
        {
            List<string> projects = new List<string>();
            string sql = "SELECT project FROM Files WHERE lang=@lang GROUP BY project";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                SQLiteParameter param = new SQLiteParameter("@lang");
                param.Value = selectedLang.Language;
                param.DbType = System.Data.DbType.String;
                cmd.Parameters.Add(param);

                SQLiteDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    projects.Add((string)dr[0]);
                }
                dr.Close();
            }
            return projects;
            // TODO: Test
        }

        internal void RemoveProject(string language, string folder)
        {
            SQLiteParameter langParam = new SQLiteParameter("@lang");
            langParam.Value = language;
            langParam.DbType = System.Data.DbType.String;

            SQLiteParameter projectParam = new SQLiteParameter("@project");
            projectParam.Value = folder;
            projectParam.DbType = System.Data.DbType.String;

            string sql = "DELETE FROM Files WHERE lang=@lang AND project=@project";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.Parameters.Add(langParam);
                cmd.Parameters.Add(projectParam);
                cmd.ExecuteNonQuery();
            }

            sql = "DELETE FROM Words WHERE lang=@lang AND project=@project";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.Parameters.Add(langParam);
                cmd.Parameters.Add(projectParam);
                cmd.ExecuteNonQuery();
            }
            // TODO: Test
        }
    }
}
