using Game.Audio;
using Game.Echo;
using Game.Localization;
using Game.Player;
using UnityEngine;

namespace Game.Chamber
{
    // Gözetleme tareti. Kurallar:
    // 1) İlk gördüğü kişiye kilitlenir (oyuncu ya da yankı; tesis yankıları da insan sayar).
    //    Kilitliyken başka kimseyi görmez.
    // 2) İzlediği kişiyi gördüğü sürece ateş eder. Oyuncu hasar alır (tek atışta ölmez);
    //    yankıya ateş etmek bir şey değiştirmez, kurşun içinden geçer.
    // 3) İzlediğini gözden kaybederse kısa bir süre bekler, sonra yeniden arar.
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

        [Header("Ses")]
        [SerializeField] private SoundSet shotSound;
        [SerializeField] private SoundSet lockSound;
        [SerializeField] private SoundSet lostSound;
        [Tooltip("Yankıya atılan kurşun arkadaki yüzeye çarpınca.")]
        [SerializeField] private SoundSet ricochetSound;
        [Tooltip("Kafanın motoru: döngü. Sesi ve perdesi dönüş hızına bağlıdır.")]
        [SerializeField] private AudioSource servo;
        [SerializeField] private float servoVolume = 0.16f;
        [Tooltip("Ses, hedef değere saniyede bu kadar yaklaşır.")]
        [SerializeField] private float servoResponse = 12f;

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
        private Vector3 shotEnd;           // Son atışın bittiği yer (hedef ya da arkadaki duvar)
        private float lostTimer;
        private float fireTimer;
        private float flashTimer;
        private Quaternion restRotation;
        private Mode mode = Mode.Scanning;
        private Quaternion lastHeadRotation;
        private float servoAmount;

        private void Awake()
        {
            restRotation = head.localRotation;
            lastHeadRotation = head.rotation;
            laser.positionCount = 2;
            servo.volume = 0f;
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
                UpdateServo(false);
                return;
            }

            bool seeing = UpdateLock();
            UpdateFire(seeing);
            UpdateAim();
            UpdateVisuals();
            UpdateServo(true);
        }

        // Motor sesi kafanın o anki dönüş hızını izler: tararken hafif, hedefe dönerken yüksek ve tiz.
        private void UpdateServo(bool running)
        {
            float turned = Quaternion.Angle(lastHeadRotation, head.rotation);
            lastHeadRotation = head.rotation;
            float rate = running && Time.deltaTime > 0f ? Mathf.Clamp01(turned / Time.deltaTime / turnSpeed) : 0f;
            float target = running ? Mathf.Sqrt(rate) : 0f;
            servoAmount = Mathf.MoveTowards(servoAmount, target, servoResponse * Time.deltaTime);
            servo.volume = servoAmount * servoVolume;
            servo.pitch = Mathf.Lerp(0.8f, 1.2f, servoAmount);
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
            Sfx.Play(lockSound, eye.position);
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
            if (target != null && chamber.State == ChamberState.Running)
            {
                Sfx.Play(lostSound, eye.position);
            }

            target = null;
            targetGhost = null;
            lostTimer = 0f;
            fireTimer = 0f;
            mode = Mode.Scanning;
        }

        // Göz ile hedefin göğsü ya da başı arasında engel yoksa görülür.
        private bool CanSee(Transform root, GhostActor ghost, out Vector3 seenPoint)
        {
            Vector3 chest = root.position + Vector3.up * chestHeight;
            Vector3 headPoint = ghost != null ? ghost.Eye.position : root.position + Vector3.up * headHeight;

            if (HasLineOfSight(chest))
            {
                seenPoint = chest;
                return true;
            }

            if (HasLineOfSight(headPoint))
            {
                seenPoint = headPoint;
                return true;
            }

            seenPoint = default;
            return false;
        }

        // Yankının collider'ı yok, oyuncunun katmanı maskede yok: ikisi de kendini örtmez.
        private bool HasLineOfSight(Vector3 point)
        {
            Vector3 from = eye.position;
            Vector3 toPoint = point - from;
            return !Physics.Raycast(from, toPoint.normalized, toPoint.magnitude - 0.05f,
                occluderMask, QueryTriggerInteraction.Ignore);
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
            Sfx.Play(shotSound, eye.position);

            if (targetGhost != null)
            {
                // Kurşun yankının içinden geçip arkadaki yüzeye çarpar: yankıya hasar işlemediği görünsün.
                // (Yankı canı bittiği için değil, kaydı bittiği için kaybolur.)
                Vector3 direction = (lastSeenPoint - eye.position).normalized;
                shotEnd = Physics.Raycast(eye.position, direction, out RaycastHit wall, 100f, occluderMask, QueryTriggerInteraction.Ignore)
                    ? wall.point
                    : eye.position + direction * 100f;
                Sfx.Play(ricochetSound, shotEnd);
                return;
            }

            shotEnd = lastSeenPoint;
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damagePerShot, Loc.T(deathReason));
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
