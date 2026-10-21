namespace Game.Chamber
{
    public enum ChamberState
    {
        Preparing, // Sahne yüklendi, deneme henüz başlamadı.
        Running,   // Saat işliyor.
        Dead,      // Oyuncu öldü, kısa bekleme.
        Resetting, // Sahne yeniden yükleniyor.
        Solved     // Oda çözüldü.
    }
}
