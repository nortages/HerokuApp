using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TwitchBot.Main.Enums;

#nullable disable

namespace TwitchBot.Models
{
    [Table("multilang_answer")]
    public class MultiLangAnswer
    {
        [Key] [Column("id")] public int Id { get; set; }

        [Column("option_id")] public int OptionId { get; set; }

        [Column("lang")] public Lang Lang { get; set; }

        [Column("text")] public string Text { get; set; }

        [ForeignKey(nameof(OptionId))] public virtual Option Option { get; set; }
    }
}