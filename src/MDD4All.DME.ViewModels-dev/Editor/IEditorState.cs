using MDD4All.UI.DataModels.Tree;
using System.ComponentModel;

namespace MDD4All.DME.ViewModels.Editor
{
    public interface IEditorState : INotifyPropertyChanged
    {
        ObjectTreeViewModel? TreeViewModel { get; }

        ITreeNode? SelectedEditorViewModel { get; }

        bool ShowRawData { get; set; }
    }
}
