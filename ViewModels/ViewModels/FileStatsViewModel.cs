using LangTools.Models;
using LangTools.Shared;
using Prism.Mvvm;

namespace LangTools.ViewModels
{
    /// <summary>
    /// Represents statistical data of the text file.
    /// </summary>
    public class FileStatsViewModel : BindableBase
    {
        private FileStats fileStats;
        private bool highlighted;
        private IUIBaseService windowService;

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
        public bool Highlighted
        {
            get { return highlighted; }
            set
            {
                SetProperty(ref highlighted, value);
            }
        }

        public FileStatsViewModel(IUIBaseService windowService, FileStats fileStats)
        {
            this.windowService = windowService;
            this.fileStats = fileStats;
            this.fileStats.PropertyChanged += (obj, e) =>
            {
                // Raise all properties changed
                RaisePropertyChanged(string.Empty);
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
            if (OutPath != null)
            {
                if (!IOTools.OpenWithDefault(OutPath))
                {
                    windowService.ShowMessage(string.Format("Can't open {0}.", OutPath));
                }
            }
        }

        public void OpenFile()
        {
            if (!IOTools.OpenWithDefault(FilePath))
            {
                windowService.ShowMessage(string.Format("Can't open {0}.", FilePath));
            }
        }

        public void DeleteOutput()
        {
            if (OutPath != null && windowService.Confirm(string.Format("Do you want to delete\n {0} ?", OutPath)))
            {
                if (!IOTools.DeleteFile(OutPath))
                {
                    windowService.ShowMessage(string.Format("Can't delete {0}.", OutPath));
                }
            }
        }

        public void DeleteFile()
        {
            // Model will be updated after FileWatcher catches the event
            if (windowService.Confirm(string.Format("Do you want to delete\n {0} ?", FilePath)))
            {
                if (!IOTools.DeleteFile(FilePath))
                {
                    windowService.ShowMessage(string.Format("Can't delete {0}.", FilePath));
                }
            }
            // Delete output file together
            DeleteOutput();
        }

        // Equals implementation
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
