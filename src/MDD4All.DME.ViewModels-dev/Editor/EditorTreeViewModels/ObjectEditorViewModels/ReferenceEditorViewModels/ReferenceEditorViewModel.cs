using CommunityToolkit.Mvvm.Input;
using MDD4All.Reflection;
using MDD4All.ObjectGraph.Access;
using MDD4All.UI.DataModels.Tree;
using System;
using System.Windows.Input;

namespace MDD4All.DME.ViewModels.Editor
{
    public abstract class ReferenceEditorViewModel : ObjectEditorViewModel
    {
        #region Constructors and Initialization
        public ReferenceEditorViewModel(ITree tree, Access access, object item, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, item, null, title, parent, preAnalyzedResult)
        {
            this.InitializeCommands();
        }

        public ReferenceEditorViewModel(ITree tree, Access access, Type targetType, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, null, targetType, title, parent, preAnalyzedResult)
        {
            this.InitializeCommands();
        }

        public ReferenceEditorViewModel(ITree tree, Access access, object? item, Type? targetType, string? title = "", ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, item, targetType, title, parent, preAnalyzedResult)
        {
            this.InitializeCommands();
        }

        private void InitializeCommands()
        {
            // RelayCommand comes from CommunityToolkit.Mvvm.Input -> RelayCommand(Action (Functionpointer) , bool (execute possible))
            this.DeleteItemCommand = new RelayCommand(this.ExecuteDeleteItem);
        }
        #endregion

        #region Factory Method
        public static ObjectEditorViewModel? CreateChildViewModel(ITree tree, Access access, object? item, Type? targetType, string? title, ITreeNode parent, TypeAnalyzer? preAnalyzer = null)
        {
            ObjectEditorViewModel? result = null;
            TypeAnalyzer analyzer;

            if (preAnalyzer != null)
            {
                analyzer = preAnalyzer;
            }
            else
            {
                analyzer = new TypeAnalyzer();

                if (item != null)
                {
                    analyzer.Analyze(item);
                }
                else if (targetType != null)
                {
                    analyzer.Analyze(targetType);
                }
                else
                {
                    analyzer.Analyze(typeof(object));
                }
            }

            Type effectiveType = analyzer.AnalyzeType!;

            switch (analyzer.TypeCategory)
            {
                case TypeCategory.Simple:
                case TypeCategory.SimpleNullable:
                    result = new PrimitivePropertyViewModel(tree, access, item, effectiveType, title, parent, analyzer);
                    break;

                case TypeCategory.IList:
                    result = new ListEditorViewModel(tree, access, item, effectiveType, title, parent, analyzer);
                    break;

                case TypeCategory.Array:
                    result = new ArrayEditorViewModel(tree, access, item, effectiveType, title, parent, analyzer);
                    break;

                case TypeCategory.IDictionary:
                    result = new DictionaryEditorViewModel(tree, access, item, effectiveType, title, parent, analyzer);
                    break;

                case TypeCategory.None:
                    result = new ComplexObjectEditorViewModel(tree, access, item, effectiveType, title, parent, analyzer);
                    break;
            }

            return result;
        }
        #endregion

        #region Commands
        public ICommand DeleteItemCommand { get; private set; } = null!;

        private void ExecuteDeleteItem()
        {
            if (this.Parent != null)
            {
                // Handle deletion for properties of a complex object
                if (this.Access is PropertyAccess propertyAccess)
                {
                    ComplexObjectEditorViewModel parentViewModel = (ComplexObjectEditorViewModel)this.Parent;

                    if (parentViewModel.Item != null)
                    {
                        // Reset the value in the underlying data model via reflection
                        propertyAccess.PropertyInfo.SetValue(parentViewModel.Item, null);

                        // Clear the local state of this editor. Collapsing matters: with the
                        // children gone the expander disappears, so an expanded card would have
                        // no way left to close itself.
                        this.Item = null;
                        this.Children.Clear();
                        this.EditorState.IsExpanded = false;

                        // Notify the parent about the state change
                        parentViewModel.RaiseStateChanged();
                    }
                }
                else if (this.Access is IndexedAccess indexedAccess && this.Parent is IndexedCollectionEditorViewModel indexedParent)
                {
                    // Handle deletion for items in a List or Array
                    if (indexedParent.DeleteAtIndexCommand.CanExecute(indexedAccess.Index))
                    {
                        indexedParent.DeleteAtIndexCommand.Execute(indexedAccess.Index);
                    }
                }
                else if (this is DictionaryEntryViewModel entryVM && this.Parent is DictionaryEditorViewModel dictParent)
                {
                    // Handle deletion of the entire dictionary entry (Key and Value)
                    // We use the CurrentKey stored in the DictionaryEntryViewModel
                    if (dictParent.DeleteByKeyCommand.CanExecute(entryVM.CurrentKey))
                    {
                        dictParent.DeleteByKeyCommand.Execute(entryVM.CurrentKey);
                    }
                }
                else if (this.Parent is DictionaryEntryViewModel entryParent)
                {
                    // Handle deletion of just the Value part (setting it to null)
                    // This is triggered if the delete button on the Value editor is clicked
                    entryParent.ChangeChild(this.Access, null);

                    // Clear the local state of the value editor, same reason as above
                    this.Item = null;
                    this.Children.Clear();
                    this.EditorState.IsExpanded = false;

                    entryParent.RaiseStateChanged();
                }
            }
        }
        #endregion

        #region UI / Display
        protected override string DefaultTitle
        {
            get
            {
                string result = "";

                // Check if it's an indexed access (List or Array)
                if (Access is IndexedAccess indexedAccess)
                {
                    result = $"{indexedAccess.Index + 1}. {base.DefaultTitle}";
                }
                // Fallback to the base logic (e.g., for PropertyAccess)
                else
                {
                    result = base.DefaultTitle;
                }

                return result;
            }
        }
        #endregion
    }
}
