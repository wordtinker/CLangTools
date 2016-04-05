using LangTools.Shared;
using LangTools.ViewModels;
using LangTools.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LangTools
{
    public partial class MainWindow : Window
    {
        // Constructor
        public MainWindow()
        {
            MainWindowService service = new MainWindowService(this);
            this.DataContext = new MainViewModel(service);

            Log.Logger.Debug("Starting MainWindow.");

            InitializeComponent();
            // Fix the view so some language would be selected;
            languagesBox.SelectedIndex = 0;

            Log.Logger.Debug("MainWindow has started.");
        }

        /// <summary>
        /// Responds to language changed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            MainViewModel vm = (MainViewModel)base.DataContext;
            vm.LanguageIsAboutToChange();
            // Ensure that one of the languages is always selected
            if (languagesBox.SelectedIndex == - 1)
            {
                Log.Logger.Debug("Language box selection is fixed.");
                // if there are no languages to select "set;" will be ignored
                // and wont raise new SelectionChanged Event.
                languagesBox.SelectedIndex = 0;
            }
            // Work with valid language
            else
            {
                Log.Logger.Debug("Language box selection changed.");
                object item = languagesBox.SelectedItem;
                vm.SelectLanguage(item);
                projectsBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Responds to project changed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProjectChanged(object sender, SelectionChangedEventArgs e)
        {
            MainViewModel vm = (MainViewModel)base.DataContext;
            vm.ProjectIsAboutToChange();
            // Ensure that one of the projects is always selected
            if (projectsBox.SelectedIndex == -1)
            {
                Log.Logger.Debug("Project box selection is fixed.");
                // if there are no project to select set; will be ignored
                // and wont raise new SelectionChanged Event.
                projectsBox.SelectedIndex = 0;
            }
            // Work with valid project
            else
            {
                Log.Logger.Debug("Project box selection changed.");
                object item = projectsBox.SelectedItem;
                vm.SelectProject(item);
            }
        }

        /// <summary>
        /// Responds to rowChanged event in the dataGrid with files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileRowChanged(object sender, SelectionChangedEventArgs e)
        {
            MainViewModel vm = (MainViewModel)base.DataContext;
            vm.FileRowIsAboutToChange();
            DataGrid grid = (DataGrid)sender;
            FileStatsViewModel row = grid.SelectedItem as FileStatsViewModel;
            if (row != null)
            {
                // Let the viewModel update the list of words related to selected file.
                vm.ShowWords(row);
            }
        }

        ///// <summary>
        ///// Shows modal window to manage languages.
        ///// </summary>
        private void LanguagesManage_click(object sender, RoutedEventArgs e)
        {
            LangWindow dialog = new LangWindow();
            dialog.DataContext = base.DataContext;
            // Ensure the alt+tab is working properly.
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        /// <summary>
        /// Responds to DoubleClick even in the dataGrid with files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilesRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = (DataGridRow)sender;
            // Let the view model open the report file.
            ((FileStatsViewModel)row.DataContext).OpenOutput();
        }

        /// <summary>
        /// Responds to click event in the context menu of the dataGrid with files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilesContextMenu_ClickOpenFile(object sender, RoutedEventArgs e)
        {
            FileStatsViewModel item = filesGrid.SelectedItem as FileStatsViewModel;
            if (item != null)
            {
                // Let the view model open origin file.
                item.OpenFile();
            }
        }

        /// <summary>
        /// Responds to click event in the context menu of the dataGrid with files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilesContextMenu_ClickOpenOutput(object sender, RoutedEventArgs e)
        {
            FileStatsViewModel item = filesGrid.SelectedItem as FileStatsViewModel;
            if (item != null)
            {
                // Let the view model open report file.
                item.OpenOutput();
            }
        }

        /// <summary>
        /// Responds to click event in the context menu of the dataGrid with files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilesContextMenu_ClickDeleteFile(object sender, RoutedEventArgs e)
        {
            FileStatsViewModel item = filesGrid.SelectedItem as FileStatsViewModel;
            if (item != null)
            {
                // Let the view model delete the origin file.
                item.DeleteFile();
            }
        }

        /// <summary>
        /// Responds to click event in the context menu of the dataGrid with files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilesContextMenu_ClickDeleteOutput(object sender, RoutedEventArgs e)
        {
            FileStatsViewModel item = filesGrid.SelectedItem as FileStatsViewModel;
            if (item != null)
            {
                // Let the view model delete the report file.
                item.DeleteOutput();
            }
        }

        /// <summary>
        /// Responds to doubleClick event in the dataGrid with dictionaries.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DictsRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = (DataGridRow)sender;
            // Let the view model open the dictionary.
            ((DictViewModel)row.DataContext).OpenFile();
        }

        /// <summary>
        /// Responds to click event in the context menu of the dataGrid with dictionaries.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DictContextMenu_ClickOpen(object sender, RoutedEventArgs e)
        {
            DictViewModel item = dictsGrid.SelectedItem as DictViewModel;
            if (item != null)
            {
                // Let the view model open the dictionary.
                item.OpenFile();
            }
        }

        /// <summary>
        /// Responds to click event in the context menu of the dataGrid with dictionaries.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DictContextMenu_ClickDelete(object sender, RoutedEventArgs e)
        {
            DictViewModel item = dictsGrid.SelectedItem as DictViewModel;
            if (item != null)
            {
                // Let the view model delete the dicionary.
                item.DeleteFile();
            }
        }

        /// <summary>
        /// Responds to doubleClick event in the dataGrid with words.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WordRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            MainViewModel vm = (MainViewModel)base.DataContext;
            DataGridRow item = sender as DataGridRow;
            if (item != null)
            {
                // Let view model add the word into dictionary.
                vm.AddWordToDictionary((WordViewModel)item.DataContext);
            }
        }

        /// <summary>
        /// Responds to MouseDown event in the dataGrid with words.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WordRow_SingleClick(object sender, MouseButtonEventArgs e)
        {
            MainViewModel vm = (MainViewModel)base.DataContext;
            DataGridRow item = sender as DataGridRow;
            if (item != null)
            {
                // Let view model mark files to highlight.
                vm.HighlightFilesWithWord((WordViewModel)item.DataContext);
            }
        }
    }
}
