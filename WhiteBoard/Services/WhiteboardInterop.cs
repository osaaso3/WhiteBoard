using System;
using System.Threading.Tasks;
using Board.Client.Models;
using Microsoft.JSInterop;

namespace Board.Client.Services
{
    public class WhiteboardInterop
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;

        public WhiteboardInterop(IJSRuntime jsRuntime)
        {
            moduleTask = new Lazy<Task<IJSObjectReference>>(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/appJs.js").AsTask());
        }
        public async ValueTask DownloadProjectFile(string filename, byte[] projectBytes)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("saveAsFile", filename, projectBytes);
        }

        public async ValueTask<Location> GetContainerLocation(string containerId)
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<Location>("getContainerLocation", containerId);
        }
    }
}
