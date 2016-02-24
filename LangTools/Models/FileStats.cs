using System.IO;
using Prism.Mvvm;

namespace LangTools.Models
{
    class FileStats : BindableBase
    {
        // Members
        private string fileName;
        private string filePath;
        private Lingva lingva;
        private string project;
                
        // Properties
        public string FileName { get { return fileName; } }
        public string FilePath { get { return filePath; } }
        public Lingva Lingva { get { return lingva; } }
        public string Project { get { return project; } }

        public int? Size { get; set; }
        public int? Known { get; set; }
        public int? Maybe { get; set; }
        public int? Unknown { get; set; }
        public string OutPath
        {
            get
            {
                string outName = Path.ChangeExtension(FileName, ".html");
                string outPath = Path.Combine(
                    Lingva.Folder,
                    (string)App.Current.Properties["outputDir"],
                    Project,
                    outName);
                return outPath;
            }
        }

        // Constructor
        public FileStats(string fileName, string filePath, Lingva language, string project)
        {
            this.fileName = fileName;
            this.filePath = filePath;
            this.lingva = language;
            this.project = project;
        }

        // Methods
        public bool Update(int newSize, int newKnown, int newMaybe)
        {
            if (this.Size != newSize || this.Known != newKnown ||
                this.Maybe != newMaybe)
            {
                this.Size = newSize;
                this.Known = newKnown;
                this.Maybe = newMaybe;
                this.Unknown = newSize - newKnown - newMaybe;
                // Raise All properties changed
                OnPropertyChanged(string.Empty);
                return true;
            }

            return false;
        }

        // Overrided Equals
        public override bool Equals(object obj)
        {
            FileStats item = obj as FileStats;

            if (item == null)
            {
                return false;
            }

            return this.FilePath == item.FilePath;
        }

        public override int GetHashCode()
        {
            return FilePath.GetHashCode();
        }
    }
}
