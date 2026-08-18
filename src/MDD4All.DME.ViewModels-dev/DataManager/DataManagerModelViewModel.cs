using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDD4All.AssemblyLoading.Contracts;
using MDD4All.DME.AssemblyTree.ViewModels;
using MDD4All.DME.Configurations;
using MDD4All.FileAccess.Contracts;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Input;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class DataManagerModelViewModel : ObservableObject
    {
        #region constructor
        public DataManagerModelViewModel(IFileLoader fileLoader,
                                        IAssemblyProvider assemblyProvider,
                                        DataManagerSettingsViewModel dataManagerSettings)
        {
            _fileLoader = fileLoader;
            _assemblyProvider = assemblyProvider;
            _dataManagerSettings = dataManagerSettings;

            this.InitializeCommands();
        }

        private void InitializeCommands()
        {
            this.OpenDataModelCommand = new RelayCommand(this.ExecuteOpenDataModel);
            this.ConfirmOpenDataModelCommand = new RelayCommand<DataModelDescriptor>(this.ExecuteConfirmOpenDataModelCommand);
            this.SetDataModelFromRecentListCommand = new RelayCommand<int>(this.ExecuteSetDataModelFromRecentList);
        }
        #endregion

        #region Properties
        private readonly IFileLoader _fileLoader;

        private readonly IAssemblyProvider _assemblyProvider;

        private readonly DataManagerSettingsViewModel _dataManagerSettings;

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

        #region Commands
        public ICommand OpenDataModelCommand { get; private set; } = null!;

        public ICommand ConfirmOpenDataModelCommand { get; private set; } = null!;

        public ICommand SetDataModelFromRecentListCommand { get; private set; } = null!;
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
                                                                 initialDirectory: _dataManagerSettings.LastUsedDataModelPath
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
                this.ActivateDataModel(descriptor);

                FileInfo fileInfo = new FileInfo(descriptor.DllPath);

                if (fileInfo.DirectoryName != null)
                {
                    _dataManagerSettings.LastUsedDataModelPath = fileInfo.DirectoryName;
                }
            }

            // Closes the type-selection dialog: MainViewModel watches this
            // property to know when to switch back to the start page.
            this.AssemblyTreeViewModel = null;
        }

        private void ExecuteSetDataModelFromRecentList(int index)
        {
            DataModelDescriptor descriptor = _dataManagerSettings.RecentDataModels[index];

            this.ActivateDataModel(descriptor);
        }
        #endregion

        #region Helpers
        // Makes the given model the current one. Everything that opens a file ends up here,
        // so the model only ever changes in one place.
        public void ActivateDataModel(DataModelDescriptor descriptor)
        {
            _dataManagerSettings.CurrentDataModel = descriptor;

            _dataManagerSettings.SetTopRecentDataModel(descriptor);
        }

        // The DLL is picked at runtime, so a stored descriptor only becomes a usable type
        // by going through the assembly provider.
        public Type? ResolveDataModelType(DataModelDescriptor descriptor)
        {
            Assembly assembly = _assemblyProvider.GetAssemblyByPath(descriptor.DllPath);

            return assembly.GetType(descriptor.FullTypeName);
        }

        // A saved file names its own type assembly-qualified ("Namespace.Type, AssemblyName"), so the
        // model it belongs to can be derived from the file instead of having to be picked beforehand.
        public DataModelDescriptor? FindDataModelForFile(string filePath)
        {
            DataModelDescriptor? result = null;

            if (filePath.ToLower().EndsWith("json"))
            {
                string? qualifiedTypeName = DataSerializationViewModel.ReadTypeNameFromJson(File.ReadAllText(filePath));

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
        #endregion
    }
}
