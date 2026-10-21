namespace Game.Echo
{
    // Kayıttaki bir eylem. Örnek: 3.2 sn'de kimliği "a1b2..." olan kapıyla etkileşti.
    public readonly struct EchoEvent
    {
        public readonly float Time;
        public readonly EchoEventType Type;
        public readonly string TargetId; // Drop için null.

        public EchoEvent(float time, EchoEventType type, string targetId)
        {
            Time = time;
            Type = type;
            TargetId = targetId;
        }
    }
}
