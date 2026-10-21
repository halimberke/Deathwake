namespace Game.Interaction
{
    // Basılı tutulan nesneler (düğme, kol, vana). Tutma sürdükçe etkisi sürer.
    // Aynı anda birden fazla aktör tutabilir (oyuncu ve yankıları).
    public interface IHoldable
    {
        bool CanHold(IInteractor interactor);
        void BeginHold(IInteractor interactor);
        void EndHold(IInteractor interactor);
    }
}
