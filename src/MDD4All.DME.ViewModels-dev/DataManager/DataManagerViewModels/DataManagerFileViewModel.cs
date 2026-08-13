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

        // The path lives here, not in the serialization view model - that one only ever sees content.
        public string CurrentFilePath { get; private set; } = "";

        public string StatusText
        {
            get
            {
                string result = "";
                if (this.DataSerializationViewModel != null)
                {
                    result = "Filename: " + this.CurrentFilePath;
                    result += " ● Data Model: " + _dataManagerSettings.CurrentDataModel!.FullTypeName;
                }
                return result;
            }
        }

        private string _loadErrorMessage = "";

        // Why opening a file failed. MainViewModel watches this and puts it in front of the user -
        // the status bar cannot, because it only exists once the editor is on screen.
        public string LoadErrorMessage
        {
            get
            {
                return _loadErrorMessage;
            }
            private set
            {
                _loadErrorMessage = value;
                this.OnPropertyChanged(nameof(LoadErrorMessage));
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
        // Creates an empty instance of the selected data model and writes it to a new file.
        // The simple direction: the type is already known, so nothing has to be resolved from text.
        private void ExecuteNewDataFile()
        {
            // The dialog blocks until the user answers. Running it directly would block the thread
            // Blazor is currently rendering on, so it is queued and this call returns right away.
            SynchronizationContext.Current?.Post((_) =>
            {
                string fileName = "";

                // Only asks where to save - writing happens further down. The overwrite prompt
                // is part of this dialog, so a true result means the user already agreed to it.
                bool saveLocationChosen = _fileSaver.ShowFileSaveDialog(out fileName,
                                                                        initialDirectory: _dataManagerSettings.LastUsedDataFilePath,
                                                                        title: "New data file...",
                                                                        filter: "JSON file (*.json)|*.json|XML file (*.xml)|*.xml|All files (*.*)|*.*",
                                                                        defaultFileExtension: "json");

                if (saveLocationChosen)
                {
                    // Only a DLL path and a type name - what survived from picking the data model.
                    DataModelDescriptor? currentType = _dataManagerSettings.CurrentDataModel;

                    if (currentType != null)
                    {
                        // Turns those two strings back into a usable type, loading the DLL again.
                        Type? type = _dataManagerModel.ResolveDataModelType(currentType);

                        if (type != null)
                        {
                            this.CurrentFilePath = fileName;

                            this.DataSerializationViewModel = new DataSerializationViewModel(type);

                            // A plain Activator.CreateInstance - no serialization involved, which is
                            // why this path needs none of the machinery that opening a file does.
                            this.DataSerializationViewModel.CreateNewInstance();

                            // Written out immediately, so a new file exists on disk even if the user
                            // never edits anything.
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

                            // Remembers file and model together, so reopening it later does not
                            // depend on which model happens to be selected then.
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
                    // The model was stored together with the file, so there is nothing to verify.
                    bool loaded = this.LoadDataFile(descriptor.FilePath, type, verifyRootType: false);

                    // Same reasoning as when opening by dialog: a file that will not open should
                    // neither switch the active model nor reorder the recent list.
                    if (loaded)
                    {
                        _dataManagerSettings.SetRecentDataFileToTop(index);

                        _dataManagerModel.ActivateDataModel(descriptor.DataModelDescription);
                    }
                }
                else
                {
                    this.LoadErrorMessage = "Der Typ \"" + descriptor.DataModelDescription.FullTypeName
                                            + "\" wurde in der Datenmodell-DLL nicht gefunden.";
                }
            }
        }

        // Rebuilds an object graph from a file. Unlike creating one, this has to turn the type
        // names stored in the file back into real types, which is where the load context matters.
        private void ExecuteOpenDataFile()
        {
            // Queued for the same reason as when creating a file - the dialog blocks.
            SynchronizationContext.Current?.Post((_) =>
            {
                string filename = "";
                bool fileChosen = _fileLoader.ShowOpenFileDialog(out filename,
                                                                 initialDirectory: _dataManagerSettings.LastUsedDataFilePath,
                                                                 filter: "JSON file (*.json)|*.json|XML file (*.xml)|*.xml|All files (*.*)|*.*",
                                                                 title: "Open data file...",
                                                                 defaultFileExtension: "json");

                if (fileChosen)
                {
                    // Stays empty unless the file was saved with the type information setting on.
                    DataModelDescriptor? descriptorFromFile = _dataManagerModel.FindDataModelForFile(filename);

                    bool typeFoundInFile = descriptorFromFile != null;

                    // Which source it came from decides whether it can be trusted further down.
                    DataModelDescriptor? descriptor;

                    if (typeFoundInFile)
                    {
                        descriptor = descriptorFromFile;
                    }
                    else
                    {
                        descriptor = _dataManagerSettings.CurrentDataModel;
                    }

                    if (descriptor == null)
                    {
                        // Neither source produced anything - a fresh installation where no data
                        // model was ever picked, opening a file that does not name one either.
                        this.LoadErrorMessage = "Es ist kein Datenmodell ausgewählt, und die Datei nennt selbst keines.";
                    }
                    else
                    {
                        Type? type = _dataManagerModel.ResolveDataModelType(descriptor);

                        if (type == null)
                        {
                            // The DLL is there but no longer holds that type - renamed or replaced.
                            this.LoadErrorMessage = "Der Typ \"" + descriptor.FullTypeName + "\" wurde in der Datenmodell-DLL nicht gefunden.";
                        }
                        else
                        {
                            // Verified only when the file did not state the type - then it is a guess.
                            bool loaded = this.LoadDataFile(filename, type, verifyRootType: !typeFoundInFile);

                            // Both write to the configuration file, so a file that would not open
                            // must not switch the model or take the top spot in the recent list.
                            if (loaded)
                            {
                                // The file decides which model is active, not the other way round.
                                _dataManagerModel.ActivateDataModel(descriptor);

                                DataFileDescriptor dataFileDescriptor = new DataFileDescriptor
                                {
                                    DataModelDescription = descriptor,
                                    FilePath = filename
                                };

                                _dataManagerSettings.AddNewRecentDataFile(dataFileDescriptor);
                            }
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
            FileInfo fileInfo = new FileInfo(this.CurrentFilePath);

            if (fileInfo.DirectoryName != null)
            {
                _dataManagerSettings.LastUsedDataFilePath = fileInfo.DirectoryName;
            }

            // Read at save time, so toggling the setting takes effect without reopening the file.
            this.DataSerializationViewModel!.IncludeTypeInformation = _dataManagerSettings.SaveTypeInformation;

            if (fileInfo.Extension.ToLower() == ".xml")
            {
                this.SerializeToXml();
            }
            else
            {
                File.WriteAllText(this.CurrentFilePath, this.DataSerializationViewModel.ActiveObjectJsonString);
            }
        }

        private void ExecuteSaveDataFileAs()
        {
            SynchronizationContext.Current?.Post((_) =>
            {
                string fileName = "";

                bool saveLocationChosen = _fileSaver.ShowFileSaveDialog(out fileName,
                                                                        initialDirectory: _dataManagerSettings.LastUsedDataFilePath,
                                                                        title: "Save data file as...",
                                                                        filter: "JSON file (*.json)|*.json|XML file (*.xml)|*.xml|All files (*.*)|*.*");

                if (saveLocationChosen)
                {
                    FileInfo fileInfo = new FileInfo(fileName);

                    this.CurrentFilePath = fileName;

                    // Read at save time, so toggling the setting takes effect without reopening the file.
                    this.DataSerializationViewModel!.IncludeTypeInformation = _dataManagerSettings.SaveTypeInformation;

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
        // Hands nothing over until the load worked - a failed attempt used to leave the path
        // pointing at a file that was never read, so the next save wrote over it.
        private bool LoadDataFile(string filePath, Type dataModelRootType, bool verifyRootType)
        {
            bool loaded = false;

            string content = "";
            bool contentRead = false;

            try
            {
                content = File.ReadAllText(filePath);
                contentRead = true;
            }
            catch (Exception exception)
            {
                this.LoadErrorMessage = "Die Datei konnte nicht gelesen werden.";
                Console.WriteLine(exception);
            }

            if (contentRead)
            {
                // Shown to nobody yet, so a failure below leaves the open file exactly as it was.
                DataSerializationViewModel candidate = new DataSerializationViewModel(dataModelRootType);

                LoadResult result;

                // The format follows from the file name, which is this class's business. What to
                // make of the content is not.
                if (filePath.ToLower().EndsWith("xml"))
                {
                    result = candidate.LoadFromXml(content);
                }
                else
                {
                    result = candidate.LoadFromJson(content, verifyRootType);
                }

                if (result == LoadResult.Loaded)
                {
                    this.LoadErrorMessage = "";

                    this.CurrentFilePath = filePath;
                    this.DataSerializationViewModel = candidate;

                    // The object was filled before anyone subscribed, so the usual notification
                    // from the ActiveObject setter never fired - the editor is told here instead.
                    this.OnPropertyChanged(nameof(DataSerializationViewModel));
                    this.OnPropertyChanged(nameof(StatusText));

                    loaded = true;
                }
                else
                {
                    this.LoadErrorMessage = this.DescribeLoadFailure(result, dataModelRootType);
                }
            }

            return loaded;
        }

        // The serialization view model reports the cause and stays free of wording - phrasing it
        // for the user belongs closer to the screen.
        private string DescribeLoadFailure(LoadResult result, Type dataModelRootType)
        {
            string message;

            switch (result)
            {
                case LoadResult.NotReadableAsJson:
                    message = "Die Datei ist kein gültiges JSON.";
                    break;

                case LoadResult.DoesNotMatchType:
                    message = "Die Datei passt nicht zum gewählten Datenmodell \""
                              + dataModelRootType.Name
                              + "\". Sie enthält keine Typinformation, daher lässt sich das richtige Modell nicht bestimmen.";
                    break;

                case LoadResult.NoObject:
                    message = "Die Datei enthält kein verwertbares Objekt.";
                    break;

                default:
                    message = "Die Datei konnte nicht gelesen werden - sie passt vermutlich nicht zum Datenmodell \""
                              + dataModelRootType.Name + "\".";
                    break;
            }

            return message;
        }

        private void SerializeToXml()
        {
            XmlSerializer mySerializer = new XmlSerializer(this.DataSerializationViewModel!.SelectedType!);

            StreamWriter myWriter = new StreamWriter(this.CurrentFilePath);
            mySerializer.Serialize(myWriter, this.DataSerializationViewModel.ActiveObject);
            myWriter.Close();
        }
        #endregion
    }
}
