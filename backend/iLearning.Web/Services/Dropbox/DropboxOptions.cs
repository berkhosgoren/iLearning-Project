namespace iLearning.Web.Services.Dropbox
{
    public class DropboxOptions
    {
        public string AppKey { get; set; } = string.Empty;
        public string AppSecret { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string FolderPath { get; set; } = "/iLearningSupportTickets";
    }
}
