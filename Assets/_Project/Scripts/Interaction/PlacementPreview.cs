using UnityEngine;

namespace Game.Interaction
{
    // Elde eşya varken, nişan alınan yerde eşyanın yarı saydam kopyasını gösterir.
    // Yeşil: buraya konabilir. Kırmızı: konamaz. E'ye basınca eşya tam bu poza konur.
    public class PlacementPreview : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private HandSlot hand;
        [SerializeField] private Material validMaterial;
        [SerializeField] private Material invalidMaterial;

        private GameObject preview;
        private MeshFilter previewMesh;
        private MeshRenderer previewRenderer;
        private Pickup shownItem;

        private void Awake()
        {
            // Oyuncunun alt nesnesi değil: dünya konumunda durur, oyuncuyla birlikte dönmez.
            preview = new GameObject("PlacementPreview");
            previewMesh = preview.AddComponent<MeshFilter>();
            previewRenderer = preview.AddComponent<MeshRenderer>();
            previewRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            previewRenderer.receiveShadows = false;
            preview.SetActive(false);
        }

        private void OnDisable()
        {
            if (preview != null)
            {
                preview.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (preview != null)
            {
                Destroy(preview);
            }
        }

        // Kamera ve oyuncu Update'te hareket ettikten sonra, karenin son pozuyla hesaplanır.
        private void LateUpdate()
        {
            Pickup item = hand.HeldItem;
            Pose pose = default;
            bool valid = false;
            bool show = item != null && interactor.isActiveAndEnabled && interactor.CanDrop &&
                        hand.TryGetPlacement(out pose, out valid);

            if (!show)
            {
                preview.SetActive(false);
                return;
            }

            if (shownItem != item)
            {
                MeshFilter source = item.GetComponentInChildren<MeshFilter>();
                previewMesh.sharedMesh = source != null ? source.sharedMesh : null;
                shownItem = item;
            }

            preview.transform.SetPositionAndRotation(pose.position, pose.rotation);
            preview.transform.localScale = item.OriginalScale;
            previewRenderer.sharedMaterial = valid ? validMaterial : invalidMaterial;
            preview.SetActive(true);
        }
    }
}
