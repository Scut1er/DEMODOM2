using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.UI
{
    // Как выглядит карта: рамка художницы, иконка и цвет — по категории. Арт — у самой карты (поле «Арт»),
    // пусто — Art/UI/Cards/Art/<id>.png, нет и его — иконка категории на тёмном фоне.
    // Ассет: Resources/Content/CardVisuals. Нет ассета — встроенная раскладка (CardVisuals.Defaults).
    [CreateAssetMenu(menuName = "RealityDirector/Card Visuals", fileName = "CardVisuals")]
    public class CardVisualConfig : ScriptableObject
    {
        [Serializable]
        public class CategoryStyle
        {
            [Tooltip("Категория, как в поле category карты: Provocation, Social, Control, Environment, Reveal, Comedy, Sponsor, DeckManagement, Confession.")]
            public string category;
            [Tooltip("Рамка художницы (Art/UI/Cards/Frames). Рамки не перекрашиваются кодом.")]
            public Sprite frame;
            [Tooltip("Иконка категории над названием и на карте без своего арта.")]
            public Sprite icon;
            [Tooltip("Подпись категории, фон карты без арта, свечение выбранной карты.")]
            public Color accent = Color.white;
        }

        [Tooltip("Категории карт. Категории нет в списке — берётся «Остальные».")]
        public List<CategoryStyle> categories = new List<CategoryStyle>();

        [Tooltip("Для карт без категории или с категорией не из списка.")]
        public CategoryStyle fallback = new CategoryStyle { category = "*" };

        public CategoryStyle Find(string category)
        {
            if (!string.IsNullOrEmpty(category))
            {
                foreach (var style in categories)
                {
                    if (style != null && string.Equals(style.category, category, StringComparison.OrdinalIgnoreCase))
                        return style;
                }
            }

            return fallback;
        }
    }
}
