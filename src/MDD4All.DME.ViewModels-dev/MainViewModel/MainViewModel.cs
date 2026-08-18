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
                             DataManagerModelViewModel dataModelManager,
                             DataManagerObjectViewModel dataManagerObject)
        {
            _languageSetter = languageSetter;
            _languageSetter.CultureChanged += OnCultureChanged;

            _dataFileManager = dataFileManager;
            _dataFileManager.PropertyChanged += OnDataFileManagerPropertyChanged;

            _dataModelManager = dataModelManager;
            _dataModelManager.PropertyChanged += OnDataModelManagerPropertyChanged;

            // Never replaced, so subscribing once here is enough for the whole run.
            _dataManagerObject = dataManagerObject;
            _dataManagerObject.PropertyChanged += OnDataManagerObjectPropertyChanged;
        }
        #endregion

        #region Properties

        private ILanguageSetter _languageSetter;

        private DataManagerFileViewModel _dataFileManager;

        private DataManagerModelViewModel _dataModelManager;

        private DataManagerObjectViewModel _dataManagerObject;

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

                // A message about a file that would not open is stale as soon as another one did.
                DismissNotification();
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

        // Sits below both the start page and the editor, so it can report things that happen
        // before the editor is ever on screen - which the status bar cannot.
        private string _notificationMessage = "";

        public string NotificationMessage
        {
            get
            {
                return _notificationMessage;
            }

            private set
            {
                _notificationMessage = value;
                OnPropertyChanged(nameof(NotificationMessage));
            }
        }

        private NotificationSeverity _notificationSeverity = NotificationSeverity.Info;

        public NotificationSeverity NotificationSeverity
        {
            get
            {
                return _notificationSeverity;
            }

            private set
            {
                _notificationSeverity = value;
                OnPropertyChanged(nameof(NotificationSeverity));
            }
        }

        #endregion

        #region Notifications

        public void ShowNotification(string message, NotificationSeverity severity)
        {
            NotificationSeverity = severity;
            NotificationMessage = message;
        }

        // Called by the view once it has hidden the message, either on its own or by the user.
        public void DismissNotification()
        {
            NotificationMessage = "";
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

        // A file was opened or created - that is the moment the editor takes over the screen.
        private void OnDataManagerObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataManagerObjectViewModel.RootObject))
            {
                ViewState = ViewState.ShowEditor;
            }
        }

        private void OnDataFileManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataManagerFileViewModel.LoadErrorMessage))
            {
                if (_dataFileManager.LoadErrorMessage != "")
                {
                    ShowNotification(_dataFileManager.LoadErrorMessage, NotificationSeverity.Error);
                }
                else
                {
                    // Cleared after a load that worked, so the previous complaint can go.
                    DismissNotification();
                }
            }
            else if (e.PropertyName == nameof(DataManagerFileViewModel.SaveWarningMessage))
            {
                if (_dataFileManager.SaveWarningMessage != "")
                {
                    ShowNotification(_dataFileManager.SaveWarningMessage, NotificationSeverity.Error);
                }
            }
            else if (e.PropertyName == nameof(DataManagerFileViewModel.ShowComplexKeyWarning))
            {
                // A save that would drop data has to be answered, not just mentioned - the
                // notification below the content is too easy to save straight past.
                if (_dataFileManager.ShowComplexKeyWarning)
                {
                    ActiveOverlay = OverlayState.ComplexKeyWarning;
                }
                else
                {
                    ActiveOverlay = OverlayState.None;
                }
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
