using MDD4All.DME.DataAccess.DataFiles;
using MDD4All.DME.DataAccess.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDD4All.DME.Configurations;
using MDD4All.FileAccess.Contracts;
using System;
using System.IO;
using System.Threading;
using System.Windows.Input;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class DataManagerFileViewModel : ObservableObject
    {
        #region constructor
        public DataManagerFileViewModel(IFileLoader fileLoader,
                                        IFileSaver fileSaver,
                                        DataManagerSettingsViewModel dataManagerSettings,
                                        DataManagerModelViewModel dataManagerModel,
                                        DataManagerObjectViewModel dataManagerObject,
                                        DataFileProvider dataFileProvider,
                                        DataSerializer dataSerializer,
                                        DictionaryKeyAnalyzer dictionaryKeyAnalyzer)
        {
            _fileLoader = fileLoader;
            _fileSaver = fileSaver;
            _dataManagerSettings = dataManagerSettings;
            _dataManagerModel = dataManagerModel;
            _dataManagerObject = dataManagerObject;
            _dataFileProvider = dataFileProvider;
            _dataSerializer = dataSerializer;
            _dictionaryKeyAnalyzer = dictionaryKeyAnalyzer;

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

        // Where the loaded object lives. This class is the only one that fills it.
        private readonly DataManagerObjectViewModel _dataManagerObject;

        // Path to object and back. Everything that touches the disk goes through here.
        private readonly DataFileProvider _dataFileProvider;

        // Object to text, for the raw data view. Writing goes through the provider above.
        private readonly DataSerializer _dataSerializer;

        // Asked before every save whether anything would be dropped.
        private readonly DictionaryKeyAnalyzer _dictionaryKeyAnalyzer;

        #region Logic
        // The path lives here, not with the object - that one only ever sees content.
        // It only ever names a file that was really read or written, which is why the pending one
        // below waits separately instead of moving this one early.
        public string CurrentFilePath { get; private set; } = "";

        // Where a confirmed save will write to. Set while the warning below is on screen, because
        // the command returns before the answer arrives and Save As must not ask for a path twice.
        private string _pendingSavePath = "";

        // The open object as text. The same string goes to disk and onto the screen, and the
        // settings are read here rather than kept, so changing one takes effect right away.
        public string JsonString
        {
            get
            {
                string result = string.Empty;

                if (_dataManagerObject.RootObject != null)
                {
                    result = _dataSerializer.ToJson(_dataManagerObject.RootObject,
                                                       _dataManagerSettings.SaveTypeInformation,
                                                       _dataManagerSettings.WriteComplexDictionaryKeys);
                }

                return result;
            }
        }

        public string XmlString
        {
            get
            {
                string result = string.Empty;

                if (_dataManagerObject.RootObject != null)
                {
                    result = _dataSerializer.ToXml(_dataManagerObject.RootObject);
                }

                return result;
            }
        }
        #endregion

        #region UI
        public string StatusText
        {
            get
            {
                string result = "";
                if (_dataManagerObject.HasContent)
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

        private bool _showComplexKeyWarning;

        // Kept apart from LoadErrorMessage: that one reports a file that could not be opened and
        // clears itself, this one blocks until it is answered.
        public bool ShowComplexKeyWarning
        {
            get
            {
                return _showComplexKeyWarning;
            }
            private set
            {
                if (_showComplexKeyWarning != value)
                {
                    _showComplexKeyWarning = value;
                    this.OnPropertyChanged(nameof(ShowComplexKeyWarning));
                }
            }
        }

        private string _complexKeyWarningMessage = "";

        public string ComplexKeyWarningMessage
        {
            get
            {
                return _complexKeyWarningMessage;
            }
            private set
            {
                _complexKeyWarningMessage = value;
                this.OnPropertyChanged(nameof(ComplexKeyWarningMessage));
            }
        }

        private string _saveWarningMessage = "";

        // What a finished save had to leave out. The counterpart to LoadErrorMessage, kept apart
        // from it so neither one clears the other.
        public string SaveWarningMessage
        {
            get
            {
                return _saveWarningMessage;
            }
            private set
            {
                _saveWarningMessage = value;
                this.OnPropertyChanged(nameof(SaveWarningMessage));
            }
        }
        #endregion

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

                            // A plain Activator.CreateInstance - no serialization involved, which is
                            // why this path needs none of the machinery that opening a file does.
                            object? newInstance = _dataSerializer.CreateInstance(type);

                            _dataManagerObject.SetObject(type, newInstance);

                            // Written out immediately, so a new file exists on disk even if the user
                            // never edits anything. Straight to the file rather than through the
                            // save command: the warning about dropped entries belongs to a save the
                            // user asked for, not to a file that is only just being created.
                            this.WriteDataFile(fileName);

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
                            // Reads the file and builds the object from it. The type is held against
                            // the file's contents only when it was guessed - one the file named
                            // itself needs no checking.
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
            this.SaveOrAskFirst(this.CurrentFilePath);
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
                    this.SaveOrAskFirst(fileName);
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

            // Handed back rather than stored, so a failure leaves the open file exactly as it
            // was - there is simply nothing to take over.
            object? loadedObject;

            LoadResult result = _dataFileProvider.Read(filePath, dataModelRootType, verifyRootType,
                                                       out loadedObject);

            if (result == LoadResult.Loaded)
            {
                this.LoadErrorMessage = "";

                this.CurrentFilePath = filePath;

                // The one moment a document changes. Everything watching the object hears it
                // from here, whether it was opened, created or replaced.
                _dataManagerObject.SetObject(dataModelRootType, loadedObject);

                this.OnPropertyChanged(nameof(StatusText));

                loaded = true;
            }
            else
            {
                this.LoadErrorMessage = this.DescribeLoadFailure(result, dataModelRootType);
            }

            return loaded;
        }

        // Saving with complex dictionary keys turned off drops those entries. Losing part of an
        // object graph is not something to mention in passing, so nothing is written until the
        // user has answered - and the affected properties are named, because "some data will be
        // lost" is not something anyone can act on.
        private void SaveOrAskFirst(string filePath)
        {
            string[] affected = Array.Empty<string>();

            if (!_dataManagerSettings.WriteComplexDictionaryKeys)
            {
                affected = _dictionaryKeyAnalyzer.FindDictionariesWithComplexKey(_dataManagerObject.RootType);
            }

            if (affected.Length == 0)
            {
                this.WriteDataFile(filePath);
            }
            else if (_dataManagerSettings.ConfirmComplexKeyLossWithDialog)
            {
                // Kept until the answer arrives. Save As has already asked for the path by now
                // and must not open its file dialog a second time.
                _pendingSavePath = filePath;

                this.ComplexKeyWarningMessage = "Diese Einträge werden beim Speichern verworfen, weil komplexe "
                                                + "Dictionary-Schlüssel abgeschaltet sind: "
                                                + string.Join(", ", affected) + ".";

                this.ShowComplexKeyWarning = true;
            }
            else
            {
                // The other setting: write first, report afterwards. Nothing left to answer, so
                // the wording states what happened instead of what is about to.
                this.WriteDataFile(filePath);

                this.SaveWarningMessage = "Diese Einträge wurden beim Speichern verworfen, weil komplexe "
                                          + "Dictionary-Schlüssel abgeschaltet sind: "
                                          + string.Join(", ", affected) + ".";
            }
        }

        // The answer to the warning above. Cancelling leaves the file on disk untouched.
        public void AnswerComplexKeyWarning(bool writeAnyway)
        {
            this.ShowComplexKeyWarning = false;

            if (writeAnyway)
            {
                this.WriteDataFile(_pendingSavePath);
            }

            _pendingSavePath = "";
        }

        // The one place a data file is written. Both save commands end up here, and so does the
        // warning dialog once it has been confirmed.
        private void WriteDataFile(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            this.CurrentFilePath = filePath;

            _dataFileProvider.Write(filePath, _dataManagerObject.RootObject!,
                                    _dataManagerSettings.SaveTypeInformation,
                                    _dataManagerSettings.WriteComplexDictionaryKeys);

            DataFileDescriptor dataFileDescriptor = new DataFileDescriptor()
            {
                DataModelDescription = _dataManagerSettings.CurrentDataModel!,
                FilePath = filePath
            };

            _dataManagerSettings.AddNewRecentDataFile(dataFileDescriptor);

            if (fileInfo.DirectoryName != null)
            {
                _dataManagerSettings.LastUsedDataFilePath = fileInfo.DirectoryName;
            }

            this.OnPropertyChanged(nameof(StatusText));
        }

        // The serialization view model reports the cause and stays free of wording - phrasing it
        // for the user belongs closer to the screen.
        private string DescribeLoadFailure(LoadResult result, Type dataModelRootType)
        {
            string message;

            switch (result)
            {
                case LoadResult.FileNotReadable:
                    message = "Die Datei konnte nicht gelesen werden.";
                    break;

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

        #endregion
    }
}
