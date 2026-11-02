using System.Collections.Generic;
using Game.Chamber;
using Game.Echo;
using UnityEngine;

namespace Game.Narrative
{
    // Karakterin kendi düşünceleri: ölüp yeniden başlayınca ve yankısını görünce.
    // Listeler sıralıdır: n. ölümde n. düşünce, yankının ilk görüldüğü her odada sıradaki düşünce.
    // Liste bitince susar; karakter alıştıkça daha az konuşur.
    public class PlayerThoughts : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [Tooltip("n. ölümden sonra oda yeniden başlarken n. düşünce.")]
        [SerializeField] private AnnouncementDefinition[] deathThoughts;
        [Tooltip("Yankının ilk görüldüğü her odada sıradaki düşünce.")]
        [SerializeField] private AnnouncementDefinition[] echoThoughts;
        [Tooltip("Gaz ilk kez püskürdüğünde, bir kez.")]
        [SerializeField] private AnnouncementDefinition gasThought;

        [Header("Yankıyı görme")]
        [SerializeField] private float sightDistance = 16f;
        [Tooltip("Yankı ekran kenarına bu kadar yakınsa görülmüş sayılmaz (0-0.5).")]
        [SerializeField] private float screenMargin = 0.15f;
        [Tooltip("Görülmüş sayılması için yankının ekranda kalma süresi (sn).")]
        [SerializeField] private float lookTime = 0.5f;
        [SerializeField] private float chestHeight = 1.1f;
        [SerializeField] private LayerMask occluders = ~0;

        private readonly HashSet<ChamberDefinition> echoRooms = new();
        private readonly RaycastHit[] hits = new RaycastHit[8];
        private ChamberController chamber;
        private int deaths;
        private int echoesSeen;
        private bool returningFromDeath;
        private bool gasSeen;
        private float looking;

        private void OnEnable()
        {
            ActiveChamber.Changed += HandleChamberChanged;
            GasRelease.Released += HandleGasReleased;
        }

        private void OnDisable()
        {
            ActiveChamber.Changed -= HandleChamberChanged;
            GasRelease.Released -= HandleGasReleased;
            HandleChamberChanged(null);
        }

        // Ölümden sonra oda yeniden yüklenince aktif oda değişir; düşünce, odanın anonslarından önce sıraya girer.
        private void HandleChamberChanged(ChamberController next)
        {
            if (chamber != null)
            {
                chamber.StateChanged -= HandleStateChanged;
            }

            chamber = next;
            looking = 0f;
            if (chamber == null)
            {
                return;
            }

            chamber.StateChanged += HandleStateChanged;
            if (returningFromDeath)
            {
                returningFromDeath = false;
                Say(deathThoughts, deaths - 1);
            }
        }

        private void HandleGasReleased()
        {
            if (!gasSeen)
            {
                gasSeen = true;
                Announcer.Play(gasThought);
            }
        }

        private void HandleStateChanged(ChamberState previous, ChamberState next)
        {
            if (next == ChamberState.Dead)
            {
                deaths++;
                returningFromDeath = true;
            }
        }

        private void Update()
        {
            if (chamber == null || chamber.State != ChamberState.Running || echoesSeen >= echoThoughts.Length
                || echoRooms.Contains(chamber.Definition))
            {
                return;
            }

            looking = EchoInSight() ? looking + Time.deltaTime : 0f;
            if (looking >= lookTime)
            {
                echoRooms.Add(chamber.Definition);
                Say(echoThoughts, echoesSeen++);
            }
        }

        private bool EchoInSight()
        {
            foreach (GhostActor ghost in GhostActor.Active)
            {
                if (!ghost.isActiveAndEnabled)
                {
                    continue;
                }

                Vector3 chest = ghost.transform.position + Vector3.up * chestHeight;
                Vector3 point = view.WorldToViewportPoint(chest);
                bool onScreen = point.z > 0f && point.z < sightDistance
                    && point.x > screenMargin && point.x < 1f - screenMargin
                    && point.y > screenMargin && point.y < 1f - screenMargin;
                if (onScreen && !Blocked(view.transform.position, chest))
                {
                    return true;
                }
            }

            return false;
        }

        // Saydam yüzeyler (Room02'deki cam) görüşü kesmez.
        private bool Blocked(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            int count = Physics.RaycastNonAlloc(from, direction.normalized, hits, direction.magnitude, occluders,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Renderer surface = hits[i].collider.GetComponent<Renderer>();
                bool transparent = surface != null && surface.sharedMaterial != null
                    && surface.sharedMaterial.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent;
                if (!transparent)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Say(AnnouncementDefinition[] thoughts, int index)
        {
            if (index >= 0 && index < thoughts.Length)
            {
                Announcer.Play(thoughts[index]);
            }
        }
    }
}
