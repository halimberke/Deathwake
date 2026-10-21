namespace Game.Interaction
{
    // Bir aktörün (oyuncu ya da yankı) etkileşebildiği her nesnenin uyguladığı arayüz.
    // Kapı, düğme, eşya gibi nesneler bunu uygular.
    public interface IInteractable
    {
        // Ekranda gösterilecek yazı. Örnek: "Aç", "Kilitli: anahtar gerekli".
        string GetPrompt(IInteractor interactor);

        // Şu an kullanılabilir mi? False ise E'ye basmak bir şey yapmaz.
        bool CanInteract(IInteractor interactor);

        void Interact(IInteractor interactor);
    }
}
