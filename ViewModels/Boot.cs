using LangTools.Data;

namespace LangTools.ViewModels
{
    public static class VMBoot
    {
        public static bool IsReadyToLoad(string appDir)
        {
            // Ensure we have db file to store data.
            return Storage.CreateFile(appDir);
        }
    }
}
