using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ItemManagement.Model
{
    public class Cart
    {
        [Key]
        public Guid Id { get; set; } 

        [Required]
        public Guid UserId { get; set; } 


        //Relationship
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        public virtual ICollection<Item>? Items { get; set; }

      


    }
}
