using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Visual
{
    // PS1 görünümünün temeli: oyun kamerasını düşük çözünürlüklü bir dokuya çizer ve bu dokuyu
    // keskin piksellerle tam ekran gösterir. Arayüz (HUD) ayrı Canvas'ta tam çözünürlükte kalır.
    public class RetroScreen : MonoBehaviour
    {
        [Tooltip("Oyuncunun kamerası. Görüntüsü düşük çözünürlüklü dokuya çizilir.")]
        [SerializeField] private Camera worldCamera;
        [Tooltip("Dokunun ekranda gösterildiği tam ekran RawImage (HUD'un altındaki Canvas'ta).")]
        [SerializeField] private RawImage screenImage;

        [Header("Görünüm")]
        [Tooltip("Dikey çözünürlük. Genişlik ekran oranından hesaplanır. PS1 oyunları genelde 240 civarı.")]
        [SerializeField] private int verticalResolution = 270;
        [SerializeField] private bool retroEnabled = true;

        [Header("Geliştirme")]
        [Tooltip("Editor ve geliştirme build'inde bu tuş PS1 görünümünü açıp kapatır (karşılaştırma için).")]
        [SerializeField] private Key toggleKey = Key.F1;

        private static readonly int DesaturateId = Shader.PropertyToID("_Desaturate");

        private RenderTexture target;
        private Material screenMaterial;
        private int lastScreenWidth;
        private int lastScreenHeight;

        // Ortak malzeme asset'i Play'de değişmesin diye çalışma anında kopyası kullanılır.
        private void Awake()
        {
            screenMaterial = new Material(screenImage.material);
            screenImage.material = screenMaterial;
        }

        private void OnDestroy()
        {
            Destroy(screenMaterial);
        }

        // 0 = normal, 1 = tamamen gri ve tonlu (ölüm efekti).
        public void SetDesaturation(float amount)
        {
            screenMaterial.SetFloat(DesaturateId, amount);
        }

        private void OnEnable()
        {
            Apply();
        }

        // Play'den çıkarken ya da sahne kapanırken kamera ve görüntü bu bileşenden önce yok edilmiş olabilir.
        private void OnDisable()
        {
            if (worldCamera != null)
            {
                worldCamera.targetTexture = null;
            }

            if (screenImage != null)
            {
                screenImage.enabled = false;
            }

            ReleaseTarget();
        }

        private void Update()
        {
            if (Debug.isDebugBuild && Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                retroEnabled = !retroEnabled;
                Apply();
            }

            // Pencere boyutu değişirse doku yeniden oluşturulur (oran korunsun).
            if (retroEnabled && (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight))
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (!retroEnabled)
            {
                worldCamera.targetTexture = null;
                screenImage.enabled = false;
                ReleaseTarget();
                return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            int height = Mathf.Max(1, verticalResolution);
            int width = Mathf.Max(1, Mathf.RoundToInt(height * (float)Screen.width / Screen.height));

            ReleaseTarget();
            target = new RenderTexture(width, height, 24)
            {
                name = "RetroScreen",
                filterMode = FilterMode.Point, // büyütülürken pikseller keskin kalsın
                wrapMode = TextureWrapMode.Clamp
            };
            target.Create();

            worldCamera.targetTexture = target;
            screenImage.texture = target;
            screenImage.enabled = true;
        }

        private void ReleaseTarget()
        {
            if (target == null)
            {
                return;
            }

            if (worldCamera != null && worldCamera.targetTexture == target)
            {
                worldCamera.targetTexture = null;
            }

            target.Release();
            Destroy(target);
            target = null;
        }
    }
}
