using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TwitchBot.Models
{
    [Table("user")]
    public class User
    {
        [Key] [Column("id")]
        public string Id { get; set; }

        [Required] [Column("name")] 
        public string Name { get; set; }
    }
}