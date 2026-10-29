using Game.Flow;
using Game.Player;
using UnityEngine;

namespace Game.Story
{
    // Hikâye sahnesinden Core'daki oyuncuya ve ekrana verilen komutlar.
    // Sahneler arası doğrudan referans kurulamadığı için hedefler çağrıldıkları anda bulunur.
    // Senaryo adımları (UnityEvent) bu metotları çağırır.
    public class SceneCues : MonoBehaviour
    {
        private PlayerRig player;
        private ScreenFader fader;

        private PlayerRig Player
        {
            get
            {
                if (player == null)
                {
                    GameObject go = GameObject.FindWithTag("Player");
                    player = go != null ? go.GetComponent<PlayerRig>() : null;
                }

                return player;
            }
        }

        private ScreenFader Fader
        {
            get
            {
                if (fader == null)
                {
                    fader = FindAnyObjectByType<ScreenFader>();
                }

                return fader;
            }
        }

        public void BlockPlayer() => Player.SetScriptedBlock(true);

        public void ReleasePlayer() => Player.SetScriptedBlock(false);

        public void WakeUp(float duration) => Player.PlayWakeUp(duration);

        public void SetBlack() => Fader.SetBlack();

        // Karartma bu nesnede çalışır; sahne atılırsa yarıda kalır (zaten sahne değişiyordur).
        public void FadeIn(float duration) => StartCoroutine(Fader.FadeTo(0f, duration));

        public void FadeOut(float duration) => StartCoroutine(Fader.FadeTo(1f, duration));

        public void AdvanceScene() => FlowRequests.Advance();
    }
}
