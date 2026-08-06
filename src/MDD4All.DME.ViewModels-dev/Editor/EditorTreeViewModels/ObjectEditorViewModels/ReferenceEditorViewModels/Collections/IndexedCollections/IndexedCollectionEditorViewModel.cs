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
            // Holt den Typ der Elemente in der Liste/im Array
            this.UnderlyingTypeAnalyzer = TypeAnalyzer.CreateAnalyst(base.TypeAnalyzer.UnderlyingTypes[0]);
        }

        private void InitializeCommands()
        {
            // Die Commands rufen die abstrakten Methoden auf
            this.AddElementCommand = new RelayCommand(ExecuteAddItem);
            this.CreateInstanceCommand = new RelayCommand(ExecuteCreateInstance);
            this.DeleteAtIndexCommand = new RelayCommand<int>(ExecuteDeleteAtIndex);
        }
        #endregion

        #region Abstract Logic
        protected abstract void ExecuteAddItem();

        protected abstract void ExecuteCreateInstance();

        protected abstract void ExecuteDeleteAtIndex(int index);

        protected abstract string CollectionTypePrefix { get; } // "list of" or "array of"
        #endregion

        #region Comand Definitions
        public ICommand AddElementCommand { get; protected set; } = null!;

        public ICommand CreateInstanceCommand { get; protected set; } = null!;

        public ICommand DeleteAtIndexCommand { get; protected set; } = null!;
        #endregion

        #region properties
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

        public override string BadgeText
        {
            get
            {
                string prefix = CollectionTypePrefix;
                string typeDisplayName = "objects";

                if (UnderlyingType != null)
                {
                    if (UnderlyingType == typeof(string))
                    {
                        typeDisplayName = "text";
                    }
                    else if (UnderlyingType == typeof(int) || UnderlyingType == typeof(long) ||
                             UnderlyingType == typeof(short) || UnderlyingType == typeof(byte))
                    {
                        typeDisplayName = "whole numbers";
                    }
                    else if (UnderlyingType == typeof(double) || UnderlyingType == typeof(float) ||
                             UnderlyingType == typeof(decimal))
                    {
                        typeDisplayName = "decimal numbers";
                    }
                    else if (UnderlyingType == typeof(bool))
                    {
                        typeDisplayName = "yes/no values";
                    }
                    else if (UnderlyingType == typeof(DateTime))
                    {
                        typeDisplayName = "dates and times";
                    }
                    else if (UnderlyingType == typeof(char))
                    {
                        typeDisplayName = "single characters";
                    }
                    else if (UnderlyingType == typeof(Guid))
                    {
                        typeDisplayName = "unique ids";
                    }
                    else
                    {
                        typeDisplayName = UnderlyingType.Name.ToLower();
                    }
                }

                return $"{prefix} {typeDisplayName}";
            }
        }
        #endregion

        #region Collection Synchronization
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
    }
}