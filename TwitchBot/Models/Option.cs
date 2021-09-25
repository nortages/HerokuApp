using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using Fasterflect;
using TwitchBot.Main;
using TwitchBot.Main.Callbacks;
using TwitchBot.Main.ExtensionsMethods;
using TwitchLib.Client.Events;

#nullable disable

namespace TwitchBot.Models
{
    [Table("option")]
    public partial class Option
    {
        [Key] [Column("id")]
        public int Id { get; set; }
        [Required] [Column("is_enabled")]
        public bool IsEnabled { get; set; }
        [Column("is_mention_required")]
        public bool? IsMentionRequired { get; set; }
        [Column("callback_id")]
        public string CallbackId { get; set; }
        [Column("parent_option_id")]
        public int? ParentOptionId { get; set; }
        [Column("probability")]
        public double? Probability { get; set; }
        [Column("name")]
        public string Name { get; set; }

        [ForeignKey(nameof(CallbackId))]
        public virtual CallbackInfo CallbackInfo { get; set; }
        [ForeignKey(nameof(ParentOptionId))]
        public virtual Option ParentOption { get; set; }
        public virtual List<Command> Commands { get; set; }
        public virtual MessageCommand MessageCommand { get; set; }
        public virtual ICollection<Option> ChildOptions { get; set; }
        public virtual ICollection<MultiLangAnswer> MultiLangAnswer { get; set; }
    }
    
    public partial class Option
    {
        [NotMapped]
        public Dictionary<string, int> UsageFrequency { get; } = new();
       
        public string GetAnswer(object o, EventArgs e, CallbackArgs args)
        {
            var answer = "";
            
            if (CallbackId != null)
            {
                args.Option = this;
                answer = (string) typeof(CommandCallbacks).CallMethod(CallbackId, o, e, args);
            }
            else if (MultiLangAnswer is {Count: > 0})
            {
                answer = GetMultiLangAnswer(args);
            }
            else if (ChildOptions is {Count: > 0})
            {
                answer = GetAnswerFromOptions(o, e, args);
            }
            
            const string optionVariable = "${option}";
            if (answer != null && answer.Contains(optionVariable))
            {
                answer = answer.Replace(optionVariable, GetAnswerFromOptions(this, e, args));
            }
            args.IsMentionRequired ??= IsMentionRequired;
            
            return answer;
        }
        
        public string GetMultiLangAnswer(CallbackArgs args)
        {
            var answer = MultiLangAnswer.SingleOrDefault(a => a.Lang == args.ChannelBotInfo.Lang) ?? 
                         MultiLangAnswer.SingleOrDefault(a => a.Lang == Lang.ru);
            return answer!.Text;
        }

        public string GetAnswerFromOptions(object o, EventArgs e, CallbackArgs args)
        {
            var command = (e as OnChatCommandReceivedArgs)?.Command;
            var username = command!.ChatMessage.Username;
            var randDouble = Program.Rand.NextDouble();

            if (args.ChannelBotInfo.IsTestMode)
            {
                var numberOfNesting = GetNumberOfNesting();
                if (command.ArgumentsAsList.Count >= numberOfNesting + 1)
                {
                    randDouble = GetValueFromArguments(command.ArgumentsAsList, numberOfNesting, randDouble);
                }
            }

            string answer;
            try
            {
                var option = ChildOptions.GetProbableOption(randDouble);
                answer = option.GetAnswer(option, e, args);
                if (Commands is {Count: > 0}) Commands.First(c => c.Names.Contains(command.CommandText)).ChannelCommand.LastOption[username] = option;
            }
            catch (ArgumentException ex)
            {
                answer = ex.Message;
            }

            return answer;
        }

        private int GetNumberOfNesting()
        {
            var currentOption = this;
            var numberOfNesting = 0;
            while (true)
            {
                if (currentOption.ParentOptionId == null) break;
                currentOption = currentOption.ParentOption;
                numberOfNesting++;
            }
            return numberOfNesting;
        }

        private static double GetValueFromArguments(IReadOnlyList<string> commandArguments, int numberOfNesting, double randDouble)
        {
            var argument = commandArguments[numberOfNesting];
            return double.Parse(argument, NumberStyles.Any, CultureInfo.InvariantCulture);
        }
        
        public void IncreaseUsageFrequency(string username)
        {
            if (!UsageFrequency.ContainsKey(username))
            {
                UsageFrequency.Add(username, default);
            }
            UsageFrequency[username]++;
        }
    }
}
