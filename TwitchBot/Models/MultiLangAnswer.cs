using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace TwitchBot.Models
{
    public enum Lang
    {
        ru,
        ua
    }
    
    [Table("multilang_answer")]
    public partial class MultiLangAnswer
    {
        [Key] [Column("id")]
        public int Id { get; set; }
        [Column("option_id")]
        public int OptionId { get; set; }
        [Column("lang")]
        public Lang Lang { get; set; }
        [Column("text")]
        public string Text { get; set; }

        [ForeignKey(nameof(OptionId))]
        public virtual Option Option { get; set; }
    }
}
