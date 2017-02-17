﻿using LangTools.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LangTools.Shared;
using LangTools.Core;

namespace LangTools.Models
{
    public class TypedEventArgs<T> : EventArgs
    {
        public readonly T Content;
        public TypedEventArgs(T content)
        {
            this.Content = content;
        }
    }

    public class MainModel
    {
        // Singleton implementation
        private static readonly MainModel instance = new MainModel();
        public static MainModel Instance
        {
            get
            {
                return instance;
            }
        }
        // Singleton ctor
        private MainModel() { watcher = new WatchTower(this); }


        // Memmbers
        private const string COMMONDICTNAME = "Common.txt";   

        private IStorage storage;

        private WatchTower watcher;

        private List<Lingva> languages = new List<Lingva>();
        private List<string> projects = new List<string>();
        private List<Dict> dicts = new List<Dict>();
        private List<FileStats> files = new List<FileStats>();

        internal Lingva currentLanguage;
        internal string currentProject;

        public string CorpusDir { get; set; } = "corpus";
        public string DicDir { get; set; } = "dics";
        public string OutDir { get; set; } = "output";
        // TODO check null
        internal string ProjectDicPath
        {
            get
            {
                return Path.Combine(currentLanguage.Folder, DicDir, currentProject);
            }
        }
        // TODO check null
        internal string GenDicPath
        {
            get
            {
                return Path.Combine(currentLanguage.Folder, DicDir);
            }
        }
        // TODO check null
        internal string ProjectFilesPath
        {
            get
            {
                return Path.Combine(currentLanguage.Folder, CorpusDir, currentProject);
            }
        }

        public event EventHandler<TypedEventArgs<string>> ProjectAdded;
        public event EventHandler<TypedEventArgs<string>> ProjectRemoved;
        public event EventHandler<TypedEventArgs<Dict>> DictAdded;
        public event EventHandler<TypedEventArgs<Dict>> DictRemoved;
        public event EventHandler<TypedEventArgs<FileStats>> FileStatsAdded;
        public event EventHandler<TypedEventArgs<FileStats>> FileStatsRemoved;
        public event EventHandler<TypedEventArgs<Lingva>> LanguageAdded;
        public event EventHandler<TypedEventArgs<Lingva>> LanguageRemoved;

        /// <summary>
        /// Method that sets new storage for model.
        /// </summary>
        /// <param name="storage"></param>
        public void SetStorage(IStorage storage)
        {
            // Establish DB Connection
            this.storage = storage;
        }

        /// <summary>
        /// Request languages from DB.
        /// </summary>
        public void InitializeLanguages()
        {
            foreach (Lingva lang in storage.GetLanguages())
            {
                AddLanguage(lang);
            }
        }

        // Private signaling methods
        internal void AddProject(string newProject)
        {
            projects.Add(newProject);
            ProjectAdded?.Invoke(this, new TypedEventArgs<string>(newProject));
        }

        internal void RemoveProject(string oldProject)
        {
            projects.Remove(oldProject);
            ProjectRemoved?.Invoke(this, new TypedEventArgs<string>(oldProject));
        }

        internal void AddDict(Dict dictionary)
        {
            dicts.Add(dictionary);
            DictAdded?.Invoke(this, new TypedEventArgs<Dict>(dictionary));
        }

        internal void RemoveDict(Dict dictionary)
        {
            dicts.Remove(dictionary);
            DictRemoved?.Invoke(this, new TypedEventArgs<Dict>(dictionary));
        }

        internal void AddFileStats(FileStats fs)
        {
            files.Add(fs);
            FileStatsAdded?.Invoke(this, new TypedEventArgs<FileStats>(fs));
        }

        internal void RemoveFileStats(FileStats fs)
        {
            files.Remove(fs);
            FileStatsRemoved?.Invoke(this, new TypedEventArgs<FileStats>(fs));
        }

        private void AddLanguage(Lingva language)
        {
            languages.Add(language);
            LanguageAdded?.Invoke(this, new TypedEventArgs<Lingva>(language));
        }

        private void RemoveLanguage(Lingva language)
        {
            languages.Remove(language);
            LanguageRemoved?.Invoke(this, new TypedEventArgs<Lingva>(language));
        }

        // Methods

        /// <summary>
        /// Stops previously selected language from being watched.
        /// </summary>
        public void UnselectLanguage()
        {
            currentLanguage = null;
            // Stop watching previous corpus folder. Have to do stop watcher
            // from raising event that lead to insertion into Null project list.
            watcher.ToggleOffCorpus();
            // Remove old projects
            while (projects.Count > 0)
            {
                RemoveProject(projects[0]);
            };
        }

        /// <summary>
        /// Sets new language as current.
        /// </summary>
        /// <param name="lang"></param>
        public void SelectLanguage(Lingva lang)
        {
            Log.Logger.Debug("New language is chosen.");
            currentLanguage = lang;
            // Determine corpus directory
            string corpusDir = Path.Combine(
                currentLanguage.Folder,
                CorpusDir);

            IEnumerable<string> projectsInDir;
            if (IOTools.ListDirectories(corpusDir, out projectsInDir))
            {
                // Start watching new language corpus folder
                watcher.ToggleOnCorpus(corpusDir);
                // Remove old unused projects from DB
                RemoveOldProjects(projectsInDir, currentLanguage);
            }
            // Add new projects
            foreach (string prj in projectsInDir)
            {
                AddProject(prj);
            }
        }

        /// <summary>
        /// Removes leftover projects from DB.
        /// </summary>
        /// <param name="projectsInDir"></param>
        /// <param name="selectedLang"></param>
        private void RemoveOldProjects(IEnumerable<string> projectsInDir, Lingva selectedLang)
        {
            // Get known projects from DB
            List<string> projectsInDB = storage.GetProjects(selectedLang);
            // Find projects left only in DB
            foreach (string leftover in projectsInDB.Except(projectsInDir))
            {
                // Remove leftover projects
                Log.Logger.Debug(string.Format("Deleting project:{0} - {1}", selectedLang.Language, leftover));
                storage.RemoveProject(selectedLang, leftover);
            }
        }

        /// <summary>
        /// Stops previously selected project from being watched.
        /// </summary>
        public void UnselectProject()
        {
            currentProject = null;
            // Stop watching project specific files.
            watcher.ToggleOffProject();
            
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

        /// <summary>
        /// Sets new project as current.
        /// </summary>
        /// <param name="project"></param>
        public void SelectProject(string project)
        {
            // language might be null during the proccess
            // of changing language.
            if (currentLanguage == null) return;

            Log.Logger.Debug("New project is chosen.");
            currentProject = project;
            // Get custom project dictionaries
            IEnumerable<string> projectSpecificDics;
            if (IOTools.ListFiles(ProjectDicPath, out projectSpecificDics))
            {
                // Add found dictionaries to dict collection.
                foreach (string fName in projectSpecificDics)
                {
                    AddDict(new Dict
                    {
                        FileName = fName,
                        DictType = DictType.Project,
                        FilePath = Path.Combine(ProjectDicPath, fName)
                    });
                }
            }

            // Get general project dictionaries.
            IEnumerable<string> generalDics;
            if (IOTools.ListFiles(GenDicPath, out generalDics))
            {
                // Combine both specific and general dictionaries
                foreach (string fName in generalDics)
                {
                    AddDict(new Dict
                    {
                        FileName = fName,
                        DictType = DictType.General,
                        FilePath = Path.Combine(GenDicPath, fName)
                    });
                }
            }

            // Get file names from project dir
            IEnumerable<string> fileNames;
            if (!IOTools.ListFiles(ProjectFilesPath, out fileNames))
            {
                // Can't reach the folder, no sense to proceed further.
                // TODO watcher?
                return;
            }

            // Create FileStats object for every file name
            IEnumerable<FileStats> inDir = fileNames.Select(fName => new FileStats(
                fName,
                Path.Combine(ProjectFilesPath, fName),
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

            // Add files that we have in dir but no stats in DB
            foreach (FileStats item in inDir.Except(inDB))
            {
                AddFileStats(item);
            }

            // Remove leftover stats from DB.
            foreach (FileStats item in inDB.Except(inDir))
            {
                Log.Logger.Debug(string.Format("Going to remove {0} from DB", item.FileName));
                storage.RemoveFileStats(item);
            }
            // Start watching files in the project.
            watcher.ToggleOnProject();
        }

        /// <summary>
        /// Adds new language to DB and model.
        /// </summary>
        /// <param name="newLang"></param>
        public void AddNewLanguage(Lingva newLang)
        {
            // Add new language to DB.
            storage.AddLanguage(newLang);
            EnsureCorpusStructure(newLang.Folder);
            AddLanguage(newLang);
        }

        /// <summary>
        /// Removes language from DB and model.
        /// </summary>
        /// <param name="language"></param>
        public void RemoveOldLanguage(Lingva language)
        {
            // Remove old language from DB
            storage.RemoveLanguage(language);
            RemoveLanguage(language);
        }

        /// <summary>
        /// Creates folder structer for the language.
        /// </summary>
        /// <param name="directory"></param>
        private void EnsureCorpusStructure(string directory)
        {
            // Define subfolders names
            string corpusDir = Path.Combine(directory, CorpusDir);
            string dicDir = Path.Combine(directory, DicDir);
            string outputDir = Path.Combine(directory, OutDir);

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
                Log.Logger.Error(msg);
            }
        }

        /// <summary>
        /// Makes analysis of the project files.
        /// </summary>
        /// <param name="progress"></param>
        public void Analyze(IProgress<Tuple<double, string>> progress)
        {
            if (currentProject == null || currentLanguage == null || files.Count == 0 || dicts.Count == 0)
            {
                // Nothing to analyze here
                return;
            }

            // Stop watching for new files and dictionaries.
            watcher.ToggleOffProject();

            Log.Logger.Debug("Project analysis has started.");
            progress.Report(Tuple.Create(0d, (string)null));
            // Ensure directory structure, dict and output project specific dirs
            //  are misssing on first run.
            EnsureProjectStructure(currentLanguage.Folder, currentProject);

            // Remove old stats and words for project from DB.
            storage.RemoveProject(currentLanguage, currentProject);

            //// Create object that handles analysis.
            Analyzer worker = new Analyzer(currentLanguage.Language);
            worker.AddDictionaries(dicts.Select(d => d.FilePath));
            worker.PrepareDictionaries();
            //// Create printer that will print analysis
            Printer printer = new Printer(currentLanguage.Language);

            progress.Report(Tuple.Create(30d, (string)null));

            double percentValue = 30;
            double step = 70.0 / files.Count();
            foreach (FileStats file in files)
            {
                percentValue += step;
                Document docRoot = worker.AnalyzeFile(file.FilePath);
                if (docRoot != null)
                {
                    // Compare old and new stats
                    if (file.Update(docRoot.Size, docRoot.Known, docRoot.Maybe))
                    {
                        // Produce new output page
                        printer.Print(file.Project, currentLanguage.Folder, docRoot);
                    }
                    // Update stats in the DB
                    storage.UpdateStats(file);
                    // Add new list of unknown words into DB
                    var newWords = docRoot.Tokens
                                   .Where(t => t.Stats?.Know == Klass.UNKNOWN)
                                   .Select(t => t.Stats)
                                   .Distinct();
                    // commit prevents memory leak
                    storage.UpdateWords(file.FilePath, newWords);
                    storage.CommitWords();
                }
                progress.Report(Tuple.Create(percentValue, file.FileName));
            }
            // Commit changes to DB
            storage.CommitStats();

            // Start watching for files again
            watcher.ToggleOnProject();
        }

        /// <summary>
        /// Creates folder structure for the project.
        /// </summary>
        /// <param name="langPath"></param>
        /// <param name="project"></param>
        private void EnsureProjectStructure(string langPath, string project)
        {
            string projectDictDir =
                Path.Combine(langPath,
                DicDir,
                project);
            string projectOutDir =
                Path.Combine(langPath,
                OutDir,
                project);

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
                Log.Logger.Error(msg);
            }
        }

        /// <summary>
        /// Provides a dictionary with unknown words.
        /// </summary>
        /// <param name="fs"></param>
        /// <returns></returns>
        public Dictionary<string, int> GetUnknownWords(FileStats fs)
        {
            return storage.GetUnknownWords(fs);
        }

        /// <summary>
        /// Provides a dictionary with unknown words for whole project.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, int> GetUnknownWords()
        {
            if (currentProject != null && currentLanguage != null)
            {
                return storage.GetUnknownWords(currentLanguage, currentProject);
            }
            
            return new Dictionary<string, int>();
        }

        /// <summary>
        /// Appends the word to common dictionary.
        /// </summary>
        /// <param name="word"></param>
        public void AddWordToDictionary(string word)
        {
            string filePath = Path.Combine(
                currentLanguage.Folder,
                DicDir,
                currentProject, COMMONDICTNAME);
            string wordToAppend = string.Format("{0}{1}", word, Environment.NewLine);
            IOTools.AppendToFile(filePath, wordToAppend);
        }

        /// <summary>
        /// Returns a list with file names.
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public List<string> GetFilenamesWithWord(string word)
        {
            return storage.GetFilenamesWithWord(word);
        }

        public bool LanguageExists(string lang)
        {
            return languages.Any(l => l.Language == lang);
        }

        public bool FolderExists(string dir)
        {
            return languages.Any(l => l.Folder == dir);
        }
    }
}
