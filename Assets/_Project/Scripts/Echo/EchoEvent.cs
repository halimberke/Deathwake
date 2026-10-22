using UnityEngine;

namespace Game.Echo
{
    // Kayıttaki bir eylem. Örnek: 3.2 sn'de kimliği "a1b2..." olan kapıyla etkileşti.
    // Yerleştirme (Drop) olaylarında eşyanın konduğu poz da saklanır; yankı tam oraya koyar.
    public readonly struct EchoEvent
    {
        public readonly float Time;
        public readonly EchoEventType Type;
        public readonly string TargetId; // Drop ve HoldEnd için null.
        public readonly Vector3 Position; // Sadece Drop.
        public readonly float Yaw;        // Sadece Drop.

        public EchoEvent(float time, EchoEventType type, string targetId)
            : this(time, type, targetId, Vector3.zero, 0f)
        {
        }

        public EchoEvent(float time, EchoEventType type, string targetId, Vector3 position, float yaw)
        {
            Time = time;
            Type = type;
            TargetId = targetId;
            Position = position;
            Yaw = yaw;
        }
    }
}
