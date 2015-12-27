using System.IO;
using MicroMvvm;

namespace LangTools.Models
{
    class FileStats : ObservableObject
    {
        // Members
        string fileName;
        string filePath;
        Lingva lingva;
        string project;
                
        // Properties
        internal string FileName { get { return fileName; } }
        internal string FilePath { get { return filePath; } }
        internal Lingva Lingva { get { return lingva; } }
        internal string Project { get { return project; } }

        internal int? Size { get; set; }
        internal int? Known { get; set; }
        internal int? Maybe { get; set; }
        internal int? Unknown { get; set; }
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
                RaiseAllPropertiesChanged();
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
