using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace iLearning.Web.Services.Dropbox
{
    public class DropboxTicketUploadService : IDropboxTicketUploadService
    {
        private readonly HttpClient _http;
        private readonly DropboxOptions _options;
        private readonly ILogger<DropboxTicketUploadService> _logger;

        public DropboxTicketUploadService(HttpClient http, IOptions<DropboxOptions> options, ILogger<DropboxTicketUploadService> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<DropboxUploadResult> UploadSupportTicketAsync(DropboxUploadRequest request, CancellationToken cancellationToken = default)
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            var fullPath = BuildFullPath(request.FileName);

            using var msg = new HttpRequestMessage(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload");
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            msg.Headers.TryAddWithoutValidation("Dropbox-API-Arg", JsonSerializer.Serialize(new
            {
                path = fullPath,
                mode = "add",
                autorename = true,
                mute = false,
                strict_conflict = false
            }));

            msg.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(request.JsonContent));
            msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            using var response = await _http.SendAsync(msg, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Dropbox upload failed: {body}");
            }

            var uploaded = JsonSerializer.Deserialize<DropboxFileMetadata>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (uploaded == null || string.IsNullOrWhiteSpace(uploaded.Id) || string.IsNullOrWhiteSpace(uploaded.Name))
            {
                throw new InvalidOperationException("Dropbox upload response is incomplete.");
            }

            return new DropboxUploadResult
            {
                FileId = uploaded.Id,
                FileName = uploaded.Name,
                PathDisplay = uploaded.PathDisplay ?? fullPath
            };
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = _options.RefreshToken,
                ["client_id"] = _options.AppKey,
                ["client_secret"] = _options.AppSecret
            });

            using var response = await _http.PostAsync("https://api.dropboxapi.com/oauth2/token", content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Dropbox token request failed: {body}");
            }

            var token = JsonSerializer.Deserialize<DropboxTokenResponse>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (token == null || string.IsNullOrWhiteSpace(token.AccessToken))
            {
                throw new InvalidOperationException("Dropbox token response is incomplete.");
            }

            return token.AccessToken;
        }

        private string BuildFullPath(string fileName)
        {
            var folder = (_options.FolderPath ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(folder))
            {
                folder = "/";
            }

            if (!folder.StartsWith("/"))
            {
                folder = "/" + folder;
            }

            folder = folder.TrimEnd('/');

            var safeFileName = fileName.Trim();
            return $"{folder}/{safeFileName}";
        }

        private class DropboxTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;

            [JsonPropertyName("token_type")]
            public string TokenType { get; set; } = string.Empty;

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }

        private class DropboxFileMetadata
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("path_display")]
            public string? PathDisplay { get; set; }
        }
    }
}
