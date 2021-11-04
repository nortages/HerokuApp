using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchBot.Main.Hearthstone;
using TwitchBot.Main.Interfaces;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace TwitchBot.Main.MiniGames
{
    public class BattlegroundsDuel : IMiniGame
    {
        private readonly List<DuelOffer> DuelOffersPendingAcceptance = new();

        public string OfferCombatCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            return PerformDuel(e.Command, args, true);
        }

        public string OfferDuelCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            return PerformDuel(e.Command, args);
        }

        public string AcceptDuelCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var username = command.ChatMessage.Username;
            var argsAsStr = command.ArgumentsAsString;
            DuelOffer offerToAccept;
            if (!string.IsNullOrEmpty(argsAsStr))
                offerToAccept = DuelOffersPendingAcceptance
                    .Where(n => string.Equals(n.WhomIsOffered, username, BotService.StringComparison))
                    .SingleOrDefault(n => string.Equals(n.WhoOffers, argsAsStr, BotService.StringComparison));
            else
                offerToAccept = DuelOffersPendingAcceptance.FirstOrDefault(n =>
                    string.Equals(n.WhomIsOffered, username, BotService.StringComparison));
            if (offerToAccept == null) return null;

            DuelOffersPendingAcceptance.Remove(offerToAccept);

            return offerToAccept.IsCombat ? PerformCombat(offerToAccept, e.Command, args) : PerformDuel(offerToAccept);
        }

        private string PerformDuel(ChatCommand command, CallbackArgs args, bool isCombat = false)
        {
            var whoOffers = command.ChatMessage.Username;
            string whomIsOffered = null;
            var argsAsList = command.ArgumentsAsList;

            if (argsAsList.Count > 0)
                whomIsOffered = argsAsList[0].TrimStart('@');

            if (string.IsNullOrEmpty(whomIsOffered))
                return null;

            if (string.Equals(whoOffers, whomIsOffered, BotService.StringComparison))
                return $"@{whoOffers} нельзя вызвать самого себя на дуэль Kappa";

            var newOffer = new DuelOffer(whoOffers, whomIsOffered, isCombat);
            if (string.Equals(whomIsOffered, BotService.BotUsername, BotService.StringComparison))
            {
                // Ex. 778-40428|49279
                const string numbersPart = @"((\d{3,})-?)*";
                var regex = new Regex($@"{numbersPart}\|{numbersPart}");
                Tuple<int[], int[]> specialMinionsIds = null;
                if (args.ChannelInfo.IsTestMode && argsAsList.Count >= 2 && regex.IsMatch(argsAsList[1]))
                {
                    var allIds = argsAsList[1].Split("|");
                    var allIdsParsed = allIds.Select(n => n.Split("-").Select(int.Parse).ToArray()).ToArray();
                    specialMinionsIds = Tuple.Create(allIdsParsed[0], allIdsParsed[1]);
                }

                return isCombat ? PerformCombat(newOffer, command, args) : PerformDuel(newOffer, specialMinionsIds);
            }

            if (DuelOffersPendingAcceptance.Contains(newOffer))
                return null;

            DuelOffersPendingAcceptance.Add(newOffer);
            Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(_ =>
            {
                if (!DuelOffersPendingAcceptance.Contains(newOffer))
                    return;
                DuelOffersPendingAcceptance.Remove(newOffer);
            });
            return null;
        }

        private string PerformCombat(DuelOffer offer, ChatCommand command, CallbackArgs args)
        {
            var player1 = offer.WhoOffers;
            var player2 = offer.WhomIsOffered;

            var standings = new Dictionary<string, int>
            {
                {player1, 0},
                {player2, 0}
            };

            var startMinionsOnEachRound = new List<Tuple<MinionInfo, MinionInfo>>();
            do
            {
                var round = new BattlegroundsRound(offer.WhoOffers, offer.WhomIsOffered);
                var startMinions = round.SummonStartMinions();
                startMinionsOnEachRound.Add(startMinions);
                var outcome = round.Play();
                if (outcome == Outcome.Tie)
                {
                    standings[player1]++;
                    standings[player2]++;
                }
                else
                {
                    var playerWhoWon = outcome == Outcome.Win ? player1 : player2;
                    standings[playerWhoWon]++;
                }
            } while (!(standings.Any(n => n.Value >= 2) && standings[player1] != standings[player2]));

            var ordered = standings.OrderBy(k => k.Value).ToArray();
            var playerWhoLost = ordered.First().Key;
            if (!args.ChannelInfo.IsTestMode)
                UtilityFunctions.TimeoutCommandUser(command, args, TimeSpan.FromMinutes(5), playerWhoLost,
                    "Проиграл в сражении в миниигре");
            var playerWhoWin = ordered.Last().Key;
            var answer = $"{player1} бросил вызов {player2}! ";
            for (var i = 0; i < startMinionsOnEachRound.Count; i++)
                answer +=
                    $"{i + 1}й раунд - {startMinionsOnEachRound[i].Item1} vs {startMinionsOnEachRound[i].Item2}, ";
            answer = answer.TrimEnd(',', ' ');
            return $"{answer}. Победил {playerWhoWin} со счётом {standings[playerWhoWin]}:{standings[playerWhoLost]}!";
        }

        private string PerformDuel(DuelOffer offer, Tuple<int[], int[]> specialMinionsIds = null)
        {
            var round = new BattlegroundsRound(offer.WhoOffers, offer.WhomIsOffered);

            var minions = round.SummonStartMinions(specialMinionsIds);
            if (minions == null) return null;
            var (minion1, minion2) = minions;

            var outcome = round.Play();

            string result;
            if (outcome == Outcome.Tie)
                result = "Ничья!";
            else
                result = $"Победил {(outcome == Outcome.Win ? offer.WhoOffers : offer.WhomIsOffered)}!";
            return $"{offer.WhoOffers} выпало {minion1}, а {offer.WhomIsOffered} - {minion2}! {result}";
        }
    }
}