using MDD4All.Reflection;

namespace MDD4All.DME.ViewModels.Editor
{
    public class EditorState
    {
        private ObjectEditorViewModel _viewModel;

        public EditorState(ObjectEditorViewModel editorViewModel)
        {
            _viewModel = editorViewModel;
        }

        public int MaxDepth { get; set; } = 0;

        public int CurrentDepth { get; set; } = 1;

        public string? BadgeText { get; set; }

        public bool ShowCreateButton
        {
            get
            {
                bool result = false;

                if (_viewModel.Item == null && _viewModel.Access.CanWrite)
                {
                    result = true;
                }

                return result;
            }
        }

        public bool ShowAddButton
        {
            get
            {
                bool result = false;

                // Nothing to add an element to until the collection/dictionary itself exists.
                if (_viewModel.Item != null)
                {
                    if (_viewModel.TypeCategory == TypeCategory.IList || _viewModel.TypeCategory == TypeCategory.Array)
                    {
                        if (_viewModel is IndexedCollectionEditorViewModel)
                        {
                            result = true;
                        }
                    }
                    else if (_viewModel.TypeCategory == TypeCategory.IDictionary)
                    {
                        result = true;
                    }
                }

                return result;
            }
        }

        public bool ShowDeleteModeButton
        {
            get
            {
                bool result = false;

                // Nothing to toggle delete mode for until there's an object with children.
                if (_viewModel.Item != null && _viewModel.HasChildNodes)
                {
                    if (_viewModel.TypeCategory == TypeCategory.IList || _viewModel.TypeCategory == TypeCategory.Array)
                    {
                        if (_viewModel is IndexedCollectionEditorViewModel)
                        {
                            IndexedCollectionEditorViewModel indexedCollectionEditorViewModel = (IndexedCollectionEditorViewModel)_viewModel;
                            if (indexedCollectionEditorViewModel.IsUnderlyingTypeSimple)
                            {
                                result = true;
                            }
                        }
                    }
                    else if (_viewModel.TypeCategory == TypeCategory.IDictionary)
                    {
                        result = true;
                    }
                }

                return result;
            }

        }

        public bool ShowDeleteButton
        {
            get
            {
                bool result = false;

                // Can't delete an object that was never created.
                if (_viewModel.Parent != null && _viewModel.Item != null && _viewModel.Access.CanWrite)
                {
                    result = true;
                }

                return result;
            }
        }

        public bool ShowExpander
        {
            get
            {
                bool result = false;

                // The root card has nothing to collapse into, so it's never collapsible.
                if (_viewModel.HasChildNodes && CurrentDepth > 1)
                {
                    result = true;
                }

                return result;
            }
        }

        public bool IsExpanded { get; set; } = false;

        public bool IsDeleteMode { get; set; } = false;



        public bool CanRenderChildren
        {
            get
            {
                bool result = false;

                // Check depth limit explicitly
                if (MaxDepth == 0 || CurrentDepth < MaxDepth)
                {
                    result = true;
                }

                return result;
            }
        }


    }
}