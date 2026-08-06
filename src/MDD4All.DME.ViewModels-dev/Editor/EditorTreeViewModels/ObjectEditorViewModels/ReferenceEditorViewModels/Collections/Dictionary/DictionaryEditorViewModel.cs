using CommunityToolkit.Mvvm.Input;
using MDD4All.Reflection;
using MDD4All.ObjectGraph.Access;
using MDD4All.UI.DataModels.Tree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace MDD4All.DME.ViewModels.Editor
{
    public class DictionaryEditorViewModel : ReferenceEditorViewModel
    {
        #region Constructors and Initialization
        public DictionaryEditorViewModel(ITree tree, Access access, object item, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, item, null, title, parent, preAnalyzedResult)
        {
            this.InitializeDictData();
            this.CreateTree();
            this.InitializeCommands();
        }

        public DictionaryEditorViewModel(ITree tree, Access access, Type targetType, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, null, targetType, title, parent, preAnalyzedResult)
        {
            this.InitializeDictData();
            this.CreateTree();
            this.InitializeCommands();
        }

        public DictionaryEditorViewModel(ITree tree, Access access, object? item, Type? targetType, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, item, targetType, title, parent, preAnalyzedResult)
        {
            this.InitializeDictData();
            this.CreateTree();
            this.InitializeCommands();
        }

        private void InitializeDictData()
        {
            this.KeyTypeAnalyzer = TypeAnalyzer.CreateAnalyst(base.TypeAnalyzer.UnderlyingTypes[0]);
            this.ValueTypeAnalyzer = TypeAnalyzer.CreateAnalyst(base.TypeAnalyzer.UnderlyingTypes[1]);
        }

        private void InitializeCommands()
        {
            this.DeleteByKeyCommand = new RelayCommand<object?>(this.ExecuteDeleteByKey);
            this.AddElementCommand = new RelayCommand(this.ExecuteAddItem);
            this.CreateInstanceCommand = new RelayCommand(this.ExecuteCreateInstance);
        }

        private void CreateTree()
        {
            if (ItemAsDictionary != null)
            {
                foreach (DictionaryEntry entry in ItemAsDictionary)
                {
                    DictionaryEntryViewModel entryViewModel = new DictionaryEntryViewModel(this.Tree!,
                                                                                            entry.Key,
                                                                                            entry.Value,
                                                                                            this.KeyType,
                                                                                            this.ValueType,
                                                                                            this);

                    if (entryViewModel != null)
                    {
                        this.Children.Add(entryViewModel);
                    }
                }
            }
        }

        #endregion

        #region Comand Definitions
        public ICommand AddElementCommand { get; protected set; } = null!;

        public ICommand CreateInstanceCommand { get; protected set; } = null!;

        public ICommand DeleteByKeyCommand { get; protected set; } = null!;
        #endregion

        #region Properties
        public override string BadgeText
        {
            get
            {
                return "dictionary";
            }
        }
        public IDictionary? ItemAsDictionary
        {
            get
            {
                System.Collections.IDictionary? result = null;

                if (base.Item != null)
                {
                    result = base.Item as IDictionary;
                }

                return result;
            }
            private set
            {
                Item = value;
            }
        }

        public TypeAnalyzer KeyTypeAnalyzer { get; private set; } = null!;

        public Type KeyType
        {
            get
            {
                Type result = typeof(object);
                if (KeyTypeAnalyzer?.AnalyzeType != null)
                {
                    result = KeyTypeAnalyzer.AnalyzeType;
                }
                return result;
            }
        }

        public bool IsKeyTypeSimple
        {
            get
            {
                bool result = false;
                if (KeyTypeAnalyzer != null)
                {
                    result = KeyTypeAnalyzer.IsSimpleOrSimpleNullable();
                }
                return result;
            }
        }

        public TypeCategory KeyTypeCategory
        {
            get
            {
                TypeCategory result = TypeCategory.Error;
                if (KeyTypeAnalyzer != null)
                {
                    result = KeyTypeAnalyzer.TypeCategory;
                }
                return result;
            }
        }

        public TypeAnalyzer ValueTypeAnalyzer { get; private set; } = null!;

        public Type ValueType
        {
            get
            {
                Type result = typeof(object);
                if (ValueTypeAnalyzer?.AnalyzeType != null)
                {
                    result = ValueTypeAnalyzer.AnalyzeType;
                }
                return result;
            }
        }

        public bool IsValueTypeSimple
        {
            get
            {
                bool result = false;
                if (ValueTypeAnalyzer != null)
                {
                    result = ValueTypeAnalyzer.IsSimpleOrSimpleNullable();
                }
                return result;
            }
        }

        public TypeCategory ValueTypeCategory
        {
            get
            {
                TypeCategory result = TypeCategory.Error;
                if (ValueTypeAnalyzer != null)
                {
                    result = ValueTypeAnalyzer.TypeCategory;
                }
                return result;
            }
        }
        #endregion

        #region Dictionary Item Generation and Manipulation
        private void CreatInstance()
        {
            Type genericDictType = typeof(Dictionary<,>);
            Type concreteDictType = genericDictType.MakeGenericType(this.KeyType, this.ValueType);

            object? dynamicDict = Activator.CreateInstance(concreteDictType);


            this.Item = dynamicDict;
            this.Children.Clear();
            UpdateParentReference();
        }

        private object? CreatUniqueKey()
        {
            object? result = null;

            if (this.ItemAsDictionary != null)
            {
                // string
                if (this.KeyType == typeof(string))
                {
                    object emptyCandidate = string.Empty;

                    // Check if the empty string is already used as a key
                    if (this.ItemAsDictionary.Contains(emptyCandidate) == false)
                    {
                        result = emptyCandidate;
                    }
                    else
                    {
                        bool isUniqueGuidFound = false;

                        while (isUniqueGuidFound == false)
                        {
                            string guidCandidate = Guid.NewGuid().ToString();

                            if (this.ItemAsDictionary.Contains(guidCandidate) == false)
                            {
                                result = guidCandidate;
                                isUniqueGuidFound = true;
                            }
                        }

                    }
                }
                // boolean
                else if (this.KeyType == typeof(bool))
                {
                    if (this.ItemAsDictionary.Contains(false) == false)
                    {
                        result = false;
                    }
                    else if (this.ItemAsDictionary.Contains(true) == false)
                    {
                        result = true;
                    }
                }
                // enum
                else if (this.KeyType.IsEnum)
                {
                    foreach (object enumValue in Enum.GetValues(this.KeyType))
                    {
                        if (this.ItemAsDictionary.Contains(enumValue) == false)
                        {
                            result = enumValue;
                            break;
                        }
                    }
                }
                // date time
                else if (this.KeyType == typeof(DateTime) || this.KeyType == typeof(DateTimeOffset))
                {
                    DateTime dateTimeCandidate = DateTime.Today;

                    while (this.ItemAsDictionary.Contains(Convert.ChangeType(dateTimeCandidate, this.KeyType)))
                    {
                        dateTimeCandidate = dateTimeCandidate.AddDays(1);
                    }

                    result = Convert.ChangeType(dateTimeCandidate, this.KeyType);
                }
                // time span
                else if (this.KeyType == typeof(TimeSpan))
                {
                    TimeSpan timeSpanCandidate = TimeSpan.Zero;

                    while (this.ItemAsDictionary.Contains(timeSpanCandidate))
                    {
                        timeSpanCandidate = timeSpanCandidate.Add(TimeSpan.FromHours(1));
                    }

                    result = timeSpanCandidate;
                }
                // numeric
                else if (this.KeyTypeAnalyzer.IsSimpleOrSimpleNullable())
                {
                    int counter = 0;
                    bool isKeyFound = false;

                    while (isKeyFound == false)
                    {
                        try
                        {
                            object numericKeyCandidate = Convert.ChangeType(counter, this.KeyType);

                            if (this.ItemAsDictionary.Contains(numericKeyCandidate) == false)
                            {
                                result = numericKeyCandidate;
                                isKeyFound = true;
                            }

                            counter = counter + 1;
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
                // complex object
                else
                {
                    try
                    {
                        object? complexObjectCandidate = Activator.CreateInstance(this.KeyType);

                        if (complexObjectCandidate != null && this.ItemAsDictionary.Contains(complexObjectCandidate) == false)
                        {
                            result = complexObjectCandidate;
                        }
                    }
                    catch
                    {
                        result = null;
                    }
                }
            }

            return result;
        }

        private object? CreatDefaultValue()
        {
            object? result = null;

            // string
            if (this.ValueType == typeof(string))
            {
                result = string.Empty;
            }
            // numeric or simple types
            else if (this.ValueTypeAnalyzer.IsSimpleOrSimpleNullable())
            {
                try
                {
                    // Activator creates the default value (e.g., 0 for int, false for bool)
                    result = Activator.CreateInstance(this.ValueType);
                }
                catch
                {
                    result = null;
                }
            }
            // complex object
            else
            {
                try
                {
                    result = Activator.CreateInstance(this.ValueType);
                }
                catch
                {
                    result = null;
                }
            }

            return result;
        }

        private void DeleteByKey(object? key)
        {
            if (this.ItemAsDictionary != null && key != null && this.ItemAsDictionary.Contains(key))
            {
                this.ItemAsDictionary.Remove(key);

                ObjectEditorViewModel? nodeToRemove = this.Children.OfType<DictionaryEntryViewModel>().FirstOrDefault(entry => Equals(entry.CurrentKey, key));
                if (nodeToRemove != null)
                {
                    this.Children.Remove(nodeToRemove);
                }
                if (this.Tree is ObjectTreeViewModel objectTree)
                {
                    objectTree.HasBeenProcessed = true;
                }
                this.StateChanged = true;
            }
        }

        private void AddNewItem()
        {
            if (this.ItemAsDictionary == null)
            {
                this.ExecuteCreateInstance();
            }

            if (this.ItemAsDictionary != null)
            {
                object? uniqueKey = this.CreatUniqueKey();

                // We only proceed if a unique key was successfully generated
                if (uniqueKey != null)
                {
                    object? defaultValue = this.CreatDefaultValue();

                    // Add to the underlying dictionary
                    this.ItemAsDictionary.Add(uniqueKey, defaultValue);

                    // Create the UI ViewModel with aligned parameters
                    DictionaryEntryViewModel entryViewModel = new DictionaryEntryViewModel(this.Tree!,
                                                                                           uniqueKey,
                                                                                           defaultValue,
                                                                                           this.KeyType,
                                                                                           this.ValueType,
                                                                                           this);

                    if (entryViewModel != null)
                    {
                        this.Children.Add(entryViewModel);

                        if (this.Tree is ObjectTreeViewModel objectTree)
                        {
                            objectTree.HasBeenProcessed = true;
                        }
                    }

                    this.StateChanged = true;
                }
            }
        }
        #endregion

        #region Comand Execute
        private void ExecuteCreateInstance()
        {
            this.CreatInstance();
        }

        private void ExecuteAddItem()
        {
            this.AddNewItem();
        }

        private void ExecuteDeleteByKey(object? key)
        {
            this.DeleteByKey(key);
        }
        #endregion
    }
}
