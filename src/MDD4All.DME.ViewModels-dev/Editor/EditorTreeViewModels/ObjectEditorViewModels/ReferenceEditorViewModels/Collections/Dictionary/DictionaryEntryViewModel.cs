using MDD4All.Reflection;
using MDD4All.ObjectGraph.Access;
using MDD4All.UI.DataModels.Tree;
using System;
using System.Collections;

namespace MDD4All.DME.ViewModels.Editor
{
    public class DictionaryEntryViewModel : ReferenceEditorViewModel
    {
        #region Constructors and Initialization
        public DictionaryEntryViewModel(ITree tree, object key, object? value, Type keyType, Type valueType, ITreeNode? parent = null)
            : base(tree, new DictionaryAccess(), null, null, null, parent, null)
        {
            this.CurrentKey = key;

            this.KeyTypeAnalyzer = TypeAnalyzer.CreateAnalyst(keyType);
            this.ValueTypeAnalyzer = TypeAnalyzer.CreateAnalyst(valueType);

            // Index 0: Key
            this.Children.Add(ReferenceEditorViewModel.CreateChildViewModel(tree, new DictionaryKeyAccess(), key, keyType, "Key", this, this.KeyTypeAnalyzer)!);

            // Index 1: Value
            this.Children.Add(ReferenceEditorViewModel.CreateChildViewModel(tree, new DictionaryValueAccess(), value, valueType, "Value", this, this.ValueTypeAnalyzer)!);
        }
        #endregion

        #region properties
        private object _currentKey = null!;

        public object CurrentKey
        {
            get
            {
                return this._currentKey;
            }
            set
            {
                if (this._currentKey != value)
                {
                    this._currentKey = value;
                    this.OnPropertyChanged(nameof(CurrentKey));
                    this.OnPropertyChanged(nameof(Title));
                }
            }
        }

        public IDictionary? ParentDictionary
        {
            get
            {
                IDictionary? result = null;

                if (this.Parent is DictionaryEditorViewModel parentViewModel)
                {
                    result = parentViewModel.ItemAsDictionary;
                }

                return result;
            }
        }

        public ObjectEditorViewModel? KeyEditor
        {
            get
            {
                ObjectEditorViewModel? result = null;

                if (this.Children.Count > 0 && this.Children[0] is ObjectEditorViewModel viewModel)
                {
                    result = viewModel;
                }

                return result;
            }
        }

        public ObjectEditorViewModel? ValueEditor
        {
            get
            {
                ObjectEditorViewModel? result = null;

                if (this.Children.Count > 1 && this.Children[1] is ObjectEditorViewModel viewModel)
                {
                    result = viewModel;
                }

                return result;
            }
        }

        public TypeAnalyzer KeyTypeAnalyzer { get; private set; } = null!;

        public TypeAnalyzer ValueTypeAnalyzer { get; private set; } = null!;

        public Type KeyType
        {
            get
            {
                Type result = typeof(object);
                if (this.KeyTypeAnalyzer?.AnalyzeType != null)
                {
                    result = this.KeyTypeAnalyzer.AnalyzeType;
                }
                return result;
            }
        }

        public Type ValueType
        {
            get
            {
                Type result = typeof(object);
                if (this.ValueTypeAnalyzer?.AnalyzeType != null)
                {
                    result = this.ValueTypeAnalyzer.AnalyzeType;
                }
                return result;
            }
        }

        protected override string DefaultTitle
        {
            get
            {
                string result = $"[{this.CurrentKey}]";
                return result;
            }
        }
        #endregion

        public void ChangeChild(Access childAccess, object? newValue)
        {
            if (this.ParentDictionary != null)
            {
                if (childAccess is DictionaryKeyAccess)
                {
                    if (newValue != null && !ParentDictionary.Contains(newValue))
                    {
                        object? existingValue = ParentDictionary[this.CurrentKey];
                        ParentDictionary.Remove(this.CurrentKey);
                        ParentDictionary.Add(newValue, existingValue);

                        this.CurrentKey = newValue;
                    }
                    else
                    {
                        if (this.KeyEditor != null)
                        {
                            this.KeyEditor.Item = this.CurrentKey;
                        }

                    }

                }
                else if (childAccess is DictionaryValueAccess)
                {
                    ParentDictionary[this.CurrentKey] = newValue;

                    if (this.Tree is ObjectTreeViewModel objectTree)
                    {
                        objectTree.HasBeenProcessed = true;
                    }
                }
            }
        }
    }
}