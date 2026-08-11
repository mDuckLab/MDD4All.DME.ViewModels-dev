using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.Configuration;
using MDD4All.Configuration.Contracts;
using MDD4All.DME.Configurations;
using System.Collections.Generic;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class DataManagerSettingsViewModel : ObservableObject
    {
        #region constructor
        public DataManagerSettingsViewModel()
        {
            _configurationReaderWriter = new FileConfigurationReaderWriter<DmeConfiguration>("DME");

            _configuration = _configurationReaderWriter.GetConfiguration();

            if (_configuration == null)
            {
                _configuration = new DmeConfiguration();
            }
        }
        #endregion

        #region Properties
        private readonly IConfigurationReaderWriter<DmeConfiguration> _configurationReaderWriter;

        private DmeConfiguration _configuration;

        // Read-only outward - all mutation goes through the properties/methods below so persistence can't be forgotten.
        public DmeConfiguration Configuration
        {
            get
            {
                return _configuration;
            }
        }

        public DataModelDescriptor? CurrentDataModel
        {
            get
            {
                return _configuration.CurrentDataModel;
            }
            set
            {
                _configuration.CurrentDataModel = value;
                this.Persist();
                this.OnPropertyChanged(nameof(CurrentDataModel));
            }
        }

        public List<DataModelDescriptor> RecentDataModels
        {
            get
            {
                return _configuration.RecentDataModels;
            }
        }

        public List<DataFileDescriptor> RecentDataFiles
        {
            get
            {
                return _configuration.RecentDataFiles;
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

        public string LastUsedDataModelPath
        {
            get
            {
                return _configuration.LastUsedDataModelPath;
            }
            set
            {
                _configuration.LastUsedDataModelPath = value;
                this.Persist();
                this.OnPropertyChanged(nameof(LastUsedDataModelPath));
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
        private void Persist()
        {
            _configurationReaderWriter.StoreConfiguration(_configuration);
        }

        public void SetTopRecentDataModel(DataModelDescriptor modelDescriptor)
        {
            if (_configuration.RecentDataModels.Find(dm => dm.DllPath == modelDescriptor.DllPath &&
                                                          dm.FullTypeName == modelDescriptor.FullTypeName) == null)
            {
                if (_configuration.RecentDataModels.Count == 5)
                {
                    _configuration.RecentDataModels.RemoveAt(4);
                }

                _configuration.RecentDataModels.Insert(0, modelDescriptor);
            }
            else
            {
                int searchIndex = -1;
                for (int index = 0; index < _configuration.RecentDataModels.Count; index++)
                {
                    DataModelDescriptor currentDescriptor = _configuration.RecentDataModels[index];
                    if (currentDescriptor.FullTypeName == modelDescriptor.FullTypeName &&
                       currentDescriptor.DllPath == modelDescriptor.DllPath)
                    {
                        searchIndex = index;
                        break;
                    }
                }

                if (searchIndex >= 0)
                {
                    DataModelDescriptor existingDescriptor = _configuration.RecentDataModels[searchIndex];
                    _configuration.RecentDataModels.RemoveAt(searchIndex);
                    _configuration.RecentDataModels.Insert(0, existingDescriptor);
                }
            }

            this.Persist();
        }

        public void SetRecentDataFileToTop(int index)
        {
            DataFileDescriptor fileDescriptor = _configuration.RecentDataFiles[index];

            _configuration.RecentDataFiles.RemoveAt(index);
            _configuration.RecentDataFiles.Insert(0, fileDescriptor);

            this.Persist();
        }

        public void AddNewRecentDataFile(DataFileDescriptor dataFileDescriptor)
        {
            // Reopening a known file moves it back to the top instead of listing it twice.
            DataFileDescriptor? existingDescriptor = _configuration.RecentDataFiles.Find(file => file.FilePath == dataFileDescriptor.FilePath);

            if (existingDescriptor != null)
            {
                _configuration.RecentDataFiles.Remove(existingDescriptor);
            }

            while (_configuration.RecentDataFiles.Count >= 5)
            {
                _configuration.RecentDataFiles.RemoveAt(_configuration.RecentDataFiles.Count - 1);
            }

            _configuration.RecentDataFiles.Insert(0, dataFileDescriptor);

            this.Persist();
        }
        #endregion
    }
}
