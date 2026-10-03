using RealityDirector.Events;
using UnityEngine;

namespace RealityDirector.UI
{
    // Внешний вид карты по её данным: рамка и иконка по категории (CardVisualConfig), арт по карте.
    // Один источник для руки на съёмке, колоды и магазина хаба, полёта карты и окон дизайнера.
    public static class CardVisuals
    {
        public const string ConfigPath = "Content/CardVisuals";
        public const string ArtFolder = "Art/UI/Cards/Art/";
        public const string FramesFolder = "Art/UI/Cards/Frames/";
        public const string ElementsFolder = "Art/UI/Cards/Elements/";
        public const string IconsFolder = "Art/UI/Cards/Icons/";

        // Пропорции рамки художницы (1188×2048).
        public const float Aspect = 1188f / 2048f;

        static CardVisualConfig _config;
        static CardVisualConfig _defaults;

        public static CardVisualConfig Config
        {
            get
            {
                if (_config == null)
                    _config = Resources.Load<CardVisualConfig>(ConfigPath);
                return _config != null ? _config : Defaults;
            }
        }

        // Раскладка пака, если ассета нет: красная — провокация и хаос, зелёная — общение и контроль, синяя — остальное.
        public static CardVisualConfig Defaults
        {
            get
            {
                if (_defaults != null)
                    return _defaults;
                _defaults = ScriptableObject.CreateInstance<CardVisualConfig>();
                _defaults.hideFlags = HideFlags.HideAndDontSave;
                Add("Provocation", "Red", "Icon_Provocation", new Color(1f, 0.32f, 0.3f));
                Add("Comedy", "Red", "Icon_Chaos", new Color(1f, 0.5f, 0.25f));
                Add("Social", "Green", "Icon_Social", new Color(0.42f, 0.92f, 0.55f));
                Add("Control", "Green", "Icon_Control", new Color(0.42f, 0.92f, 0.75f));
                Add("DeckManagement", "Blue", "Icon_Deck", new Color(0.5f, 0.68f, 1f));
                Add("Reveal", "Blue", "Icon_Reveal", new Color(0.72f, 0.55f, 1f));
                Add("Confession", "Blue", "Icon_Reveal", new Color(0.72f, 0.55f, 1f));
                Add("Environment", "Blue", "Icon_Environment", new Color(0.45f, 0.8f, 1f));
                Add("Sponsor", "Blue", "Icon_Sponsor", new Color(0.98f, 0.78f, 0.36f));
                _defaults.fallback = Style("*", "Blue", "Icon_Card", new Color(0.8f, 0.75f, 0.7f));
                return _defaults;
            }
        }

        static void Add(string category, string frame, string icon, Color accent)
        {
            _defaults.categories.Add(Style(category, frame, icon, accent));
        }

        static CardVisualConfig.CategoryStyle Style(string category, string frame, string icon, Color accent)
        {
            return new CardVisualConfig.CategoryStyle
            {
                category = category,
                frame = UiKit.Load(FramesFolder + "Frame_" + frame + "_Artist"),
                icon = UiKit.Load(IconsFolder + icon),
                accent = accent
            };
        }

        public static CardVisualConfig.CategoryStyle StyleOf(string category)
        {
            var style = Config.Find(category);
            if (style == null || style.frame == null)
                style = Defaults.Find(category);
            return style;
        }

        public static Sprite Frame(string category) => StyleOf(category)?.frame;
        public static Sprite CategoryIcon(string category) => StyleOf(category)?.icon;
        public static Color Accent(string category) => StyleOf(category)?.accent ?? Color.white;

        // Арт карты: поле карты → файл по id → нет (карта покажет иконку категории).
        public static Sprite Art(EventDefinition def)
        {
            if (def == null)
                return null;
            return def.cardArt != null ? def.cardArt : ArtById(def.id);
        }

        public static Sprite ArtById(string id) => string.IsNullOrEmpty(id) ? null : UiKit.Load(ArtFolder + id);

        public static Sprite Element(string name) => UiKit.Load(ElementsFolder + name);

        public static Sprite Die(DieSize die)
        {
            switch (die)
            {
                case DieSize.D4: return UiKit.Load(IconsFolder + "Dice_d4");
                case DieSize.D8: return UiKit.Load(IconsFolder + "Dice_d8");
                case DieSize.D10: return UiKit.Load(IconsFolder + "Dice_d10");
                case DieSize.D12: return UiKit.Load(IconsFolder + "Dice_d12");
                default: return UiKit.Load(IconsFolder + "Dice_d6");
            }
        }

        // Текст на карте: описание из таблицы; нет (или там та же подсказка «клик по…») — что делает по эффектам;
        // нет и его — как играть.
        public static string Text(EventDefinition def)
        {
            if (def == null)
                return "";
            if (!string.IsNullOrEmpty(def.description) && !Same(def.description, def.hint))
                return def.description;
            var what = Cards.CardBrief.What(def);
            if (what.Count > 0)
                return string.Join("\n", what);
            return def.hint ?? "";
        }

        static bool Same(string a, string b)
        {
            return string.Equals((a ?? "").Trim(), (b ?? "").Trim(), System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
