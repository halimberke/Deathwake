using Game.Audio;
using UnityEngine;

namespace Game.Echo
{
    // Yankının görünümünü sürer: belirme, eylem öncesi parlama, kayıt bitince dağılma.
    // Shader değerleri MaterialPropertyBlock ile verilir: her yankı kendi değerini taşır,
    // ortak malzeme (asset) değişmez ve kopyası oluşturulmaz.
    [RequireComponent(typeof(GhostActor))]
    public class EchoVisual : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;
        [Tooltip("Belirme ve dağılma süresi (sn).")]
        [SerializeField] private float materializeDuration = 0.6f;
        [SerializeField] private float vanishDuration = 0.5f;
        [Tooltip("Eylemden bu kadar saniye önce parlamaya başlar.")]
        [SerializeField] private float telegraphLead = 0.6f;
        [Tooltip("Eylem anındaki parlama ve sönme hızı.")]
        [SerializeField] private float actionFlash = 1.6f;
        [SerializeField] private float flashFadeSpeed = 4f;
        [SerializeField] private ProceduralSound appearSound;
        [SerializeField] private SoundSet vanishSound;

        [Header("Yürüyüş")]
        [SerializeField] private MeshFilter bodyMesh;
        [Tooltip("Adım pozları. Yürüdükçe sırayla değişir: yumuşak geçiş yok, PS1 tarzı kesik yürüyüş.")]
        [SerializeField] private Mesh[] stepPoses;
        [Tooltip("Elinde eşya varken kullanılan adım pozları: kollar önde.")]
        [SerializeField] private Mesh[] carryPoses;
        [Tooltip("Bir poz kaç metre yürüyüşte değişir. Yankı durunca poz da durur.")]
        [SerializeField] private float stepLength = 0.7f;

        private static readonly int MaterializeId = Shader.PropertyToID("_Materialize");
        private static readonly int GlowId = Shader.PropertyToID("_Glow");
        private static readonly int BaseHeightId = Shader.PropertyToID("_BaseHeight");

        private GhostActor actor;
        private MaterialPropertyBlock block;
        private float materialize;
        private float flash;
        private bool vanishing;
        private Vector3 lastPosition;
        private float walked;

        private void Awake()
        {
            actor = GetComponent<GhostActor>();
            block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            actor.ActionPerformed += HandleAction;
            actor.Finished += HandleFinished;
            materialize = 0f;
            vanishing = false;
            lastPosition = transform.position;
            walked = 0f;
            if (appearSound != null)
            {
                appearSound.Play();
            }
        }

        private void OnDisable()
        {
            actor.ActionPerformed -= HandleAction;
            actor.Finished -= HandleFinished;
        }

        private void HandleAction()
        {
            flash = actionFlash;
        }

        private void HandleFinished()
        {
            vanishing = true;
            Sfx.Play(vanishSound, transform.position);
        }

        private void Update()
        {
            if (vanishing)
            {
                materialize -= Time.deltaTime / vanishDuration;
                if (materialize <= 0f)
                {
                    gameObject.SetActive(false);
                    return;
                }
            }
            else
            {
                materialize = Mathf.Min(1f, materialize + Time.deltaTime / materializeDuration);
            }

            UpdateStep();

            // Eyleme yaklaştıkça 0'dan 1'e çıkan uyarı; eylem anında ayrıca parlama.
            float lead = actor.TimeUntilNextAction;
            float warning = lead <= telegraphLead ? 1f - Mathf.Max(0f, lead) / telegraphLead : 0f;
            flash = Mathf.Max(0f, flash - flashFadeSpeed * Time.deltaTime);

            block.SetFloat(MaterializeId, materialize);
            block.SetFloat(GlowId, Mathf.Max(warning * 0.8f, flash));
            block.SetFloat(BaseHeightId, transform.position.y);
            foreach (Renderer target in renderers)
            {
                target.SetPropertyBlock(block);
            }
        }

        // Yürünen yatay mesafeye göre poz seçilir.
        private void UpdateStep()
        {
            Vector3 moved = transform.position - lastPosition;
            moved.y = 0f;
            walked += moved.magnitude;
            lastPosition = transform.position;

            Mesh[] poses = actor.Hand.HeldItem != null ? carryPoses : stepPoses;
            if (poses.Length > 0)
            {
                bodyMesh.sharedMesh = poses[Mathf.FloorToInt(walked / stepLength) % poses.Length];
            }
        }
    }
}
