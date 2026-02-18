using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.Domain
{
    public class Inventory
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        [MaxLength(1000)]
        public string? ImageUrl { get; set; }

        public bool IsPublic { get; set; } = false;

        public Guid CreatorId { get; set; }
        public AppUser Creator { get; set; } = null!;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public int Version { get; set; } = 1;

        public bool CustomString1Enabled { get; set; }
        [MaxLength(100)] public string? CustomString1Name {  get; set; }

        public bool CustomString2Enabled { get; set; }
        [MaxLength(100)] public string? CustomString2Name { get; set; }

        public bool CustomString3Enabled { get; set; }
        [MaxLength(100)] public string? CustomString3Name { get; set; }

        public bool CustomText1Enabled { get; set; }
        [MaxLength(100)] public string? CustomText1Name { get; set; }

        public bool CustomText2Enabled { get; set; }
        [MaxLength(100)] public string? CustomText2Name { get; set; }

        public bool CustomText3Enabled { get; set; }
        [MaxLength(100)] public string? CustomText3Name { get; set; }

        public bool CustomNumber1Enabled { get; set; }
        [MaxLength(100)] public string? CustomNumber1Name { get; set; }

        public bool CustomNumber2Enabled { get; set; }
        [MaxLength(100)] public string? CustomNumber2Name { get; set; }

        public bool CustomNumber3Enabled { get; set; }
        [MaxLength(100)] public string? CustomNumber3Name { get; set; }

        public bool CustomBool1Enabled { get; set; }
        [MaxLength(100)] public string? CustomBool1Name { get; set; }

        public bool CustomBool2Enabled { get; set; }
        [MaxLength(100)] public string? CustomBool2Name { get; set; }

        public bool CustomBool3Enabled { get; set; }
        [MaxLength(100)] public string? CustomBool3Name { get; set; }

        public bool CustomLink1Enabled { get; set; }
        [MaxLength(100)] public string? CustomLink1Name { get; set; }

        public bool CustomLink2Enabled { get; set; }
        [MaxLength(100)] public string? CustomLink2Name { get; set; }

        public bool CustomLink3Enabled { get; set; }
        [MaxLength(100)] public string? CustomLink3Name { get; set; }

        public ICollection<Item> Items { get; set; } = new List<Item>();
        public ICollection<InventoryAccess> AccessList { get; set; } = new List<InventoryAccess>();
        public ICollection<InventoryTag> InventoryTags { get; set; } = new List<InventoryTag>();

    }
}
