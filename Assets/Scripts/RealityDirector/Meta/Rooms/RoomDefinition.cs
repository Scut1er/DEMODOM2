using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Тип комнаты выпуска. Значения сериализуются числом — не переставлять.
    public enum RoomType
    {
        Situation,
        Event,
        Marketing,
        Montage
    }

    // Комната карты выпуска. Конкретные типы — наследники (Situation/Event/Marketing/Montage).
    // Создавать: ПКМ в Project → Create → RealityDirector → Rooms. Класть в Resources/Content/Rooms.
    public abstract class RoomDefinition : ContentDefinition
    {
        [Header("Как выглядит на карте")]
        public string title;
        public string subtitle;
        [TextArea(2, 4)] public string description;
        [TextArea(1, 3)] public string goal;
        [Tooltip("Иконка, если нет арта.")]
        public MapNodeKind icon;
        public Color color = new Color(0.3f, 0.28f, 0.34f, 1f);
        public Sprite art;

        [Header("Когда попадает на карту")]
        [Tooltip("Вес при случайном выборе среди комнат этого типа. 0 — только если комнату указали напрямую (например, старт выпуска).")]
        [Min(0f)] public float weight = 1f;
        [Tooltip("Комната попадает на карту, только если все условия выполнены (номер выпуска, каст, прокачка, флаги сезона). Перед входом проверяются ещё раз — так флаги из прошлых комнат могут её закрыть.")]
        public List<Condition> conditions = new List<Condition>();
        [Tooltip("Не встречается дважды на одном пути выпуска.")]
        public bool uniquePerEpisode = true;

        [Header("При выборе")]
        [Tooltip("Эффекты сразу при входе в комнату (бюджет, тон, флаги...).")]
        public List<Effect> onEnter = new List<Effect>();

        public abstract RoomType Type { get; }
    }
}
