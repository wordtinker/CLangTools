using LangTools.Shared;
using System;
using System.IO;

namespace LangTools.Models
{
    /// <summary>
    /// Bunch of FileSystemWatchers to track creation and 
    /// deletion of text files and dictionaries.
    /// </summary>
    internal class WatchTower
    {
        private FileSystemWatcher corpusWatcher;
        private FileSystemWatcher specDictWatcher;
        private FileSystemWatcher genDictWatcher;
        private FileSystemWatcher filesWatcher;

        public WatchTower(MainModel mediator)
        {
            // Corpus Watcher
            corpusWatcher = new FileSystemWatcher();
            corpusWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            corpusWatcher.Filter = "*.*";
            // Remark : deleting of old projects from storage is postponed
            // until after next time LanguageChanged is called.
            corpusWatcher.Created += (obj, e) => mediator.Projects.Add(e.Name);
            corpusWatcher.Deleted += (obj, e) => mediator.Projects.Remove(e.Name);
            corpusWatcher.Renamed += (obj, e) =>
            {
                mediator.Projects.Remove(e.OldName);
                mediator.Projects.Add(e.Name);
            };

            // Project specific dictionaries Watcher
            specDictWatcher = new FileSystemWatcher();
            specDictWatcher.NotifyFilter = NotifyFilters.FileName;
            specDictWatcher.Filter = "*.txt";
            specDictWatcher.Created += (obj, e) =>
                mediator.Dictionaries.Add(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.Project,
                    FilePath = e.FullPath
                });
            specDictWatcher.Deleted += (obj, e) =>
                mediator.Dictionaries.Remove(new Dict
                {
                    FilePath = e.FullPath
                });
            specDictWatcher.Renamed += (obj, e) =>
            {
                mediator.Dictionaries.Add(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.Project,
                    FilePath = e.FullPath
                });
                mediator.Dictionaries.Remove(new Dict
                {
                    FilePath = e.OldFullPath
                });
            };

            // General dictionaries Watcher
            genDictWatcher = new FileSystemWatcher();
            genDictWatcher.NotifyFilter = NotifyFilters.FileName;
            genDictWatcher.Filter = "*.txt";
            genDictWatcher.Created += (obj, e) =>
                mediator.Dictionaries.Add(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.General,
                    FilePath = e.FullPath
                });
            genDictWatcher.Deleted += (obj, e) =>
                mediator.Dictionaries.Remove(new Dict
                {
                    FilePath = e.FullPath
                });
            genDictWatcher.Renamed += (obj, e) =>
            {
                mediator.Dictionaries.Add(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.General,
                    FilePath = e.FullPath
                });
                mediator.Dictionaries.Remove(new Dict
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
                mediator.Files.Add(new FileStats(
                    e.Name,
                    e.FullPath,
                    mediator.currentLanguage,
                    mediator.currentProject
                    ));
            filesWatcher.Deleted += (obj, e) =>
                mediator.Files.Remove(new FileStats(
                    e.Name,
                    e.FullPath,
                    mediator.currentLanguage,
                    mediator.currentProject
                    ));
            filesWatcher.Renamed += (obj, e) =>
            {
                mediator.Files.Add(new FileStats(
                    e.Name,
                    e.FullPath,
                    mediator.currentLanguage,
                    mediator.currentProject
                    ));
                mediator.Files.Remove(new FileStats(
                    e.OldName,
                    e.OldFullPath,
                    mediator.currentLanguage,
                    mediator.currentProject
                    ));
            };
        }

        public void ToggleOnCorpus(string corpusDir)
        {
            // folder must exist before we start watching
            if (IOTools.CreateDirectory(corpusDir))
            {
                corpusWatcher.Path = corpusDir;
                corpusWatcher.EnableRaisingEvents = true;
            }
        }

        public void ToggleOffCorpus()
        {
            corpusWatcher.EnableRaisingEvents = false;
        }

        public void ToggleOnProject(string projectDicPath, string genDicPath, string projectFilesPath)
        {

            // folder must exist before we start watching
            if (IOTools.CreateDirectory(projectDicPath))
            {
                specDictWatcher.Path = projectDicPath;
                specDictWatcher.EnableRaisingEvents = true;
            }
                
            if (IOTools.CreateDirectory(genDicPath))
            {
                genDictWatcher.Path = genDicPath;
                genDictWatcher.EnableRaisingEvents = true;
            }
                
            if (IOTools.CreateDirectory(projectFilesPath))
            {
                filesWatcher.Path = projectFilesPath;
                filesWatcher.EnableRaisingEvents = true;
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
