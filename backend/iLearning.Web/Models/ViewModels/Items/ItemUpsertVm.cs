using System.ComponentModel.DataAnnotations;
using iLearning.Web.Models.ViewModels.Shared;

namespace iLearning.Web.Models.ViewModels.Items
{
    public class ItemUpsertVm
    {
        public Guid InventoryId { get; set; }
        public Guid ItemId { get; set; }

        public int Version { get; set; }

        [Required, MaxLength(200)]
        public string CustomId { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Title {  get; set; } = string.Empty;

        public InventoryFieldConfigVm Fields { get; set; } = new();

        [MaxLength(500)]
        public string? String1 { get; set; }
        [MaxLength(500)]
        public string? String2 { get; set; }
        [MaxLength(500)]
        public string? String3 { get; set; }

        [MaxLength(4000)]
        public string? Text1 { get; set; }
        [MaxLength(4000)]
        public string? Text2 { get; set; }
        [MaxLength(4000)]
        public string? Text3 { get; set; }

        public decimal? Number1 { get; set; }
        public decimal? Number2 { get; set; }
        public decimal? Number3 { get; set; }

        public bool? Bool1 { get; set; }
        public bool? Bool2 { get; set; }
        public bool? Bool3 { get; set; }

        [MaxLength(1000)]
        public string? Link1 { get; set; }
        [MaxLength(1000)]
        public string? Link2 { get; set; }
        [MaxLength(1000)]
        public string? Link3 { get; set; }
    }
}
