using LangTools.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.Commands;
using L = LangTools.Shared.Log;
using LangTools.Data;
using System.Collections.Specialized;
using LangTools.Shared;
using System.IO;

namespace LangTools.ViewModels
{
    /// <summary>
    /// Simple class that does initial configuaration of the model.
    /// </summary>
    internal static class ModelConfigurator
    {
        public static void Configure(IUIMainWindowService windowService)
        {
            MainModel model = MainModel.Instance;
            model.Storage = new Storage(windowService.AppDir);
            model.Config.CommonDictionaryName = windowService.CommonDictionaryName;
            model.Config.CorpusDir = windowService.CorpusDir;
            model.Config.DicDir = windowService.DicDir;
            model.Config.OutDir = windowService.OutDir;
            model.Config.StyleDirectoryPath = IOTools.CombinePath(Directory.GetCurrentDirectory(), "plugins");
        }
    }

    /// <summary>
    /// Class that binds one Observable collection to another collection and
    /// dispatches proper method invocation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class CollectionBinder<T>
    {
        private Action<T> addition;
        private Action<T> deletion;
        private IUIMainWindowService windowService;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="addition">Action that handles addition to the new collection.</param>
        /// <param name="deletion">Action that handles deletion from the new collection.</param>
        /// <param name="windowService"></param>
        public CollectionBinder(Action<T> addition, Action<T> deletion, IUIMainWindowService windowService)
        {
            this.addition = addition;
            this.deletion = deletion;
            this.windowService = windowService;
        }

        public void Connect(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (T item in e.NewItems)
                {
                    windowService.BeginInvoke(new Action(() => addition?.Invoke(item)));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (T item in e.OldItems)
                {
                    windowService.BeginInvoke(new Action(() => deletion?.Invoke(item)));
                }
            }
        }
    }

    public class MainViewModel : BindableBase
    {
        // Members
        private MainModel model;
        private int totalWords; // total words in the project files
        private int totalUnknown; // unknown words in the project files
        private string log;
        private int progressValue;
        private bool projectSelectable = true; // defines if the user can switch project
        private FileStatsViewModel currentFile; // currently selected file
        private IUIMainWindowService windowService;

        // Properties
        public ObservableCollection<LingvaViewModel> Languages { get; }
        public ObservableCollection<string> Projects { get; }
        public ObservableCollection<DictViewModel> Dictionaries { get; }
        public ObservableCollection<FileStatsViewModel> Files { get; }
        public ObservableCollection<WordViewModel> Words { get; }
        public ObservableCollection<WordViewModel> WordsInProject { get; }

        public int TotalWords
        {
            get { return totalWords; }
            set
            {
                if (SetProperty(ref totalWords, value))
                {
                    RaisePropertyChanged(nameof(UnknownPercent));
                }
            }
        }
        public double UnknownPercent
        {
            get
            {
                if (totalWords == 0) return 0;

                return (double)totalUnknown / totalWords;
            }
        }
        public string Log
        {
            get { return log; }
            set { SetProperty(ref log, value); }
        }
        public int ProgressValue
        {
            get { return progressValue; }
            set { SetProperty(ref progressValue, value); }
        }
        public bool ProjectSelectable
        {
            get { return projectSelectable; }
            set { SetProperty(ref projectSelectable, value); }
        }

        // Constructors
        public MainViewModel(IUIMainWindowService windowService)
        {
            this.windowService = windowService;
            // Configure the model before usage.
            ModelConfigurator.Configure(windowService);
            model = MainModel.Instance;
            L.Logger.Debug("MainView is starting.");

            Languages = new ObservableCollection<LingvaViewModel>();
            CollectionBinder<Lingva> langBinder = new CollectionBinder<Lingva>(
                newLang => Languages.Add(new LingvaViewModel(newLang)),
                oldLang => Languages.Remove(new LingvaViewModel(oldLang)),
                windowService
                );
            model.Languages.CollectionChanged += langBinder.Connect;

            Projects = new ObservableCollection<string>();
            CollectionBinder<string> projectBinder = new CollectionBinder<string>(
                newProject => Projects.Add(newProject),
                oldProject => Projects.Remove(oldProject),
                windowService
                );
            model.Projects.CollectionChanged += projectBinder.Connect;

            Dictionaries = new ObservableCollection<DictViewModel>();
            CollectionBinder<Dict> dictBinder = new CollectionBinder<Dict>(
                newDict => Dictionaries.Add(new DictViewModel(windowService, newDict)),
                oldDict => Dictionaries.Remove(new DictViewModel(windowService, oldDict)),
                windowService
                );
            model.Dictionaries.CollectionChanged += dictBinder.Connect;

            Files = new ObservableCollection<FileStatsViewModel>();
            CollectionBinder<FileStats> fileBinder = new CollectionBinder<FileStats>(
                newFile =>
                {
                    FileStatsViewModel fsvm = new FileStatsViewModel(windowService, newFile);
                    Files.Add(fsvm);
                    totalUnknown += fsvm.Unknown.GetValueOrDefault();
                    TotalWords += fsvm.Size.GetValueOrDefault();
                },
                oldFile =>
                {
                    FileStatsViewModel fsvm = new FileStatsViewModel(windowService, oldFile);
                    Files.Remove(fsvm);
                    totalUnknown -= fsvm.Unknown.GetValueOrDefault();
                    TotalWords -= fsvm.Size.GetValueOrDefault();
                },
                windowService);
            model.Files.CollectionChanged += fileBinder.Connect;

            Words = new ObservableCollection<WordViewModel>();
            WordsInProject = new ObservableCollection<WordViewModel>();

            model.InitializeLanguages();
            ProgressValue = 100;
            L.Logger.Debug("MainView has started.");
        }

        // Methods
        /// <summary>
        /// Prepares modelView for new language.
        /// </summary>
        public void LanguageIsAboutToChange()
        {
            L.Logger.Debug("Language is about to change.");
            model.UnselectLanguage();
        }

        /// <summary>
        /// Changes state of viewModel according to selected language.
        /// </summary>
        /// <param name="item"></param>
        public void SelectLanguage(object item)
        {
            L.Logger.Debug("Language is selected.");
            LingvaViewModel lang = (LingvaViewModel)item;
            // Let the model know that selected language changed
            model.SelectLanguage(lang.CurrentLanguage);
        }

        /// <summary>
        /// Prepares viewModel for new project.
        /// </summary>
        public void ProjectIsAboutToChange()
        {
            L.Logger.Debug("Project is about to change.");
            model.UnselectProject();
            // Clear log and list of unknown words
            Log = "";
            WordsInProject.Clear();
        }

        /// <summary>
        /// Changes state of viewmodel according to selected project.
        /// </summary>
        /// <param name="item"></param>
        public void SelectProject(object item)
        {
            L.Logger.Debug("Project is selected.");
            string project = (string)item;
            // Let the model know that selected project changed
            model.SelectProject(project);
            // Update list of words related to project
            ShowWordsForProject();
        }

        /// <summary>
        /// Adds new language to viewmodel and model.
        /// </summary>
        /// <param name="languageViewModel"></param>
        public void AddNewLanguage(LingvaViewModel languageViewModel)
        {
            L.Logger.Debug("Adding new language.");
            // Use copy constructor.
            Lingva lang = new Lingva(languageViewModel.CurrentLanguage);
            model.AddNewLanguage(lang);
        }

        /// <summary>
        /// Removes language from viewModel and model.
        /// </summary>
        /// <param name="languageViewModel"></param>
        public void RemoveLanguage(LingvaViewModel languageViewModel)
        {
            Lingva lang = languageViewModel.CurrentLanguage;
            L.Logger.Debug(string.Format("Removing {0} language from {1}.",
                lang.Language, lang.Folder));
            model.RemoveOldLanguage(lang);
        }

        /// <summary>
        /// Starts the analysis of the project files.
        /// </summary>
        /// <returns></returns>
        private async Task HandleAnalysis()
        {
            // Prevent changing of the project.
            ProjectSelectable = false;
            // Clear old project data.
            Words.Clear();
            WordsInProject.Clear();
            RemoveHighlighting();

            // Get the old project stats
            int oldKnownQty = Files.Sum(x => x.Known.GetValueOrDefault());
            int oldMaybeQty = Files.Sum(x => x.Maybe.GetValueOrDefault());
            ProgressValue = 0;
            L.Logger.Debug("Requesting Project analysis.");

            await Task.Run(() => model.Analyze(new Progress<Tuple<double, string>>(
                p =>
                {
                    //Update the visual progress of the analysis.
                    ProgressValue = Convert.ToInt32(p.Item1);
                    if (p.Item2 != null)
                    {
                        Log = string.Format("{0} is ready!", p.Item2);
                    }
                }
                )));

            // Get the new project stats
            int newKnownQty = Files.Sum(x => x.Known.GetValueOrDefault());
            int newMaybeQty = Files.Sum(x => x.Maybe.GetValueOrDefault());
            // Update the visual progress.
            ProgressValue = 100;
            Log = string.Format(
                "Analysis is finished. Known: {0:+#;-#;0}, Maybe {1:+#;-#;0}", // Force sign, no sign for zero
                newKnownQty - oldKnownQty, newMaybeQty - oldMaybeQty);
            L.Logger.Debug("Project analysis is ready.");
            // Update totals
            UpdateTotalStats();
            ProjectSelectable = true;
            // Update WordList
            if (currentFile != null) { ShowWords(currentFile); }
            ShowWordsForProject();
        }

        /// <summary>
        /// Recalculates the stats of the whole project.
        /// </summary>
        private void UpdateTotalStats()
        {
            totalUnknown = Files.Sum(x => x.Unknown.GetValueOrDefault());
            TotalWords = Files.Sum(x => x.Size.GetValueOrDefault());
        }

        /// <summary>
        /// Prepares viewModel for new file selection.
        /// </summary>
        public void FileRowIsAboutToChange()
        {
            currentFile = null;
            Words.Clear();
        }

        /// <summary>
        /// Fills the Words table with words.
        /// </summary>
        /// <param name="fileStatsVM"></param>
        public void ShowWords(FileStatsViewModel fileStatsVM)
        {
            currentFile = fileStatsVM;
            if (!ProjectSelectable) { return; }
            foreach (var item in model.GetUnknownWords(fileStatsVM.FileStats))
            {
                Words.Add(new WordViewModel {Word=item.Key, Quantity=item.Value });
            }
        }

        /// <summary>
        /// Fills the words table for the whole project.
        /// </summary>
        private void ShowWordsForProject()
        {
            foreach (var item in model.GetUnknownWords())
            {
                WordsInProject.Add(new WordViewModel { Word = item.Key, Quantity = item.Value });
            }
        }

        /// <summary>
        /// Adds one word into ditionary file.
        /// </summary>
        /// <param name="word"></param>
        public void AddWordToDictionary(WordViewModel word)
        {
            model.AddWordToDictionary(word.Word);
        }

        /// <summary>
        /// Removes highligting from every file.
        /// </summary>
        public void RemoveHighlighting()
        {
            foreach (FileStatsViewModel file in Files.Where(i => i.Highlighted==true))
            {
                file.Highlighted = false;
            }
        }

        /// <summary>
        /// Marks the files that contain the word.
        /// </summary>
        /// <param name="word"></param>
        public void HighlightFilesWithWord(WordViewModel word)
        {
            // Remove previously highlighted files
            RemoveHighlighting();
            // Request list of file names from model
            List<string> pathNames = model.GetFilenamesWithWord(word.Word);
            // Select files to highlight
            var filesToHighLight = Files.Where(i => pathNames.Contains(i.FilePath));
            foreach (FileStatsViewModel file in filesToHighLight)
            {
                file.Highlighted = true;
            }
        }

        // Commands
        public DelegateCommand RunProject
        {
            get
            {
                return new DelegateCommand(async () => await HandleAnalysis())
                    .ObservesCanExecute(() => ProjectSelectable);
            }
        }

        public DelegateCommand ShowHelp
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    windowService.ShowMessage(string.Format(
                        "{0}: {1}",
                        windowService.AppName,
                        CoreAssembly.Version));
                });
            }
        }

        public DelegateCommand ExitApp
        {
            get
            {
                return new DelegateCommand(() => windowService.Shutdown());
            }
        }
    }
}
