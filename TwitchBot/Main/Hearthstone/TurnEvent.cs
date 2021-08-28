namespace TwitchBot.Main.Hearthstone
{
    public abstract class TurnEvent
    {
        public string EventInitiator { get; set; }
        public string WhoHitByTheEvent { get; set; }
    }
}