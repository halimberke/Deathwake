using Game.Echo;
using UnityEngine;

namespace Game.Chamber
{
    // Gözetleme tareti. Kurallar:
    // 1) İlk gördüğü kişiye kilitlenir (oyuncu ya da yankı; tesis yankıları da insan sayar) ve onu izler.
    //    Kilitliyken başka kimseyi görmez.
    // 2) İzlediğini gözden kaybederse kısa bir süre bekler, sonra yeniden arar.
    // 3) İzlediği kişi yasak bölgeye girerse kısa bir uyarıdan sonra ateş eder. Oyuncuysa ölür;
    //    yankıysa mermi içinden geçer (yankı zaten o anda kaydını bitirip kaybolur).
    public class SecurityTurret : MonoBehaviour
    {
        private enum Mode { Scanning, Tracking, Alarm }

        [SerializeField] private ChamberController chamber;
        [SerializeField] private RestrictedZone zone;

        [Header("Parçalar")]
        [Tooltip("Dönen kafa. İleri yönü (mavi ok) namlu yönüdür.")]
        [SerializeField] private Transform head;
        [Tooltip("Görüşün ve lazerin çıktığı nokta.")]
        [SerializeField] private Transform eye;
        [SerializeField] private LineRenderer laser;
        [SerializeField] private Light spot;

        [Header("Davranış")]
        [Tooltip("Gözden kaybettikten sonra kilidi bırakmadan önce bekleme (sn).")]
        [SerializeField] private float relockDelay = 1f;
        [Tooltip("İzlenen kişi yasak bölgeye girdikten sonra ateşe kadar geçen süre (sn).")]
        [SerializeField] private float fireDelay = 0.5f;
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
        [SerializeField] private Color trackColor = new Color(1f, 0.85f, 0.2f, 0.9f);
        [SerializeField] private Color alarmColor = new Color(1f, 0.15f, 0.1f, 1f);
        [SerializeField] private float shotFlashDuration = 0.12f;
        [SerializeField] private float laserWidth = 0.02f;
        [SerializeField] private float shotWidth = 0.08f;

        private Transform player;
        private Transform target;          // Kilitli hedefin kökü (oyuncu ya da yankı)
        private GhostActor targetGhost;    // Hedef yankıysa
        private Vector3 lastSeenPoint;
        private float lostTimer;
        private float alarmTimer;
        private float flashTimer;
        private Quaternion restRotation;
        private Mode mode = Mode.Scanning;

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
                player = found != null ? found.transform : null;
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

            UpdateLock();
            UpdateAlarm();
            UpdateAim();
            UpdateVisuals();
        }

        // ---------- Kilit ----------

        private void UpdateLock()
        {
            if (target != null)
            {
                bool alive = targetGhost == null || targetGhost.isActiveAndEnabled;
                if (alive && CanSee(target, targetGhost, out Vector3 seenPoint))
                {
                    lostTimer = 0f;
                    lastSeenPoint = seenPoint;
                }
                else
                {
                    lostTimer += Time.deltaTime;
                    if (!alive || lostTimer >= relockDelay)
                    {
                        Unlock();
                    }
                }
            }

            if (target == null)
            {
                TryAcquire();
            }
        }

        // Aynı karede birden fazla kişi görünürse yankı önce gelir (sonuç her denemede aynı olsun).
        private void TryAcquire()
        {
            foreach (GhostActor ghost in GhostActor.Active)
            {
                if (CanSee(ghost.transform, ghost, out Vector3 point))
                {
                    Lock(ghost.transform, ghost, point);
                    return;
                }
            }

            if (player != null && CanSee(player, null, out Vector3 playerPoint))
            {
                Lock(player, null, playerPoint);
            }
        }

        private void Lock(Transform newTarget, GhostActor ghost, Vector3 point)
        {
            target = newTarget;
            targetGhost = ghost;
            lastSeenPoint = point;
            lostTimer = 0f;
            alarmTimer = 0f;
            mode = Mode.Tracking;
        }

        private void Unlock()
        {
            target = null;
            targetGhost = null;
            lostTimer = 0f;
            alarmTimer = 0f;
            mode = Mode.Scanning;
        }

        // Göz ile hedefin göğsü ya da başı arasında engel yoksa görülür.
        // Yankının collider'ı yok, oyuncunun katmanı maskede yok: ikisi de kendini örtmez.
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

        private bool HasLineOfSight(Vector3 point)
        {
            Vector3 from = eye.position;
            Vector3 toPoint = point - from;
            return !Physics.Raycast(from, toPoint.normalized, toPoint.magnitude - 0.05f,
                occluderMask, QueryTriggerInteraction.Ignore);
        }

        // ---------- Alarm ve ateş ----------

        private void UpdateAlarm()
        {
            bool targetInZone = target != null && lostTimer == 0f && zone.Contains(target.position);
            if (!targetInZone)
            {
                alarmTimer = 0f;
                if (target != null)
                {
                    mode = Mode.Tracking;
                }
                return;
            }

            mode = Mode.Alarm;
            alarmTimer += Time.deltaTime;
            if (alarmTimer < fireDelay)
            {
                return;
            }

            alarmTimer = 0f;
            flashTimer = shotFlashDuration;

            // Yankıya ateş etmek bir şey değiştirmez: mermi içinden geçer.
            if (targetGhost == null)
            {
                chamber.Kill(deathReason);
            }
        }

        // ---------- Nişan ve görsel ----------

        private void UpdateAim()
        {
            Quaternion desired;
            if (target != null)
            {
                Vector3 direction = lastSeenPoint - head.position;
                desired = Quaternion.LookRotation(direction, Vector3.up);
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
                Mode.Alarm => alarmColor,
                Mode.Tracking => trackColor,
                _ => scanColor
            };

            bool flashing = flashTimer > 0f;
            Vector3 end = target != null
                ? lastSeenPoint
                : eye.position + eye.forward * 30f;

            laser.enabled = true;
            laser.SetPosition(0, eye.position);
            laser.SetPosition(1, end);
            laser.startColor = laser.endColor = flashing ? alarmColor : color;
            laser.startWidth = laser.endWidth = flashing ? shotWidth : laserWidth;

            spot.enabled = true;
            spot.color = color;
        }
    }
}
