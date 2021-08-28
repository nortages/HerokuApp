using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using TwitchBot.Main.Hearthstone;

namespace BattlegroundsDuelsTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var round = new BattlegroundsRound("player1", "player2");
            // 40428 - 2/2 крыса, 778 - 2/3 голем
            var player1Minions = new int[] { 40428 };
            var player2Minions = new int[] { 778 };
            round.SummonStartMinions(Tuple.Create(player1Minions, player2Minions));
            var outcome = round.Play(0);
            var expectedRecords = new List<List<Minion>>
            {
                new List<Minion>
                {

                },
            };
            Assert.AreEqual(Outcome.Tie, outcome);
        }
    }
}
