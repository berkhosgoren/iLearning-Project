using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.Domain
{
    public class ItemComment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public Guid UserId { get; set; }
        public AppUser User { get; set; } = null!;

        [Required, MaxLength(4000)]
        public string BodyMarkdown { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
