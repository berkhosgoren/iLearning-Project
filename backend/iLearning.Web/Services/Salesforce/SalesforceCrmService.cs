using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace iLearning.Web.Services.Salesforce
{
    public class SalesforceCrmService : ISalesforceCrmService
    {
        private readonly HttpClient _http;
        private readonly SalesforceOptions _options;

        public SalesforceCrmService(HttpClient http, IOptions<SalesforceOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public async Task<SalesforceCreateResult> CreateAccountWithContactAsync(SalesforceCreateRequest request, CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync(cancellationToken);

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            var accountId = await CreateAccountAsync(token.InstanceUrl, request, cancellationToken);
            var contactId = await CreateContactAsync(token.InstanceUrl, accountId, request, cancellationToken);

            return new SalesforceCreateResult
            {
                AccountId = accountId,
                ContactId = contactId
            };
        }

        private async Task<SalesforceTokenResponse> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            var tokenUrl = $"{_options.LoginUrl.TrimEnd('/')}/services/oauth2/token";

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret
            });

            using var response = await _http.PostAsync(tokenUrl, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Salesforce token request failed: {body}");

            var token = JsonSerializer.Deserialize<SalesforceTokenResponse>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (token == null || string.IsNullOrWhiteSpace(token.AccessToken) || string.IsNullOrWhiteSpace(token.InstanceUrl))
                throw new InvalidOperationException("Salesforce token response is incomplete.");

            return token;
        }

        private async Task<string> CreateAccountAsync(string instanceUrl, SalesforceCreateRequest request, CancellationToken cancellationToken)
        {
            var url = BuildSObjectUrl(instanceUrl, "Account");

            var payload = new
            {
                Name = request.AccountName,
                Website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
            };

            using var response = await _http.PostAsJsonAsync(url, payload, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Salesforce Account creation failed: {body}");

            var created = JsonSerializer.Deserialize<SalesforceCreateSObjectResponse>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (created == null || string.IsNullOrWhiteSpace(created.Id))
                throw new InvalidOperationException("Salesforce Account creation returned no id.");

            return created.Id;
        }

        private async Task<string> CreateContactAsync(string instanceUrl, string accountId, SalesforceCreateRequest request, CancellationToken cancellationToken)
        {
            var url = BuildSObjectUrl(instanceUrl, "Contact");

            var payload = new
            {
                AccountId = accountId,
                FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim()
            };

            using var response = await _http.PatchAsJsonAsync(url, payload, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Salesforce Contact creation failed: {body}");

            var created = JsonSerializer.Deserialize<SalesforceCreateSObjectResponse>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (created == null || string.IsNullOrWhiteSpace(created.Id))
                throw new InvalidOperationException("Salesforce Contact creation returned no id.");

            return created.Id;
        }

        private string BuildSObjectUrl(string instanceUrl, string objectName)
        {
            return $"{instanceUrl.TrimEnd('/')}/services/data/{_options.ApiVersion}/sobjects/{objectName}";
        }

        private class SalesforceTokenResponse
        {
            public string AccessToken { get; set; } = string.Empty;
            public string InstanceUrl { get; set; } = string.Empty;
        }

        private class SalesforceCreateSObjectResponse
        {
            public string Id { get; set; } = string.Empty;
            public bool Success { get; set; }
        }
    }
}
