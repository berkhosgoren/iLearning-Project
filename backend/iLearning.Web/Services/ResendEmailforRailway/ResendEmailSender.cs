using System.Net.Http.Json;
using iLearning.Web.Services.Email;
using Microsoft.Extensions.Options;

namespace iLearning.Web.Services.ResendEmailforRailway
{
    public class ResendEmailSender : IEmailSender
    {
        private readonly HttpClient _http;
        private readonly ResendOptions _opt;

        public ResendEmailSender(HttpClient http, IOptions<ResendOptions> opt)
        {
            _http = http;
            _opt = opt.Value;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var payload = new
            {
                from = string.IsNullOrWhiteSpace(_opt.FromName) ? _opt.FromEmail : $"{_opt.FromName} <{_opt.FromEmail}>",
                to = new[] { toEmail },
                subject,
                text = body
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _opt.ApiKey);
            req.Content = JsonContent.Create(payload);

            using var res = await _http.SendAsync(req);

            if (!res.IsSuccessStatusCode)
            {
                var responseBody = await res.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Resend email send failed: {responseBody}");
            }
        }
    }
}
