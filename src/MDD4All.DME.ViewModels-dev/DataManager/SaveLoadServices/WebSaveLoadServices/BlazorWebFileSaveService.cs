using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class BlazorWebFileSaveService : IFileSaveService
    {
        // IJSRuntime acts as the interface between C# and JavaScript.
        private readonly IJSRuntime _jsRuntime;

        public BlazorWebFileSaveService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SaveFileAsync(string fileName, string base64Data)
        {
            await _jsRuntime.InvokeVoidAsync("saveAsFile", fileName, base64Data);
        }
    }
}