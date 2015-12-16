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
using System.Collections;

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

    class Lingva
    {
        public string Language { get; set; }
        public string Folder { get; set; }
    }

    class FileStats
    {
        private string fileName;
        private string filePath;
        private Lingva lingva;
        private string project;

        // Properties
        public string FileName{ get { return fileName; } }
        public string FilePath { get { return filePath; } }
        public Lingva Lingva { get { return lingva; } }
        public string Project { get { return project; } }

        public int? Size { get; set; }
        public int? Known { get; set; }
        public int? Maybe { get; set; }
        public int? Unknown { get; set; }
        public string OutPath
        {
            get
            {
                string outName = Path.ChangeExtension(FileName, ".html");
                string outPath = Path.Combine(
                    Lingva.Folder,
                    (string)App.Current.Properties["outputDir"],
                    Project,
                    outName);
                return outPath;
            }
        }

        public FileStats(string fileName, string filePath, Lingva language, string project)
        {
            this.fileName = fileName;
            this.filePath = filePath;
            this.lingva = language;
            this.project = project;
        }

        public override bool Equals(object obj)
        {
            FileStats item = obj as FileStats;

            if (item == null)
            {
                return false;
            }

            return this.FilePath == item.FilePath;
        }

        public override int GetHashCode()
        {
            return FilePath.GetHashCode();
        }

        public bool Update(FileStats other)
        {
            if (this.Size != other.Size || this.Known != other.Known ||
                this.Maybe != other.Maybe || this.Unknown != other.Unknown)
            {
                this.Size = other.Size;
                this.Known = other.Known;
                this.Maybe = other.Maybe;
                this.Unknown = other.Unknown;
                return true;
            }

            return false;
        }
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
            Dict item = obj as Dict;

            if (item == null)
            {
                return false;
            }

            return this.FilePath == item.FilePath;
        }

        public override int GetHashCode()
        {
            return FilePath.GetHashCode();
        }
    }

    class Storage
    {
        // DB connection
        private SQLiteConnection dbConn;
        // temp variables to store data for transactions
        private Dictionary<string, Dictionary<string, int>> wordList;
        private List<FileStats> statList;

        internal Storage(string dbFile)
        {
            wordList = new Dictionary<string, Dictionary<string, int>>();
            statList = new List<FileStats>();

            string connString = string.Format("Data Source={0};Version=3;foreign keys=True;", dbFile);
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
            string sql = "CREATE TABLE IF NOT EXISTS Languages(lang TEXT PRIMARY KEY, directory TEXT)";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.ExecuteNonQuery();
            }

            sql = @"CREATE TABLE IF NOT EXISTS Files(" +
                "name TEXT, path TEXT PRIMARY KEY, " +
                "lang TEXT REFERENCES Languages(lang) ON DELETE CASCADE, project TEXT," +
                " size INTEGER, known INTEGER," +
                "maybe INTEGER, unknown INTEGER)";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.ExecuteNonQuery();
            }

            sql = "CREATE TABLE IF NOT EXISTS Words(" +
                "word TEXT, file TEXT REFERENCES Files(path) ON DELETE CASCADE, quantity INTEGER)";
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
                    langs.Add(new Lingva { Language = dr.GetString(0), Folder = dr.GetString(1) });
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
                    projects.Add(dr.GetString(0));
                }
                dr.Close();
            }
            return projects;
        }

        internal void RemoveProject(Lingva language, string project)
        {
            SQLiteParameter langParam = new SQLiteParameter("@lang");
            langParam.Value = language.Language;
            langParam.DbType = System.Data.DbType.String;

            SQLiteParameter projectParam = new SQLiteParameter("@project");
            projectParam.Value = project;
            projectParam.DbType = System.Data.DbType.String;

            string sql = "DELETE FROM Files WHERE lang=@lang AND project=@project";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.Parameters.Add(langParam);
                cmd.Parameters.Add(projectParam);
                cmd.ExecuteNonQuery();
            }
        }

        internal List<FileStats> GetFilesStats(Lingva language, string project)
        {
            SQLiteParameter langParam = new SQLiteParameter("@lang");
            langParam.Value = language.Language;
            langParam.DbType = System.Data.DbType.String;

            SQLiteParameter projectParam = new SQLiteParameter("@project");
            projectParam.Value = project;
            projectParam.DbType = System.Data.DbType.String;

            string sql = "SELECT name, path, size, known, maybe, unknown FROM Files " +
                "WHERE lang=@lang AND project=@project";
            List<FileStats> stats = new List<FileStats>();
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.Parameters.Add(langParam);
                cmd.Parameters.Add(projectParam);
                SQLiteDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    stats.Add(new FileStats(
                        dr.GetString(0),
                        dr.GetString(1),
                        language, project)
                    {
                        Size = dr.GetInt32(2),
                        Known = dr.GetInt32(3),
                        Maybe = dr.GetInt32(4),
                        Unknown = dr.GetInt32(5)
                    });
                }
                dr.Close();
            }
            return stats;
        }

        /// <summary>
        /// Removes the stats of the given file from the DB.
        /// </summary>
        /// <param name="file"></param>
        internal void RemoveFileStats(FileStats file)
        {
            SQLiteParameter path = new SQLiteParameter("@path");
            path.Value = file.FilePath;
            path.DbType = System.Data.DbType.String;
            string sql = "DELETE FROM Files WHERE path=@path";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.Parameters.Add(path);
                cmd.ExecuteNonQuery();
            }
        }

        internal void UpdateStats(FileStats stats)
        {
            statList.Add(stats);
        }

        internal void CommitStats()
        {
            string sql = "INSERT OR REPLACE INTO Files " +
            //    //"(name, path, lang, project, size, known, maybe, unknown) " +
                "VALUES(@name, @path, @lang, @project, @size, @known, @maybe, @unknown)";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                using (SQLiteTransaction transaction = dbConn.BeginTransaction())
                {
                    foreach (FileStats stats in statList)
                    {
                        //        cmd.CommandText = "INSERT OR REPLACE INTO Files " +
                        ////"(name, path, lang, project, size, known, maybe, unknown) " +
                        //"VALUES(@name, @path, @lang, @project, @size, @known, @maybe, @unknown)";

                        SQLiteParameter param = new SQLiteParameter("@name");
                        param.Value = stats.FileName;
                        param.DbType = System.Data.DbType.String;
                        cmd.Parameters.Add(param);

                        param = new SQLiteParameter("@path");
                        param.Value = stats.FilePath;
                        param.DbType = System.Data.DbType.String;
                        cmd.Parameters.Add(param);

                        param = new SQLiteParameter("@lang");
                        param.Value = stats.Lingva.Language;
                        param.DbType = System.Data.DbType.String;
                        cmd.Parameters.Add(param);

                        param = new SQLiteParameter("@project");
                        param.Value = stats.Project;
                        param.DbType = System.Data.DbType.String;
                        cmd.Parameters.Add(param);

                        param = new SQLiteParameter("@size");
                        param.Value = stats.Size.GetValueOrDefault();
                        cmd.Parameters.Add(param);

                        param = new SQLiteParameter("@known");
                        param.Value = stats.Known.GetValueOrDefault();
                        cmd.Parameters.Add(param);

                        param = new SQLiteParameter("@maybe");
                        param.Value = stats.Maybe.GetValueOrDefault();
                        cmd.Parameters.Add(param);

                        param = new SQLiteParameter("@unknown");
                        param.Value = stats.Unknown.GetValueOrDefault();
                        cmd.Parameters.Add(param);

                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
            statList.Clear();
            GC.Collect();
        }

        /// <summary>
        /// Updates the word list for the given project. Delayed insert is used.
        /// CommitWords must be called afterward to apply changes.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="unknownWords"></param>
        internal void UpdateWords(string filePath, Dictionary<string, int> unknownWords)
        {
            wordList.Add(filePath, unknownWords);
        }

        internal void CommitWords()
        {
            using (var cmd = new SQLiteCommand(dbConn))
            {
                using (SQLiteTransaction transaction = dbConn.BeginTransaction())
                {
                    foreach (string filePath in wordList.Keys)
                    {
                        SQLiteParameter pathParam = new SQLiteParameter("@file");
                        pathParam.Value = filePath;
                        pathParam.DbType = System.Data.DbType.String;
                        // Delete all words from previous analysis
                        cmd.CommandText = "DELETE FROM Words WHERE file=@file";
                        cmd.Parameters.Add(pathParam);
                        cmd.ExecuteNonQuery();

                        string command = "INSERT INTO Words VALUES(@word, @file, @quantity)";
                        foreach (var item in wordList[filePath])
                        {
                            cmd.CommandText = command;
                            cmd.Parameters.Add(pathParam);

                            SQLiteParameter param = new SQLiteParameter("@word");
                            param.Value = item.Key;
                            pathParam.DbType = System.Data.DbType.String;
                            cmd.Parameters.Add(param);

                            param = new SQLiteParameter("@quantity");
                            param.Value = item.Value;
                            cmd.Parameters.Add(param);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
            wordList.Clear();
            GC.Collect();
        }
    }

    class Printer
    {
        HTMLPrinter printer;

        internal Printer(string language)
        {

            printer = new HTMLPrinter();
            // Load CSS file
            string cssContent;
            string cssPath = Path.Combine(Directory.GetCurrentDirectory(), "plugins", language);
            cssPath = Path.ChangeExtension(cssPath, ".css");
            if (IOTools.ReadAllText(cssPath, out cssContent))
            {
                printer.LoadCSS(cssContent);
            }
        }

        internal void Print(string fileName, string project, string language, List<Token> tokens)
        {
            // Create proper name for output file;
            string outName = Path.ChangeExtension(fileName, ".html");
            string outPath = Path.Combine(language, (string)App.Current.Properties["outputDir"],
                                          project, outName);
            // Get the HTML and save
            string HTML = printer.toHTML(fileName, tokens);
            IOTools.SaveFile(outPath, HTML);
        }
    }

    class HTMLPrinter
    {
        private class Wrapper : IEnumerable<string>
        {
            private List<Token> tokens;
            private List<string> tags;

            public Wrapper(List<Token> tokens)
            {
                this.tokens = tokens;
                this.tags = new List<string>();
            }

            private string getParagraph(string endOfParagraph)
            {
                tags.Add(endOfParagraph);
                tags.Insert(0, "<p>");
                tags.Add("</p>");
                string result = string.Join("", tags);
                tags.Clear();
                return result;
            }

            public IEnumerator<string> GetEnumerator()
            {
                foreach (Token tkn in tokens)
                {
                    if (tkn.Type == TokenType.WORD)
                    {
                        // Put into list of words
                        string tag = string.Format(
                            "<span class={0}>{1}</span>",
                            tkn.Know,
                            tkn.Word);
                        tags.Add(tag);
                    }
                    else if (tkn.Word.Contains('\n'))
                    {
                        // yield the paragraphs
                        foreach (string p in tkn.Word.Split('\n'))
                        {
                            yield return getParagraph(p);
                        }
                    }
                    else
                    {
                        // Put !?,. etc into the list
                        tags.Add(tkn.Word);
                    }
                }
                // if list is not empty yield the last paragraph
                yield return getParagraph("");
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private string css;

        internal void LoadCSS(string cssContent)
        {
            css = cssContent;
        }

        internal string toHTML(string fileName, List<Token> tokens)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<html><head><meta charset='utf-8'>");
            sb.Append(string.Format("<title>{0}</title></head>", fileName));
            sb.Append("<body>");
            sb.Append("<article>");
            foreach (string paragraph in new Wrapper(tokens))
            {
                sb.Append(paragraph);
            }
            sb.Append("</article>");
            sb.Append("<style>");
            if (css != null)
            {
                sb.Append(css);
            }
            else
            {
                // Use default style
                sb.Append(@"
    body {font-family:sans-serif;
            line-height: 1.5;}
    span.KNOWN
            {background-color: white;
            font-weight: normal;font-style: normal;
            border-bottom: 3px solid green;}
    span.MAYBE
            {background-color: white;
            font-weight: normal;font-style: normal;
            border-bottom: 3px solid yellowgreen;}
                ");
            }
            sb.Append("</style></body></html>");
            return sb.ToString();
        }
    }
}
