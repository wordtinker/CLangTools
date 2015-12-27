﻿using LangTools.Core;
using LangTools.DataAccess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Threading;

namespace LangTools.Models
{
    class TypedEventArgs<T> : EventArgs
    {
        public readonly T Content;
        public TypedEventArgs(T content)
        {
            this.Content = content;
        }
    }

    internal class AnalysisProgress
    {
        internal readonly double Percent;
        internal readonly string FileName;

        internal AnalysisProgress(double progressValue, string fileName=null)
        {
            this.Percent = progressValue;
            this.FileName = fileName;
        }
    }

    class MainModel
    {
        // Memmbers
        private Storage storage;

        private FileSystemWatcher corpusWatcher;
        private FileSystemWatcher specDictWatcher;
        private FileSystemWatcher genDictWatcher;
        private FileSystemWatcher filesWatcher;

        private List<Lingva> languages;
        private List<string> projects;
        private List<Dict> dicts;
        private List<FileStats> files;

        private Lingva currentLanguage;
        private string currentProject;

        internal event EventHandler<TypedEventArgs<string>> ProjectAdded;
        internal event EventHandler<TypedEventArgs<string>> ProjectRemoved;
        internal event EventHandler<TypedEventArgs<Dict>> DictAdded;
        internal event EventHandler<TypedEventArgs<Dict>> DictRemoved;
        internal event EventHandler<TypedEventArgs<FileStats>> FileStatsAdded;
        internal event EventHandler<TypedEventArgs<FileStats>> FileStatsRemoved;
        internal event EventHandler<TypedEventArgs<Lingva>> LanguageAdded;
        internal event EventHandler<TypedEventArgs<Lingva>> LanguageRemoved;

        internal MainModel()
        {
            storage = (Storage)App.Current.Properties["storage"];
            languages = new List<Lingva>();
            projects = new List<string>();
            dicts = new List<Dict>();
            files = new List<FileStats>();
            InitializeWatchers();
        }

        void InitializeWatchers()
        {
            // Corpus Watcher
            corpusWatcher = new FileSystemWatcher();
            corpusWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            corpusWatcher.Filter = "*.*";
            // Remark : deleting of old projects from storage is postponed
            // until the next time LanguageChanged is called.
            // TODO: Later.

            // Have to use Dispatcher to execute Action in the same thread as ViewModel
            corpusWatcher.Created += (obj, e) =>  App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                AddProject(e.Name)
                ));

            corpusWatcher.Deleted += (obj, e) => App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                RemoveProject(e.Name)
                ));

            corpusWatcher.Renamed += (obj, e) => App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                {
                    RemoveProject(e.OldName);
                    AddProject(e.Name);
                }
                ));

            // Project specific dictionaries Watcher
            specDictWatcher = new FileSystemWatcher();
            specDictWatcher.NotifyFilter = NotifyFilters.FileName;
            specDictWatcher.Filter = "*.txt";

            specDictWatcher.Created += (obj, e) => App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                AddDict(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.Project,
                    FilePath = e.FullPath
                })));

            specDictWatcher.Deleted += (obj, e) => App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                RemoveDict(new Dict
                {
                    FilePath = e.FullPath
                })));

            specDictWatcher.Renamed += (obj, e) => App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                {
                    AddDict(new Dict
                    {
                        FileName = e.Name,
                        DictType = DictType.Project,
                        FilePath = e.FullPath
                    });
                    RemoveDict(new Dict
                    {
                        FilePath = e.OldFullPath
                    });
                }
                ));

            // General dictionaries Watcher
            genDictWatcher = new FileSystemWatcher();
            genDictWatcher.NotifyFilter = NotifyFilters.FileName;
            genDictWatcher.Filter = "*.txt";

            genDictWatcher.Created += (obj, e) => App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                AddDict(new Dict
                {
                    FileName = e.Name,
                    DictType = DictType.General,
                    FilePath = e.FullPath
                })));

            genDictWatcher.Deleted += (obj, e) => App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                RemoveDict(new Dict
                {
                    FilePath = e.FullPath
                })));

            genDictWatcher.Renamed += (obj, e) => App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                {
                    AddDict(new Dict
                    {
                        FileName = e.Name,
                        DictType = DictType.General,
                        FilePath = e.FullPath
                    });
                    RemoveDict(new Dict
                    {
                        FilePath = e.OldFullPath
                    });
                }
                ));

            // Files Directory Watcher
            // Remark : deleting of stats for deleted filese from storage is postponed
            // until the next time ProjectChanged is called.
            // TODO: Later
            filesWatcher = new FileSystemWatcher();
            filesWatcher.NotifyFilter = NotifyFilters.FileName;
            filesWatcher.Filter = "*.txt";

            filesWatcher.Created += (obj, e) => App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                AddFileStats(new FileStats(
                    e.Name,
                    e.FullPath,
                    currentLanguage,
                    currentProject
                    ))));

            filesWatcher.Deleted += (obj, e) => App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                RemoveFileStats(new FileStats(
                    e.Name,
                    e.FullPath,
                    currentLanguage,
                    currentProject
                    ))));

            filesWatcher.Renamed += (obj, e) => App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                {
                    AddFileStats(new FileStats(
                        e.Name,
                        e.FullPath,
                        currentLanguage,
                        currentProject
                        ));
                    RemoveFileStats(new FileStats(
                        e.OldName,
                        e.OldFullPath,
                        currentLanguage,
                        currentProject
                        ));
                }
                ));
        }

        public void InitializeLanguages()
        {
            foreach (Lingva lang in storage.GetLanguages())
            {
                AddLanguage(lang);
            }
        }

        // Private signaling methods
        private void onProjectAdded(TypedEventArgs<string> newProject)
        {
            if (ProjectAdded != null)
            {
                ProjectAdded(this, newProject);
            }
        }

        private void onProjectRemoved(TypedEventArgs<string> oldProject)
        {
            if (ProjectRemoved != null)
            {
                ProjectRemoved(this, oldProject);
            }
        }

        private void AddProject(string newProject)
        {
            projects.Add(newProject);
            onProjectAdded(new TypedEventArgs<string>(newProject));
        }

        private void RemoveProject(string oldProject)
        {
            projects.Remove(oldProject);
            onProjectRemoved(new TypedEventArgs<string>(oldProject));
        }

        private void onDictAdded(TypedEventArgs<Dict> newDict)
        {
            if (DictAdded != null)
            {
                DictAdded(this, newDict);
            }
        }

        private void onDictRemoved(TypedEventArgs<Dict> oldDict)
        {
            if(DictRemoved != null)
            {
                DictRemoved(this, oldDict);
            }
        }

        private void AddDict(Dict dictionary)
        {
            dicts.Add(dictionary);
            onDictAdded(new TypedEventArgs<Dict>(dictionary));
        }

        private void RemoveDict(Dict dictionary)
        {
            dicts.Remove(dictionary);
            onDictRemoved(new TypedEventArgs<Dict>(dictionary));
        }

        private void onFileStatsAdded(TypedEventArgs<FileStats> newFileStats)
        {
            if(FileStatsAdded != null)
            {
                FileStatsAdded(this, newFileStats);
            }
        }

        private void onFileStatsRemoved(TypedEventArgs<FileStats> oldFileStats)
        {
            if(FileStatsRemoved != null)
            {
                FileStatsRemoved(this, oldFileStats);
            }
        }

        private void AddFileStats(FileStats fs)
        {
            files.Add(fs);
            onFileStatsAdded(new TypedEventArgs<FileStats>(fs));
        }

        private void RemoveFileStats(FileStats fs)
        {
            files.Remove(fs);
            onFileStatsRemoved(new TypedEventArgs<FileStats>(fs));
        }

        private void onLanguageAdded(TypedEventArgs<Lingva> newLang)
        {
            if (LanguageAdded != null)
            {
                LanguageAdded(this, newLang);
            }
        }

        private void onLanguageRemoved(TypedEventArgs<Lingva> oldLang)
        {
            if (LanguageRemoved != null)
            {
                LanguageRemoved(this, oldLang);
            }
        }

        private void AddLanguage(Lingva language)
        {
            languages.Add(language);
            onLanguageAdded(new TypedEventArgs<Lingva>(language));
        }

        private void RemoveLanguage(Lingva language)
        {
            languages.Remove(language);
            onLanguageRemoved(new TypedEventArgs<Lingva>(language));
        }

        // Methods

        internal void UnselectLanguage()
        {
            currentLanguage = null;
            // Stop watching previous corpus folder. Have to do stop watcher
            // from raising event that lead to insertion into Null project list.
            corpusWatcher.EnableRaisingEvents = false;
            // Remove old projects
            while (projects.Count > 0)
            {
                RemoveProject(projects[0]);
            };
        }

        internal void SelectLanguage(Lingva lang)
        {
            Logger.Write("New language is chosen.", Severity.DEBUG);
            currentLanguage = lang;
            // Determine corpus directory
            string corpusDir = Path.Combine(currentLanguage.Folder,
                (string)App.Current.Properties["corpusDir"]);

            IEnumerable<string> projectsInDir;
            if (IOTools.ListDirectories(corpusDir, out projectsInDir))
            {
                // Start watching new language corpus folder
                corpusWatcher.Path = corpusDir;
                corpusWatcher.EnableRaisingEvents = true;
                // Remove old unused projects from DB
                RemoveOldProjects(projectsInDir, currentLanguage);
            }
            // Add new projects
            foreach (string prj in projectsInDir)
            {
                AddProject(prj);
            }
        }

        private void RemoveOldProjects(IEnumerable<string> projectsInDir, Lingva selectedLang)
        {
            // Get known projects from DB
            List<string> projectsInDB = storage.GetProjects(selectedLang);
            // Find projects left only in DB
            foreach (var leftover in projectsInDB.Except(projectsInDir))
            {
                // Remove leftover projects
                Logger.Write(string.Format("Deleting project:{0} - {1}", selectedLang.Language, leftover), Severity.DEBUG);
                storage.RemoveProject(selectedLang, leftover);
            }
        }

        internal void UnselectProject()
        {
            currentProject = null;
            // Stop watching both dict folders
            specDictWatcher.EnableRaisingEvents = false;
            genDictWatcher.EnableRaisingEvents = false;
            // Stop watching files folder
            filesWatcher.EnableRaisingEvents = false;
            // Remove old dictionaries
            while (dicts.Count > 0)
            {
                RemoveDict(dicts[0]);
            }
            // Remove old FileStats
            while (files.Count > 0)
            {
                RemoveFileStats(files[0]);
            }
        }

        internal void SelectProject(string project)
        {
            // language might be null during the proccess
            // of changing language.
            if (currentLanguage == null) return;

            // TODO Later refactor
            Logger.Write("New project is chosen.", Severity.DEBUG);
            currentProject = project;
            // Get custom project dictionaries
            string dictionariesDir = Path.Combine(currentLanguage.Folder,
                (string)App.Current.Properties["dicDir"], project);

            IEnumerable<string> projectSpecificDics;
            if (IOTools.ListFiles(dictionariesDir, out projectSpecificDics))
            {
                // Add found dictionaries to dict collection.
                foreach (string fName in projectSpecificDics)
                {
                    AddDict(new Dict
                    {
                        FileName = fName,
                        DictType = DictType.Project,
                        FilePath = Path.Combine(dictionariesDir, fName)
                    });
                }
                // Start watching for new specific dict files.
                specDictWatcher.Path = dictionariesDir;
                specDictWatcher.EnableRaisingEvents = true;
            }

            // Get general project dictionaries.
            string generalDir = Path.Combine(currentLanguage.Folder,
                (string)App.Current.Properties["dicDir"]);
            IEnumerable<string> generalDics;
            if (IOTools.ListFiles(generalDir, out generalDics))
            {
                // Combine both specific and general dictionaries
                foreach (string fName in generalDics)
                {
                    AddDict(new Dict
                    {
                        FileName = fName,
                        DictType = DictType.General,
                        FilePath = Path.Combine(generalDir, fName)
                    });
                }
                // Start watching for general dictionaries.
                genDictWatcher.Path = generalDir;
                genDictWatcher.EnableRaisingEvents = true;
            }

            // Get file names from project dir
            string filesDir = Path.Combine(currentLanguage.Folder,
                (string)App.Current.Properties["corpusDir"], project);
            IEnumerable<string> fileNames;
            if (!IOTools.ListFiles(filesDir, out fileNames))
            {
                // Can't reach the folder, no sense to proceed further.
                return;
            }

            // Create FileStats object for every file name
            IEnumerable<FileStats> inDir = fileNames.Select(fName => new FileStats(
                fName,
                Path.Combine(filesDir, fName),
                currentLanguage,
                project
                ));

            // Get list of objects from DB
            List<FileStats> inDB = storage.GetFilesStats(currentLanguage, project);
            // NB: inDB.Intersect will return elements from inDB.
            // Need this order since they have more information.
            foreach (FileStats item in inDB.Intersect(inDir))
            {
                AddFileStats(item);
            }

            // Start watching files in the project.
            filesWatcher.Path = filesDir;
            filesWatcher.EnableRaisingEvents = true;

            // Add files that we have in dir but no stats in DB
            foreach (FileStats item in inDir.Except(inDB))
            {
                AddFileStats(item);
            }

            // Remove leftover stats from DB.
            foreach (FileStats item in inDB.Except(inDir))
            {
                Logger.Write(string.Format("Going to remove {0} from DB", item.FileName), Severity.DEBUG);
                storage.RemoveFileStats(item);
            }
        }

        internal void AddNewLanguage(Lingva newLang)
        {
            // Add new language to DB.
            storage.AddLanguage(newLang);
            EnsureCorpusStructure(newLang.Folder);
            AddLanguage(newLang);
        }

        internal void RemoveOldLanguage(Lingva language)
        {
            // Remove old language from DB
            storage.RemoveLanguage(language);
            RemoveLanguage(language);
        }

        private void EnsureCorpusStructure(string directory)
        {
            // Define subfolders names
            string corpusDir = Path.Combine(directory, (string)App.Current.Properties["corpusDir"]);
            string dicDir = Path.Combine(directory, (string)App.Current.Properties["dicDir"]);
            string outputDir = Path.Combine(directory, (string)App.Current.Properties["outputDir"]);

            // Create subfolders
            try
            {
                Directory.CreateDirectory(corpusDir);
                Directory.CreateDirectory(dicDir);
                Directory.CreateDirectory(outputDir);
            }
            catch (Exception e)
            {
                // Not a critical error, could be fixed later.
                string msg = string.Format("Something is wrong during subfolder creation: {0}", e.ToString());
                Logger.Write(msg);
            }
        }

        internal void Analyze(IProgress<AnalysisProgress> progress)
        {
            if (currentProject == null || currentLanguage == null || files.Count == 0 || dicts.Count == 0)
            {
                // Nothing to analyze here
                return;
            }

            Logger.Write("Project analysis has started.", Severity.DEBUG);
            progress.Report(new AnalysisProgress(0));
            // Ensure directory structure, dict and output project specific dirs
            //  are misssing on first run.
            EnsureProjectStructure(currentLanguage.Folder, currentProject);

            //// Create object that handles analysis.
            Analyzer worker = new Analyzer(currentLanguage.Language);
            worker.AddDictionaries(dicts.Select(d => d.FilePath));
            worker.PrepareDictionaries();
            //// Create printer that will print analysis
            Printer printer = new Printer(currentLanguage.Language);

            progress.Report(new AnalysisProgress(30));
            double percentValue = 30;
            double step = 70.0 / files.Count();
            foreach (FileStats file in files)
            {
                percentValue += step;
                Report item = worker.AnalyzeFile(file.FilePath);
                if (item != null)
                {
                    // Compare old and new stats
                    if (file.Update(item.Size, item.Known, item.Maybe))
                    {
                        // Produce new output page
                        printer.Print(file.FileName, file.Project,
                            currentLanguage.Folder, item.Tokens);
                        // Update stats in the DB
                        storage.UpdateStats(file);
                        // Add new word list to DB
                        storage.UpdateWords(file.FilePath, item.UnknownWords);
                    }
                }
                progress.Report(new AnalysisProgress(
                    percentValue,
                    file.FileName
                    ));
            }
            // Commit changes to DB
            storage.CommitStats();
            storage.CommitWords();
        }

        private void EnsureProjectStructure(string langPath, string project)
        {
            string projectDictDir =
                Path.Combine(langPath, (string)App.Current.Properties["dicDir"], project);
            string projectOutDir =
                Path.Combine(langPath, (string)App.Current.Properties["outputDir"], project);

            // Create subfolders
            try
            {
                Directory.CreateDirectory(projectDictDir);
                Directory.CreateDirectory(projectOutDir);
            }
            catch (Exception e)
            {
                // Not a critical error.
                string msg = string.Format("Something is wrong during subfolder creation: {0}", e.ToString());
                Logger.Write(msg);
            }
        }
    }
}