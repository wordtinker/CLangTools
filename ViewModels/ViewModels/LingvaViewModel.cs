using LangTools.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Prism.Mvvm;

namespace LangTools.ViewModels
{
    /// <summary>
    /// Represents language.
    /// </summary>
    public class LingvaViewModel : BindableBase, IDataErrorInfo
    {
        // Members
        private readonly Lingva currentLanguage;
        // Used for IDataErrorInfo
        private Dictionary<string, bool> validProperties = new Dictionary<string, bool>();
        private bool allPropertiesValid = false;

        // Properties
        public Lingva CurrentLanguage { get { return currentLanguage; } }

        public string Language
        {
            get { return currentLanguage.Language; }
            set
            {
                if (currentLanguage.Language != value)
                {
                    currentLanguage.Language = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string Folder
        {
            get { return currentLanguage.Folder; }
            set
            {
                if(currentLanguage.Folder != value)
                {
                    currentLanguage.Folder = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool AllPropertiesValid
        {
            get { return allPropertiesValid; }
            set
            {
                if (allPropertiesValid != value)
                {
                    allPropertiesValid = value;
                    RaisePropertyChanged();
                }
            }
        }

        // Constructors
        public LingvaViewModel(Lingva language)
        {
            currentLanguage = language;
            validProperties.Add("Language", false);
            validProperties.Add("Folder", false);
        }

        public LingvaViewModel() : this(new Lingva()) {}

        // Methods
        private void ValidateProperties()
        {
            foreach (bool isValid in validProperties.Values)
            {
                if (!isValid)
                {
                    AllPropertiesValid = false;
                    return;
                }
            }
            AllPropertiesValid = true;
        }

        // Equals override
        public override bool Equals(object obj)
        {
            LingvaViewModel item = obj as LingvaViewModel;
            if (item == null)
            {
                return false;
            }
            return this.currentLanguage.Equals(item.currentLanguage);
        }

        public override int GetHashCode()
        {
            return currentLanguage.GetHashCode();
        }

        // DataErrorInfo interface
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
                string error = null;
                switch (propertyName)
                {
                    case "Language":
                        switch (currentLanguage.ValidateLanguageName())
                        {
                            case ValidationError.LANGNAMEEMPTY:
                                error = "Language name can't be empty.";
                                break;
                            case ValidationError.LANGTAKEN:
                                error = "Language is already in the database.";
                                break;
                            case ValidationError.LANGWITHSPACES:
                                error = "Language name contains trailing spaces.";
                                break;
                        }
                        break;
                    case "Folder":
                        switch (currentLanguage.ValidateLanguageFolder())
                        {
                            case ValidationError.FOLDERNAMEEMPTY:
                                error = "Select the folder.";
                                break;
                            case ValidationError.FOLDERTAKEN:
                                error = "Folder name is already taken.";
                                break;
                        }
                        break;
                    default:
                        throw new ApplicationException("Unknown Property being validated on Product.");
                }

                validProperties[propertyName] = String.IsNullOrEmpty(error);
                ValidateProperties();
                return error;
            }
        }
    }
}
