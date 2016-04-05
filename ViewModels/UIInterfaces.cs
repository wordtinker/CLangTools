using System;
using System.Windows.Threading;

namespace LangTools.ViewModels
{
    public interface IUIBaseService
    {
        void ShowMessage(string message);
        bool Confirm(string message);
        DispatcherOperation BeginInvoke(Action method);
    }

    public interface IUIMainWindowService : IUIBaseService
    {
        string AppDir { get; }
        string AppName { get; }
        string CorpusDir { get; }
        string DicDir { get; }
        string OutDir { get; }
        void Shutdown();
    }
}
