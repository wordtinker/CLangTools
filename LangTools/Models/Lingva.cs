using LangTools.DataAccess;
using System;
using System.ComponentModel;

namespace LangTools.Models
{
    class Lingva : IDataErrorInfo
    {
        // Properties
        public string Language { get; set; }
        public string Folder { get; set; }

        // Validation logic
        // TODO: Later make it ENUM, text in ViewModel
        private string ValidateLanguageName()
        {
            if (Language.Length == 0)
            {
                return "Language name can't be empty.";
            }

            Storage storage = (Storage)App.Current.Properties["storage"];
            if (storage.LanguageExists(Language))
            {
                return "Language is already in the database.";
            }

            return string.Empty;
        }

        private string ValidateLanguageFolder()
        {
            if (Folder.Length == 0)
            {
                return "Select the folder.";
            }

            Storage storage = (Storage)App.Current.Properties["storage"];
            if (storage.FolderExists(Folder))
            {
                return "Folder name is already taken.";
            }

            return string.Empty;
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

        // IDataErrorInfo interface
        public string Error
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string this[string propertyName]
        {
            get
            {
                string validationResult = null;
                switch (propertyName)
                {
                    case "Language":
                        validationResult = ValidateLanguageName();
                        break;
                    case "Folder":
                        validationResult = ValidateLanguageFolder();
                        break;
                    default:
                        throw new ApplicationException("Unknown Property being validated on Product.");
                }
                return validationResult;
            }
        }


    }
}
