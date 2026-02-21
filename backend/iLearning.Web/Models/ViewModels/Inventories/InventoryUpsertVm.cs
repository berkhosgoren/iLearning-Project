using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.ViewModels.Inventories
{
    public class InventoryUpsertVm
    {
        public Guid? Id { get; set; }

        [Required, MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Public inventory")]
        public bool IsPublic { get; set; }

        [Display(Name = "Tags (comma seperated for now)")]
        [MaxLength(500)]
        public string? TagsCsv { get; set; }


        public int Version { get; set; }


        public bool CustomString1Enabled { get; set; }
        [MaxLength(100)] public string? CustomString1Name { get; set; }
        public bool CustomString2Enabled { get; set; }
        [MaxLength(100)] public string? CustomString2Name{ get; set; }
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
    }
}
