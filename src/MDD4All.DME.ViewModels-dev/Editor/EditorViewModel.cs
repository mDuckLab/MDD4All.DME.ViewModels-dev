using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.DME.ViewModels.Editor;
using MDD4All.DME.ViewModels.Editor.Settings;
using MDD4All.UI.DataModels.Tree;
using System;
using System.ComponentModel;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class EditorViewModel : ObservableObject
    {
        #region constructor
        public EditorViewModel(DataManagerObjectViewModel dataManagerObject,
                               EditorAppearanceSettingsViewModel editorSettings)
        {
            _dataManagerObject = dataManagerObject;
            _dataManagerObject.PropertyChanged += OnDataManagerObjectPropertyChanged;

            _editorSettings = editorSettings;

            // As a lazily-constructed singleton, this can be built after a data file
            // was already loaded (e.g. the switch to the Editor screen itself triggers
            // this construction) - sync with the current state instead of only
            // waiting for the next change notification, which would otherwise be missed.
            RebuildTree();
        }
        #endregion

        #region Properties

        private ObjectTreeViewModel? _treeViewModel;

        private readonly DataManagerObjectViewModel _dataManagerObject;

        // The visible depth is stored across sessions, so it has to be held against whatever
        // document is open now.
        private readonly EditorAppearanceSettingsViewModel _editorSettings;

        // The deepest level the stepper offers as a number: one below what the document has.
        //
        // That last level belongs to "All", and the difference is not cosmetic. A number names a
        // fixed level and stays where it is when the tree grows - which it does the moment Create
        // builds a subtree. "All" is a rule instead of a level, so it keeps meaning everything.
        // Were the highest number the full depth, the two would be the same until the first
        // Create silently turned one of them into "almost everything".
        //
        // Two at the least, below that the stepper has nothing to show.
        public int MaxSelectableDepth
        {
            get
            {
                int result = 2;

                if (this.TreeViewModel != null && this.TreeViewModel.MaxDepth - 1 > result)
                {
                    result = this.TreeViewModel.MaxDepth - 1;
                }

                return result;
            }
        }

        public ObjectTreeViewModel? TreeViewModel
        {
            get
            {
                return _treeViewModel;
            }

            private set
            {
                if (_treeViewModel != value)
                {
                    _treeViewModel = value;
                    OnPropertyChanged(nameof(TreeViewModel));
                    OnPropertyChanged(nameof(SelectedEditorViewModel));
                }
            }
        }

        public ITreeNode? SelectedEditorViewModel
        {
            get
            {
                ITreeNode? result = null;
                if (TreeViewModel != null)
                {
                    result = TreeViewModel.SelectedNode;
                }
                return result;
            }
        }

        private bool _showRawData = false;

        public bool ShowRawData
        {
            get
            {
                return _showRawData;
            }

            set
            {
                _showRawData = value;
                OnPropertyChanged(nameof(ShowRawData));
            }
        }

        private bool _showXml = false;

        // Which of the two formats the raw data view shows. Purely a question of what is on
        // screen - the serializer produces both either way.
        public bool ShowXml
        {
            get
            {
                return _showXml;
            }

            set
            {
                _showXml = value;
                OnPropertyChanged(nameof(ShowXml));
            }
        }

        #endregion

        #region Event Handlers
        private void OnDataManagerObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataManagerObjectViewModel.RootObject))
            {
                RebuildTree();
            }
        }

        private void RebuildTree()
        {
            if (this.TreeViewModel != null)
            {
                this.TreeViewModel.PropertyChanged -= this.OnTreePropertyChanged;
            }

            object? rootObject = _dataManagerObject.RootObject;
            Type? rootType = _dataManagerObject.RootType;

            if (rootObject != null || rootType != null)
            {
                ObjectTreeViewModel newTree = new ObjectTreeViewModel(rootObject, rootType);
                newTree.PropertyChanged += this.OnTreePropertyChanged;
                TreeViewModel = newTree;
            }
            else
            {
                TreeViewModel = null;
            }

            this.ClampVisibleDepth();
        }

        // The visible depth survives a restart, so a document opened later can be shallower than
        // whatever was set for the last one. Left alone, the stepper would show a level the tree
        // does not have.
        private void ClampVisibleDepth()
        {
            // 0 stands for "All" and fits every document.
            if (_editorSettings.MaxDepth > this.MaxSelectableDepth)
            {
                _editorSettings.MaxDepth = this.MaxSelectableDepth;
            }
        }

        private void OnTreePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedNode")
            {
                if (TreeViewModel?.SelectedNode is ObjectEditorViewModel selectedNode)
                {
                    selectedNode.EditorState.IsExpanded = true;
                }

                OnPropertyChanged(nameof(SelectedEditorViewModel));
            }
            else if (e.PropertyName == "TreeChanged")
            {
                OnPropertyChanged(nameof(SelectedEditorViewModel));
            }
        }

        #endregion
    }
}
