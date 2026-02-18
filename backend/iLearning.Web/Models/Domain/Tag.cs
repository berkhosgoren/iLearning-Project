using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.Domain
{
    public class Tag
    {
        public int Id { get; set; }

        [Required, MaxLength(60)]
        public string Name { get; set; } = string.Empty;

        public ICollection<InventoryTag> InventoryTags { get; set; } = new List<InventoryTag>();
    }
}
