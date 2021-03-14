using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HerokuApp.Main.CustomTypes.Hearthstone
{
    public enum Outcome { Win, Lose, Tie }

    public class TurnRecord
    {
        public Tuple<Board, Board> FullBoardState { get; set; }
        public List<TurnEvent> TurnEvents { get; set; }
    }

    public abstract class TurnEvent
    {
        public string EventInitiator { get; set; }
        public string WhoHitByTheEvent { get; set; }
    }

    public class AttackEvent : TurnEvent { }
    public class TakeDamageEvent : TurnEvent { }
    public class BuffEvent : TurnEvent { }

    public class BattlegroundsRound
    {
        readonly Player player1;
        readonly Player player2;
        readonly List<Player> players = new List<Player>();

        public string Id { get; set; }
        public List<TurnRecord> BoardStatesDuringRound2 = new List<TurnRecord>();
        public List<Tuple<Board, Board>> BoardStatesDuringRound = new List<Tuple<Board, Board>>();

        public BattlegroundsRound(string player1, string player2)
        {
            Id = DateTime.Now.ToString("d-H-m-s").Replace("-", "");

            this.player1 = new Player(player1);
            this.player2 = new Player(player2);
            players.Add(this.player1);
            players.Add(this.player2);

            for (int i = 0; i < players.Count; i++)
            {
                players[i].Opponent = players[Math.Abs(i - 1)];
            }
        }

        public Outcome Play(int? whoseFirstTurn = null)
        {
            Console.WriteLine("\n~~~Battleground round starts!~~~");
            int indexOfPlayer;
            if (whoseFirstTurn != null)
            {
                indexOfPlayer = (int)whoseFirstTurn;
            }
            else
            {
                indexOfPlayer = Config.rand.Next(0, players.Count);
            }
            OutputFullBoard();
            var playerWhoseTurn = players[indexOfPlayer];
            var indexOfOpponent = Math.Abs(indexOfPlayer - 1);
            var opponent = players[indexOfOpponent];

            playerWhoseTurn.PerformActionsBeforeStart();
            opponent.PerformActionsBeforeStart();
            do
            {
                playerWhoseTurn.TakeTurn();
                OutputFullBoard();
                SwapPlayers(ref playerWhoseTurn, ref opponent);
            } while (!player1.Board.IsEmpty() && !player2.Board.IsEmpty());

            Outcome outcome;
            if (player1.Board.IsEmpty() && player2.Board.IsEmpty())
            {
                outcome = Outcome.Tie;
                Console.WriteLine("Outcome: tie!");
            }
            else
            {
                var playerThatWon = players.Single(n => !n.Board.IsEmpty());
                outcome = playerThatWon.Equals(player1) ? Outcome.Win : Outcome.Lose;
                Console.WriteLine($"Outcome: {playerThatWon.Username} wins!");
            }

            Console.WriteLine("~~~Battleground round ends!~~~");
            return outcome;
        }

        public Tuple<MinionInfo, MinionInfo> SummonStartMinions(Tuple<int[], int[]> chosenMinionsIds = null)
        {
            var allMinions = HearthstoneApiClient.GetBattlegroundsMinions();
            if (chosenMinionsIds != null)
            {
                foreach (var item in chosenMinionsIds.Item1)
                {
                    var minion = allMinions.SingleOrDefault(n => n.Id == item);
                    if (minion == null) return null;
                    player1.Board.StartSummon(minion);
                }
                foreach (var item in chosenMinionsIds.Item2)
                {
                    var minion = allMinions.SingleOrDefault(n => n.Id == item);
                    if (minion == null) return null;
                    player2.Board.StartSummon(minion);
                }
            }
            else
            {
                foreach (var player in players)
                {
                    var newMinion = allMinions.RandomElement();
                    player.Board.StartSummon(newMinion);
                }
            }
            return new Tuple<MinionInfo, MinionInfo>(player1.Board[0].Info, player2.Board[0].Info);
        }

        void SwapPlayers(ref Player player1, ref Player player2)
        {
            var temp = player1;
            player1 = player2;
            player2 = temp;
        }

        public void OutputFullBoard()
        {
            Console.WriteLine($"{player1.Username} board: {player1.Board}");
            Console.WriteLine($"{player2.Username} board: {player2.Board}");
            var board1Copy = player1.Board.GetBoardCopy();
            var board2Copy = player2.Board.GetBoardCopy();
            BoardStatesDuringRound.Add(Tuple.Create(board1Copy, board2Copy));
        }
    }
}
