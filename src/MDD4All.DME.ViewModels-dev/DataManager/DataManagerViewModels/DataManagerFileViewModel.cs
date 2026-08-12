using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDD4All.DME.Configurations;
using MDD4All.FileAccess.Contracts;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Input;
using System.Xml.Serialization;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class DataManagerFileViewModel : ObservableObject
    {
        #region constructor
        public DataManagerFileViewModel(IFileLoader fileLoader,
                                        IFileSaver fileSaver,
                                        DataManagerSettingsViewModel dataManagerSettings,
                                        DataManagerModelViewModel dataManagerModel)
        {
            _fileLoader = fileLoader;
            _fileSaver = fileSaver;
            _dataManagerSettings = dataManagerSettings;
            _dataManagerModel = dataManagerModel;

            this.InitializeCommands();
        }

        private void InitializeCommands()
        {
            this.NewDataFileCommand = new RelayCommand(this.ExecuteNewDataFile);
            this.OpenRecentDataFileCommand = new RelayCommand<int>(this.ExecuteOpenRecentDataFile);
            this.OpenDataFileCommand = new RelayCommand(this.ExecuteOpenDataFile);
            this.ConfirmOpenDataFileCommand = new RelayCommand(this.ExecuteConfirmOpenDataFile);
            this.SaveDataFileCommand = new RelayCommand(this.ExecuteSaveDataFile);
            this.SaveDataFileAsCommand = new RelayCommand(this.ExecuteSaveDataFileAs);
        }
        #endregion

        #region Properties
        private readonly IFileLoader _fileLoader;
        private readonly IFileSaver _fileSaver;

        private readonly DataManagerSettingsViewModel _dataManagerSettings;

        // Asked for the type behind a stored descriptor and for switching the active model,
        // so that stays in one place instead of being repeated per file command.
        private readonly DataManagerModelViewModel _dataManagerModel;

        private DataSerializationViewModel? _dataSerializationViewModel;

        public DataSerializationViewModel? DataSerializationViewModel
        {
            get
            {
                return _dataSerializationViewModel;
            }
            private set
            {
                if (_dataSerializationViewModel != null)
                {
                    _dataSerializationViewModel.PropertyChanged -= this.OnDataSerializationPropertyChanged;
                }

                _dataSerializationViewModel = value;

                if (_dataSerializationViewModel != null)
                {
                    _dataSerializationViewModel.PropertyChanged += this.OnDataSerializationPropertyChanged;
                }
            }
        }

        public string StatusText
        {
            get
            {
                string result = "";
                if (this.DataSerializationViewModel != null)
                {
                    result = "Filename: " + this.DataSerializationViewModel.FileName;
                    result += " ● Data Model: " + _dataManagerSettings.CurrentDataModel!.FullTypeName;
                }
                return result;
            }
        }

        #endregion

        #region Event Handlers
        // A freshly assigned DataSerializationViewModel starts out empty - LoadFromFile()/
        // CreateNewInstance() populate ActiveObject a moment later. Notifying our own
        // subscribers (MainViewModel's tree rebuild) right here would rebuild the tree
        // from the still-empty object, so wait for ActiveObject itself to change instead.
        private void OnDataSerializationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataSerializationViewModel.ActiveObject))
            {
                this.OnPropertyChanged(nameof(DataSerializationViewModel));
            }
        }
        #endregion

        #region Commands
        public ICommand NewDataFileCommand { get; private set; } = null!;

        public ICommand OpenDataFileCommand { get; private set; } = null!;

        public ICommand ConfirmOpenDataFileCommand { get; private set; } = null!;

        public ICommand OpenRecentDataFileCommand { get; private set; } = null!;

        public ICommand SaveDataFileCommand { get; private set; } = null!;

        public ICommand SaveDataFileAsCommand { get; private set; } = null!;
        #endregion

        #region Command Implementations
        private void ExecuteNewDataFile()
        {
            SynchronizationContext.Current?.Post((_) =>
            {
                string fileName = "";

                bool dialogResult = _fileSaver.ShowFileSaveDialog(out fileName,
                                                                  initialDirectory: _dataManagerSettings.LastUsedDataFilePath,
                                                                  title: "New data file...",
                                                                  filter: "JSON file (*.json)|*.json|XML file (*.xml)|*.xml|All files (*.*)|*.*",
                                                                  defaultFileExtension: "json");

                if (dialogResult == true)
                {
                    DataModelDescriptor? currentType = _dataManagerSettings.CurrentDataModel;

                    if (currentType != null)
                    {
                        Type? type = _dataManagerModel.ResolveDataModelType(currentType);

                        if (type != null)
                        {
                            this.DataSerializationViewModel = new DataSerializationViewModel(fileName, type);

                            this.DataSerializationViewModel.CreateNewInstance();

                            this.SaveDataFileCommand.Execute(null);

                            DataFileDescriptor dataFileDescriptor = new DataFileDescriptor
                            {
                                FilePath = fileName,
                                DataModelDescription = new DataModelDescriptor
                                {
                                    DllPath = _dataManagerSettings.CurrentDataModel!.DllPath,
                                    FullTypeName = _dataManagerSettings.CurrentDataModel.FullTypeName
                                }
                            };

                            _dataManagerSettings.AddNewRecentDataFile(dataFileDescriptor);
                        }
                    }

                }
            }, null);
        }

        private void ExecuteOpenRecentDataFile(int index)
        {
            DataFileDescriptor descriptor = _dataManagerSettings.RecentDataFiles[index];

            if (descriptor != null)
            {
                Type? type = _dataManagerModel.ResolveDataModelType(descriptor.DataModelDescription);

                if (type != null)
                {
                    _dataManagerSettings.SetRecentDataFileToTop(index);

                    _dataManagerModel.ActivateDataModel(descriptor.DataModelDescription);

                    this.DataSerializationViewModel = new DataSerializationViewModel(descriptor.FilePath, type);

                    this.DataSerializationViewModel.LoadFromFile();
                }
            }
        }

        private void ExecuteOpenDataFile()
        {
            SynchronizationContext.Current?.Post((_) =>
            {
                string filename = "";
                bool openResult = _fileLoader.ShowOpenFileDialog(out filename,
                                                                 initialDirectory: _dataManagerSettings.LastUsedDataFilePath,
                                                                 filter: "JSON file (*.json)|*.json|XML file (*.xml)|*.xml|All files (*.*)|*.*",
                                                                 title: "Open data file...",
                                                                 defaultFileExtension: "json");

                if (openResult == true)
                {
                    // The file names the type it was saved as, so its model is looked up from the
                    // file itself - only if that fails does the currently selected one apply.
                    DataModelDescriptor? descriptor = _dataManagerModel.FindDataModelForFile(filename);

                    if (descriptor == null)
                    {
                        descriptor = _dataManagerSettings.CurrentDataModel;
                    }

                    if (descriptor != null)
                    {
                        Type? type = _dataManagerModel.ResolveDataModelType(descriptor);

                        if (type != null)
                        {
                            _dataManagerModel.ActivateDataModel(descriptor);

                            DataFileDescriptor dataFileDescriptor = new DataFileDescriptor
                            {
                                DataModelDescription = descriptor,
                                FilePath = filename
                            };

                            _dataManagerSettings.AddNewRecentDataFile(dataFileDescriptor);

                            this.DataSerializationViewModel = new DataSerializationViewModel(filename, type);

                            this.DataSerializationViewModel.LoadFromFile();
                        }
                    }
                }
            }, null);
        }

        private void ExecuteConfirmOpenDataFile()
        {
            throw new NotImplementedException();
        }

        private void ExecuteSaveDataFile()
        {
            FileInfo fileInfo = new FileInfo(this.DataSerializationViewModel!.FileName);

            if (fileInfo.DirectoryName != null)
            {
                _dataManagerSettings.LastUsedDataFilePath = fileInfo.DirectoryName;
            }

            // Read at save time, so toggling the setting takes effect without reopening the file.
            this.DataSerializationViewModel.IncludeTypeInformation = _dataManagerSettings.SaveTypeInformation;

            if (fileInfo.Extension.ToLower() == ".xml")
            {
                this.SerializeToXml();
            }
            else
            {
                File.WriteAllText(this.DataSerializationViewModel!.FileName, this.DataSerializationViewModel.ActiveObjectJsonString);
            }
        }

        private void ExecuteSaveDataFileAs()
        {
            SynchronizationContext.Current?.Post((_) =>
            {
                string fileName = "";

                bool dialogResult = _fileSaver.ShowFileSaveDialog(out fileName,
                                                                  initialDirectory: _dataManagerSettings.LastUsedDataFilePath,
                                                                  title: "Save data file as...",
                                                                  filter: "JSON file (*.json)|*.json|XML file (*.xml)|*.xml|All files (*.*)|*.*");

                if (dialogResult == true)
                {
                    FileInfo fileInfo = new FileInfo(fileName);

                    this.DataSerializationViewModel!.FileName = fileName;

                    // Read at save time, so toggling the setting takes effect without reopening the file.
                    this.DataSerializationViewModel.IncludeTypeInformation = _dataManagerSettings.SaveTypeInformation;

                    if (fileInfo.Extension.ToLower() == ".xml")
                    {
                        this.SerializeToXml();
                    }
                    else
                    {
                        File.WriteAllText(fileName, this.DataSerializationViewModel.ActiveObjectJsonString);
                    }

                    DataFileDescriptor dataFileDescriptor = new DataFileDescriptor()
                    {
                        DataModelDescription = _dataManagerSettings.CurrentDataModel!,
                        FilePath = fileName
                    };

                    _dataManagerSettings.AddNewRecentDataFile(dataFileDescriptor);

                    if (fileInfo.DirectoryName != null)
                    {
                        _dataManagerSettings.LastUsedDataFilePath = fileInfo.DirectoryName;
                    }

                    this.OnPropertyChanged(nameof(StatusText));
                }

            }, null);
        }
        #endregion

        #region Helpers
        private void SerializeToXml()
        {

            // Insert code to set properties and fields of the object.
            XmlSerializer mySerializer = new
            XmlSerializer(this.DataSerializationViewModel!.SelectedType!);
            // To write to a file, create a StreamWriter object.
            StreamWriter myWriter = new StreamWriter(this.DataSerializationViewModel.FileName);
            mySerializer.Serialize(myWriter, this.DataSerializationViewModel.ActiveObject);
            myWriter.Close();
        }
        #endregion
    }
}
