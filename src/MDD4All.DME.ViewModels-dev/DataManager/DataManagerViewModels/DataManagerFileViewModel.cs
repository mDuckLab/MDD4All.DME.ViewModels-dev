using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDD4All.AssemblyLoading.Contracts;
using MDD4All.DME.AssemblyTree.ViewModels;
using MDD4All.DME.Configurations;
using MDD4All.FileAccess.Contracts;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
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
                                        IAssemblyProvider assemblyProvider,
                                        DataManagerSettingsViewModel dataManagerSettings,
                                        DataManagerModelViewModel dataManagerModel)
        {
            _fileLoader = fileLoader;
            _fileSaver = fileSaver;
            _assemblyProvider = assemblyProvider;
            _dataManagerSettings = dataManagerSettings;
            _dataManagerModel = dataManagerModel;
            _dataManagerModel.PropertyChanged += this.OnDataManagerModelPropertyChanged;

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

        // Needed because the data model is a DLL picked by the user at runtime, not known at compile time.
        private readonly IAssemblyProvider _assemblyProvider;

        private readonly DataManagerSettingsViewModel _dataManagerSettings;

        private readonly DataManagerModelViewModel _dataManagerModel;

        private DataEditorViewModel? _dataEditorViewModel;

        public DataEditorViewModel? DataEditorViewModel
        {
            get
            {
                return _dataEditorViewModel;
            }
            private set
            {
                if (_dataEditorViewModel != null)
                {
                    _dataEditorViewModel.PropertyChanged -= this.OnActiveDataEditorPropertyChanged;
                }

                _dataEditorViewModel = value;

                if (_dataEditorViewModel != null)
                {
                    _dataEditorViewModel.PropertyChanged += this.OnActiveDataEditorPropertyChanged;
                }
            }
        }

        public string StatusText
        {
            get
            {
                string result = "";
                if (this.DataEditorViewModel != null)
                {
                    result = "Filename: " + this.DataEditorViewModel.FileName;
                    result += " ● Data Model: " + _dataManagerSettings.CurrentDataModel!.FullTypeName;
                }
                return result;
            }
        }

        public DmeConfiguration Configuration
        {
            get
            {
                return _dataManagerSettings.Configuration;
            }
        }

        public AssemblyTreeViewModel? AssemblyTreeViewModel
        {
            get
            {
                return _dataManagerModel.AssemblyTreeViewModel;
            }
        }
        #endregion

        #region Event Handlers
        // A freshly assigned DataEditorViewModel starts out empty - LoadFromFile()/
        // CreateNewInstance() populate ActiveObject a moment later. Notifying our own
        // subscribers (MainViewModel's tree rebuild) right here would rebuild the tree
        // from the still-empty object, so wait for ActiveObject itself to change instead.
        private void OnActiveDataEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataEditorViewModel.ActiveObject))
            {
                this.OnPropertyChanged(nameof(DataEditorViewModel));
            }
        }

        // Forwards the sub-viewmodel's own notification so MainViewModel (which watches
        // this class, not DataManagerModelViewModel) still finds out about the change.
        private void OnDataManagerModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataManagerModelViewModel.AssemblyTreeViewModel))
            {
                this.OnPropertyChanged(nameof(AssemblyTreeViewModel));
            }
        }
        #endregion

        #region Commands
        public ICommand OpenDataModelCommand
        {
            get
            {
                return _dataManagerModel.OpenDataModelCommand;
            }
        }

        public ICommand ConfirmOpenDataModelCommand
        {
            get
            {
                return _dataManagerModel.ConfirmOpenDataModelCommand;
            }
        }

        public ICommand SetDataModelFromRecentListCommand
        {
            get
            {
                return _dataManagerModel.SetDataModelFromRecentListCommand;
            }
        }

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
                        Assembly assembly = _assemblyProvider.GetAssemblyByPath(currentType.DllPath);

                        Type? type = assembly.GetType(currentType.FullTypeName);

                        if (type != null)
                        {
                            this.DataEditorViewModel = new DataEditorViewModel(fileName, type, _fileSaver);

                            this.DataEditorViewModel.CreateNewInstance();

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
                Assembly assembly = _assemblyProvider.GetAssemblyByPath(descriptor.DataModelDescription.DllPath);

                Type? type = assembly.GetType(descriptor.DataModelDescription.FullTypeName);

                if (type != null)
                {
                    _dataManagerSettings.SetRecentDataFileToTop(index);
                    _dataManagerSettings.CurrentDataModel = descriptor.DataModelDescription;

                    _dataManagerSettings.SetTopRecentDataModel(descriptor.DataModelDescription);

                    this.DataEditorViewModel = new DataEditorViewModel(descriptor.FilePath, type, _fileSaver);

                    this.DataEditorViewModel.LoadFromFile();
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
                    DataModelDescriptor? descriptor = this.FindDataModelForFile(filename);

                    if (descriptor == null)
                    {
                        descriptor = _dataManagerSettings.CurrentDataModel;
                    }

                    if (descriptor != null)
                    {
                        Assembly assembly = _assemblyProvider.GetAssemblyByPath(descriptor.DllPath);

                        Type? type = assembly.GetType(descriptor.FullTypeName);

                        if (type != null)
                        {
                            _dataManagerSettings.CurrentDataModel = descriptor;
                            _dataManagerSettings.SetTopRecentDataModel(descriptor);

                            DataFileDescriptor dataFileDescriptor = new DataFileDescriptor
                            {
                                DataModelDescription = descriptor,
                                FilePath = filename
                            };

                            _dataManagerSettings.AddNewRecentDataFile(dataFileDescriptor);

                            this.DataEditorViewModel = new DataEditorViewModel(filename, type, _fileSaver);

                            this.DataEditorViewModel.LoadFromFile();
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
            FileInfo fileInfo = new FileInfo(this.DataEditorViewModel!.FileName);

            if (fileInfo.DirectoryName != null)
            {
                _dataManagerSettings.LastUsedDataFilePath = fileInfo.DirectoryName;
            }

            // Read at save time, so toggling the setting takes effect without reopening the file.
            this.DataEditorViewModel.IncludeTypeInformation = _dataManagerSettings.SaveTypeInformation;

            if (fileInfo.Extension.ToLower() == ".xml")
            {
                this.SerializeToXml();
            }
            else
            {
                File.WriteAllText(this.DataEditorViewModel!.FileName, this.DataEditorViewModel.ActiveObjectJsonString);
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

                    this.DataEditorViewModel!.FileName = fileName;

                    // Read at save time, so toggling the setting takes effect without reopening the file.
                    this.DataEditorViewModel.IncludeTypeInformation = _dataManagerSettings.SaveTypeInformation;

                    if (fileInfo.Extension.ToLower() == ".xml")
                    {
                        this.SerializeToXml();
                    }
                    else
                    {
                        File.WriteAllText(fileName, this.DataEditorViewModel.ActiveObjectJsonString);
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
        // A saved file names its own type assembly-qualified ("Namespace.Type, AssemblyName"), so the
        // model it belongs to can be derived from the file instead of having to be picked beforehand.
        private DataModelDescriptor? FindDataModelForFile(string filePath)
        {
            DataModelDescriptor? result = null;

            if (filePath.ToLower().EndsWith("json"))
            {
                string? qualifiedTypeName = DataEditorViewModel.ReadTypeNameFromJson(File.ReadAllText(filePath));

                if (qualifiedTypeName != null)
                {
                    string[] typeNameParts = qualifiedTypeName.Split(',');

                    string typeName = typeNameParts[0].Trim();

                    if (typeNameParts.Length > 1)
                    {
                        // Data model DLLs referenced by the application end up next to it.
                        string assemblyName = typeNameParts[1].Trim();
                        string dllPath = Path.Combine(AppContext.BaseDirectory, assemblyName + ".dll");

                        if (File.Exists(dllPath))
                        {
                            result = new DataModelDescriptor
                            {
                                DllPath = dllPath,
                                FullTypeName = typeName
                            };
                        }
                    }

                    // Not next to the application, so it can only be a model loaded from elsewhere -
                    // keep that DLL and take just the type the file actually names.
                    if (result == null && _dataManagerSettings.CurrentDataModel != null)
                    {
                        result = new DataModelDescriptor
                        {
                            DllPath = _dataManagerSettings.CurrentDataModel.DllPath,
                            FullTypeName = typeName
                        };
                    }
                }
            }

            return result;
        }

        private void SerializeToXml()
        {

            // Insert code to set properties and fields of the object.
            XmlSerializer mySerializer = new
            XmlSerializer(this.DataEditorViewModel!.SelectedType!);
            // To write to a file, create a StreamWriter object.
            StreamWriter myWriter = new StreamWriter(this.DataEditorViewModel.FileName);
            mySerializer.Serialize(myWriter, this.DataEditorViewModel.ActiveObject);
            myWriter.Close();
        }
        #endregion
    }
}
