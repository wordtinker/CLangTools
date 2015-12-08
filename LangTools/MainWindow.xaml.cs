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

    public class OutLinkConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fs = (FileStats)value;

            string outName = Path.ChangeExtension(fs.FileName, ".html");
            string outPath = Path.Combine(fs.Lingva.Folder, (string)App.Current.Properties["outputDir"],
                                          fs.Project, outName);
            if (File.Exists(outPath))
            {
                return outPath;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Can't restore from URI.
            throw new NotImplementedException();
        }
    }

    public class LinkNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            OutLinkConverter conv = new OutLinkConverter();
            if (conv.Convert(value, targetType, parameter, culture) != null)
            {
                var fs = (FileStats)value;
                return Path.ChangeExtension(fs.FileName, ".html");
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Can't restore from LinkName
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
                    FileName = e.Name,
                    DictType = DictType.Project.ToString(),
                    FilePath = e.FullPath // TODO: Simplify, test
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
                        FileName = e.OldName,
                        DictType = DictType.Project.ToString(),
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
                    FileName = e.Name,
                    DictType = DictType.General.ToString(),
                    FilePath = e.FullPath // TODO: Simplify, test
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
                        FileName = e.OldName,
                        DictType = DictType.General.ToString(),
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
                files.Add(new FileStats {
                    FileName = e.Name,
                    FilePath = e.FullPath,
                    Lingva = (Lingva)languagesBox.SelectedItem,
                    Project = (string)projectsBox.SelectedItem
                })));

            filesWatcher.Deleted += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                files.Remove(new FileStats
                {
                    FileName = e.Name,
                    FilePath = e.FullPath, // TODO: Simplify
                    Lingva = (Lingva)languagesBox.SelectedItem,
                    Project = (string)projectsBox.SelectedItem
                })));

            filesWatcher.Renamed += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                {
                    files.Add(new FileStats
                    {
                        FileName = e.Name,
                        FilePath = e.FullPath,
                        Lingva = (Lingva)languagesBox.SelectedItem,
                        Project = (string)projectsBox.SelectedItem
                    });
                    files.Remove(new FileStats
                    {
                        FileName = e.OldName,
                        FilePath = e.OldFullPath, // TODO: Simplify
                        Lingva = (Lingva)languagesBox.SelectedItem,
                        Project = (string)projectsBox.SelectedItem
                    });
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
                List<string> projectsInDir;
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

        private void RemoveOldProjects(List<string> projectsInDir, Lingva selectedLang)
        {
            // Get known projects from DB
            List<string> projectsInDB = storage.GetProjects(selectedLang);
            // Find projects left only in DB
            foreach (var leftover in projectsInDB.Except(projectsInDir))
            {
                // Remove leftover projects
                Logger.Write(string.Format("Deleting project:{0} - {1}", selectedLang.Language, leftover), Severity.DEBUG);
                storage.RemoveProject(selectedLang.Language, leftover);
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
            List<string> fileNames;
            if (!IOTools.ListFiles(filesDir, out fileNames))
            {
                // Can't reach the folder, no sense to proceed further.
                return;
            }

            // Create FileStats object for every file name
            IEnumerable<FileStats> inDir = fileNames.Select(fName => new FileStats
            {
                FileName = fName,
                FilePath = Path.Combine(filesDir, fName),
                Lingva = lang,
                Project = projectName
            });

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
            List<string> projectSpecificDics;
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
            List<string> generalDics;
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

        private void OpenDict(object sender, RoutedEventArgs e)
        {
            // Cant cast directly, Selected item will be null if nothing is selected
            Dict d = dictsGrid.SelectedItem as Dict;
            if (d != null)
            {
                IOTools.OpenWithDefaul(d.FilePath);
            }
        }

        private void DeleteDict(object sender, RoutedEventArgs e)
        {
            Dict d = dictsGrid.SelectedItem as Dict;
            if (d != null)
            {
                IOTools.DeleteFile(d.FilePath);
            }
        }
    }
}
