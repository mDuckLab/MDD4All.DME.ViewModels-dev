using MDD4All.Reflection;
using MDD4All.ObjectGraph.Access;
using MDD4All.UI.DataModels.Tree;
using System;

namespace MDD4All.DME.ViewModels.Editor
{
    public class ArrayEditorViewModel : IndexedCollectionEditorViewModel
    {
        #region Constructors and Initialization
        public ArrayEditorViewModel(ITree tree, Access access, object item, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, item, null, title, parent, preAnalyzedResult)
        {
            this.InitializeArrayData();
            this.CreateTree();
        }

        public ArrayEditorViewModel(ITree tree, Access access, Type targetType, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, null, targetType, title, parent, preAnalyzedResult)
        {
            this.InitializeArrayData();
            this.CreateTree();
        }

        public ArrayEditorViewModel(ITree tree, Access access, object? item, Type? targetType, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, item, targetType, title, parent, preAnalyzedResult)
        {
            this.InitializeArrayData();
            this.CreateTree();
        }

        private void InitializeArrayData()
        {
            this.UnderlyingTypeAnalyzer = TypeAnalyzer.CreateAnalyst(base.TypeAnalyzer.UnderlyingTypes[0]);
        }

        public void CreateTree()
        {
            if (this.ItemAsArray != null)
            {
                for (int index = 0; index < this.ItemAsArray.Length; index++)
                {
                    object? element = this.ItemAsArray.GetValue(index);
                    ArrayAccess arrayAccess = new ArrayAccess(index);
                    ObjectEditorViewModel? childViewModel = ReferenceEditorViewModel.CreateChildViewModel(this.Tree!,
                                                                                                            arrayAccess,
                                                                                                            element,
                                                                                                            this.UnderlyingType,
                                                                                                            null,
                                                                                                            this,
                                                                                                            UnderlyingTypeAnalyzer);

                    if (childViewModel != null)
                    {
                        this.Children.Add(childViewModel);
                    }
                }
            }
        }

        private void CreateArrayInstance(int length = 0)
        {
            if (length >= 0)
            {
                this.Item = Array.CreateInstance(UnderlyingType, length);
                this.Children.Clear();
                UpdateParentReference();
            }
        }
        #endregion

        #region properties
        public Array? ItemAsArray
        {
            get
            {
                Array? result = null;

                if (base.Item != null)
                {
                    result = (Array)base.Item;
                }
                return result;
            }
            private set
            {
                Item = value;
            }
        }

        override protected string CollectionTypePrefix
        {
            get
            {
                return "array of";
            }
        }   
        #endregion

        #region Comand Execute
        override protected void ExecuteCreateInstance()
        {
            this.CreateArrayInstance();
        }

        override protected void ExecuteAddItem()
        {
            if (this.ItemAsArray == null)
            {
                this.CreateArrayInstance();
                EditorState.IsExpanded = true;
            }

            if (this.ItemAsArray != null)
            {
                object? newElement = null;

                try
                {
                    if (this.UnderlyingType == typeof(string))
                    {
                        newElement = string.Empty;
                    }
                    else if (this.UnderlyingType != null)
                    {
                        newElement = Activator.CreateInstance(this.UnderlyingType);
                    }
                }
                catch
                {
                }

                if (newElement != null && this.UnderlyingType != null)
                {
                    int oldLength = ItemAsArray.Length;
                    int newLength = oldLength + 1;
                    int newIndex = oldLength;

                    Array newArray = Array.CreateInstance(this.UnderlyingType, newLength);

                    Array.Copy(this.ItemAsArray, newArray, oldLength);

                    newArray.SetValue(newElement, newIndex);

                    this.Item = newArray;
                    UpdateParentReference();

                    ObjectEditorViewModel? childViewModel = ReferenceEditorViewModel.CreateChildViewModel(this.Tree!,
                                                                                                            new ArrayAccess(newIndex),
                                                                                                            newElement,
                                                                                                            this.UnderlyingType,
                                                                                                            null,
                                                                                                            this,
                                                                                                            this.UnderlyingTypeAnalyzer);

                    if (childViewModel != null)
                    {
                        childViewModel.EditorState.IsExpanded = true;

                        this.Children.Add(childViewModel);

                        if (this.Tree is ObjectTreeViewModel objectTree)
                        {
                            objectTree.HasBeenProcessed = true;
                        }
                    }
                    this.StateChanged = true;
                }
            }
        }

        override protected void ExecuteDeleteAtIndex(int index)
        {
            if (ItemAsArray != null && index >= 0 && index < ItemAsArray.Length)
            {
                if (index < Children.Count)
                {
                    Children.RemoveAt(index);
                }
                int oldLength = ItemAsArray.Length;
                int newLength = oldLength - 1;

                if (newLength <= 0)
                {
                    CreateArrayInstance();
                }
                else
                {
                    Array newArray = Array.CreateInstance(UnderlyingType, newLength);
                    // Array.Copy(source, source start, destination, destination start, number)
                    // index : 0 1 2 3 4
                    // object: a b c d e 
                    // remove at index 1
                    // neberBefor Index = index
                    // new length = oldLength -1 -> 4
                    // numberAfterIndex = 4 (new length) - index
                    int numberBeforIndex = index;
                    int numberAfterIndex = newLength - index;

                    if (numberBeforIndex > 0)
                    {
                        Array.Copy(ItemAsArray, 0, newArray, 0, numberBeforIndex);
                    }

                    if (numberAfterIndex > 0)
                    {
                        Array.Copy(ItemAsArray, index + 1, newArray, index, numberAfterIndex);
                    }

                    this.Item = newArray;
                    UpdateParentReference();
                }

                this.ReorderIndexChild(index);

                if (this.Tree is ObjectTreeViewModel objectTree)
                {
                    objectTree.HasBeenProcessed = true;
                }

                this.StateChanged = true;
            }
        }
        #endregion
    }
}