namespace Game.Echo
{
    public enum EchoEventType
    {
        Interact,  // Hedefle etkileşti (kimlik TargetId'de).
        Drop,      // Elindekini bıraktı.
        HoldStart, // Hedefi basılı tutmaya başladı (kimlik TargetId'de).
        HoldEnd    // Basılı tutmayı bıraktı.
    }
}
