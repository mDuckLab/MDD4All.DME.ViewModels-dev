using MDD4All.DME.DataAccess.DataFiles;
using MDD4All.DME.DataAccess.DataModels;
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
                                        DataModelCatalog dataModelCatalog,
                                        DataManagerObjectViewModel dataManagerObject,
                                        DataFileProvider dataFileProvider,
                                        DataSerializer dataSerializer)
        {
            _fileLoader = fileLoader;
            _fileSaver = fileSaver;
            _dataManagerSettings = dataManagerSettings;
            _dataModelCatalog = dataModelCatalog;
            _dataManagerObject = dataManagerObject;
            _dataFileProvider = dataFileProvider;
            _dataSerializer = dataSerializer;

            this.InitializeCommands();
        }

        private void InitializeCommands()
        {
            this.NewDataFileCommand = new RelayCommand<Type>(this.ExecuteNewDataFile);
            this.OpenDataFileCommand = new RelayCommand(this.ExecuteOpenDataFile);
            this.SaveDataFileCommand = new RelayCommand(this.ExecuteSaveDataFile);
            this.SaveDataFileAsCommand = new RelayCommand(this.ExecuteSaveDataFileAs);
        }
        #endregion

        #region Properties
        private readonly IFileLoader _fileLoader;
        private readonly IFileSaver _fileSaver;

        private readonly DataManagerSettingsViewModel _dataManagerSettings;

        // The data models compiled into the solution - asked which ones exist and what a type
        // name out of a file refers to.
        private readonly DataModelCatalog _dataModelCatalog;

        // Where the loaded object lives. This class is the only one that fills it.
        private readonly DataManagerObjectViewModel _dataManagerObject;

        // Path to object and back. Everything that touches the disk goes through here.
        private readonly DataFileProvider _dataFileProvider;

        // Only asked to build an empty instance - writing goes through the provider above.
        private readonly DataSerializer _dataSerializer;


        #region Logic
        // The path lives here, not with the object - that one only ever sees content. Empty
        // for an object that was created and never saved, which is what makes Save ask first.
        public string CurrentFilePath { get; private set; } = "";


        #endregion

        #region UI
        // The status bar is the only place left that talks to the user, so a failed load is
        // reported here rather than in a bar of its own.
        public string StatusText
        {
            get
            {
                string result = "";

                if (this.LoadErrorMessage.Length > 0)
                {
                    result = this.LoadErrorMessage;
                }
                else if (_dataManagerObject.HasContent)
                {
                    if (this.CurrentFilePath.Length > 0)
                    {
                        result = "File: " + this.CurrentFilePath;
                    }
                    else
                    {
                        result = "File: not saved yet";
                    }

                    result += " ● Data Model: " + _dataManagerObject.RootType!.Name;
                }

                return result;
            }
        }

        private string _loadErrorMessage = "";

        // Why opening a file failed. Shown in the status bar above, and cleared by the next
        // load that works.
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
                this.OnPropertyChanged(nameof(StatusText));
            }
        }



        #endregion

        // Takes the complaint off the bar. The next load overwrites or clears it anyway, this is
        // just so it does not have to sit there until then.
        public void DismissLoadError()
        {
            this.LoadErrorMessage = "";
        }

        #endregion

        #region Commands
        public ICommand NewDataFileCommand { get; private set; } = null!;

        public ICommand OpenDataFileCommand { get; private set; } = null!;

        public ICommand SaveDataFileCommand { get; private set; } = null!;

        public ICommand SaveDataFileAsCommand { get; private set; } = null!;
        #endregion

        #region Command Implementations
        // Creates an empty instance of the given data model. Nothing is written yet and no
        // dialog appears - a new object lives in memory until the first save, which is when a
        // file name is asked for.
        private void ExecuteNewDataFile(Type? dataModelRootType)
        {
            if (dataModelRootType != null)
            {
                // A plain Activator.CreateInstance - no serialization involved, which is why this
                // path needs none of the machinery that opening a file does.
                object? newInstance = _dataSerializer.CreateInstance(dataModelRootType);

                // No file behind it yet. Save falls back to Save As while this is empty.
                this.CurrentFilePath = "";

                _dataManagerObject.SetObject(dataModelRootType, newInstance);

                this.OnPropertyChanged(nameof(StatusText));
            }
        }

        // Rebuilds an object graph from a file. Unlike creating one, this has to turn the type
        // name stored in the file back into a real type.
        private void ExecuteOpenDataFile()
        {
            // Queued for the same reason as when creating a file - the dialog blocks.
            SynchronizationContext.Current?.Post((_) =>
            {
                string filename = "";
                bool fileChosen = _fileLoader.ShowOpenFileDialog(out filename,
                                                                 initialDirectory: _dataManagerSettings.LastUsedDataFilePath,
                                                                 filter: "JSON file (*.json)|*.json|All files (*.*)|*.*",
                                                                 title: "Open data file...",
                                                                 defaultFileExtension: "json");

                if (fileChosen)
                {
                    // Empty unless the file was saved with the type information setting on.
                    string? typeNameFromFile = _dataFileProvider.ReadTypeName(filename);

                    bool typeFoundInFile = false;

                    Type? type = null;

                    if (typeNameFromFile != null)
                    {
                        type = _dataModelCatalog.ResolveTypeName(typeNameFromFile);

                        typeFoundInFile = (type != null);
                    }

                    if (type == null)
                    {
                        // Nothing in the file, so the only candidate left is whatever is open.
                        type = _dataManagerObject.RootType;
                    }

                    if (type == null)
                    {
                        this.LoadErrorMessage = "The file does not name a data model, and nothing is open to compare it against.";
                    }
                    else
                    {
                        // Reads the file and builds the object from it. The type is held against
                        // the file's contents only when it was guessed - one the file named
                        // itself needs no checking.
                        this.LoadDataFile(filename, type, verifyRootType: !typeFoundInFile);
                    }
                }
            }, null);
        }

        private void ExecuteSaveDataFile()
        {
            // A newly created object has no file yet, so the first save has to ask for one.
            if (this.CurrentFilePath.Length == 0)
            {
                this.ExecuteSaveDataFileAs();
            }
            else
            {
                this.WriteDataFile(this.CurrentFilePath);
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
                                                                        filter: "JSON file (*.json)|*.json|All files (*.*)|*.*");

                if (saveLocationChosen)
                {
                    this.WriteDataFile(fileName);
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

        // The one place a data file is written. Both save commands end up here.
        private void WriteDataFile(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            this.CurrentFilePath = filePath;

            _dataFileProvider.Write(filePath, _dataManagerObject.RootObject!,
                                    _dataManagerSettings.SaveTypeInformation,
                                    // Always written in the Key/Value form - dropping them was a
                                    // setting this branch does not carry.
                                    writeComplexDictionaryKeys: true);


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
                    message = "The file could not be read.";
                    break;

                case LoadResult.NotReadableAsJson:
                    message = "The file is not valid JSON.";
                    break;

                case LoadResult.DoesNotMatchType:
                    message = "The file does not match the selected data model \""
                              + dataModelRootType.Name
                              + "\". It carries no type information, so the right model cannot be determined.";
                    break;

                case LoadResult.NoObject:
                    message = "The file contains no usable object.";
                    break;

                default:
                    message = "The file could not be read - it probably does not match the data model \""
                              + dataModelRootType.Name + "\".";
                    break;
            }

            return message;
        }

        #endregion
    }
}
