using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Localization
{
    // Oyundaki bütün yazıların tek çeviri noktası. Türkçe metin anahtardır: sahnede ve veride Türkçe yazılır,
    // ekrana basılırken Loc.T ile seçili dile çevrilir. Çevirisi olmayan metin Türkçe kalır, oyun bozulmaz.
    // Tablo: Resources/Localization/Strings.txt, sekmeyle ayrılmış sütunlar. İlk satır dil kodları,
    // ikinci satır dil adları. Yeni dil = yeni sütun, kod değişmez. Metin içinde alt satır \n diye yazılır.
    public static class Loc
    {
        private const string TablePath = "Localization/Strings";

        // Seçili dil değişince. Ekrandaki sabit yazılar (LocalizedText) bunu dinler.
        public static event Action Changed;

        private static readonly List<string> codes = new();
        private static readonly List<string> names = new();
        private static readonly Dictionary<string, string[]> rows = new();
#if UNITY_EDITOR
        private static readonly HashSet<string> warned = new();
#endif
        private static int column;
        private static bool loaded;

        public static IReadOnlyList<string> Languages
        {
            get
            {
                EnsureLoaded();
                return codes;
            }
        }

        public static string Current
        {
            get
            {
                EnsureLoaded();
                return codes[column];
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Changed = null;
            loaded = false;
            column = 0;
        }

        public static string NameOf(string code)
        {
            EnsureLoaded();
            int index = codes.IndexOf(code);
            return index >= 0 ? names[index] : code;
        }

        public static void SetLanguage(string code)
        {
            EnsureLoaded();
            int index = codes.IndexOf(code);
            if (index < 0 || index == column)
            {
                return;
            }

            column = index;
            Changed?.Invoke();
        }

        public static string T(string source)
        {
            EnsureLoaded();
            if (column == 0 || string.IsNullOrEmpty(source))
            {
                return source;
            }

            if (rows.TryGetValue(source, out string[] row) && column < row.Length && row[column].Length > 0)
            {
                return row[column];
            }

            WarnMissing(source);
            return source;
        }

        public static string Format(string source, params object[] args)
        {
            return string.Format(T(source), args);
        }

        // Türkçede i → İ, ı → I. Sistem kültürü değişse de sonuç aynı kalsın diye elle yapılır.
        public static string Upper(string text)
        {
            EnsureLoaded();
            return column == 0
                ? text.Replace('i', 'İ').Replace('ı', 'I').ToUpperInvariant()
                : text.ToUpperInvariant();
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            codes.Clear();
            names.Clear();
            rows.Clear();

            var table = Resources.Load<TextAsset>(TablePath);
            if (table != null)
            {
                Parse(table.text);
            }

            if (codes.Count == 0)
            {
                Debug.LogError($"Çeviri tablosu okunamadı: Resources/{TablePath}");
                codes.Add("tr");
                names.Add("Türkçe");
            }
        }

        private static void Parse(string text)
        {
            int line = 0;
            foreach (string raw in text.Split('\n'))
            {
                string entry = raw.TrimEnd('\r');
                if (entry.Length == 0 || entry[0] == '#')
                {
                    continue;
                }

                string[] cells = entry.Split('\t');
                for (int i = 0; i < cells.Length; i++)
                {
                    cells[i] = cells[i].Replace("\\n", "\n");
                }

                if (line == 0)
                {
                    codes.AddRange(cells);
                }
                else if (line == 1)
                {
                    names.AddRange(cells);
                }
                else
                {
                    rows[cells[0]] = cells;
                }

                line++;
            }
        }

        private static void WarnMissing(string source)
        {
#if UNITY_EDITOR
            if (warned.Add(source))
            {
                Debug.LogWarning($"Çeviri yok ({codes[column]}): {source}");
            }
#endif
        }
    }
}
