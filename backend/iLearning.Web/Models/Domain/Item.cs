using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.Domain
{
    public class Item
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid InventoryId { get; set; }
        public Inventory Inventory { get; set; } = null!;

        [Required, MaxLength(200)]
        public string CustomId { get; set; } = string.Empty;

        public Guid CreatedById { get; set; }
        public AppUser CreatedBy { get; set; } = null!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public Guid? UpdatedById { get; set; }
        public AppUser? UpdatedBy { get; set; }

        public DateTime? UpdatedAtUtc { get;set; }

        public int Version { get; set; } = 1;

        [MaxLength(500)] public string? String1 { get; set; }
        [MaxLength(500)] public string? String2 { get; set; }
        [MaxLength(500)] public string? String3 { get; set; }

        [MaxLength(4000)] public string? Text1 { get; set; }
        [MaxLength(4000)] public string? Text2 { get; set; }
        [MaxLength(4000)] public string? Text3 { get; set; }

        public decimal? Number1 { get; set; }
        public decimal? Number2 { get; set; }
        public decimal? Number3 { get; set; }

        public bool? Bool1 { get; set; }
        public bool? Bool2 { get; set; }
        public bool? Bool3 { get; set; }

        [MaxLength(1000)] public string? Link1 { get; set; }
        [MaxLength(1000)] public string? Link2 { get; set; }
        [MaxLength(1000)] public string? Link3 { get; set; }

        public ICollection<ItemLike> Likes { get; set; } = new List<ItemLike>();
        public ICollection<ItemComment> Comments { get; set; } = new List<ItemComment>();

    }
}
