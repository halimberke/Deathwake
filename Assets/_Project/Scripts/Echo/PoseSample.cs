using UnityEngine;

namespace Game.Echo
{
    // Kayıttaki tek bir an: oda saatine göre zaman, gövde konumu, gövde açısı (yaw), bakış açısı (pitch).
    public readonly struct PoseSample
    {
        public readonly float Time;
        public readonly Vector3 Position;
        public readonly float Yaw;
        public readonly float Pitch;

        public PoseSample(float time, Vector3 position, float yaw, float pitch)
        {
            Time = time;
            Position = position;
            Yaw = yaw;
            Pitch = pitch;
        }
    }
}
