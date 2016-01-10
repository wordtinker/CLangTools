using LangTools.Models;
using MicroMvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LangTools.ViewModels
{
    class MainViewModel : ObservableObject
    {
        // Members
        private MainModel model = new MainModel();
        private int totalWords; // total words in the project files
        private int totalUnknown; // unknown words in the project files
        private string log;
        private int progressValue;
        private bool projectSelectable = true; // switch letting choose another project
        private FileStatsViewModel currentFile; // currently selected file

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
                totalWords = value;
                RaisePropertyChanged("TotalWords");
                RaisePropertyChanged("UnknownPercent");
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
            set { log = value; RaisePropertyChanged("Log"); }
        }
        public int ProgressValue
        {
            get { return progressValue; }
            set { progressValue = value;  RaisePropertyChanged("ProgressValue"); }
        }
        public bool ProjectSelectable
        {
            get { return projectSelectable; }
            set
            {
                projectSelectable = value;
                RaisePropertyChanged("ProjectSelectable");
            }
        }

        // Constructors
        public MainViewModel()
        {
            Logger.Write("MainView is starting.", Severity.DEBUG);

            Languages = new ObservableCollection<LingvaViewModel>();
            model.LanguageAdded += (obj, args) => Languages.Add(new LingvaViewModel(args.Content));
            model.LanguageRemoved += (obj, args) => Languages.Remove(new LingvaViewModel(args.Content));

            Projects = new ObservableCollection<string>();
            model.ProjectAdded += (obj, args) => Projects.Add(args.Content);
            model.ProjectRemoved += (obj, args) => Projects.Remove(args.Content);

            Dictionaries = new ObservableCollection<DictViewModel>();
            model.DictAdded += (obj, args) => Dictionaries.Add(new DictViewModel(args.Content));
            model.DictRemoved += (obj, args) => Dictionaries.Remove(new DictViewModel(args.Content));

            Files = new ObservableCollection<FileStatsViewModel>();
            model.FileStatsAdded += (obj, args) =>
            {
                FileStatsViewModel fsvm = new FileStatsViewModel(args.Content);
                Files.Add(fsvm);
                totalUnknown += fsvm.Unknown.GetValueOrDefault();
                TotalWords += fsvm.Size.GetValueOrDefault();
            };
            model.FileStatsRemoved += (obj, args) =>
            {
                FileStatsViewModel fsvm = new FileStatsViewModel(args.Content);
                Files.Remove(fsvm);
                totalUnknown -= fsvm.Unknown.GetValueOrDefault();
                TotalWords -= fsvm.Size.GetValueOrDefault();
            };

            Words = new ObservableCollection<WordViewModel>();
            WordsInProject = new ObservableCollection<WordViewModel>();

            model.InitializeLanguages();
            ProgressValue = 100;
            Logger.Write("MainView has started.", Severity.DEBUG);
        }

        // Methods
        /// <summary>
        /// Prepares modelView for new language.
        /// </summary>
        public void LanguageIsAboutToChange()
        {
            Logger.Write("Language is about to change.", Severity.DEBUG);
            model.UnselectLanguage();
        }

        /// <summary>
        /// Changes state of viewModel according to selected language.
        /// </summary>
        /// <param name="item"></param>
        public void SelectLanguage(object item)
        {
            Logger.Write("Language is selected.", Severity.DEBUG);
            LingvaViewModel lang = (LingvaViewModel)item;
            // Let the model know that selected language changed
            model.SelectLanguage(lang.CurrentLanguage);
        }

        /// <summary>
        /// Prepares viewModel for new project.
        /// </summary>
        public void ProjectIsAboutToChange()
        {
            Logger.Write("Project is about to change.", Severity.DEBUG);
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
            Logger.Write("Project is selected.", Severity.DEBUG);
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
            Logger.Write("Adding new language.", Severity.DEBUG);
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
            Logger.Write(string.Format("Removing {0} language from {1}.",
                lang.Language, lang.Folder), Severity.DEBUG);
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
            //// Callback function to react on the progress
            //// during analysis
            Progress<AnalysisProgress> progress = new Progress<AnalysisProgress>(ev =>
            {
                //Update the visual progress of the analysis.
                ProgressValue = Convert.ToInt32(ev.Percent);
                if (ev.FileName != null)
                {
                    Log = string.Format("{0} is ready!", ev.FileName);
                }
            });

            // Get the old project stats
            int oldKnownQty = Files.Sum(x => x.Known.GetValueOrDefault());
            int oldMaybeQty = Files.Sum(x => x.Maybe.GetValueOrDefault());
            ProgressValue = 0;
            Logger.Write("Requesting Project analysis.", Severity.DEBUG);

            await Task.Run(() => model.Analyze(progress));

            // Get the new project stats
            int newKnownQty = Files.Sum(x => x.Known.GetValueOrDefault());
            int newMaybeQty = Files.Sum(x => x.Maybe.GetValueOrDefault());
            // Update the visual progress.
            ProgressValue = 100;
            Log = string.Format(
                "Analysis is finished. Known: {0:+#;-#;0}, Maybe {1:+#;-#;0}", // Force sign, no sign for zero
                newKnownQty - oldKnownQty, newMaybeQty - oldMaybeQty);
            Logger.Write("Project analysis is ready.", Severity.DEBUG);
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

        // Commands
        public IAsyncCommand RunProject
        {
            get
            {
                return AsyncRelayCommand.Create(HandleAnalysis);
            }
        }

        public ICommand ShowHelp
        {
            get
            {
                return new RelayCommand(() =>
                {
                    MessageBox.Show(string.Format("{0}: {1}",
                        App.Current.Properties["appName"],
                        CoreAssembly.Version), "About");
                },
                () => true);
            }
        }

        public ICommand ExitApp
        {
            get
            {
                return new RelayCommand(() =>
                {
                    App.Current.Shutdown();
                },
                () => true);
            }
        }
    }
}
