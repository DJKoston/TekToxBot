using System.ComponentModel.DataAnnotations;

namespace TekTox.DAL
{
    public abstract class Entity
    {
        [Key]
        public int Id { get; set; }
    }
}
