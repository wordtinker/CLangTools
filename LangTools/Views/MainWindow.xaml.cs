using LangTools.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LangTools
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            Logger.Write("Starting MainWindow.", Severity.DEBUG);
            InitializeComponent();
            // Fix the view so some language would be selected;
            languagesBox.SelectedIndex = 0;
            Logger.Write("MainWindow has started.", Severity.DEBUG);
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
                Logger.Write("Language box selection is fixed.", Severity.DEBUG);
                // if there are no languages to select "set;" will be ignored
                // and wont raise new SelectionChanged Event.
                languagesBox.SelectedIndex = 0;
            }
            // Work with valid language
            else
            {
                Logger.Write("Language box selection changed.", Severity.DEBUG);
                object item = languagesBox.SelectedItem;
                vm.SelectLanguage(item);
                projectsBox.SelectedIndex = 0;
            }
        }

        private void ProjectChanged(object sender, SelectionChangedEventArgs e)
        {
            MainViewModel vm = (MainViewModel)base.DataContext;
            vm.ProjectIsAboutToChange();
            // Ensure that one of the projects is always selected
            if (projectsBox.SelectedIndex == -1)
            {
                Logger.Write("Project box selection is fixed.", Severity.DEBUG);
                // if there are no project to select set; will be ignored
                // and wont raise new SelectionChanged Event.
                projectsBox.SelectedIndex = 0;
            }
            // Work with valid project
            else
            {
                Logger.Write("Project box selection changed.", Severity.DEBUG);
                object item = projectsBox.SelectedItem;
                vm.SelectProject(item);
            }
        }

        private void FileRowChanged(object sender, SelectionChangedEventArgs e)
        {
            MainViewModel vm = (MainViewModel)base.DataContext;
            vm.FileRowIsAboutToChange();
            DataGrid grid = (DataGrid)sender;
            FileStatsViewModel row = grid.SelectedItem as FileStatsViewModel;
            if (row != null)
            {
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

        private void FilesRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = (DataGridRow)sender;
            ((FileStatsViewModel)row.DataContext).OpenOutput();
        }

        private void FilesContextMenu_ClickOpenFile(object sender, RoutedEventArgs e)
        {
            var item = filesGrid.SelectedItem as FileStatsViewModel;
            if (item != null)
            {
                item.OpenFile();
            }
        }

        private void FilesContextMenu_ClickOpenOutput(object sender, RoutedEventArgs e)
        {
            var item = filesGrid.SelectedItem as FileStatsViewModel;
            if (item != null)
            {
                item.OpenOutput();
            }
        }

        private void FilesContextMenu_ClickDeleteFile(object sender, RoutedEventArgs e)
        {
            var item = filesGrid.SelectedItem as FileStatsViewModel;
            if (item != null)
            {
                item.DeleteFile();
            }
        }

        private void FilesContextMenu_ClickDeleteOutput(object sender, RoutedEventArgs e)
        {
            var item = filesGrid.SelectedItem as FileStatsViewModel;
            if (item != null)
            {
                item.DeleteOutput();
            }
        }

        private void DictsRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = (DataGridRow)sender;
            ((DictViewModel)row.DataContext).OpenFile();
        }

        private void DictContextMenu_ClickOpen(object sender, RoutedEventArgs e)
        {
            var item = dictsGrid.SelectedItem as DictViewModel;
            if (item != null)
            {
                item.OpenFile();
            }
        }

        private void DictContextMenu_ClickDelete(object sender, RoutedEventArgs e)
        {
            var item = dictsGrid.SelectedItem as DictViewModel;
            if (item != null)
            {
                item.DeleteFile();
            }
        }
    }
}
