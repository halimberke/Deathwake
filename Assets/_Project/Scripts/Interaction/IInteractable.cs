namespace Game.Interaction
{
    // Oyuncunun E ile etkileşebildiği her nesnenin uyguladığı arayüz.
    // Kapı, düğme, eşya gibi nesneler bunu uygular. PlayerInteractor sadece bu arayüzü tanır.
    public interface IInteractable
    {
        // Ekranda gösterilecek yazı. Örnek: "Aç", "Kilitli: anahtar gerekli".
        string GetPrompt(PlayerInteractor interactor);

        // Şu an kullanılabilir mi? False ise E'ye basmak bir şey yapmaz.
        bool CanInteract(PlayerInteractor interactor);

        void Interact(PlayerInteractor interactor);
    }
}
