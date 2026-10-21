using System.Collections.Generic;

namespace Game.Echo
{
    // Bir denemenin tam kaydı. Yankı bunu oda saatine göre oynatır.
    public class GhostRecording
    {
        private readonly List<PoseSample> samples = new();
        private readonly List<EchoEvent> events = new();

        public IReadOnlyList<PoseSample> Samples => samples;
        public IReadOnlyList<EchoEvent> Events => events;

        // Kaydın bittiği oda saati (ölüm anı).
        public float Duration { get; private set; }
        public string EndReason { get; private set; }

        public void AddSample(PoseSample sample)
        {
            samples.Add(sample);
        }

        public void AddEvent(EchoEvent echoEvent)
        {
            events.Add(echoEvent);
        }

        public void Finish(float duration, string endReason)
        {
            Duration = duration;
            EndReason = endReason;
        }
    }
}
