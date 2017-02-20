using System.Configuration;

namespace LangTools
{
    public static class Tools
    {
        public static string ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key] ?? string.Empty;
                return result;
            }
            catch (ConfigurationErrorsException)
            {
                return string.Empty;
            }
        }
    }
}
