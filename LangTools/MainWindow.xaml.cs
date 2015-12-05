using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Storage storage;
        private ObservableCollection<Lingva> languages;
        private ObservableCollection<string> projects;
        FileSystemWatcher corpusWatcher;

        public MainWindow()
        {
            storage = (Storage)App.Current.Properties["storage"];
            InitializeComponent();
            InitializeCorpusWatcher();
            InitializeLanguageBox();
            Logger.Write("Main has started.", Severity.DEBUG);
        }

        private void InitializeCorpusWatcher()
        {
            corpusWatcher = new FileSystemWatcher();
            corpusWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            corpusWatcher.Filter = "*.*";
            // Remark : deleting of old projects from storage is postponed
            // until the next time LanguageChanged is called.

            // Have to use Dispatcher to execute Action in the UI thread
            corpusWatcher.Created += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() => 
                projects.Add(e.Name)));

            corpusWatcher.Deleted += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() =>
                projects.Remove(e.Name)));

            corpusWatcher.Renamed += (obj, e) => Dispatcher.BeginInvoke(
                DispatcherPriority.Send, new Action(() => {
                    projects.Remove(e.OldName);
                    projects.Add(e.Name);
                }));
        }

        private void InitializeLanguageBox()
        {
            languages = new ObservableCollection<Lingva>(storage.GetLanguages());
            languagesBox.ItemsSource = languages;
            languagesBox.SelectedIndex = 0;
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

        /// <summary>
        /// Responds to language changed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ensure that one of the languages is always selected
            if (languagesBox.SelectedIndex == - 1)
            {
                Logger.Write("Language box selection if fixed.", Severity.DEBUG);
                // if there are no languages to select "set;" will be ignored
                // and wont raise new SelectionChanged Event.
                projectsBox.ItemsSource = null;
                languagesBox.SelectedIndex = 0;
            }
            // Work with valid language
            else
            {
                Logger.Write("New language is chosen.", Severity.DEBUG);
                // Fetch the language
                Lingva selectedLang = (Lingva)languagesBox.SelectedItem;
                string corpusDir = Path.Combine(selectedLang.Folder, (string)App.Current.Properties["corpusDir"]);
                // Stop watching previous corpus folder.
                corpusWatcher.EnableRaisingEvents = false;
                // Load the list of projects into projects combobox
                List<string> projectsInDir;
                if (InitializeProjectBox(corpusDir, out projectsInDir))
                {
                    // Start watching new language corpus folder
                    corpusWatcher.Path = corpusDir;
                    corpusWatcher.EnableRaisingEvents = true;
                    // Remove old projects from DB
                    RemoveOldProjects(projectsInDir, selectedLang);
                }
            }
        }

        private bool InitializeProjectBox(string corpusDir, out List<string> projectsInDir)
        {
            //Remove old binding from combobox.
            projectsBox.ItemsSource = null;
            
            // Get every project from corpus directory
            Logger.Write(string.Format("Going to check {0} for projects.", corpusDir), Severity.DEBUG);
            try
            {
                DirectoryInfo di = new DirectoryInfo(corpusDir);
                projectsInDir = new List<string>(di.GetDirectories().Select(d => d.Name));
            }
            catch (Exception err)
            {
                // Do nothing but log and return
                Logger.Write(string.Format("Something is wrong during corpus access: {0}", err.ToString()));
                projectsInDir = new List<string>();
                return false;
            }
            // Add new binding
            projects = new ObservableCollection<string>(projectsInDir);
            projectsBox.ItemsSource = projects;
            projectsBox.SelectedIndex = 0;

            return true;
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
                // TODO:
            }
        }
    }
}
