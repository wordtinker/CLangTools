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

namespace LangTools.ViewModels
{
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
                    OnPropertyChanged(() => UnknownPercent);
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
            // TODO move to builder
            IStorage storage = new Storage(windowService.AppDir);
            model = MainModel.Instance;
            model.SetStorage(storage);
            model.CorpusDir = windowService.CorpusDir;
            model.DicDir = windowService.DicDir;
            model.OutDir = windowService.OutDir;

            this.windowService = windowService;

            L.Logger.Debug("MainView is starting.");

            Languages = new ObservableCollection<LingvaViewModel>();
            model.Languages.CollectionChanged += (obj, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (Lingva item in args.NewItems)
                    {
                        Languages.Add(new LingvaViewModel(item));
                    }
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (Lingva item in args.OldItems)
                    {
                        Languages.Remove(new LingvaViewModel(item));
                    }
                }
            };

            Projects = new ObservableCollection<string>();
            model.Projects.CollectionChanged += (obj, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (string item in args.NewItems)
                    {
                        Projects.Add(item);
                    }
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (string item in args.OldItems)
                    {
                        Projects.Remove(item);
                    }
                }
            };

            Dictionaries = new ObservableCollection<DictViewModel>();
            model.Dictionaries.CollectionChanged += (obj, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (Dict item in args.NewItems)
                    {
                        Dictionaries.Add(new DictViewModel(windowService, item));
                    }
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (Dict item in args.OldItems)
                    {
                        Dictionaries.Remove(new DictViewModel(windowService, item));
                    }
                }
            };

            Files = new ObservableCollection<FileStatsViewModel>();
            model.Files.CollectionChanged += (obj, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (FileStats item in args.NewItems)
                    {
                        FileStatsViewModel fsvm = new FileStatsViewModel(windowService, item);
                        Files.Add(fsvm);
                        totalUnknown += fsvm.Unknown.GetValueOrDefault();
                        TotalWords += fsvm.Size.GetValueOrDefault();
                    }
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (FileStats item in args.OldItems)
                    {
                        FileStatsViewModel fsvm = new FileStatsViewModel(windowService, item);
                        Files.Remove(fsvm);
                        totalUnknown -= fsvm.Unknown.GetValueOrDefault();
                        TotalWords -= fsvm.Size.GetValueOrDefault();
                    }
                }
            };

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
                return DelegateCommand
                    .FromAsyncHandler(() => HandleAnalysis())
                    .ObservesCanExecute(p => ProjectSelectable);
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
