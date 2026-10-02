using UnityEngine;

namespace RealityDirector.Core
{
    public enum ShowMood
    {
        Drama,
        Trash,
        Family
    }

    public static class MoodStyle
    {
        public static Color ColorOf(ShowMood mood)
        {
            switch (mood)
            {
                case ShowMood.Drama: return new Color(0.49f, 0.78f, 1f, 1f);
                case ShowMood.Trash: return new Color(0.96f, 0.34f, 0.28f, 1f);
                default: return new Color(0.42f, 0.82f, 0.48f, 1f);
            }
        }

        public static string Hex(ShowMood mood)
        {
            Color c = ColorOf(mood);
            return "#" + ColorUtility.ToHtmlStringRGB(c);
        }

        public static string Short(ShowMood mood)
        {
            switch (mood)
            {
                case ShowMood.Drama: return "драма";
                case ShowMood.Trash: return "трэш";
                default: return "семья";
            }
        }

        public static string Full(ShowMood mood)
        {
            switch (mood)
            {
                case ShowMood.Drama: return "драматичность";
                case ShowMood.Trash: return "трэшовость";
                default: return "семейность";
            }
        }

        public static string Paint(string word, ShowMood mood)
        {
            return "<color=" + Hex(mood) + ">" + word + "</color>";
        }
    }
}
