using System;
using System.Linq;
using Newtonsoft.Json;
using TwitchBot.Main.ExtensionsMethods;

namespace TwitchBot.Main.Hearthstone
{
    public class Minion
    {
        [JsonIgnore] public Player Player { get; private set; }

        [JsonProperty] public MinionInfo Info { get; private set; }
        public int HealthPoints { get; set; }
        public int AttackPoints { get; set; }
        public bool IsDead { get; private set; }
        public bool HasReborn { get; set; }
        public bool HasDivineShield { get; set; }

        public event EventHandler OnDied;
        public event EventHandler OnLostDivineShield;

        public Minion(MinionInfo battlegroundsMinionInfo, Player player)
        {
            Player = player;
            Info = battlegroundsMinionInfo;
            if (Info == null) return;
            HealthPoints = Info.Health;
            AttackPoints = Info.Attack;
            HasReborn = Info.HasReborn;
            HasDivineShield = Info.HasDivineShield;

            if (Info.AdditionalInfo != null && Info.AdditionalInfo.OnSummoned != null)
            {
                Info.AdditionalInfo.OnSummoned(this, player.Board);
            }
        }

        public override string ToString()
        {
            return $"{Info.Name} {AttackPoints}-{HealthPoints}";
        }

        void Attack(Minion minionToAttack)
        {
            Console.WriteLine($"{this} attacks {minionToAttack}");
            if (true)
            {

            }
            TakeDamage(minionToAttack.AttackPoints);
            minionToAttack.TakeDamageFromMinion(this);
        }

        public void AttackRandom(Board enemyBoard)
        {
            Minion minionToAttack;
            var enemyMinions = enemyBoard.GetMinions();
            if (Info.AdditionalInfo?.OnAttack != null)
            {
                minionToAttack = Info.AdditionalInfo.OnAttack.Invoke(enemyBoard);
            }
            else
            {
                var enemyTauntMinions = enemyMinions.Where(n => n.Info.HasTaunt);
                minionToAttack = enemyTauntMinions.RandomElement();
                if (minionToAttack == null) minionToAttack = enemyMinions.RandomElement();
            }
            if (minionToAttack == null) return;
            Attack(minionToAttack);
        }

        public void AttackRandom()
        {
            AttackRandom(Player.Opponent.Board);
        }

        public bool TakeDamage(int attackPoints)
        {
            if (HasDivineShield)
            {
                OnLostDivineShield?.Invoke(this, null);
                HasDivineShield = false;
                return false;
            }

            HealthPoints -= attackPoints;
            if (HealthPoints <= 0)
            {
                Die();
            }
            return true;
        }

        public void TakeDamageFromMinion(Minion minion)
        {
            TakeDamage(minion.AttackPoints);
            Info.AdditionalInfo?.OnWasAttacked?.Invoke(this);
        }

        private void Die()
        {
            IsDead = true;
            OnDied?.Invoke(this, null);
        }        
    }        
}