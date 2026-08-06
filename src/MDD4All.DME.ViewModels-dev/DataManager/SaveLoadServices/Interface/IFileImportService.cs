using System.Threading.Tasks;

namespace MDD4All.DME.ViewModels.DataManager
{
    public interface IFileImportService
    {
        Task OpenImportDialogAsync(string elementId);
    }
}
