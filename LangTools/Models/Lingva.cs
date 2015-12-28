using LangTools.DataAccess;

namespace LangTools.Models
{
    enum ValidationError{
        LANGNAMEEMPTY,
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

        // Validation logic
        public ValidationError ValidateLanguageName()
        {
            if (Language.Length == 0)
            {
                return ValidationError.LANGNAMEEMPTY;
            }

            Storage storage = (Storage)App.Current.Properties["storage"];
            if (storage.LanguageExists(Language))
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
