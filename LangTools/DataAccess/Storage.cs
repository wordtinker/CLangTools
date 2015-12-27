﻿using LangTools.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LangTools.DataAccess
{
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
        internal void AddLanguage(Lingva lang)
        {
            string sql = "INSERT INTO Languages VALUES(@lang, @directory)";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                SQLiteParameter param = new SQLiteParameter("@lang");
                param.Value = lang.Language;
                param.DbType = System.Data.DbType.String;
                cmd.Parameters.Add(param);

                param = new SQLiteParameter("@directory");
                param.Value = lang.Folder;
                param.DbType = System.Data.DbType.String;
                cmd.Parameters.Add(param);

                cmd.ExecuteNonQuery();
            }
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
                "VALUES(@name, @path, @lang, @project, @size, @known, @maybe, @unknown)";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                using (SQLiteTransaction transaction = dbConn.BeginTransaction())
                {
                    foreach (FileStats stats in statList)
                    {
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
}