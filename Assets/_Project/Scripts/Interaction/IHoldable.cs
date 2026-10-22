namespace Game.Interaction
{
    // Basılı tutulan nesneler (düğme, kol, kelepçe). Tutma sürdükçe etkisi sürer.
    // Aynı anda birden fazla aktör tutabilir (oyuncu ve yankıları).
    public interface IHoldable
    {
        // False ise E bırakılınca ya da başka yere bakınca tutma bitmez; sadece ölümle biter (örnek: kelepçe).
        bool ReleasesOnInput { get; }

        // True ise tutan oyuncu yürüyemez (bakabilir ve başka şeylerle etkileşebilir).
        bool LocksMovement { get; }

        bool CanHold(IInteractor interactor);
        void BeginHold(IInteractor interactor);
        void EndHold(IInteractor interactor);
    }
}
