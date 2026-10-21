using System.Collections.Generic;
using Game.Echo;

namespace Game.Chamber
{
    // Bir odada sahne yeniden yüklense de korunan bilgiler: deneme sayısı ve yankı kayıtları.
    public class ChamberProgress
    {
        private readonly List<GhostRecording> recordings = new();

        public int AttemptCount { get; private set; }

        // En eskiden en yeniye.
        public IReadOnlyList<GhostRecording> Recordings => recordings;

        public void BeginAttempt()
        {
            AttemptCount++;
        }

        // Sınır aşılınca en eski kayıt silinir.
        public void AddRecording(GhostRecording recording, int maxEchoes)
        {
            recordings.Add(recording);

            while (recordings.Count > maxEchoes && recordings.Count > 0)
            {
                recordings.RemoveAt(0);
            }
        }
    }
}
