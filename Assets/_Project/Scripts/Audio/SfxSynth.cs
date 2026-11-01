using UnityEngine;

namespace Game.Audio
{
    public enum SfxType
    {
        PhoneRing, // Ev telefonu zili (döngü)
        Knock,     // Kapıya üç vuruş
        TvStatic,  // Televizyon cızırtısı (döngü)
        Thud,      // Tek, ağır, tok ses
        Heartbeat, // Kalp atışı (döngü)
        EchoAppear, // Yankı belirirken: yükselen, cızırtılı bir süpürme
        Tinnitus,   // Ölüm anı: kulak çınlaması
        UiHover,    // Arayüz: seçim değişti (çok kısa, ince)
        UiClick,    // Arayüz: onay (iki tonlu)
        Drone,      // Ana menü: alçak tesis uğultusu (döngü)
        GasHiss,    // Gaz nozulları: basınçlı tıslama (döngü)
        Rain,       // Pencerede yağmur (döngü)
        Thunder,    // Uzaktan gök gürültüsü
        WaterRun,   // Musluktan akan su (döngü)
        Gulp,       // Yutkunma
        Click       // Lamba düğmesi
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
                SfxType.EchoAppear => EchoAppear(),
                SfxType.Tinnitus => Tinnitus(),
                SfxType.UiHover => UiBlip(1800f, 0.028f),
                SfxType.UiClick => UiClick(),
                SfxType.Drone => Drone(),
                SfxType.GasHiss => GasHiss(),
                SfxType.Rain => Rain(),
                SfxType.Thunder => Thunder(),
                SfxType.WaterRun => WaterRun(),
                SfxType.Gulp => Gulp(),
                SfxType.Click => Click(),
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

        // Frekansı 150 Hz'den 1200 Hz'e süzülen bir ton + giderek artan cızırtı. Başta ve sonda yumuşak.
        private static float[] EchoAppear()
        {
            var data = new float[Samples(0.8f)];
            var random = new System.Random(11);
            float phase = 0f;
            for (int n = 0; n < data.Length; n++)
            {
                float t = n / (float)data.Length;
                phase += Mathf.Lerp(150f, 1200f, t * t) / SampleRate;
                float tone = Mathf.Sin(2f * Mathf.PI * phase);
                float noise = ((float)random.NextDouble() * 2f - 1f) * t;
                float envelope = Mathf.Sin(Mathf.PI * t);
                data[n] = (tone * 0.6f + noise * 0.4f) * envelope;
            }

            return data;
        }

        // 3.8 kHz'lik ince bir ton: aniden başlar, yavaşça söner. Başında boğuk bir darbe.
        private static float[] Tinnitus()
        {
            var data = new float[Samples(3f)];
            for (int n = 0; n < data.Length; n++)
            {
                float t = n / (float)SampleRate;
                float ring = Mathf.Sin(2f * Mathf.PI * 3800f * t) * Mathf.Min(1f, t / 0.05f) * Mathf.Exp(-t * 0.8f) * 0.35f;
                float hit = Mathf.Sin(2f * Mathf.PI * 50f * t) * Mathf.Exp(-t * 10f);
                data[n] = ring + hit;
            }

            return data;
        }

        private static float[] UiBlip(float frequency, float duration)
        {
            var data = new float[Samples(duration)];
            AddTone(data, 0f, frequency, 3f / duration, 1f);
            return data;
        }

        private static float[] UiClick()
        {
            var data = new float[Samples(0.09f)];
            AddTone(data, 0f, 980f, 90f, 1f);
            AddTone(data, 0.035f, 1470f, 80f, 0.8f);
            return data;
        }

        // Frekanslar 8 saniyeye tam sayıda sığar: döngü tıkırtısız birleşir. Yavaş vuruş iki yakın tondan gelir.
        private static float[] Drone()
        {
            const float length = 8f;
            var data = new float[Samples(length)];
            for (int n = 0; n < data.Length; n++)
            {
                float t = n / (float)data.Length * length;
                float swell = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * t / length);
                data[n] = swell * (Mathf.Sin(2f * Mathf.PI * 55f * t)
                                   + 0.7f * Mathf.Sin(2f * Mathf.PI * 55.5f * t)
                                   + 0.3f * Mathf.Sin(2f * Mathf.PI * 110.25f * t)
                                   + 0.12f * Mathf.Sin(2f * Mathf.PI * 330f * t));
            }

            return data;
        }

        // Beyaz gürültüden fark alınarak tiz bırakılır: basınçlı gaz sesi. Gürültü olduğu için döngü fark edilmez.
        private static float[] GasHiss()
        {
            var data = new float[Samples(3f)];
            var random = new System.Random(13);
            float last = 0f;
            for (int n = 0; n < data.Length; n++)
            {
                float noise = (float)random.NextDouble() * 2f - 1f;
                data[n] = noise - last * 0.7f;
                last = noise;
            }

            return data;
        }

        // Yumuşak uğultu + cama vuran rastgele damlalar. Baş ve son birbirine karıştırılır: döngü duyulmaz.
        private static float[] Rain()
        {
            const float length = 8f;
            const float blend = 1f;
            var data = new float[Samples(length + blend)];
            var random = new System.Random(17);
            float low = 0f;
            float band = 0f;
            for (int n = 0; n < data.Length; n++)
            {
                float noise = (float)random.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.12f;
                band += (noise - band) * 0.45f;
                data[n] = low * 0.9f + (band - low) * 0.22f;
            }

            for (int i = 0; i < 900; i++)
            {
                AddDrop(data, (float)random.NextDouble() * (length + blend), 900f + (float)random.NextDouble() * 2200f,
                    0.05f + (float)random.NextDouble() * 0.14f);
            }

            return Crossfade(data, Samples(blend));
        }

        // Tek çatırtı değil, uzakta yuvarlanan alçak gürültü.
        private static float[] Thunder()
        {
            var data = new float[Samples(5f)];
            var random = new System.Random(19);
            float low = 0f;
            for (int n = 0; n < data.Length; n++)
            {
                float t = n / (float)SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.012f;
                float roll = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * (1.1f + 0.5f * Mathf.Sin(t * 1.7f)) * t);
                data[n] = low * roll * Mathf.Min(1f, t / 0.25f) * Mathf.Exp(-t * 0.95f);
            }

            return data;
        }

        private static float[] WaterRun()
        {
            const float length = 3f;
            const float blend = 0.5f;
            var data = new float[Samples(length + blend)];
            var random = new System.Random(23);
            float low = 0f;
            for (int n = 0; n < data.Length; n++)
            {
                float noise = (float)random.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.3f;
                data[n] = (noise - low) * 0.22f + low * 0.5f;
            }

            for (int i = 0; i < 260; i++)
            {
                AddDrop(data, (float)random.NextDouble() * (length + blend), 500f + (float)random.NextDouble() * 900f, 0.2f);
            }

            return Crossfade(data, Samples(blend));
        }

        // Alçalıp yükselen boğuk bir ton.
        private static float[] Gulp()
        {
            var data = new float[Samples(0.28f)];
            float phase = 0f;
            for (int n = 0; n < data.Length; n++)
            {
                float t = n / (float)data.Length;
                phase += Mathf.Lerp(150f, 320f, Mathf.Sin(Mathf.PI * t)) / SampleRate;
                data[n] = Mathf.Sin(2f * Mathf.PI * phase) * Mathf.Sin(Mathf.PI * t);
            }

            return data;
        }

        private static float[] Click()
        {
            var data = new float[Samples(0.07f)];
            AddImpact(data, 0f, 1400f, 180f, 260f, new System.Random(29));
            return data;
        }

        // Kısa, hızla sönen yüksek ton: tek damla.
        private static void AddDrop(float[] data, float start, float frequency, float gain)
        {
            int offset = Samples(start);
            int length = Samples(0.04f);
            for (int n = 0; n < length && offset + n < data.Length; n++)
            {
                float t = n / (float)SampleRate;
                data[offset + n] += Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * 170f) * gain;
            }
        }

        // Son parçayı başa karıştırır, klip o kadar kısalır. Döngü noktasında tık sesi kalmaz.
        private static float[] Crossfade(float[] data, int overlap)
        {
            var result = new float[data.Length - overlap];
            System.Array.Copy(data, result, result.Length);
            for (int n = 0; n < overlap; n++)
            {
                float t = n / (float)overlap;
                result[n] = data[n] * Mathf.Sqrt(t) + data[result.Length + n] * Mathf.Sqrt(1f - t);
            }

            return result;
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
