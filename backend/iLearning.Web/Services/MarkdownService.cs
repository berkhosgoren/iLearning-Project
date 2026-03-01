using Markdig;
using Ganss;
using Ganss.Xss;

namespace iLearning.Web.Services
{
    public interface IMarkdownService
    {
        string ToSafeHtml(string? markdown);
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
    }
}
