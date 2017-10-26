using Prism.Mvvm;

namespace LangTools.Models
{
    public class FileStats : BindableBase
    {
        // Properties
        public string FileName { get; private set; }
        public string FilePath { get; private set; }
        public Lingva Lingva { get; private set; }
        public string Project { get; private set; }

        public int? Size { get; set; }
        public int? Known { get; set; }
        public int? Maybe { get; set; }
        public int? Unknown { get; set; }
        public string OutPath { get; internal set; }

        // Constructor
        public FileStats(string fileName, string filePath, Lingva language, string project)
        {
            FileName = fileName;
            FilePath = filePath;
            Lingva = language;
            Project = project;
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
                RaisePropertyChanged(string.Empty);
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
