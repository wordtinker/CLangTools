using LangTools.Models;

namespace LangTools.ViewModels
{
    /// <summary>
    /// Represents dictionary file.
    /// </summary>
    class DictViewModel
    {
        // Members
        private readonly Dict currentDictionary;

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
        public DictViewModel(Dict dictionary)
        {
            this.currentDictionary = dictionary;
        }

        // Methods
        public void OpenFile()
        {
            IOTools.OpenWithDefault(FilePath);
        }

        public void DeleteFile()
        {
            IOTools.DeleteFile(FilePath);
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
