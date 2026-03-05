using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.ViewModels.Inventories
{
    public class InventoryCustomIdVm
    {
        public Guid InventoryId { get; set; }
        
        public int Version { get; set; }

        [MaxLength(20)]
        public string? Prefix { get; set; }

        [Range(1, 8)]
        public int Digits { get; set; } = 4;

        [Range(1, int.MaxValue)]
        public int NextNumber { get; set; } = 1;

        public string Preview { get; set; } = "";
    }
}
