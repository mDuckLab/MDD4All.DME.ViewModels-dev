using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.Configuration;
using MDD4All.Configuration.Contracts;

namespace MDD4All.DME.ViewModels.Editor.Settings
{
    public class ExplorerSettingsViewModel : ObservableObject
    {
        private readonly IConfigurationReaderWriter<ExplorerSettings> _configurationReaderWriter;

        private ExplorerSettings _settings;

        public ExplorerSettingsViewModel()
        {
            _configurationReaderWriter = new FileConfigurationReaderWriter<ExplorerSettings>("DME");

            _settings = _configurationReaderWriter.GetConfiguration() ?? new ExplorerSettings();
        }

        public bool ShowIcons
        {
            get => _settings.ShowIcons;
            set => SetAndStore(value, _settings.ShowIcons, v => _settings.ShowIcons = v);
        }


        public bool ShowTypeSymbols
        {
            get
            {
                return _settings.ShowTypeSymbols;
            }

            set
            {
                SetAndStore(value, _settings.ShowTypeSymbols, v => _settings.ShowTypeSymbols = v);
            }
        }

        private void SetAndStore<T>(T value, T currentValue, System.Action<T> apply, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(currentValue, value))
            {
                apply(value);
                _configurationReaderWriter.StoreConfiguration(_settings);
                OnPropertyChanged(propertyName);
            }
        }
    }
}
