namespace Game.Interaction
{
    // Etkileşimi yapan taraf: oyuncu ya da yankı.
    // Kapı, düğme, eşya gibi nesneler karşılarında kimin olduğunu bilmez, sadece bu arayüzü görür.
    public interface IInteractor
    {
        // Eşya taşımayan bir aktör için null olabilir.
        HandSlot Hand { get; }
    }
}
