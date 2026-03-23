namespace iLearning.Web.Services.Dropbox
{
    public interface IDropboxTicketUploadService
    {
        Task<DropboxUploadResult> UploadSupportTicketAsync(DropboxUploadRequest request, CancellationToken cancellationToken = default);
    }

    public class DropboxUploadRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string JsonContent { get; set; } = string.Empty;
    }

    public class DropboxUploadResult
    {
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string PathDisplay { get; set; } = string.Empty;
    }
}
