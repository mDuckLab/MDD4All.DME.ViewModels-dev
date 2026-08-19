using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.Configuration;
using MDD4All.Configuration.Contracts;
using MDD4All.DME.Configurations;
using System.Collections.Generic;

namespace MDD4All.DME.ViewModels.DataManager
{
    /// <summary>
    /// Owns the DME configuration file - every setter persists immediately so a change can never be lost.
    /// </summary>
    public class DataManagerSettingsViewModel : ObservableObject
    {
        #region constructor
        public DataManagerSettingsViewModel()
        {
            _configurationReaderWriter = new FileConfigurationReaderWriter<DmeConfiguration>("DME");

            _configuration = _configurationReaderWriter.GetConfiguration();

            // No config file yet on first run - fall back to defaults instead of failing.
            if (_configuration == null)
            {
                _configuration = new DmeConfiguration();
            }
        }
        #endregion

        #region Properties
        private readonly IConfigurationReaderWriter<DmeConfiguration> _configurationReaderWriter;

        private DmeConfiguration _configuration;

        public DmeConfiguration Configuration
        {
            get
            {
                return _configuration;
            }
        }

        public string LastUsedDataFilePath
        {
            get
            {
                return _configuration.LastUsedDataFilePath;
            }
            set
            {
                _configuration.LastUsedDataFilePath = value;
                this.Persist();
                this.OnPropertyChanged(nameof(LastUsedDataFilePath));
            }
        }


        public bool SaveTypeInformation
        {
            get
            {
                return _configuration.SaveTypeInformation;
            }
            set
            {
                _configuration.SaveTypeInformation = value;
                this.Persist();
                this.OnPropertyChanged(nameof(SaveTypeInformation));
            }
        }


        #endregion

        #region Helpers
        // Called by every setter above, so a change can never be forgotten to save.
        private void Persist()
        {
            _configurationReaderWriter.StoreConfiguration(_configuration);
        }



        #endregion
    }
}
