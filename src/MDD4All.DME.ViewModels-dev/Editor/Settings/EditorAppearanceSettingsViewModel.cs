using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.Configuration;
using MDD4All.Configuration.Contracts;

namespace MDD4All.DME.ViewModels.Editor.Settings
{
    public class EditorAppearanceSettingsViewModel : ObservableObject
    {
        private readonly IConfigurationReaderWriter<EditorAppearanceSettings> _configurationReaderWriter;

        private EditorAppearanceSettings _settings;

        public EditorAppearanceSettingsViewModel()
        {
            _configurationReaderWriter = new FileConfigurationReaderWriter<EditorAppearanceSettings>("DME");

            _settings = _configurationReaderWriter.GetConfiguration() ?? new EditorAppearanceSettings();
        }

        public bool TintEnabled
        {
            get
            {
                return _settings.TintEnabled;
            }
            set
            {
                SetAndStore(value, _settings.TintEnabled, v => _settings.TintEnabled = v);
            }
        }

        public int MaxDepth
        {
            get
            {
                return _settings.MaxDepth;
            }
            set
            {
                SetAndStore(value, _settings.MaxDepth, v => _settings.MaxDepth = v);
            }
        }

        public bool ShowIcons
        {
            get
            {
                return _settings.ShowIcons;
            }
            set
            {
                SetAndStore(value, _settings.ShowIcons, v => _settings.ShowIcons = v);
            }
        }

        public bool ShowIndexNumbers
        {
            get
            {
                return _settings.ShowIndexNumbers;
            }
            set
            {
                SetAndStore(value, _settings.ShowIndexNumbers, v => _settings.ShowIndexNumbers = v);
            }
        }

        public bool ShowReadOnlyBadges
        {
            get
            {
                return _settings.ShowReadOnlyBadges;
            }
            set
            {
                SetAndStore(value, _settings.ShowReadOnlyBadges, v => _settings.ShowReadOnlyBadges = v);
            }
        }

        public bool ShowTypeBadges
        {
            get
            {
                return _settings.ShowTypeBadges;
            }
            set
            {
                SetAndStore(value, _settings.ShowTypeBadges, v => _settings.ShowTypeBadges = v);
            }
        }

        // T is inferred per call site: bool for the toggles, int for MaxDepth.
        private void SetAndStore<T>(T value, T currentValue, System.Action<T> apply,
            // Compiler auto-fills this with the calling property's name.
            [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            // Generic T can't use "!=" directly, so use EqualityComparer instead.
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(currentValue, value))
            {
                apply(value); // actually writes the field, via the passed-in lambda
                _configurationReaderWriter.StoreConfiguration(_settings); // full object, whole file rewritten
                OnPropertyChanged(propertyName); // notifies subscribed UI to re-render
            }
            // unchanged value: skip write and notify entirely
        }
    }
}
