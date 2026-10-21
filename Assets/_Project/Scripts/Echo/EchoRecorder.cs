using Game.Chamber;
using Game.Interaction;
using UnityEngine;

namespace Game.Echo
{
    // Oyuncunun denemesini kaydeder: düzenli aralıkla duruş, ayrıca etkileşim olayları.
    // Deneme ölümle bitince kayıt odanın ilerlemesine eklenir ve sonraki denemede yankı olur.
    // Hangi odanın kaydedileceğini ActiveChamber'dan öğrenir; oda değişince ona geçer.
    public class EchoRecorder : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [Tooltip("Konum ve sağa-sola dönüş buradan okunur. Genelde Player.")]
        [SerializeField] private Transform body;
        [Tooltip("Yukarı-aşağı bakış buradan okunur.")]
        [SerializeField] private Transform cameraPivot;
        [Tooltip("Saniyedeki örnek sayısı.")]
        [SerializeField] private float sampleRate = 30f;
        [SerializeField] private bool logSummary = true;

        private ChamberController chamber;
        private GhostRecording current;
        private float nextSampleTime;

        private void OnEnable()
        {
            ActiveChamber.Changed += Bind;
            Bind(ActiveChamber.Current);
            interactor.Interacted += HandleInteracted;
            interactor.Dropped += HandleDropped;
            interactor.HoldStarted += HandleHoldStarted;
            interactor.HoldEnded += HandleHoldEnded;
        }

        private void OnDisable()
        {
            ActiveChamber.Changed -= Bind;
            Bind(null);
            interactor.Interacted -= HandleInteracted;
            interactor.Dropped -= HandleDropped;
            interactor.HoldStarted -= HandleHoldStarted;
            interactor.HoldEnded -= HandleHoldEnded;
        }

        private void Bind(ChamberController next)
        {
            if (chamber != null)
            {
                chamber.StateChanged -= HandleStateChanged;
            }

            chamber = next;
            current = null;

            if (chamber != null)
            {
                chamber.StateChanged += HandleStateChanged;
            }
        }

        // Hareket Update'te yapıldığı için duruş LateUpdate'te, karenin son hâliyle okunur.
        private void LateUpdate()
        {
            if (current == null || chamber == null)
            {
                return;
            }

            float time = chamber.Clock.Time;
            if (time >= nextSampleTime)
            {
                Sample(time);
                nextSampleTime = time + 1f / sampleRate;
            }
        }

        private void HandleStateChanged(ChamberState previous, ChamberState next)
        {
            switch (next)
            {
                case ChamberState.Running:
                    current = new GhostRecording();
                    nextSampleTime = 0f;
                    break;

                case ChamberState.Dead:
                    FinishRecording();
                    break;

                case ChamberState.Solved:
                    current = null;
                    break;
            }
        }

        private void FinishRecording()
        {
            if (current == null)
            {
                return;
            }

            float time = chamber.Clock.Time;
            Sample(time);
            current.Finish(time, chamber.LastDeathReason);
            chamber.Progress.AddRecording(current, chamber.Definition.MaxEchoes);

            if (logSummary)
            {
                Debug.Log($"Kayıt bitti: {current.Samples.Count} örnek, {current.Events.Count} olay, {time:0.0} sn ({current.EndReason}).", this);
            }

            current = null;
        }

        private void HandleInteracted(IInteractable target)
        {
            AddTargetEvent(EchoEventType.Interact, target as Component);
        }

        private void HandleHoldStarted(IHoldable target)
        {
            AddTargetEvent(EchoEventType.HoldStart, target as Component);
        }

        private void HandleDropped()
        {
            AddEvent(EchoEventType.Drop, null);
        }

        private void HandleHoldEnded()
        {
            AddEvent(EchoEventType.HoldEnd, null);
        }

        private void AddTargetEvent(EchoEventType type, Component target)
        {
            if (current == null)
            {
                return;
            }

            InteractableId id = target != null ? target.GetComponent<InteractableId>() : null;
            if (id == null)
            {
                Debug.LogWarning($"{target} nesnesinde InteractableId yok; yankı bu eylemi tekrar edemez.", target);
                return;
            }

            AddEvent(type, id.Id);
        }

        private void AddEvent(EchoEventType type, string targetId)
        {
            if (current != null)
            {
                current.AddEvent(new EchoEvent(chamber.Clock.Time, type, targetId));
            }
        }

        private void Sample(float time)
        {
            // localEulerAngles 0-360 arası döner; DeltaAngle ile -180..180 aralığına çevrilir.
            float pitch = Mathf.DeltaAngle(0f, cameraPivot.localEulerAngles.x);
            current.AddSample(new PoseSample(time, body.position, body.eulerAngles.y, pitch));
        }
    }
}
