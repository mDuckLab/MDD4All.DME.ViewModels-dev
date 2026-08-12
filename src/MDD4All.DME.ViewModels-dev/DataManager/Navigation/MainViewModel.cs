using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.Localization.Contracts;
using System;
using System.ComponentModel;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class MainViewModel : ObservableObject
    {
        #region constructor
        public MainViewModel(ILanguageSetter languageSetter,
                             DataManagerFileViewModel dataFileManager,
                             DataManagerModelViewModel dataModelManager)
        {
            _languageSetter = languageSetter;
            _languageSetter.CultureChanged += OnCultureChanged;

            _dataFileManager = dataFileManager;
            _dataFileManager.PropertyChanged += OnDataFileManagerPropertyChanged;

            _dataModelManager = dataModelManager;
            _dataModelManager.PropertyChanged += OnDataModelManagerPropertyChanged;
        }
        #endregion

        #region Properties

        private ILanguageSetter _languageSetter;

        private DataManagerFileViewModel _dataFileManager;

        private DataManagerModelViewModel _dataModelManager;

        private ViewState _viewState = ViewState.ShowStartPage;

        public ViewState ViewState
        {
            get
            {
                return _viewState;
            }

            set
            {
                _viewState = value;
                OnPropertyChanged(nameof(ViewState));
            }
        }

        // At most one overlay is ever open at once (modals block everything else).
        private OverlayState _activeOverlay = OverlayState.None;

        public OverlayState ActiveOverlay
        {
            get
            {
                return _activeOverlay;
            }

            set
            {
                _activeOverlay = value;
                OnPropertyChanged(nameof(ActiveOverlay));
            }
        }

        #endregion

        #region INavigation

        public void OpenSettings()
        {
            ActiveOverlay = OverlayState.Settings;
        }

        public void ShowStartPage()
        {
            // TODO save changes
            ViewState = ViewState.ShowStartPage;
        }

        #endregion

        #region Event Handlers

        private void OnCultureChanged(object? sender, EventArgs e)
        {
            ActiveOverlay = OverlayState.CultureChange;
        }

        private void OnDataFileManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataManagerFileViewModel.DataSerializationViewModel))
            {
                ViewState = ViewState.ShowEditor;
            }
        }

        private void OnDataModelManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataManagerModelViewModel.AssemblyTreeViewModel))
            {
                if (_dataModelManager.AssemblyTreeViewModel != null)
                {
                    ActiveOverlay = OverlayState.TypeSelection;
                }
                else
                {
                    ActiveOverlay = OverlayState.None;
                }
            }
        }

        #endregion
    }
}
