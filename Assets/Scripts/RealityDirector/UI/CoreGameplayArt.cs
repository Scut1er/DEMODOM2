using RealityDirector.Events;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI
{
    // Спрайты OWM Core Gameplay pack: Assets/Resources/Art/UI/CoreGameplay.
    public static class CoreGameplayArt
    {
        const string Root = "Art/UI/CoreGameplay/";

        public static Sprite Sprite(string path)
        {
            return string.IsNullOrEmpty(path) ? null : Resources.Load<Sprite>(Root + path);
        }

        public static bool Slice(Image image, string path)
        {
            var sprite = Sprite(path);
            if (image == null || sprite == null)
                return false;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            return true;
        }

        public static bool Paint(Image image, string path, bool aspect = true)
        {
            var sprite = Sprite(path);
            if (image == null || sprite == null)
                return false;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = aspect;
            image.color = Color.white;
            return true;
        }

        public static string Die(DieSize die)
        {
            switch (die)
            {
                case DieSize.D4: return "UI/Dice/d4";
                case DieSize.D8: return "UI/Dice/d8";
                case DieSize.D10: return "UI/Dice/d10";
                case DieSize.D12: return "UI/Dice/d12";
                default: return "UI/Dice/d6";
            }
        }
    }
}
