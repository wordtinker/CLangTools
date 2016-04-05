using LangTools.Models;
using System.Collections.Generic;

namespace LangTools.Data
{
    public interface IStorage
    {
        bool LanguageExists(string language);
        bool FolderExists(string folder);
        void AddLanguage(Lingva lang);
        List<Lingva> GetLanguages();
        void RemoveLanguage(Lingva language);
        List<string> GetProjects(Lingva selectedLang);
        void RemoveProject(Lingva language, string project);
        List<FileStats> GetFilesStats(Lingva language, string project);
        void RemoveFileStats(FileStats file);
        void UpdateStats(FileStats stats);
        void CommitStats();
        void UpdateWords(string filePath, Dictionary<string, int> unknownWords);
        void CommitWords();
        Dictionary<string, int> GetUnknownWords(FileStats fs);
        Dictionary<string, int> GetUnknownWords(Lingva lang, string project);
        List<string> GetFilenamesWithWord(string word);
    }
}
