using System;
using System.Collections.Generic;

namespace TwitchBot.Main.Hearthstone
{
    public class Player
    {
        private static readonly Queue<DeathrattleEffectArgs> deathrattleEffectsArgsPending = new();

        public Player(string username)
        {
            Username = username;
            Board = new Board(this);
            Board.OnMinionDied += Board_OnMinionDied;
        }

        public Board Board { get; set; }
        public string Username { get; set; }
        public Player Opponent { get; set; }

        private void Board_OnMinionDied(object sender, EventArgs e)
        {
            var minion = (Minion) sender;
            var addInfo = minion.Info.AdditionalInfo;
            if (addInfo != null && addInfo.DeathrattleEffect != null)
            {
                var args = new DeathrattleEffectArgs
                {
                    Player = this,
                    Minion = minion
                };
                deathrattleEffectsArgsPending.Enqueue(args);
            }

            if (minion.HasReborn)
            {
                var rebornMinion = Board.SummonBeside(minion.Info, minion);
                rebornMinion.HasReborn = false;
                rebornMinion.TakeDamage(rebornMinion.HealthPoints - (rebornMinion.HealthPoints - 1));
            }
        }

        public void PerformActionsBeforeStart()
        {
            Board.PerformActionsBeforeStart(this);
        }

        internal void TakeTurn()
        {
            var minionThatAttacks = Board.GetActiveMinion();
            minionThatAttacks.AttackRandom();
            if (!minionThatAttacks.IsDead && minionThatAttacks.Info.HasWindfury) minionThatAttacks.AttackRandom();

            PerformDeathrattleEffects();
            Board.RemoveDeadMinions();
            Opponent.Board.RemoveDeadMinions();

            if (!minionThatAttacks.IsDead)
            {
                Console.WriteLine("Increase index");
                Board.IndexOfMinionWhoseTurn++;
            }
        }

        private static void PerformDeathrattleEffects()
        {
            while (deathrattleEffectsArgsPending.TryDequeue(out var arg))
                arg.Minion.Info?.AdditionalInfo?.DeathrattleEffect(arg.Minion, arg.Player);
        }
    }
}