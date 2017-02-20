﻿using LangTools.Shared;
using System;
using System.IO;

namespace LangTools.Models
{
    // TODO comment
    internal class WatchTower
    {
        private FileSystemWatcher corpusWatcher;
        private FileSystemWatcher specDictWatcher;
        private FileSystemWatcher genDictWatcher;
        private FileSystemWatcher filesWatcher;
        private MainModel model;

        public WatchTower(MainModel model)
        {
            this.model = model;
            // Corpus Watcher
            corpusWatcher = new FileSystemWatcher();
            corpusWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            corpusWatcher.Filter = "*.*";
            // Remark : deleting of old projects from storage is postponed
            // until after next time LanguageChanged is called.
            corpusWatcher.Created += (obj, e) => model.Projects.Add(e.Name);
            corpusWatcher.Deleted += (obj, e) => model.Projects.Remove(e.Name);
            corpusWatcher.Renamed += (obj, e) =>
            {
                model.Projects.Remove(e.OldName);
                model.Projects.Add(e.Name);
            };

            // Project specific dictionaries Watcher
            specDictWatcher = new FileSystemWatcher();
            specDictWatcher.NotifyFilter = NotifyFilters.FileName;
            specDictWatcher.Filter = "*.txt";
            specDictWatcher.Created += (obj, e) =>
                model.Dictionaries.Add(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.Project,
                    FilePath = e.FullPath
                });
            specDictWatcher.Deleted += (obj, e) =>
                model.Dictionaries.Remove(new Dict
                {
                    FilePath = e.FullPath
                });
            specDictWatcher.Renamed += (obj, e) =>
            {
                model.Dictionaries.Add(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.Project,
                    FilePath = e.FullPath
                });
                model.Dictionaries.Remove(new Dict
                {
                    FilePath = e.OldFullPath
                });
            };

            // General dictionaries Watcher
            genDictWatcher = new FileSystemWatcher();
            genDictWatcher.NotifyFilter = NotifyFilters.FileName;
            genDictWatcher.Filter = "*.txt";
            genDictWatcher.Created += (obj, e) =>
                model.Dictionaries.Add(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.General,
                    FilePath = e.FullPath
                });
            genDictWatcher.Deleted += (obj, e) =>
                model.Dictionaries.Remove(new Dict
                {
                    FilePath = e.FullPath
                });
            genDictWatcher.Renamed += (obj, e) =>
            {
                model.Dictionaries.Add(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.General,
                    FilePath = e.FullPath
                });
                model.Dictionaries.Remove(new Dict
                {
                    FilePath = e.OldFullPath
                });
            };

            // Files Directory Watcher
            // Remark : deleting of stats for deleted files from storage is postponed
            // until the next time ProjectChanged is called.
            filesWatcher = new FileSystemWatcher();
            filesWatcher.NotifyFilter = NotifyFilters.FileName;
            filesWatcher.Filter = "*.txt";
            filesWatcher.Created += (obj, e) =>
                model.Files.Add(new FileStats(
                    e.Name,
                    e.FullPath,
                    model.currentLanguage,
                    model.currentProject
                    ));
            filesWatcher.Deleted += (obj, e) =>
                model.Files.Remove(new FileStats(
                    e.Name,
                    e.FullPath,
                    model.currentLanguage,
                    model.currentProject
                    ));
            filesWatcher.Renamed += (obj, e) =>
            {
                model.Files.Add(new FileStats(
                    e.Name,
                    e.FullPath,
                    model.currentLanguage,
                    model.currentProject
                    ));
                model.Files.Remove(new FileStats(
                    e.OldName,
                    e.OldFullPath,
                    model.currentLanguage,
                    model.currentProject
                    ));
            };
        }

        public void ToggleOnCorpus(string corpusDir)
        {
            try
            {
                Directory.CreateDirectory(corpusDir);
                corpusWatcher.Path = corpusDir;
                corpusWatcher.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {
                // Not a critical error, could be fixed later.
                string msg = string.Format("Something is wrong during subfolder creation or watching: {0}", e.ToString());
                Log.Logger.Error(msg);
            }
        }

        public void ToggleOffCorpus()
        {
            corpusWatcher.EnableRaisingEvents = false;
        }

        public void ToggleOnProject(string projectDicPath, string genDicPath, string projectFilesPath)
        {
            try
            {
                // folder must exist before we start watching
                Directory.CreateDirectory(projectDicPath);
                specDictWatcher.Path = projectDicPath;
                Directory.CreateDirectory(genDicPath);
                genDictWatcher.Path = genDicPath;
                Directory.CreateDirectory(projectFilesPath);
                filesWatcher.Path = projectFilesPath;

                specDictWatcher.EnableRaisingEvents = true;
                genDictWatcher.EnableRaisingEvents = true;
                filesWatcher.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {
                // Not a critical error, could be fixed later.
                string msg = string.Format("Something is wrong during subfolder creation or watching: {0}", e.ToString());
                Log.Logger.Error(msg);
            }
        }

        public void ToggleOffProject()
        {
            // Stop watching both dict folders
            specDictWatcher.EnableRaisingEvents = false;
            genDictWatcher.EnableRaisingEvents = false;
            //// Stop watching files folder
            filesWatcher.EnableRaisingEvents = false;
        }
    }
}
