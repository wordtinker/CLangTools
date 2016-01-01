using LangTools.DataAccess;

namespace LangTools.Models
{
    enum ValidationError{
        LANGNAMEEMPTY,
        LANGWITHSPACES,
        LANGTAKEN,
        FOLDERNAMEEMPTY,
        FOLDERTAKEN,
        NONE
    }

    class Lingva
    {
        // Properties
        public string Language { get; set; }
        public string Folder { get; set; }

        // Constructors
        public Lingva() {}

        public Lingva(Lingva other)
        {
            Language = other.Language;
            Folder = other.Folder;
        }

        // Validation logic
        public ValidationError ValidateLanguageName()
        {
            string lang = Language.Trim();
            if (lang.Length == 0)
            {
                return ValidationError.LANGNAMEEMPTY;
            }

            if (lang.Length != Language.Length)
            {
                return ValidationError.LANGWITHSPACES;
            }

            Storage storage = (Storage)App.Current.Properties["storage"];
            if (storage.LanguageExists(lang))
            {
                return ValidationError.LANGTAKEN;
            }

            return ValidationError.NONE;
        }

        public ValidationError ValidateLanguageFolder()
        {
            if (Folder.Length == 0)
            {
                return ValidationError.FOLDERNAMEEMPTY;
            }

            Storage storage = (Storage)App.Current.Properties["storage"];
            if (storage.FolderExists(Folder))
            {
                return ValidationError.FOLDERTAKEN;
            }

            return ValidationError.NONE;
        }

        // Equals ocerride
        public override bool Equals(object obj)
        {
            Lingva item = obj as Lingva;
            if (item == null)
            {
                return false;
            }

            return this.Folder == item.Folder;
        }

        public override int GetHashCode()
        {
            return Folder.GetHashCode();
        }
    }
}
