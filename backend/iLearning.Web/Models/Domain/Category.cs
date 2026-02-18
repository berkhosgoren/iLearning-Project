using System.ComponentModel.DataAnnotations;

namespace iLearning.Web.Models.Domain
{
    public class Category
    {
        public int Id { get; set; }

        [Required, MaxLength(60)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}
