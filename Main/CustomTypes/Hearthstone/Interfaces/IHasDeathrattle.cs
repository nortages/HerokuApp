namespace HerokuApp.Main.CustomTypes.Hearthstone.Interfaces
{
    public interface IHasDeathrattle
    {
        public void DeathrattleEffect(Player owner, Player opponent);
    }
}