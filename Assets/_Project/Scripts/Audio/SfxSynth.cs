using UnityEngine;

namespace Game.Audio
{
    public enum SfxType
    {
        PhoneRing, // Ev telefonu zili (döngü)
        Knock,     // Kapıya üç vuruş
        TvStatic,  // Televizyon cızırtısı (döngü)
        Thud,      // Tek, ağır, tok ses
        Heartbeat  // Kalp atışı (döngü)
    }

    // Geçici efekt sesleri kodla üretilir. Ses adımında gerçek kayıtlarla değiştirilebilir.
    // Hepsi aynı yöntem: sinüs dalgaları ve gürültü, zarfla (envelope) şekillendirilir.
    public static class SfxSynth
    {
        private const int SampleRate = 22050;

        public static AudioClip Create(SfxType type)
        {
            float[] data = type switch
            {
                SfxType.PhoneRing => PhoneRing(),
                SfxType.Knock => Knock(),
                SfxType.TvStatic => TvStatic(),
                SfxType.Thud => Thud(),
                SfxType.Heartbeat => Heartbeat(),
                _ => new float[1]
            };

            Normalize(data, 0.8f);
            AudioClip clip = AudioClip.Create(type.ToString(), data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // İki ton (440 + 480 Hz), saniyede 20 kez kesilerek "zırr" sesi. 0.4 çal, 0.2 sus, 0.4 çal, 2 sus.
        private static float[] PhoneRing()
        {
            var data = new float[Samples(3f)];
            for (int n = 0; n < data.Length; n++)
            {
                float t = n / (float)SampleRate;
                bool ringing = t < 0.4f || (t >= 0.6f && t < 1f);
                if (!ringing)
                {
                    continue;
                }

                float tone = Mathf.Sin(2f * Mathf.PI * 440f * t) + Mathf.Sin(2f * Mathf.PI * 480f * t);
                float buzz = Mathf.Sin(2f * Mathf.PI * 20f * t) > 0f ? 1f : 0.3f;
                data[n] = tone * 0.5f * buzz;
            }

            return data;
        }

        // Tahta kapıya üç vuruş: kısa gürültü patlaması + alçak bir gövde tınısı.
        private static float[] Knock()
        {
            var data = new float[Samples(1.2f)];
            var random = new System.Random(3);
            foreach (float start in new[] { 0f, 0.28f, 0.56f })
            {
                AddImpact(data, start, 110f, 25f, 60f, random);
            }

            return data;
        }

        private static float[] TvStatic()
        {
            var data = new float[Samples(2f)];
            var random = new System.Random(5);
            float smooth = 0f;
            for (int n = 0; n < data.Length; n++)
            {
                float noise = (float)random.NextDouble() * 2f - 1f;
                smooth += (noise - smooth) * 0.6f; // hafif alçak geçiren: çok tiz olmasın
                data[n] = smooth * 0.5f;
            }

            return data;
        }

        private static float[] Thud()
        {
            var data = new float[Samples(1f)];
            AddImpact(data, 0f, 60f, 8f, 20f, new System.Random(9));
            return data;
        }

        // "Lub-dub": iki alçak darbe, sonra sessizlik.
        private static float[] Heartbeat()
        {
            var data = new float[Samples(0.9f)];
            AddTone(data, 0f, 55f, 18f, 1f);
            AddTone(data, 0.28f, 65f, 20f, 0.7f);
            return data;
        }

        // Darbe sesi: alçak geçirilmiş gürültü (çarpma) + sönümlenen sinüs (gövde).
        private static void AddImpact(float[] data, float start, float bodyFrequency, float bodyDecay,
            float noiseDecay, System.Random random)
        {
            int offset = Samples(start);
            float smooth = 0f;
            for (int n = 0; offset + n < data.Length; n++)
            {
                float t = n / (float)SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                smooth += (noise - smooth) * 0.15f;
                float body = Mathf.Sin(2f * Mathf.PI * bodyFrequency * t) * Mathf.Exp(-t * bodyDecay);
                data[offset + n] += smooth * 2f * Mathf.Exp(-t * noiseDecay) + body * 0.8f;
            }
        }

        private static void AddTone(float[] data, float start, float frequency, float decay, float gain)
        {
            int offset = Samples(start);
            for (int n = 0; offset + n < data.Length; n++)
            {
                float t = n / (float)SampleRate;
                data[offset + n] += Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * decay) * gain;
            }
        }

        private static int Samples(float seconds) => Mathf.RoundToInt(seconds * SampleRate);

        private static void Normalize(float[] data, float peak)
        {
            float max = 0f;
            foreach (float s in data)
            {
                max = Mathf.Max(max, Mathf.Abs(s));
            }

            if (max <= 0f)
            {
                return;
            }

            for (int n = 0; n < data.Length; n++)
            {
                data[n] *= peak / max;
            }
        }
    }
}
