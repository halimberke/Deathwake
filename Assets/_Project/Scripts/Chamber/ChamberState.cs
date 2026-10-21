namespace Game.Chamber
{
    public enum ChamberState
    {
        Preparing, // Oda yüklendi, deneme henüz başlamadı.
        Running,   // Saat işliyor.
        Dead,      // Oyuncu öldü; oyun akışı odayı yeniden yükleyecek.
        Solved     // Oda çözüldü.
    }
}
