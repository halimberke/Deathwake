using Game.Echo;
using Game.Player;
using UnityEngine;

namespace Game.Chamber
{
    // Gözetleme tareti. Kurallar:
    // 1) İlk gördüğü kişiye kilitlenir (oyuncu ya da yankı; tesis yankıları da insan sayar).
    //    Kilitliyken başka kimseyi görmez.
    // 2) İzlediği kişiyi gördüğü sürece ateş eder. Oyuncu hasar alır (tek atışta ölmez);
    //    yankıya ateş etmek bir şey değiştirmez, kurşun içinden geçer.
    // 3) Kurşun geçirmez cam görüşü kesmez ama kurşunu keser.
    // 4) İzlediğini gözden kaybederse kısa bir süre bekler, sonra yeniden arar.
    public class SecurityTurret : MonoBehaviour
    {
        private enum Mode { Scanning, Firing, Searching }

        [SerializeField] private ChamberController chamber;

        [Header("Parçalar")]
        [Tooltip("Dönen kafa. İleri yönü (mavi ok) namlu yönüdür.")]
        [SerializeField] private Transform head;
        [Tooltip("Görüşün, lazerin ve atışın çıktığı nokta.")]
        [SerializeField] private Transform eye;
        [SerializeField] private LineRenderer laser;
        [SerializeField] private Light spot;

        [Header("Davranış")]
        [Tooltip("Gözden kaybettikten sonra kilidi bırakmadan önce bekleme (sn).")]
        [SerializeField] private float relockDelay = 1f;
        [Tooltip("İki atış arası süre (sn).")]
        [SerializeField] private float fireInterval = 0.33f;
        [Tooltip("Oyuncuya isabet eden her atışın hasarı.")]
        [SerializeField] private float damagePerShot = 12f;
        [Tooltip("Kafa dönüş hızı (derece/sn).")]
        [SerializeField] private float turnSpeed = 220f;
        [Tooltip("Ararken sağa-sola tarama açısı (toplam, derece).")]
        [SerializeField] private float scanArc = 100f;
        [Tooltip("Tarama hızı (tam gidiş-dönüş/sn).")]
        [SerializeField] private float scanFrequency = 0.15f;
        [Tooltip("Görüşü kesen katmanlar. Oyuncunun katmanı hariç tutulmalı (oyuncu kendini örtmesin).")]
        [SerializeField] private LayerMask occluderMask = ~0;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private string deathReason = "Taret seni vurdu";

        [Header("Hedef noktaları (ayaktan yükseklik)")]
        [SerializeField] private float chestHeight = 1.1f;
        [SerializeField] private float headHeight = 1.6f;

        [Header("Görsel")]
        [SerializeField] private Color scanColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color firingColor = new Color(1f, 0.15f, 0.1f, 1f);
        [SerializeField] private Color searchingColor = new Color(1f, 0.85f, 0.2f, 0.9f);
        [SerializeField] private float shotFlashDuration = 0.08f;
        [SerializeField] private float laserWidth = 0.02f;
        [SerializeField] private float shotWidth = 0.07f;

        private Transform player;
        private PlayerHealth playerHealth;
        private Transform target;          // Kilitli hedefin kökü (oyuncu ya da yankı)
        private GhostActor targetGhost;    // Hedef yankıysa
        private Vector3 lastSeenPoint;
        private Vector3 shotEnd;           // Son atışın bittiği yer (hedef ya da cam)
        private float lostTimer;
        private float fireTimer;
        private float flashTimer;
        private Quaternion restRotation;
        private Mode mode = Mode.Scanning;

        private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

        private void Awake()
        {
            restRotation = head.localRotation;
            laser.positionCount = 2;
        }

        private void OnEnable()
        {
            chamber.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            chamber.StateChanged -= HandleStateChanged;
        }

        // Oyuncu Core sahnesinde; deneme başında bir kez bulunur (Update içinde arama yok).
        private void HandleStateChanged(ChamberState previous, ChamberState next)
        {
            if (next == ChamberState.Running && player == null)
            {
                GameObject found = GameObject.FindWithTag(playerTag);
                if (found != null)
                {
                    player = found.transform;
                    playerHealth = found.GetComponent<PlayerHealth>();
                }
            }

            if (next != ChamberState.Running)
            {
                Unlock();
            }
        }

        private void Update()
        {
            flashTimer -= Time.deltaTime;

            if (chamber.State != ChamberState.Running)
            {
                laser.enabled = false;
                spot.enabled = false;
                return;
            }

            bool seeing = UpdateLock();
            UpdateFire(seeing);
            UpdateAim();
            UpdateVisuals();
        }

        // ---------- Kilit ----------

        // Kilitli hedef şu an görülüyorsa true.
        private bool UpdateLock()
        {
            if (target != null)
            {
                bool alive = targetGhost == null || targetGhost.isActiveAndEnabled;
                if (alive && CanSee(target, targetGhost, out Vector3 seenPoint))
                {
                    lostTimer = 0f;
                    lastSeenPoint = seenPoint;
                    mode = Mode.Firing;
                    return true;
                }

                lostTimer += Time.deltaTime;
                mode = Mode.Searching;
                if (!alive || lostTimer >= relockDelay)
                {
                    Unlock();
                }
            }

            if (target == null)
            {
                return TryAcquire();
            }

            return false;
        }

        // Aynı karede birden fazla kişi görünürse yankı önce gelir (sonuç her denemede aynı olsun).
        private bool TryAcquire()
        {
            foreach (GhostActor ghost in GhostActor.Active)
            {
                if (CanSee(ghost.transform, ghost, out Vector3 point))
                {
                    Lock(ghost.transform, ghost, point);
                    return true;
                }
            }

            if (player != null && CanSee(player, null, out Vector3 playerPoint))
            {
                Lock(player, null, playerPoint);
                return true;
            }

            return false;
        }

        private void Lock(Transform newTarget, GhostActor ghost, Vector3 point)
        {
            target = newTarget;
            targetGhost = ghost;
            lastSeenPoint = point;
            lostTimer = 0f;
            // İlk atış hemen değil, bir aralık sonra: kilitlenme ile ateş arasında kısa bir uyarı.
            fireTimer = 0f;
            mode = Mode.Firing;
        }

        private void Unlock()
        {
            target = null;
            targetGhost = null;
            lostTimer = 0f;
            fireTimer = 0f;
            mode = Mode.Scanning;
        }

        // Göz ile hedefin göğsü ya da başı arasında (cam hariç) engel yoksa görülür.
        private bool CanSee(Transform root, GhostActor ghost, out Vector3 seenPoint)
        {
            Vector3 chest = root.position + Vector3.up * chestHeight;
            Vector3 headPoint = ghost != null ? ghost.Eye.position : root.position + Vector3.up * headHeight;

            if (TraceTo(chest, out _) != Trace.Blocked)
            {
                seenPoint = chest;
                return true;
            }

            if (TraceTo(headPoint, out _) != Trace.Blocked)
            {
                seenPoint = headPoint;
                return true;
            }

            seenPoint = default;
            return false;
        }

        private enum Trace { Clear, Glass, Blocked }

        // Gözden noktaya ışın: arada opak bir şey varsa Blocked, sadece cam varsa Glass (en yakın camın noktası).
        // Yankının collider'ı yok, oyuncunun katmanı maskede yok: ikisi de kendini örtmez.
        private Trace TraceTo(Vector3 point, out Vector3 glassPoint)
        {
            Vector3 from = eye.position;
            Vector3 toPoint = point - from;
            float distance = toPoint.magnitude - 0.05f;
            int count = Physics.RaycastNonAlloc(from, toPoint / toPoint.magnitude, hitBuffer, distance,
                occluderMask, QueryTriggerInteraction.Ignore);

            Trace result = Trace.Clear;
            float nearestGlass = float.MaxValue;
            glassPoint = default;

            for (int i = 0; i < count; i++)
            {
                if (hitBuffer[i].collider.TryGetComponent(out BulletproofGlass _))
                {
                    if (hitBuffer[i].distance < nearestGlass)
                    {
                        nearestGlass = hitBuffer[i].distance;
                        glassPoint = hitBuffer[i].point;
                    }
                    result = Trace.Glass;
                }
                else
                {
                    return Trace.Blocked;
                }
            }

            return result;
        }

        // ---------- Ateş ----------

        private void UpdateFire(bool seeing)
        {
            if (!seeing)
            {
                fireTimer = 0f;
                return;
            }

            fireTimer += Time.deltaTime;
            if (fireTimer < fireInterval)
            {
                return;
            }

            fireTimer = 0f;
            flashTimer = shotFlashDuration;

            // Cam kurşunu durdurur. Yankıya isabet bir şey değiştirmez.
            if (TraceTo(lastSeenPoint, out Vector3 glassPoint) == Trace.Glass)
            {
                shotEnd = glassPoint;
                return;
            }

            if (targetGhost != null)
            {
                // Kurşun yankının içinden geçip arkadaki yüzeye çarpar: yankıya hasar işlemediği görünsün.
                // (Yankı canı bittiği için değil, kaydı bittiği için kaybolur.)
                Vector3 direction = (lastSeenPoint - eye.position).normalized;
                shotEnd = Physics.Raycast(eye.position, direction, out RaycastHit wall, 100f, occluderMask, QueryTriggerInteraction.Ignore)
                    ? wall.point
                    : eye.position + direction * 100f;
                return;
            }

            shotEnd = lastSeenPoint;
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damagePerShot, deathReason);
            }
        }

        // ---------- Nişan ve görsel ----------

        private void UpdateAim()
        {
            Quaternion desired;
            if (target != null)
            {
                desired = Quaternion.LookRotation(lastSeenPoint - head.position, Vector3.up);
            }
            else
            {
                // Ararken sağa-sola tarar.
                float angle = Mathf.Sin(Time.time * scanFrequency * Mathf.PI * 2f) * scanArc * 0.5f;
                desired = head.parent.rotation * restRotation * Quaternion.Euler(0f, angle, 0f);
            }

            head.rotation = Quaternion.RotateTowards(head.rotation, desired, turnSpeed * Time.deltaTime);
        }

        private void UpdateVisuals()
        {
            Color color = mode switch
            {
                Mode.Firing => firingColor,
                Mode.Searching => searchingColor,
                _ => scanColor
            };

            bool flashing = flashTimer > 0f;
            Vector3 end = flashing ? shotEnd
                : target != null ? lastSeenPoint
                : eye.position + eye.forward * 30f;

            laser.enabled = true;
            laser.SetPosition(0, eye.position);
            laser.SetPosition(1, end);
            laser.startColor = laser.endColor = color;
            laser.startWidth = laser.endWidth = flashing ? shotWidth : laserWidth;

            spot.enabled = true;
            spot.color = color;
        }
    }
}
