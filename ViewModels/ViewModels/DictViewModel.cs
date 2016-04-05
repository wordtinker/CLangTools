using LangTools.Models;
using LangTools.Shared;

namespace LangTools.ViewModels
{
    /// <summary>
    /// Represents dictionary file.
    /// </summary>
    public class DictViewModel
    {
        // Members
        private readonly Dict currentDictionary;
        private IUIBaseService windowService;

        // Properties
        public string FileName
        {
            get { return currentDictionary.FileName; }
            
        }

        public DictType DictType
        {
            get { return currentDictionary.DictType; }
        }

        public string FilePath
        {
             get { return currentDictionary.FilePath;  }
        }

        // Constructors
        public DictViewModel(IUIBaseService windowService, Dict dictionary)
        {
            this.windowService = windowService;
            this.currentDictionary = dictionary;
        }

        // Methods
        public void OpenFile()
        {
            if (!IOTools.OpenWithDefault(FilePath))
            {
                windowService.ShowMessage(string.Format("Can't open {0}.", FilePath));
            }
        }

        public void DeleteFile()
        {
            if (windowService.Confirm(string.Format("Do you want to delete\n {0} ?", FilePath)))
            {
                if (!IOTools.DeleteFile(FilePath))
                {
                    windowService.ShowMessage(string.Format("Can't delete {0}.", FilePath));
                }
            }
        }

        // Equals implementation
        public override bool Equals(object obj)
        {
            DictViewModel item = obj as DictViewModel;
            if (item == null)
            {
                return false;
            }

            return this.currentDictionary.Equals(item.currentDictionary);

        }

        public override int GetHashCode()
        {
            return currentDictionary.GetHashCode();
        }

    }
}
