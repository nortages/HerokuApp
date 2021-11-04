using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using Fasterflect;
using TwitchBot.Main;
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

        [Column("additional_data", TypeName = "json")]
        public string AdditionalData { get; set; }

        [ForeignKey(nameof(CallbackId))] 
        public virtual CallbackInfo CallbackInfo { get; set; }

        [ForeignKey(nameof(ParentOptionId))] 
        public virtual Option ParentOption { get; set; }

        public virtual ICollection<Command> Commands { get; set; }
        public virtual ICollection<Option> ChildOptions { get; set; }
        public virtual ICollection<MultiLangAnswer> MultiLangAnswer { get; set; }
    }

    public partial class Option
    {
        [NotMapped] public Dictionary<string, int> UsageFrequency { get; } = new();

        private Command GetCommand(EventArgs e)
        {
            var chatCommand = (e as OnChatCommandReceivedArgs)?.Command;
            if (chatCommand is null)
                return null;

            var command = Commands.Single(c => c.Names.Contains(chatCommand.CommandText));
            return command;
        }

        public string GetAnswer(object o, EventArgs e, CallbackArgs args)
        {
            var answer = "";

            if (CallbackId != null)
            {
                args.Option = this;

                answer = (string) args.CallMethodTarget.CallMethod(CallbackId, o, e, args);
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
                answer = answer.Replace(optionVariable, GetAnswerFromOptions(this, e, args));
            args.IsMentionRequired ??= IsMentionRequired;

            return answer;
        }

        public string GetMultiLangAnswer(CallbackArgs args)
        {
            var answer = MultiLangAnswer.SingleOrDefault(a => a.Lang == args.ChannelInfo.Lang) ??
                         MultiLangAnswer.SingleOrDefault(a => a.Lang == Lang.ru);
            return answer!.Text;
        }

        public string GetAnswerFromOptions(object o, EventArgs e, CallbackArgs args)
        {
            var chatCommand = (e as OnChatCommandReceivedArgs)?.Command;
            var username = chatCommand!.ChatMessage.Username;
            var randDouble = Program.Rand.NextDouble();

            if (args.ChannelInfo.IsTestMode)
            {
                var numberOfNesting = GetNumberOfNesting();
                if (chatCommand.ArgumentsAsList.Count >= numberOfNesting + 1)
                    randDouble = GetValueFromArguments(chatCommand.ArgumentsAsList, numberOfNesting, randDouble);
            }

            string answer;
            try
            {
                var option = ChildOptions.GetProbableOption(randDouble);
                answer = option.GetAnswer(option, e, args);
                args.UserChannelCommand.LastOptionId = option.Id;
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

        private static double GetValueFromArguments(IReadOnlyList<string> commandArguments, int numberOfNesting,
            double randDouble)
        {
            var argument = commandArguments[numberOfNesting];
            return double.Parse(argument, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public void IncreaseUsageFrequency(string username)
        {
            if (!UsageFrequency.ContainsKey(username)) UsageFrequency.Add(username, default);
            UsageFrequency[username]++;
        }
    }
}