using System.Collections.Generic;
using Game.Chamber;
using Game.Interaction;
using UnityEngine;

namespace Game.Echo
{
    // Bir kaydı oda saatine göre oynatan yankı. Oyuncu gibi IInteractor'dır:
    // kapı, düğme, eşya onu oyuncudan ayırt etmez.
    public class GhostActor : MonoBehaviour, IInteractor
    {
        [Tooltip("Yukarı-aşağı bakış buna uygulanır. HandSlot'un tutma noktası bunun altında olmalı.")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private HandSlot hand;
        [SerializeField] private bool logSkippedSteps = true;

        private GhostRecording recording;
        private ChamberClock clock;
        private int sampleCursor;
        private int eventCursor;
        private bool finished;
        private IHoldable activeHold;

        public HandSlot Hand => hand;

        public void Init(GhostRecording recording, ChamberClock clock)
        {
            this.recording = recording;
            this.clock = clock;
            sampleCursor = 0;
            eventCursor = 0;
            finished = false;

            // İlk karede başlangıç noktasında görünsün.
            ApplyPose(0f);
        }

        private void Update()
        {
            if (recording == null || finished)
            {
                return;
            }

            float time = clock.Time;

            // Önce duruş, sonra olaylar: bırakma gibi eylemler güncel konumu kullanır.
            ApplyPose(time);
            PlayEventsUpTo(time);

            if (time >= recording.Duration)
            {
                Finish();
            }
        }

        // İki kayıt örneği arasında yumuşak geçiş (interpolasyon).
        private void ApplyPose(float time)
        {
            IReadOnlyList<PoseSample> samples = recording.Samples;
            if (samples.Count == 0)
            {
                return;
            }

            // Örnekler zamana göre sıralı. İmleç sadece ileri gider, her karede baştan aranmaz.
            while (sampleCursor + 1 < samples.Count && samples[sampleCursor + 1].Time <= time)
            {
                sampleCursor++;
            }

            PoseSample a = samples[sampleCursor];
            PoseSample b = sampleCursor + 1 < samples.Count ? samples[sampleCursor + 1] : a;
            float span = b.Time - a.Time;
            float t = span > 0f ? Mathf.Clamp01((time - a.Time) / span) : 0f;

            transform.position = Vector3.Lerp(a.Position, b.Position, t);
            // LerpAngle 359° → 1° geçişini 2° olarak alır, ters yönden dönmez.
            transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(a.Yaw, b.Yaw, t), 0f);
            cameraPivot.localRotation = Quaternion.Euler(Mathf.LerpAngle(a.Pitch, b.Pitch, t), 0f, 0f);
        }

        private void PlayEventsUpTo(float time)
        {
            IReadOnlyList<EchoEvent> events = recording.Events;

            while (eventCursor < events.Count && events[eventCursor].Time <= time)
            {
                Execute(events[eventCursor]);
                eventCursor++;
            }
        }

        private void Execute(EchoEvent echoEvent)
        {
            switch (echoEvent.Type)
            {
                case EchoEventType.Interact:
                    ExecuteInteract(echoEvent);
                    break;

                case EchoEventType.Drop:
                    if (hand != null && hand.HeldItem != null)
                    {
                        hand.Drop();
                    }
                    else
                    {
                        LogSkipped(echoEvent, "elinde bırakacak eşya yok");
                    }
                    break;

                case EchoEventType.HoldStart:
                    ExecuteHoldStart(echoEvent);
                    break;

                case EchoEventType.HoldEnd:
                    ReleaseHold();
                    break;
            }
        }

        private void ExecuteHoldStart(EchoEvent echoEvent)
        {
            if (!InteractableRegistry.TryGet(echoEvent.TargetId, out InteractableId id))
            {
                LogSkipped(echoEvent, "hedef bulunamadı");
                return;
            }

            IHoldable target = id.GetComponent<IHoldable>();
            if (target == null || !target.CanHold(this))
            {
                LogSkipped(echoEvent, $"{id.name} tutulamıyor");
                return;
            }

            ReleaseHold();
            target.BeginHold(this);
            activeHold = target;
        }

        private void ReleaseHold()
        {
            if (activeHold == null)
            {
                return;
            }

            activeHold.EndHold(this);
            activeHold = null;
        }

        // Paradoks kuralı: yapılamayan adım atlanır, sonucu doğal olarak gelir.
        private void ExecuteInteract(EchoEvent echoEvent)
        {
            if (!InteractableRegistry.TryGet(echoEvent.TargetId, out InteractableId id))
            {
                LogSkipped(echoEvent, "hedef bulunamadı");
                return;
            }

            IInteractable target = id.GetComponent<IInteractable>();
            if (target == null || !target.CanInteract(this))
            {
                LogSkipped(echoEvent, $"{id.name} şu an kullanılamıyor");
                return;
            }

            target.Interact(this);
        }

        private void Finish()
        {
            finished = true;

            // Kayıt ölümle bitti: tuttuğu düğmeyi bırakır, elindeki eşya o noktada düşer.
            ReleaseHold();
            if (hand != null && hand.HeldItem != null)
            {
                hand.Drop();
            }

            gameObject.SetActive(false);
        }

        private void LogSkipped(EchoEvent echoEvent, string reason)
        {
            if (logSkippedSteps)
            {
                Debug.Log($"Yankı {echoEvent.Time:0.00} sn'deki adımı atladı: {reason}.", this);
            }
        }
    }
}
