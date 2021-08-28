using System;
using System.Collections.Generic;

namespace TwitchBot.Main.Hearthstone
{
    public class TurnRecord
    {
        public Tuple<Board, Board> FullBoardState { get; set; }
        public List<TurnEvent> TurnEvents { get; set; }
    }
}