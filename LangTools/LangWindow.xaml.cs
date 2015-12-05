using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
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
using System.IO;

namespace LangTools
{
    /// <summary>
    /// Interaction logic for LangWindow.xaml
    /// </summary>
    public partial class LangWindow : Window
    {
        private Storage storage;
        private ObservableCollection<Lingva> languages;

        public LangWindow()
        {
            storage = (Storage)App.Current.Properties["storage"];
            InitializeComponent();
            InitializeData();
        }

        /// <summary>
        /// Draws the existing languages into table.
        /// </summary>
        private void InitializeData()
        {
            languages = new ObservableCollection<Lingva>(storage.GetLanguages());
            languagesGrid.ItemsSource = languages;
        }

        /// <summary>
        /// After validation adds new language to DB and table. Creates subfolder
        /// inside new language folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddBtn_click(object sender, RoutedEventArgs e)
        {
            string lang = langEdit.Text;
            string folder = folderEdit.Text;

            Logger.Write(string.Format("Adding {0} language to {1}.", lang, folder), Severity.DEBUG);

            if (ValidateFields())
            {
                // Add new lang to DB and datagrid.
                Lingva newLang = storage.AddLanguage(lang, folder);
                languages.Add(newLang);
                // Clear text controls.
                langEdit.Clear();
                folderEdit.Clear();
                EnsureDirectoryStructure(newLang.Folder);
            }
        }

        private void EnsureDirectoryStructure(string directory)
        {
            // Define subfolders names
            string corpusDir = Path.Combine(directory, (string)App.Current.Properties["corpusDir"]);
            string dicDir = Path.Combine(directory, (string)App.Current.Properties["dicDir"]);
            string outputDir = Path.Combine(directory, (string)App.Current.Properties["outputDir"]);

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
                Logger.Write(msg);
                MessageBox.Show(msg);
            }

            // TODO: emit signal.
        }

        /// <summary>
        /// Validates fields for new language.
        /// </summary>
        /// <returns></returns>
        private bool ValidateFields()
        {
            if (folderEdit.Text.Length == 0)
            {
                MessageBox.Show("Select the folder.");
                return false;
            }
            if (langEdit.Text.Length == 0)
            {
                MessageBox.Show("Name the language.");
                return false;
            }
            if (storage.LanguageExists(langEdit.Text))
            {
                MessageBox.Show("Language is already in the database.");
                return false;
            }
            if (storage.FolderExists(folderEdit.Text))
            {
                MessageBox.Show("Folder name is already taken.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Removes the language from DB and from lang table.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveBtn_click(object sender, RoutedEventArgs e)
        {
            object item = languagesGrid.SelectedItem;
            if (item != null)
            {
                Lingva language = (Lingva)item;

                Logger.Write(string.Format("Removing {0} language from {1}.", language.Language, language.Folder), Severity.DEBUG);

                storage.RemoveLanguage(language);

                languages.Remove(language);
                
                // TODO: emit event
            }
        }

        /// <summary>
        /// Runs standard select Folder dialog, stores selected folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FolderBtn_click(object sender, RoutedEventArgs e)
        {
            // Have to use windows forms.
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string dirName = dialog.SelectedPath;

                Logger.Write(string.Format("Selected new folder for language: {0}", dirName), Severity.DEBUG);

                folderEdit.Text = dirName;
            }
        }
    }
}
