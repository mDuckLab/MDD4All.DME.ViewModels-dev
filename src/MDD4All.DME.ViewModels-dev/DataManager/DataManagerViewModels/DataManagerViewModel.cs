using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDD4All.AssemblyLoading.Contracts;
using MDD4All.Configuration;
using MDD4All.Configuration.Contracts;
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
    public class DataManagerViewModel : ObservableObject
    {
        #region constructor
        public DataManagerViewModel(IFileLoader fileLoader,
                                        IFileSaver fileSaver,
                                        IAssemblyProvider assemblyProvider)
        {
            _fileLoader = fileLoader;
            _fileSaver = fileSaver;
            _assemblyProvider = assemblyProvider;

            _configurationReaderWriter = new FileConfigurationReaderWriter<DmeConfiguration>("DME");

            _configuration = _configurationReaderWriter.GetConfiguration();

            if (_configuration == null)
            {
                _configuration = new DmeConfiguration();
            }

            this.InitializeCommands();
        }

        private void InitializeCommands()
        {
            this.OpenDataModelCommand = new RelayCommand(this.ExecuteOpenDataModel);
            this.ConfirmOpenDataModelCommand = new RelayCommand<DataModelDescriptor>(this.ExecuteConfirmOpenDataModelCommand);
            this.SetDataModelFromRecentListCommand = new RelayCommand<int>(this.ExecuteSetDataModelFromRecentList);
            this.NewDataFileCommand = new RelayCommand(this.ExecuteNewDataFile);
            this.OpenRecentDataFileCommand = new RelayCommand<int>(this.ExecuteOpenRecentDataFile);
            this.OpenDataFileCommand = new RelayCommand(this.ExecuteOpenDataFile);
            this.ConfirmOpenDataFileCommand = new RelayCommand(this.ExecuteConfirmOpenDataFile);
            this.SaveDataFileCommand = new RelayCommand(this.ExecuteSaveDataFile);
            this.SaveDataFileAsCommand = new RelayCommand(this.ExecuteSaveDataFileAs);
        }
        #endregion

        #region Properties
        private readonly IConfigurationReaderWriter<DmeConfiguration> _configurationReaderWriter;

        private readonly IFileLoader _fileLoader;
        private readonly IFileSaver _fileSaver;

        // Needed because the data model is a DLL picked by the user at runtime, not known at compile time.
        private readonly IAssemblyProvider _assemblyProvider;

        private DmeConfiguration _configuration;

        public DmeConfiguration Configuration
        {
            get
            {
                return _configuration;
            }
            set
            {
                _configuration = value;
            }
        }

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
                    result += " ● Data Model: " + this.Configuration.CurrentDataModel!.FullTypeName;
                }
                return result;
            }
        }

        private AssemblyTreeViewModel? _assemblyTreeViewModel;

        public AssemblyTreeViewModel? AssemblyTreeViewModel
        {
            get
            {
                return _assemblyTreeViewModel;
            }
            private set
            {
                _assemblyTreeViewModel = value;
                this.OnPropertyChanged(nameof(AssemblyTreeViewModel));
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
        #endregion

        #region Commands
        public ICommand OpenDataModelCommand { get; private set; } = null!;

        public ICommand ConfirmOpenDataModelCommand { get; private set; } = null!;

        public ICommand NewDataFileCommand { get; private set; } = null!;

        public ICommand OpenDataFileCommand { get; private set; } = null!;

        public ICommand ConfirmOpenDataFileCommand { get; private set; } = null!;

        public ICommand OpenRecentDataFileCommand { get; private set; } = null!;

        public ICommand SetDataModelFromRecentListCommand { get; private set; } = null!;

        public ICommand SaveDataFileCommand { get; private set; } = null!;

        public ICommand SaveDataFileAsCommand { get; private set; } = null!;
        #endregion

        #region Command Implementations
        private void ExecuteOpenDataModel()
        {
            SynchronizationContext.Current?.Post((_) =>
            {
                string filename = "";
                bool openResult = _fileLoader.ShowOpenFileDialog(out filename,
                                                                 filter: "DLL Files (*.dll)|*.dll",
                                                                 title: "Open Data Model library file...",
                                                                 initialDirectory: this.Configuration.LastUsedDataModelPath
                                                                 );

                if (openResult == true)
                {
                    this.AssemblyTreeViewModel = new AssemblyTreeViewModel(filename, _assemblyProvider);
                }
            }, null);
        }

        private void ExecuteConfirmOpenDataModelCommand(DataModelDescriptor? descriptor)
        {
            if (descriptor != null)
            {
                this.Configuration.CurrentDataModel = descriptor;

                if (this.Configuration.RecentDataModels.Find(dm => dm.DllPath == descriptor.DllPath && dm.FullTypeName == descriptor.FullTypeName) == null)
                {
                    if (this.Configuration.RecentDataModels.Count == 5)
                    {
                        this.Configuration.RecentDataModels.RemoveAt(4);

                    }
                    this.Configuration.RecentDataModels.Insert(0, descriptor);


                }

                FileInfo fileInfo = new FileInfo(descriptor.DllPath);

                if (fileInfo.DirectoryName != null)
                {
                    this.Configuration.LastUsedDataModelPath = fileInfo.DirectoryName;
                }
            }

            // Closes the type-selection dialog: MainViewModel watches this
            // property to know when to switch back to the start page.
            this.AssemblyTreeViewModel = null;
        }

        private void ExecuteSetDataModelFromRecentList(int index)
        {
            DataModelDescriptor descriptor = this.Configuration.RecentDataModels[index];

            this.Configuration.CurrentDataModel = descriptor;

            this.Configuration.RecentDataModels.RemoveAt(index);
            this.Configuration.RecentDataModels.Insert(0, descriptor);
            _configurationReaderWriter.StoreConfiguration(this.Configuration);
        }

        private void ExecuteNewDataFile()
        {
            SynchronizationContext.Current?.Post((_) =>
            {
                string fileName = "";

                bool dialogResult = _fileSaver.ShowFileSaveDialog(out fileName,
                                                                  initialDirectory: this.Configuration.LastUsedDataFilePath,
                                                                  title: "New data file...",
                                                                  filter: "JSON file (*.json)|*.json|XML file (*.xml)|*.xml|All files (*.*)|*.*",
                                                                  defaultFileExtension: "json");

                if (dialogResult == true)
                {
                    DataModelDescriptor? currentType = this.Configuration.CurrentDataModel;

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
                                    DllPath = this.Configuration.CurrentDataModel!.DllPath,
                                    FullTypeName = this.Configuration.CurrentDataModel.FullTypeName
                                }
                            };

                            if (this.Configuration.RecentDataFiles.Count == 5)
                            {
                                this.Configuration.RecentDataFiles.RemoveAt(4);
                            }

                            this.Configuration.RecentDataFiles.Insert(0, dataFileDescriptor);

                            _configurationReaderWriter.StoreConfiguration(this.Configuration);
                        }
                    }

                }
            }, null);
        }

        private void ExecuteOpenRecentDataFile(int index)
        {
            DataFileDescriptor descriptor = this.Configuration.RecentDataFiles[index];

            if (descriptor != null)
            {
                Assembly assembly = _assemblyProvider.GetAssemblyByPath(descriptor.DataModelDescription.DllPath);

                Type? type = assembly.GetType(descriptor.DataModelDescription.FullTypeName);

                if (type != null)
                {
                    this.SetRecentDataFileToTop(index);
                    this.Configuration.CurrentDataModel = descriptor.DataModelDescription;

                    this.SetTopRecentDataModel(descriptor.DataModelDescription);
                    _configurationReaderWriter.StoreConfiguration(this.Configuration);

                    this.DataEditorViewModel = new DataEditorViewModel(descriptor.FilePath, type, _fileSaver);

                    this.DataEditorViewModel.LoadFromFile();
                }
            }
        }

        private void ExecuteOpenDataFile()
        {
            SynchronizationContext.Current?.Post((_) =>
            {
                if (this.Configuration.CurrentDataModel != null)
                {
                    string filename = "";
                    bool openResult = _fileLoader.ShowOpenFileDialog(out filename,
                                                                     initialDirectory: this.Configuration.LastUsedDataFilePath,
                                                                     filter: "JSON file (*.json)|*.json|XML file (*.xml)|*.xml|All files (*.*)|*.*",
                                                                     title: "Open data file...",
                                                                     defaultFileExtension: "json");

                    if (openResult == true)
                    {
                        Assembly assembly = _assemblyProvider.GetAssemblyByPath(this.Configuration.CurrentDataModel!.DllPath);

                        Type? type = assembly.GetType(this.Configuration.CurrentDataModel!.FullTypeName);

                        if (type != null)
                        {
                            DataFileDescriptor dataFileDescriptor = new DataFileDescriptor
                            {
                                DataModelDescription = this.Configuration.CurrentDataModel,
                                FilePath = filename
                            };

                            this.AddNewRecentDataFile(dataFileDescriptor);

                            _configurationReaderWriter.StoreConfiguration(this.Configuration);

                            this.DataEditorViewModel = new DataEditorViewModel(dataFileDescriptor.FilePath, type, _fileSaver);

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
                this.Configuration.LastUsedDataFilePath = fileInfo.DirectoryName;
                _configurationReaderWriter.StoreConfiguration(this.Configuration);
            }

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
                                                                  initialDirectory: this.Configuration.LastUsedDataFilePath,
                                                                  title: "Save data file as...",
                                                                  filter: "JSON file (*.json)|*.json|XML file (*.xml)|*.xml|All files (*.*)|*.*");

                if (dialogResult == true)
                {
                    FileInfo fileInfo = new FileInfo(fileName);

                    this.DataEditorViewModel!.FileName = fileName;

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
                        DataModelDescription = this.Configuration.CurrentDataModel!,
                        FilePath = fileName
                    };

                    this.AddNewRecentDataFile(dataFileDescriptor);

                    if (fileInfo.DirectoryName != null)
                    {
                        this.Configuration.LastUsedDataFilePath = fileInfo.DirectoryName;
                    }

                    _configurationReaderWriter.StoreConfiguration(this.Configuration);

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
            XmlSerializer(this.DataEditorViewModel!.SelectedType!);
            // To write to a file, create a StreamWriter object.
            StreamWriter myWriter = new StreamWriter(this.DataEditorViewModel.FileName);
            mySerializer.Serialize(myWriter, this.DataEditorViewModel.ActiveObject);
            myWriter.Close();
        }

        private void SetTopRecentDataModel(DataModelDescriptor modelDescriptor)
        {
            if (this.Configuration.RecentDataModels.Find(dm => dm.DllPath == modelDescriptor.DllPath &&
                                                          dm.FullTypeName == modelDescriptor.FullTypeName) == null)
            {
                if (this.Configuration.RecentDataModels.Count == 5)
                {
                    this.Configuration.RecentDataModels.RemoveAt(4);
                }

                this.Configuration.RecentDataModels.Insert(0, modelDescriptor);
            }
            else
            {
                int searchIndex = -1;
                for (int index = 0; index < this.Configuration.RecentDataModels.Count; index++)
                {
                    DataModelDescriptor currentDescriptor = this.Configuration.RecentDataModels[index];
                    if (currentDescriptor.FullTypeName == modelDescriptor.FullTypeName &&
                       currentDescriptor.DllPath == modelDescriptor.DllPath)
                    {
                        searchIndex = index;
                        break;
                    }
                }

                if (searchIndex >= 0)
                {
                    DataModelDescriptor existingDescriptor = this.Configuration.RecentDataModels[searchIndex];
                    this.Configuration.RecentDataModels.RemoveAt(searchIndex);
                    this.Configuration.RecentDataModels.Insert(0, existingDescriptor);
                }
            }
        }

        private void SetRecentDataFileToTop(int index)
        {
            DataFileDescriptor fileDescriptor = this.Configuration.RecentDataFiles[index];

            this.Configuration.RecentDataFiles.RemoveAt(index);
            this.Configuration.RecentDataFiles.Insert(0, fileDescriptor);
        }

        private void AddNewRecentDataFile(DataFileDescriptor dataFileDescriptor)
        {
            if (this.Configuration.RecentDataFiles.Count == 5)
            {
                this.Configuration.RecentDataFiles.RemoveAt(4);

                this.Configuration.RecentDataFiles.Insert(0, dataFileDescriptor);
            }
        }
        #endregion
    }
}
