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
            // until the next time LanguageChanged is called.
            corpusWatcher.Created += (obj, e) => model.AddProject(e.Name);
            corpusWatcher.Deleted += (obj, e) => model.RemoveProject(e.Name);
            corpusWatcher.Renamed += (obj, e) =>
            {
                model.RemoveProject(e.OldName);
                model.AddProject(e.Name);
            };

            // Project specific dictionaries Watcher
            specDictWatcher = new FileSystemWatcher();
            specDictWatcher.NotifyFilter = NotifyFilters.FileName;
            specDictWatcher.Filter = "*.txt";
            specDictWatcher.Created += (obj, e) =>
                model.AddDict(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.Project,
                    FilePath = e.FullPath
                });
            specDictWatcher.Deleted += (obj, e) =>
                model.RemoveDict(new Dict
                {
                    FilePath = e.FullPath
                });
            specDictWatcher.Renamed += (obj, e) =>
            {
                model.AddDict(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.Project,
                    FilePath = e.FullPath
                });
                model.RemoveDict(new Dict
                {
                    FilePath = e.OldFullPath
                });
            };

            // General dictionaries Watcher
            genDictWatcher = new FileSystemWatcher();
            genDictWatcher.NotifyFilter = NotifyFilters.FileName;
            genDictWatcher.Filter = "*.txt";
            genDictWatcher.Created += (obj, e) =>
                model.AddDict(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.General,
                    FilePath = e.FullPath
                });
            genDictWatcher.Deleted += (obj, e) =>
                model.RemoveDict(new Dict
                {
                    FilePath = e.FullPath
                });
            genDictWatcher.Renamed += (obj, e) =>
            {
                model.AddDict(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.General,
                    FilePath = e.FullPath
                });
                model.RemoveDict(new Dict
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
                model.AddFileStats(new FileStats(
                    e.Name,
                    e.FullPath,
                    model.currentLanguage,
                    model.currentProject
                    ));
            filesWatcher.Deleted += (obj, e) =>
                model.RemoveFileStats(new FileStats(
                    e.Name,
                    e.FullPath,
                    model.currentLanguage,
                    model.currentProject
                    ));
            filesWatcher.Renamed += (obj, e) =>
            {
                model.AddFileStats(new FileStats(
                    e.Name,
                    e.FullPath,
                    model.currentLanguage,
                    model.currentProject
                    ));
                model.RemoveFileStats(new FileStats(
                    e.OldName,
                    e.OldFullPath,
                    model.currentLanguage,
                    model.currentProject
                    ));
            };
        }

        public void ToggleOnCorpus(string corpusDir)
        {
            // TODO drop corpusDir
            //corpusWatcher.Path = corpusDir;
            //corpusWatcher.EnableRaisingEvents = true;
        }

        public void ToggleOffCorpus()
        {
            //corpusWatcher.EnableRaisingEvents = false;
        }

        public void ToggleOnProject()
        {
            // TODO folder must exist before or we will fail
            //specDictWatcher.Path = model.ProjectDicPath;
            //genDictWatcher.Path = model.GenDicPath;
            //filesWatcher.Path = model.ProjectFilesPath;

            //specDictWatcher.EnableRaisingEvents = true;
            //genDictWatcher.EnableRaisingEvents = true;
            //filesWatcher.EnableRaisingEvents = true;
        }

        public void ToggleOffProject()
        {
            // Stop watching both dict folders
            //specDictWatcher.EnableRaisingEvents = false;
            //genDictWatcher.EnableRaisingEvents = false;
            //// Stop watching files folder
            //filesWatcher.EnableRaisingEvents = false;
        }
    }
}
