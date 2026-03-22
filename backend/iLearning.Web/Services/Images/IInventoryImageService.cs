
using Microsoft.AspNetCore.Http;

namespace iLearning.Web.Services.Images
{
    public interface IInventoryImageService
    {
        Task<string> UploadInventoryImageAsync(IFormFile file, CancellationToken cancellationToken = default);
    }
}
