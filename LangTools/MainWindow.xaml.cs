using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace LangTools
{
    public class PercentageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int? divisor = (int?)values[0];
            int? dividend = (int?)values[1];
            if (divisor == null || divisor == 0 || dividend == null)
            {
                return null;
            }

            return string.Format("{0:F}", (double)dividend / divisor);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Can't restore from percent.
            throw new NotImplementedException();
        }
    }

    public class SumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int sum = 0;
            foreach (FileStats item in (ItemCollection)value)
            {
                sum += item.Size.GetValueOrDefault();
            }
            
            return sum;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TotalPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int unknown = 0;
            int total = 0;
            foreach (FileStats item in (ItemCollection)value)
            {
                total += item.Size.GetValueOrDefault();
                unknown += item.Unknown.GetValueOrDefault();
            }
            return string.Format("{0:F}", total == 0 ? 0 : (double)unknown / total);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Storage storage;
        private ObservableCollection<Lingva> languages;
        private ObservableCollection<Dict> dicts;
        private ObservableCollection<string> projects;
        private ObservableCollection<FileStats> files;
        private FileSystemWatcher corpusWatcher;
        private FileSystemWatcher specDictWatcher;
        private FileSystemWatcher genDictWatcher;
        private FileSystemWatcher filesWatcher;

        public MainWindow()
        {
            storage = (Storage)App.Current.Properties["storage"];
            InitializeComponent();
            InitializeWatchers();
            InitializeLanguageBox();
            Logger.Write("Main has started.", Severity.DEBUG);
        }

        private void InitializeWatchers()
        {
            // Corpus Watcher
            corpusWatcher = new FileSystemWatcher();
            corpusWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            corpusWatcher.Filter = "*.*";
            // Remark : deleting of old projects from storage is postponed
            // until the next time LanguageChanged is called.
            // TODO: Later.

            // Have to use Dispatcher to execute Action in the UI thread
            corpusWatcher.Created += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() => 
                projects.Add(e.Name)));

            corpusWatcher.Deleted += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                projects.Remove(e.Name)));

            corpusWatcher.Renamed += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() => 
                {
                    projects.Remove(e.OldName);
                    projects.Add(e.Name);
                }
                ));

            // Project specific dictionaries Watcher
            specDictWatcher = new FileSystemWatcher();
            specDictWatcher.NotifyFilter = NotifyFilters.FileName;
            specDictWatcher.Filter = "*.txt";

            specDictWatcher.Created += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                dicts.Add(new Dict {
                    FileName = e.Name,
                    DictType = DictType.Project.ToString(),
                    FilePath = e.FullPath
                })));

            specDictWatcher.Deleted += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                dicts.Remove(new Dict {
                    FilePath = e.FullPath
                })));

            specDictWatcher.Renamed += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                {
                    dicts.Add(new Dict {
                        FileName = e.Name,
                        DictType = DictType.Project.ToString(),
                        FilePath = e.FullPath
                    });
                    dicts.Remove(new Dict {
                        FilePath = e.OldFullPath
                    });
                }
                ));

            // General dictionaries Watcher
            genDictWatcher = new FileSystemWatcher();
            genDictWatcher.NotifyFilter = NotifyFilters.FileName;
            genDictWatcher.Filter = "*.txt";

            genDictWatcher.Created += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                dicts.Add(new Dict {
                    FileName = e.Name,
                    DictType = DictType.General.ToString(),
                    FilePath = e.FullPath
                })));

            genDictWatcher.Deleted += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                dicts.Remove(new Dict {
                    FilePath = e.FullPath
                })));

            genDictWatcher.Renamed += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                {
                    dicts.Add(new Dict {
                        FileName = e.Name,
                        DictType = DictType.General.ToString(),
                        FilePath = e.FullPath
                    });
                    dicts.Remove(new Dict {
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

            filesWatcher.Created += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                files.Add(new FileStats(
                    e.Name,
                    e.FullPath,
                    (Lingva)languagesBox.SelectedItem,
                    (string)projectsBox.SelectedItem
                    ))));

            filesWatcher.Deleted += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                files.Remove(new FileStats(
                    e.Name,
                    e.FullPath,
                    (Lingva)languagesBox.SelectedItem,
                    (string)projectsBox.SelectedItem
                    ))));

            filesWatcher.Renamed += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                {
                    files.Add(new FileStats(
                        e.Name,
                        e.FullPath,
                        (Lingva)languagesBox.SelectedItem,
                        (string)projectsBox.SelectedItem
                        ));
                    files.Remove(new FileStats(
                        e.OldName,
                        e.OldFullPath,
                        (Lingva)languagesBox.SelectedItem,
                        (string)projectsBox.SelectedItem
                        ));
                }
                ));
        }

        private void InitializeLanguageBox()
        {
            languages = new ObservableCollection<Lingva>(storage.GetLanguages());
            languagesBox.ItemsSource = languages;
            languagesBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Responds to language changed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            //Remove old binding from projects and project combobox
            projectsBox.ItemsSource = null;
            projects = null;
            // Stop watching previous corpus folder. Have to do stop watcher
            // from raising event that lead to insertion into Null project list.
            corpusWatcher.EnableRaisingEvents = false;

            // Ensure that one of the languages is always selected
            if (languagesBox.SelectedIndex == - 1)
            {
                Logger.Write("Language box selection if fixed.", Severity.DEBUG);
                // if there are no languages to select "set;" will be ignored
                // and wont raise new SelectionChanged Event.
                languagesBox.SelectedIndex = 0;
            }
            // Work with valid language
            else
            {
                Logger.Write("New language is chosen.", Severity.DEBUG);
                // Fetch the language
                Lingva selectedLang = (Lingva)languagesBox.SelectedItem;
                string corpusDir = Path.Combine(selectedLang.Folder, (string)App.Current.Properties["corpusDir"]);
                // Load the list of projects into projects combobox
                IEnumerable<string> projectsInDir;
                if (IOTools.ListDirectories(corpusDir, out projectsInDir))
                {
                    // Add new binding
                    projects = new ObservableCollection<string>(projectsInDir);
                    projectsBox.ItemsSource = projects;
                    projectsBox.SelectedIndex = 0;
                    // Start watching new language corpus folder
                    corpusWatcher.Path = corpusDir;
                    corpusWatcher.EnableRaisingEvents = true;
                    // Remove old projects from DB
                    RemoveOldProjects(projectsInDir, selectedLang);
                }
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

        private void ProjectChanged(object sender, SelectionChangedEventArgs e)
        {
            // Remove old bindings from dictionaries
            dicts = null;
            dictsGrid.ItemsSource = null;
            // Stop watching both dict folders
            specDictWatcher.EnableRaisingEvents = false;
            genDictWatcher.EnableRaisingEvents = false;
            // Remove old bindings from files
            files = null;
            filesGrid.ItemsSource = null;
            // Stop watching files folder
            filesWatcher.EnableRaisingEvents = false;

            // Ensure that one of the projects is always selected
            if (projectsBox.SelectedIndex == -1)
            {
                Logger.Write("Project box selection if fixed.", Severity.DEBUG);
                // if there are no project to select set; will be ignored
                // and wont raise new SelectionChanged Event.
                projectsBox.SelectedIndex = 0;
            }
            // Work with valid project
            else
            {
                Logger.Write("New project is chosen.", Severity.DEBUG);
                // Update list of dictionaries related to project
                RedrawDictionaries();
                // TODO:
                // Update list of words related to project

                // Update list of project files
                RedrawFiles();
            }
        }

        /// <summary>
        /// Redraws list of files for chosen project.
        /// </summary>
        private void RedrawFiles()
        {
            string projectName = (string)projectsBox.SelectedItem;
            Lingva lang = (Lingva)languagesBox.SelectedItem;

            // Get file names from project dir
            string filesDir = Path.Combine(lang.Folder, (string)App.Current.Properties["corpusDir"], projectName);
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
                lang,
                projectName
                ));

            // Get list of objects from DB
            List<FileStats> inDB = storage.GetFilesStats(lang, projectName);

            // Set the new binding.
            // NB: inDB.Intersect will return elements from inDB. Need this order since they have more information.
            files = new ObservableCollection<FileStats>(inDB.Intersect(inDir));
            filesGrid.ItemsSource = files;
            // Start watching files in the project.
            filesWatcher.Path = filesDir;
            filesWatcher.EnableRaisingEvents = true;

            // Add files that we have in dir but no stats in DB
            foreach (FileStats item in inDir.Except(inDB))
            {
                files.Add(item);
            }

            // Remove leftover stats from DB.
            foreach (FileStats item in inDB.Except(inDir))
            {
                Logger.Write(string.Format("Going to remove {0} from DB", item.FileName), Severity.DEBUG);
                storage.RemoveFileStats(item);
            }
        }

        /// <summary>
        /// Redraws list of dictionaries used in project.
        /// </summary>
        private void RedrawDictionaries()
        {
            string projectName = (string)projectsBox.SelectedItem;
            Lingva lang = (Lingva)languagesBox.SelectedItem;

            // Get custom project dictionaries
            string dictionariesDir = Path.Combine(lang.Folder, (string)App.Current.Properties["dicDir"], projectName);
            dicts = new ObservableCollection<Dict>();
            IEnumerable<string> projectSpecificDics;
            if (IOTools.ListFiles(dictionariesDir, out projectSpecificDics))
            {
                // Add found dictionaries to dict collection.
                foreach (string fName in projectSpecificDics)
                {
                    dicts.Add(new Dict {
                        FileName = fName,
                        DictType = DictType.Project.ToString(),
                        FilePath = Path.Combine(dictionariesDir, fName)
                    });
                }
                // Start watching for new specific dict files.
                specDictWatcher.Path = dictionariesDir;
                specDictWatcher.EnableRaisingEvents = true;
            }

            // Get general project dictionaries.
            string generalDir = Path.Combine(lang.Folder, (string)App.Current.Properties["dicDir"]);
            IEnumerable<string> generalDics;
            if (IOTools.ListFiles(generalDir, out generalDics))
            {
                // Combine both specific and general dictionaries
                foreach (string fName in generalDics)
                {
                    dicts.Add(new Dict {
                        FileName = fName,
                        DictType = DictType.General.ToString(),
                        FilePath = Path.Combine(generalDir, fName)
                    });
                }
                // Start watching for general dictionaries.
                genDictWatcher.Path = generalDir;
                genDictWatcher.EnableRaisingEvents = true;
            }
            // Set the new binding
            dictsGrid.ItemsSource = dicts;
        }

        /// <summary>
        /// Manages project analysis and shows progress dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RunProject(object sender, RoutedEventArgs e)
        {
            Lingva lang = languagesBox.SelectedItem as Lingva;
            string project = projectsBox.SelectedItem as string;

            if (project == null || lang == null || files.Count == 0 || dicts.Count == 0)
            {
                // Nothing to analyze here
                return;
            }

            Logger.Write("Project analysis has started.", Severity.DEBUG);

            // Disable controls
            languagesBox.IsEnabled = false;
            projectsBox.IsEnabled = false;
            runBtn.IsEnabled = false;

            // Ensure directory structure, dict and output project specific dirs
            // are misssing on first run.
            EnsureDirectoryStructure(lang.Folder, project);

            // Get the old project stats
            int oldKnownQty = files.Sum(x => x.Known.GetValueOrDefault());
            int oldMaybeQty = files.Sum(x => x.Maybe.GetValueOrDefault());

            // Create object that handle analysis.
            Analyzer worker = new Analyzer(lang.Language);
            worker.AddFiles(files.Select(f => f.FilePath));
            worker.AddDictionaries(dicts.Select(d => d.FilePath));

            // Create printer that will print analysis
            Printer printer = new Printer(lang.Language);
            // Callback function to react on new Filestats
            // during analysis
            var progress = new Progress<RunProgress>(ev =>
            {
                // Update the visual progress of the analysis.
                projectProgress.Value = ev.Percent;
                log.Text = ev.Message;

                if (ev.Data != null)
                {
                    // compare old and new stats
                    FileStats newStats = ev.Data.Stats;
                    FileStats oldStats = files[files.IndexOf(newStats)];
                    if (oldStats.Update(newStats))
                    {
                        // Produce new output page
                        printer.Print(oldStats.FileName, oldStats.Project,
                            lang.Folder, ev.Data.Tokens);
                        // Update stats in the DB
                        storage.UpdateStats(oldStats);
                        // Add new word list to DB
                        storage.UpdateWords(oldStats.FilePath, ev.Data.UnknownWords);
                    }
                }
            });

            await Task.Run(() => worker.Run(progress));

            // Commit changes to DB
            storage.CommitStats();
            storage.CommitWords();

            // Get the new project stats
            int newKnownQty = files.Sum(x => x.Known.GetValueOrDefault());
            int newMaybeQty = files.Sum(x => x.Maybe.GetValueOrDefault());

            // Update the visual progress.
            projectProgress.Value = 100;
            log.Text = string.Format(
                "Analysis is finished. Known: {0:+#;-#;0}, Maybe {1:+#;-#;0}", // Force sign, no sign for zero
                newKnownQty - oldKnownQty, newMaybeQty - oldMaybeQty);
            // Enaable controls
            languagesBox.IsEnabled = true;
            projectsBox.IsEnabled = true;
            runBtn.IsEnabled = true;
        }

        private void EnsureDirectoryStructure(string langPath, string project)
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
                // Not a critical error, could be fixed later.
                string msg = string.Format("Something is wrong during subfolder creation: {0}", e.ToString());
                Logger.Write(msg);
            }
        }

        private void FileExit_click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Shows modal window to manage languages.
        /// </summary>
        private void LanguagesManage_click(object sender, RoutedEventArgs e)
        {
            LangWindow dialog = new LangWindow(languages);
            // Ensure the alt+tab is working properly.
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void HelpAbout_click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(string.Format("{0}: {1}",
                App.Current.Properties["appName"],
                CoreAssembly.Version), "About");
        }

        private void FilesRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = (DataGridRow) sender;
            FileStats fs = (FileStats) row.DataContext;
            IOTools.OpenWithDefault(fs.OutPath);
        }

        private void FilesContextMenu_ClickOpenFile(object sender, RoutedEventArgs e)
        {
            FileStats stats = filesGrid.SelectedItem as FileStats;
            if (stats == null) return;
            IOTools.OpenWithDefault(stats.FilePath);
        }

        private void FilesContextMenu_ClickOpenOutput(object sender, RoutedEventArgs e)
        {
            FileStats stats = filesGrid.SelectedItem as FileStats;
            if (stats == null) return;
            IOTools.OpenWithDefault(stats.OutPath);
        }

        private void FilesContextMenu_ClickDeleteFile(object sender, RoutedEventArgs e)
        {
            FileStats stats = filesGrid.SelectedItem as FileStats;
            if (stats == null) return;
            // GridView will be updated after FileWatcher catches the event
            IOTools.DeleteFile(stats.FilePath);
            // Delete output file together
            IOTools.DeleteFile(stats.OutPath);
        }

        private void FilesContextMenu_ClickDeleteOutput(object sender, RoutedEventArgs e)
        {
            FileStats stats = filesGrid.SelectedItem as FileStats;
            if (stats == null) return;
            IOTools.DeleteFile(stats.OutPath);
        }

        private void DictsRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = (DataGridRow) sender;
            Dict d = (Dict) row.DataContext;
            IOTools.OpenWithDefault(d.FilePath);
        }

        private void DictContextMenu_ClickOpen(object sender, RoutedEventArgs e)
        {
            Dict d = dictsGrid.SelectedItem as Dict;
            if (d == null) return;
            IOTools.OpenWithDefault(d.FilePath);
        }

        private void DictContextMenu_ClickDelete(object sender, RoutedEventArgs e)
        {
            Dict d = dictsGrid.SelectedItem as Dict;
            if (d == null) return;
            // GridView will be updated after FileWatcher catches the event
            IOTools.DeleteFile(d.FilePath);
        }
    }
}
