using Board.Client.Models;
using System.Threading.Tasks;

namespace Board.Client.Services.Interfaces
{
    public interface IStorageClient
    {
        Task<ImageList> GetAppImage();
        Task<ImageList> GetUserImage(string userId);
        Task<string> PostNewImage(string userId, ImageData image);
    }
}