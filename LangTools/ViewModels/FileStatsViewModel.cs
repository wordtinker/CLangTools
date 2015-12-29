using LangTools.Models;
using MicroMvvm;

namespace LangTools.ViewModels
{
    class FileStatsViewModel : ObservableObject
    {
        private FileStats fileStats;

        public FileStats FileStats { get { return fileStats; } }
        public string FileName { get { return fileStats.FileName; } }
        public string FilePath { get { return fileStats.FilePath; } }
        public Lingva Lingva { get { return fileStats.Lingva; } }
        public string Project { get { return fileStats.Project; } }
        public int? Size { get { return fileStats.Size; } }
        public int? Known { get { return fileStats.Known; } }
        public double? KnownPercent
        {
            get
            {
                return ConvertToPercent(Known, Size);
            }
        }
        public int? Maybe { get { return fileStats.Maybe; } }
        public double? MaybePercent
        {
            get
            {
                return ConvertToPercent(Maybe, Size);
            }
        }
        public int? Unknown { get { return fileStats.Unknown; } }
        public double? UnknownPercent
        {
            get
            {
                return ConvertToPercent(Unknown, Size);
            }
        }
        public string OutPath { get { return fileStats.OutPath; } }

        public FileStatsViewModel(FileStats fileStats)
        {
            this.fileStats = fileStats;
            this.fileStats.PropertyChanged += (obj, e) =>
            {
                RaiseAllPropertiesChanged();
            };
        }

        private double? ConvertToPercent(int? dividend, int? divisor)
        {
            if (divisor == null || divisor == 0 || dividend == null)
            {
                return null;
            }

            return (double)dividend / divisor;
        }

        public void OpenOutput()
        {
            IOTools.OpenWithDefault(OutPath);
        }

        public void OpenFile()
        {
            IOTools.OpenWithDefault(FilePath);
        }

        public void DeleteOutput()
        {
            IOTools.DeleteFile(OutPath);
        }

        public void DeleteFile()
        {
            //// Model will be updated after FileWatcher catches the event
            IOTools.DeleteFile(FilePath);
            // Delete output file together
            IOTools.DeleteFile(OutPath);
        }

        public override bool Equals(object obj)
        {
            FileStatsViewModel item = obj as FileStatsViewModel;
            if (item == null)
            {
                return false;
            }

            return this.fileStats.Equals(item.fileStats);

        }

        public override int GetHashCode()
        {
            return fileStats.GetHashCode();
        }
    }
}
