using System.Threading.Tasks;

namespace MDD4All.DME.ViewModels.DataManager
{
    public interface IFileSaveService
    {
        Task SaveFileAsync(string fileName, string base64Data);
    }
}
