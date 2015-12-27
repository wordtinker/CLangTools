using LangTools.Models;
using MicroMvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace LangTools.ViewModels
{
    class LingvaViewModel : ObservableObject, IDataErrorInfo
    {
        // Members
        private readonly Lingva currentLanguage;
        private Dictionary<string, bool> validProperties;
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
                    RaisePropertyChanged("Language");
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
                    RaisePropertyChanged("Folder");
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
                    RaisePropertyChanged("AllPropertiesValid");
                }
            }
        }

        // Constructors
        public LingvaViewModel(Lingva language)
        {
            currentLanguage = language;
            validProperties = new Dictionary<string, bool>();
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
                return (currentLanguage as IDataErrorInfo).Error;
            }
        }

        public string this[string propertyName]
        {
            get
            {
                string error = (currentLanguage as IDataErrorInfo)[propertyName];
                validProperties[propertyName] = String.IsNullOrEmpty(error);
                ValidateProperties();
                CommandManager.InvalidateRequerySuggested();
                return error;
            }
        }
    }
}
