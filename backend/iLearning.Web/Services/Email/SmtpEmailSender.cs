using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace iLearning.Web.Services.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _opt;

        public SmtpEmailSender(IOptions<SmtpOptions> opt)
        {
            _opt = opt.Value;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            using var msg = new MailMessage();
            msg.From = new MailAddress(_opt.FromEmail, _opt.FromName);
            msg.To.Add(toEmail);
            msg.Subject = subject;
            msg.Body = body;
            msg.IsBodyHtml = false;

            using var client = new SmtpClient(_opt.Host, _opt.Port);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(_opt.Username, _opt.Password);

            await client.SendMailAsync(msg);
        }
    }
}
