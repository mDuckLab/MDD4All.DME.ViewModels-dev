using CommunityToolkit.Mvvm.Input;
using MDD4All.Reflection;
using MDD4All.ObjectGraph.Access;
using MDD4All.UI.DataModels.Tree;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace MDD4All.DME.ViewModels.Editor
{
    public abstract class IndexedCollectionEditorViewModel : ReferenceEditorViewModel, INotifyPropertyChanged
    {
        #region Constructors and Initialization
        protected IndexedCollectionEditorViewModel(ITree tree, Access access, object? item, Type? targetType, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, item, targetType, title, parent, preAnalyzedResult)
        {
            InitializeCommonData();
            InitializeCommands();
        }

        private void InitializeCommonData()
        {
            // The type of the elements in the list or array
            this.UnderlyingTypeAnalyzer = TypeAnalyzer.CreateAnalyst(base.TypeAnalyzer.UnderlyingTypes[0]);
        }

        private void InitializeCommands()
        {
            // The commands call the abstract methods
            this.AddItemCommand = new RelayCommand(ExecuteAddItem);
            this.CreateInstanceCommand = new RelayCommand(ExecuteCreateInstance);
            this.DeleteAtIndexCommand = new RelayCommand<int>(ExecuteDeleteAtIndex);
        }
        #endregion

        #region Logic / Data
        public TypeAnalyzer UnderlyingTypeAnalyzer { get; protected set; } = null!;

        public Type UnderlyingType
        {
            get
            {
                Type result = typeof(object);
                if (UnderlyingTypeAnalyzer != null && UnderlyingTypeAnalyzer?.AnalyzeType != null)
                {
                    result = UnderlyingTypeAnalyzer.AnalyzeType;
                }
                return result;
            }
            protected set
            {
                if (value != null)
                {
                    UnderlyingTypeAnalyzer.Analyze(value);
                }
                else
                {
                    UnderlyingTypeAnalyzer.Analyze(typeof(object));
                }
            }
        }

        public TypeCategory UnderlyingTypeCategory
        {
            get
            {
                return this.UnderlyingTypeAnalyzer.TypeCategory;
            }
        }

        public bool IsUnderlyingTypeSimple
        {
            get
            {
                bool result = false;
                if (UnderlyingTypeAnalyzer.IsSimpleOrSimpleNullable())
                {
                    result = true;
                }
                return result;
            }
        }

        protected void ReorderIndexChild(int startIndex = 0)
        {
            for (int newIndex = startIndex; newIndex < this.Children.Count; newIndex++)
            {
                if (this.Children[newIndex] is ObjectEditorViewModel childViewModel)
                {
                    if (childViewModel.Access is IndexedAccess indexedAccess)
                    {
                        indexedAccess.Index = newIndex;
                        // Update title
                        childViewModel.Title = string.Empty;
                    }
                }
            }
        }
        #endregion

        #region Commands
        public ICommand AddItemCommand { get; protected set; } = null!;

        protected abstract void ExecuteAddItem();

        public ICommand CreateInstanceCommand { get; protected set; } = null!;

        protected abstract void ExecuteCreateInstance();

        public ICommand DeleteAtIndexCommand { get; protected set; } = null!;

        protected abstract void ExecuteDeleteAtIndex(int index);
        #endregion
    }
}
