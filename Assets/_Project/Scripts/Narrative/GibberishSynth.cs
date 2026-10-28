using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    // Seslendirilmiş bir satır: ses ve altyazının hangi anda kaç harfinin görüneceği.
    public sealed class SpokenLine
    {
        public AudioClip Clip { get; }
        private readonly float[] wordStartTimes;
        private readonly int[] wordEndChars;

        public SpokenLine(AudioClip clip, float[] wordStartTimes, int[] wordEndChars)
        {
            Clip = clip;
            this.wordStartTimes = wordStartTimes;
            this.wordEndChars = wordEndChars;
        }

        // Verilen anda söylenmeye başlanmış kelimeler görünür.
        public int VisibleCharactersAt(float time)
        {
            int visible = 0;
            for (int i = 0; i < wordStartTimes.Length && wordStartTimes[i] <= time; i++)
            {
                visible = wordEndChars[i];
            }

            return visible;
        }
    }

    // Anlaşılmaz konuşma üretir (Papers Please, Animal Crossing tarzı).
    // Yöntem: metindeki her ünlü için bir hece. Hecenin kaynağı testere dişi bir dalga (ses telleri),
    // iki "formant" filtresi bu dalgayı o ünlüye benzetir (ağız şekli). Kelimenin kendisi rastgele
    // sayı tohumu olduğu için aynı kelime her zaman aynı duyulur.
    public static class GibberishSynth
    {
        private const int SampleRate = 22050;

        // Ünlülerin ilk iki formant frekansı (Hz). Kabaca Türkçe ünlüler.
        private static readonly Dictionary<char, Vector2> formants = new()
        {
            { 'a', new Vector2(750f, 1250f) }, { 'â', new Vector2(750f, 1250f) },
            { 'e', new Vector2(500f, 1850f) },
            { 'ı', new Vector2(420f, 1450f) },
            { 'i', new Vector2(300f, 2250f) }, { 'î', new Vector2(300f, 2250f) },
            { 'o', new Vector2(520f, 900f) },
            { 'ö', new Vector2(430f, 1550f) },
            { 'u', new Vector2(330f, 850f) }, { 'û', new Vector2(330f, 850f) },
            { 'ü', new Vector2(300f, 1700f) },
        };

        private static readonly Vector2 fallbackFormant = new(600f, 1400f);

        public static SpokenLine Speak(string text, SpeakerVoice voice)
        {
            var samples = new List<float>(SampleRate * 4);
            var starts = new List<float>();
            var ends = new List<int>();

            AppendSilence(samples, 0.08f);

            int i = 0;
            while (i < text.Length)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    i++;
                    continue;
                }

                int wordStart = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i]))
                {
                    i++;
                }

                string word = text.Substring(wordStart, i - wordStart);
                starts.Add(samples.Count / (float)SampleRate);
                ends.Add(i);
                AppendWord(samples, word, voice);
            }

            AppendSilence(samples, 0.12f);

            float[] data = samples.ToArray();
            if (voice.Radio)
            {
                ApplyRadio(data, voice.Seed);
            }

            Normalize(data, voice.Volume);

            AudioClip clip = AudioClip.Create("Speech", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return new SpokenLine(clip, starts.ToArray(), ends.ToArray());
        }

        // Anons öncesi iki tonlu zil (ding-dong).
        public static AudioClip Chime(SpeakerVoice voice)
        {
            var samples = new List<float>();
            AppendTone(samples, 659.3f, 0.5f);
            AppendTone(samples, 523.3f, 0.8f);
            AppendSilence(samples, 0.15f);

            float[] data = samples.ToArray();
            if (voice.Radio)
            {
                ApplyRadio(data, voice.Seed + 1);
            }

            Normalize(data, voice.Volume * 0.8f);

            AudioClip clip = AudioClip.Create("Chime", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void AppendWord(List<float> samples, string word, SpeakerVoice voice)
        {
            string lower = word.ToLowerInvariant();
            var random = new System.Random(StableHash(lower) ^ voice.Seed);

            // Kelimedeki ünlüler sırayla hece olur. Ünlüsüz kelime (rakam, kısaltma) tek hece.
            var vowels = new List<Vector2>();
            foreach (char c in lower)
            {
                if (formants.TryGetValue(c, out Vector2 f))
                {
                    vowels.Add(f);
                }
            }

            if (vowels.Count == 0)
            {
                vowels.Add(fallbackFormant);
            }

            foreach (Vector2 vowel in vowels)
            {
                float duration = voice.SyllableDuration * Mathf.Lerp(0.75f, 1.25f, (float)random.NextDouble());
                float semitones = ((float)random.NextDouble() * 2f - 1f) * voice.PitchVariance;
                float pitch = voice.BasePitch * Mathf.Pow(2f, semitones / 12f);
                bool consonant = random.NextDouble() < 0.6;
                AppendSyllable(samples, duration, pitch, vowel * voice.FormantShift, consonant, random);
                AppendSilence(samples, 0.015f);
            }

            // Noktalama nefes payı bırakır.
            char last = word[word.Length - 1];
            float pause = last switch
            {
                ',' or ';' or ':' => 0.18f,
                '.' or '!' or '?' => 0.32f,
                _ => 0.06f
            };
            AppendSilence(samples, pause);
        }

        private static void AppendSyllable(List<float> samples, float duration, float pitch, Vector2 formant,
            bool consonant, System.Random random)
        {
            int count = Mathf.RoundToInt(duration * SampleRate);
            int consonantLength = consonant ? Mathf.RoundToInt(0.018f * SampleRate) : 0;
            var f1 = Biquad.BandPass(formant.x, 5f);
            var f2 = Biquad.BandPass(formant.y, 8f);
            var hiss = Biquad.BandPass(3500f, 1f);
            float phase = 0f;

            for (int n = 0; n < count; n++)
            {
                float t = n / (float)count;
                // Hece sonunda perde hafifçe düşer: konuşmaya doğal bir iniş verir.
                float freq = pitch * Mathf.Lerp(1.05f, 0.95f, t);
                phase += freq / SampleRate;
                phase -= Mathf.Floor(phase);
                float source = phase * 2f - 1f;

                float voiced = f1.Process(source) + 0.6f * f2.Process(source);
                float envelope = Mathf.Min(1f, n / (0.012f * SampleRate)) * Mathf.Min(1f, (count - n) / (0.04f * SampleRate));
                float sample = voiced * envelope;

                // Ünsüz: hecenin başında kısa bir gürültü patlaması (s, t, k gibi).
                if (n < consonantLength)
                {
                    float noise = (float)random.NextDouble() * 2f - 1f;
                    sample += hiss.Process(noise) * 0.8f * (1f - n / (float)consonantLength);
                }

                samples.Add(sample);
            }
        }

        private static void AppendTone(List<float> samples, float frequency, float duration)
        {
            int count = Mathf.RoundToInt(duration * SampleRate);
            for (int n = 0; n < count; n++)
            {
                float time = n / (float)SampleRate;
                float decay = Mathf.Exp(-time * 4f) * Mathf.Min(1f, n / (0.005f * SampleRate));
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * time) + 0.3f * Mathf.Sin(4f * Mathf.PI * frequency * time);
                samples.Add(wave * decay);
            }
        }

        private static void AppendSilence(List<float> samples, float duration)
        {
            int count = Mathf.RoundToInt(duration * SampleRate);
            for (int n = 0; n < count; n++)
            {
                samples.Add(0f);
            }
        }

        // Hoparlör sesi: alçak ve yüksek frekanslar kesilir, hafif bozulur, hışırtı ve cızırtı eklenir.
        private static void ApplyRadio(float[] data, int seed)
        {
            var highPass = Biquad.HighPass(320f);
            var lowPass = Biquad.LowPass(3200f);
            var random = new System.Random(seed);
            const float drive = 2.5f;
            float driveNorm = 1f / (float)System.Math.Tanh(drive);

            for (int n = 0; n < data.Length; n++)
            {
                float x = lowPass.Process(highPass.Process(data[n]));
                x = (float)System.Math.Tanh(x * drive) * driveNorm;
                x += ((float)random.NextDouble() * 2f - 1f) * 0.012f;
                if (random.NextDouble() < 0.0006)
                {
                    x += ((float)random.NextDouble() * 2f - 1f) * 0.25f;
                }

                data[n] = x;
            }
        }

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

            float gain = peak / max;
            for (int n = 0; n < data.Length; n++)
            {
                data[n] *= gain;
            }
        }

        // string.GetHashCode her platformda aynı olmak zorunda değil; kendi sabit özetimiz (FNV-1a).
        private static int StableHash(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in text)
                {
                    hash = (hash ^ c) * 16777619;
                }

                return (int)hash;
            }
        }

        // İkinci dereceden dijital filtre (RBJ "Audio EQ Cookbook" formülleri).
        private struct Biquad
        {
            private float b0, b1, b2, a1, a2;
            private float x1, x2, y1, y2;

            public static Biquad BandPass(float frequency, float q)
            {
                Coefficients(frequency, q, out float alpha, out float cos);
                return Make(alpha, 0f, -alpha, 1f + alpha, -2f * cos, 1f - alpha);
            }

            public static Biquad LowPass(float frequency)
            {
                Coefficients(frequency, 0.707f, out float alpha, out float cos);
                return Make((1f - cos) / 2f, 1f - cos, (1f - cos) / 2f, 1f + alpha, -2f * cos, 1f - alpha);
            }

            public static Biquad HighPass(float frequency)
            {
                Coefficients(frequency, 0.707f, out float alpha, out float cos);
                return Make((1f + cos) / 2f, -(1f + cos), (1f + cos) / 2f, 1f + alpha, -2f * cos, 1f - alpha);
            }

            public float Process(float x)
            {
                float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1;
                x1 = x;
                y2 = y1;
                y1 = y;
                return y;
            }

            private static void Coefficients(float frequency, float q, out float alpha, out float cos)
            {
                float w0 = 2f * Mathf.PI * Mathf.Min(frequency, SampleRate * 0.45f) / SampleRate;
                alpha = Mathf.Sin(w0) / (2f * q);
                cos = Mathf.Cos(w0);
            }

            private static Biquad Make(float b0, float b1, float b2, float a0, float a1, float a2)
            {
                return new Biquad
                {
                    b0 = b0 / a0, b1 = b1 / a0, b2 = b2 / a0,
                    a1 = a1 / a0, a2 = a2 / a0
                };
            }
        }
    }
}
