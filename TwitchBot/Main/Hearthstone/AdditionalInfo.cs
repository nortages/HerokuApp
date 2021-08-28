using System;

namespace TwitchBot.Main.Hearthstone
{
    public class AdditionalInfo
    {
        public int Id { get; set; }
        public Action<Minion, Player> DeathrattleEffect { get; set; }
        public Action<Minion, Board> OnSummoned { get; set; }
        public Action<Minion, Player> OnBeforeFirstTurn { get; set; }
        public Func<Board, Minion> OnAttack { get; set; }
        public Action<Minion> OnWasAttacked { get; set; }
    }
}
