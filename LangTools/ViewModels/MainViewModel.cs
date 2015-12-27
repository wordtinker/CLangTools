﻿using LangTools.Models;
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
        private MainModel model;
        private int totalWords;
        private int totalUnknown;
        private string log;
        private int progressValue;

        // Properties
        public ObservableCollection<LingvaViewModel> Languages { get; }
        public ObservableCollection<string> Projects { get; }
        public ObservableCollection<DictViewModel> Dictionaries { get; }
        public ObservableCollection<FileStatsViewModel> Files { get; }
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
        // Constructors
        public MainViewModel()
        {
            Logger.Write("MainView is starting.", Severity.DEBUG);
            model = new MainModel();

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
                var fsvm = new FileStatsViewModel(args.Content);
                Files.Add(fsvm);
                totalUnknown += fsvm.Unknown.GetValueOrDefault();
                TotalWords += fsvm.Size.GetValueOrDefault();
            };
            model.FileStatsRemoved += (obj, args) =>
            {
                var fsvm = new FileStatsViewModel(args.Content);
                Files.Remove(fsvm);
                totalUnknown -= fsvm.Unknown.GetValueOrDefault();
                TotalWords -= fsvm.Size.GetValueOrDefault();
            };

            model.InitializeLanguages();
            ProgressValue = 100;
            Logger.Write("MainView has started.", Severity.DEBUG);
        }

        // Methods
        public void LanguageIsAboutToChange()
        {
            Logger.Write("Language is about to change.", Severity.DEBUG);
            model.UnselectLanguage();
        }

        public void SelectLanguage(object item)
        {
            Logger.Write("Language is selected.", Severity.DEBUG);
            LingvaViewModel lang = (LingvaViewModel)item;
            // Let the model know that selected language changed
            model.SelectLanguage(lang.CurrentLanguage);
        }

        public void ProjectIsAboutToChange()
        {
            Logger.Write("Project is about to change.", Severity.DEBUG);
            model.UnselectProject();
        }

        public void SelectProject(object item)
        {
            Logger.Write("Project is selected.", Severity.DEBUG);
            string project = (string)item;
            // Let the model know that selected project changed
            model.SelectProject(project);
            // TODO: Update list of words related to project
        }

        public void AddNewLanguage(LingvaViewModel languageViewModel)
        {
            Lingva lang = languageViewModel.CurrentLanguage;
            model.AddNewLanguage(lang);
        }

        public void RemoveLanguage(LingvaViewModel languageViewModel)
        {
            Lingva lang = languageViewModel.CurrentLanguage;
            Logger.Write(string.Format("Removing {0} language from {1}.",
                lang.Language, lang.Folder), Severity.DEBUG);
            model.RemoveOldLanguage(lang);
        }

        // TODO
        //    // Disable controls
        //    languagesBox.IsEnabled = false;
        //    projectsBox.IsEnabled = false;
        //    // Enaable controls
        //    languagesBox.IsEnabled = true;
        //    projectsBox.IsEnabled = true;
        //}

        private async Task HandleAnalysis()
        {
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
        }

        private void UpdateTotalStats()
        {
            totalUnknown = Files.Sum(x => x.Unknown.GetValueOrDefault());
            TotalWords = Files.Sum(x => x.Size.GetValueOrDefault());
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