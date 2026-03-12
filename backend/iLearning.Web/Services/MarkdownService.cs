using Markdig;
using Ganss.Xss;
using System.Text.RegularExpressions;
using System.Net;

namespace iLearning.Web.Services
{
    public interface IMarkdownService
    {
        string ToSafeHtml(string? markdown);
        string ToPreviewText(string? markdown, int maxLength = 160);
    }

    public sealed class MarkdownService : IMarkdownService
    {
        private readonly MarkdownPipeline _pipeline;
        private readonly HtmlSanitizer _sanitizer;

        public MarkdownService()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            _sanitizer = new HtmlSanitizer();

            // allowing basic formattin + links.
            _sanitizer.AllowedTags.Clear();
            foreach (var tag in new[]
            {
                "p","br","hr",
                "strong","b","em","i","u","s",
                "blockquote",
                "code","pre",
                "ul","ol","li",
                "h1","h2","h3","h4","h5","h6",
                "a",
                "table","thead","tbody","tr","th","td"
            })
            {
                _sanitizer.AllowedTags.Add(tag);
            }

            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowedAttributes.Add("href");
            _sanitizer.AllowedAttributes.Add("title");
            _sanitizer.AllowedAttributes.Add("rel");
            _sanitizer.AllowedAttributes.Add("target");

            //only safe URL
            _sanitizer.AllowedSchemes.Clear();
            _sanitizer.AllowedSchemes.Add("http");
            _sanitizer.AllowedSchemes.Add("https");
            _sanitizer.AllowedSchemes.Add("mailto");
        }

        public string ToSafeHtml(string? markdown)
        {
            var md = (markdown ?? "").Trim();
            if (string.IsNullOrWhiteSpace(md))
                return "";

            var html = Markdig.Markdown.ToHtml(md, _pipeline);

            //sanitize for preventing scripts
            html = _sanitizer.Sanitize(html);

            //force save link behavior
            html = html.Replace("<a", "<a target=\"_blank\" rel=\"noopener noreferrer\" ");

            return html;
        }

        public string ToPreviewText(string? markdown, int maxLength = 160)
        {
            var md = (markdown ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(md))
                return string.Empty;

            var html = ToSafeHtml(md);

            html = Regex.Replace(html, @"</(p|div|h[1-6]|li|tr|blockquote|pre)>", " ", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<br\s*/?>", " ", RegexOptions.IgnoreCase);

            var text = Regex.Replace(html, "<.*?>", string.Empty);

            text = WebUtility.HtmlDecode(text);

            text = Regex.Replace(text, @"\s+", " ").Trim();

            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            if (text.Length <= maxLength)
                return text;

            var shortened = text.Substring(0, maxLength).Trim();
            var lastSpace = shortened.LastIndexOf(' ');
            if (lastSpace > 0)
                shortened = shortened.Substring(0, lastSpace);

            return shortened.TrimEnd('.', ',', ';', ':', '-', ' ') + "...";
        }

    }
}
