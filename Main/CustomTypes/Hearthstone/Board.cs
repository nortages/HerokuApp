using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HerokuApp.Main.CustomTypes.Hearthstone
{
    public class Board
    {
        [JsonIgnore] public Player Player { get; }

        [JsonProperty]
        readonly List<Minion> minions = new List<Minion>();
        const int MAXBOARDSIZE = 7;

        private int indexOfMinionWhoseTurn = 0;

        public Board(Player player)
        {
            Player = player;
        }

        public int IndexOfMinionWhoseTurn
        {
            get => indexOfMinionWhoseTurn >= minions.Count ? 0 : indexOfMinionWhoseTurn;
            set => indexOfMinionWhoseTurn = value;
        }

        public event EventHandler OnMinionDied;
        public event EventHandler OnMinionLostDivineShield;

        public Minion this[int i]
        {
            get { return minions[i]; }
        }

        public IReadOnlyCollection<Minion> GetMinions()
        {
            return minions.Where(n => !n.IsDead).ToList().AsReadOnly();
        }

        public override string ToString()
        {
            return string.Join(", ", minions);
        }

        public Minion Summon(MinionInfo minion, int index)
        {
            var newMinion = new Minion(minion, Player);
            if (GetMinions().Count >= MAXBOARDSIZE) return null;

            minions.Insert(index, newMinion);

            newMinion.OnDied += Minion_OnDied;
            newMinion.OnLostDivineShield += Minion_OnLostDivineShield;
            return newMinion;
        }

        public Minion StartSummon(MinionInfo minion)
        {
            return Summon(minion, minions.Count);
        }

        private void Minion_OnLostDivineShield(object sender, EventArgs e)
        {
            OnMinionLostDivineShield?.Invoke(sender, e);
        }

        internal void PerformActionsBeforeStart(Player player)
        {
            foreach (var minion in minions)
            {
                minion.Info.AdditionalInfo?.OnBeforeFirstTurn?.Invoke(minion, player);
            }
        }

        private void Minion_OnDied(object sender, EventArgs e)
        {
            var minion = (Minion)sender;
            //var minionPosition = board.IndexOf(minion);
            //if (minionPosition < IndexOfMinionWhoseTurn) IndexOfMinionWhoseTurn--;
            //Remove(minion);
            //var args = new MinionDiedArgs {
            //    position = minionPosition
            //};
            OnMinionDied?.Invoke(sender, null);
        }

        public void Remove(Minion minion)
        {
            if (!minions.Contains(minion)) return;
            minion.OnLostDivineShield -= Minion_OnLostDivineShield;
            minion.OnDied -= Minion_OnDied;
            minions.Remove(minion);
        }

        internal void RemoveDeadMinions() => minions.RemoveAll(n => n.IsDead);

        public Minion GetRandomMinion()
        {
            if (IsEmpty()) return null;
            return GetMinions().RandomElement();
        }

        public Minion GetActiveMinion()
        {
            return minions[IndexOfMinionWhoseTurn];
        }

        public bool IsEmpty()
        {
            return minions.Count == 0;
        }

        internal Minion SummonBeside(MinionInfo minion, Minion parent)
        {
            var parentPosition = minions.IndexOf(parent);
            return Summon(minion, parentPosition + 1);
        }

        internal List<Minion> GetCopy()
        {
            return JsonConvert.DeserializeObject<List<Minion>>(JsonConvert.SerializeObject(minions));
        }

        internal Board GetBoardCopy()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<Board>(serialized);
        }
    }
}