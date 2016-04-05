
namespace LangTools.Models
{
    public enum DictType
    {
        Project,
        General
    }

    public class Dict
    {
        // Properties
        public string FileName { get; set; }
        public DictType DictType { get; set; }
        public string FilePath { get; set; }

        // Override Equals
        public override bool Equals(object obj)
        {
            Dict item = obj as Dict;
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
