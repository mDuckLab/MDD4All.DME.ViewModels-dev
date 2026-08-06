using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class DataManagerViewModel : ObservableObject
    {
        private readonly DataEditorViewModel _objectJsonManager;
        private readonly IFileSaveService _fileSaveService;
        //private readonly IFileImportService _fileImportService;

        public DataManagerViewModel(DataEditorViewModel dataManager, IFileSaveService fileSaveService/*, IFileImportService fileImportService*/)
        {
            _objectJsonManager = dataManager;
            _fileSaveService = fileSaveService;
            //_fileImportService = fileImportService;

            _objectJsonManager.PropertyChanged += OnManagerPropertyChanged;
        }

        public object? ActiveObject
        {
            get
            {
                return _objectJsonManager.ActiveObject;
            }
        }

        public Type? SelectedType
        {
            get
            {
                return _objectJsonManager.SelectedType;
            }
        }

        public List<Type> AvailableTypes
        {
            get
            {
                return _objectJsonManager.GetAvailableDataModels();
            }
        }

        //Provides a string representation because the View cannot handle System.Type objects directly. 
        public string? SelectedTypeFullName
        {
            get
            {
                string? result = null;
                if (_objectJsonManager.SelectedType != null)
                {
                    result = _objectJsonManager.SelectedType.FullName;
                }
                return result;
            }
            set
            {
                string? current = this.SelectedTypeFullName;
                if (current != value)
                {
                    List<Type> types = _objectJsonManager.GetAvailableDataModels();
                    Type? foundType = null;

                    foreach (Type availableType in types)
                    {
                        if (availableType.FullName == value)
                        {
                            foundType = availableType;
                        }
                    }
                    _objectJsonManager.SelectedType = foundType;

                    OnPropertyChanged(nameof(SelectedTypeFullName));
                }
            }
        }

        public bool IsNamespaceFilterActive
        {
            get
            {
                return _objectJsonManager.IsNamespaceFilterActive;
            }
            set
            {
                if (_objectJsonManager.IsNamespaceFilterActive != value)
                {
                    _objectJsonManager.IsNamespaceFilterActive = value;
                    OnPropertyChanged(nameof(IsNamespaceFilterActive));
                    OnPropertyChanged(nameof(AvailableTypes));
                }
            }
        }

        public bool HasActiveObject
        {
            get
            {
                bool result = false;
                if (_objectJsonManager.ActiveObject != null)
                {
                    result = true;
                }
                return result;
            }
        }

        public void CreateNew()
        {
            _objectJsonManager.CreateNewInstance();
        }

        //public async Task OpenImportDialog()
        //{
        //    // Nutzt den Service, um das UI-Element "importInput" zu aktivieren
        //    await _fileImportService.OpenImportDialogAsync("importInput");
        //}

        // Reads the content of a browser-provided file, converts it to a JSON string,
        // and passes it to the internal manager to update the application state.
        public async Task Import(IBrowserFile file)
        {
            try
            {
                // Opens a stream to read the file data with a maximum allowed size of 10 MB.
                Stream stream = file.OpenReadStream(1024 * 1024 * 10);

                // Initializes a reader to interpret the raw byte stream as UTF-8 text.
                StreamReader reader = new StreamReader(stream);

                // Asynchronously reads the entire file content into a string.
                string json = await reader.ReadToEndAsync();

                // Instructs the manager to deserialize the JSON string into actual objects.
                _objectJsonManager.LoadFromContent(json);

                // Explicitly releases system resources to ensure memory is freed.
                reader.Dispose();
                stream.Dispose();
            }
            catch (Exception exception)
            {
#if DEBUG
                Console.WriteLine(exception.Message);
#endif
            }
        }

        public async Task Export()
        {
            (string FileName, string Base64Data)? package = _objectJsonManager.GetExportPackage();

            if (package != null)
            {
                await _fileSaveService.SaveFileAsync(package.Value.FileName, package.Value.Base64Data);
            }
        }

        private void OnManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActiveObject")
            {
                OnPropertyChanged(nameof(ActiveObject));
                OnPropertyChanged(nameof(HasActiveObject));
            }
            else if (e.PropertyName == "SelectedType")
            {
                OnPropertyChanged(nameof(SelectedType));
                OnPropertyChanged(nameof(SelectedTypeFullName));
            }
        }
    }
}