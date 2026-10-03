using System.Collections.Generic;
using RealityDirector.Capture;
using RealityDirector.Core;

namespace RealityDirector.Meta
{
    public static class MontageCut
    {
        // Доля соседних пар, у которых совпал тег или настроение. 0–100.
        public static int Coherence(IReadOnlyList<FootageClip> cut)
        {
            // Связность — про соседние кадры. Одного ролика не с чем сравнивать.
            if (cut == null || cut.Count < 2)
                return 0;
            int links = 0;
            int pairs = cut.Count - 1;
            for (int i = 0; i < pairs; i++)
            {
                if (Shares(cut[i], cut[i + 1]))
                    links++;
            }

            return UnityEngine.Mathf.RoundToInt(100f * links / pairs);
        }

        public static bool Shares(FootageClip a, FootageClip b)
        {
            if (a == null || b == null || !a.Framed || !b.Framed)
                return false;
            if (a.tags != null && b.tags != null)
            {
                for (int i = 0; i < a.tags.Count; i++)
                {
                    string tag = a.tags[i];
                    if (tag == MomentTags.Sponsor)
                        continue;
                    if (b.tags.Contains(tag))
                        return true;
                }
            }

            return a.mood == b.mood;
        }

        public static string SharedTag(FootageClip a, FootageClip b)
        {
            if (!Shares(a, b))
                return null;
            if (a.tags != null && b.tags != null)
            {
                for (int i = 0; i < a.tags.Count; i++)
                {
                    string tag = a.tags[i];
                    if (tag == MomentTags.Sponsor)
                        continue;
                    if (b.tags.Contains(tag))
                        return tag;
                }
            }

            return a.mood.ToString();
        }

        // Давление, не правильный ответ.
        public static string Boss(int coherence, int cutCount, int libraryCount)
        {
            if (cutCount <= 0)
                return "В эфир пустоту? Я такое подписывал. Потом спрашивали, куда делся сезон.";
            if (cutCount == 1)
                return "Один кадр. Соседних нет — связность не из чего считать. Добавь второй.";
            if (libraryCount > cutCount && coherence >= 70)
                return "Связно. Даже слишком. Зритель любит, когда его не уважают. Ладно, пусть так.";
            if (coherence >= 70)
                return "Смотрится как история. Мне такое не по вкусу, но циферки, наверное, вырастут.";
            if (coherence >= 40)
                return "Середина. Не позор и не премия. Кивну и сделаю вид, что так и задумано.";
            return "Куски из разных сериалов. Зритель тупой, но не настолько. Или настолько. Жми.";
        }
    }
}
