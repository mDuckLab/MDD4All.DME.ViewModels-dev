using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;
using MDD4All.ObjectGraph.Access;
using MDD4All.UI.DataModels.Tree;
using System;
using MDD4All.Reflection;

namespace MDD4All.DME.ViewModels.Editor
{
    public class PrimitivePropertyViewModel : ObjectEditorViewModel
    {
        #region Constructors and Initialization
        public PrimitivePropertyViewModel(ITree tree, Access access, object item, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, item, null, title, parent, preAnalyzedResult) { }

        public PrimitivePropertyViewModel(ITree tree, Access access, Type targetType, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, null, targetType, title, parent, preAnalyzedResult) { }

        public PrimitivePropertyViewModel(ITree tree, Access access, object? item, Type? targetType, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, item, targetType, title, parent, preAnalyzedResult) { }
        #endregion

        #region properties
        public override object? Item
        {
            get
            {
                return base.Item;
            }
            set
            {
                if (base.Item != value)
                {
                    base.Item = value;
                    this.OnPropertyChanged(nameof(Item));

                    this.UpdateValueInParent(value);
                }
                else
                {
                    this.OnPropertyChanged(nameof(Item));
                }
            }
        }

        private void UpdateValueInParent(object? newValue)
        {
            if (this.Parent != null)
            {
                if (this.Parent is DictionaryEntryViewModel entryParent)
                {
                    entryParent.ChangeChild(this.Access, newValue);
                }
                else if (this.Parent is ObjectEditorViewModel parent && parent.Item != null)
                {
                    if (this.Access is ListAccess listAccess && parent is ListEditorViewModel listParent)
                    {
                        listParent.ItemAsList![listAccess.Index] = newValue;
                    }
                    else if (this.Access is ArrayAccess arrayAccess && parent is ArrayEditorViewModel arrayParent)
                    {
                        arrayParent.ItemAsArray!.SetValue(newValue, arrayAccess.Index);
                    }
                    else if (this.Access is PropertyAccess propertyAccess)
                    {
                        propertyAccess.PropertyInfo.SetValue(parent.Item, newValue);
                    }
                }

                // Global flag for the tree
                if (this.Tree is ObjectTreeViewModel objectTree)
                {
                    objectTree.HasBeenProcessed = true;
                }
            }
        }

        protected override string DefaultTitle
        {
            get
            {
                string result;
                // Using the common base class for ListAccess and ArrayAccess
                if (Access is IndexedAccess indexedAccess)
                {
                    result = (indexedAccess.Index +1 ).ToString();
                }
                else
                {
                    // Fallback to PropertyInfo name or Type name from base class
                    result = base.DefaultTitle;
                }

                return result;
            }
        }

        #endregion
    }
}