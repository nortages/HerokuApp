using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace TwitchBot.Models
{
    [Table("credentials")]
    public class Credentials
    {
        [Key] [Column("id")] 
        public int Id { get; set; }

        [Required] [Column("access_token")] 
        public string AccessToken { get; set; }

        [Column("refresh_token")] 
        public string RefreshToken { get; set; }

        [Column("expiration_date")] 
        public DateTime? ExpirationDate { get; set; }
    }
}