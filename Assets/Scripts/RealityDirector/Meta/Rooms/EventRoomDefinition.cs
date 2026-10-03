using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Роль в тексте события: {актёр} в тексте заменяется именем участника из каста выпуска.
    [Serializable]
    public class EventRole
    {
        [Tooltip("Слово в фигурных скобках в тексте: {актёр}, {жертва}...")]
        public string key = "актёр";
        [Tooltip("Конкретный участник (id). Пусто — случайный из каста, без повторов между ролями.")]
        public string actorId;
    }

    // Цвет карточки варианта на экране события: красный — риск и хаос, зелёный — надёжно, золотой — сделка/смешанное.
    public enum ChoiceAccent
    {
        [InspectorName("Авто (по шансу и цене)")] Auto,
        [InspectorName("Красный — риск, хаос")] Risky,
        [InspectorName("Зелёный — надёжно")] Safe,
        [InspectorName("Золотой — сделка, смешанное")] Neutral
    }

    // Вариант выбора (GDD §32, EventChoice).
    [Serializable]
    public class EventChoice
    {
        [Tooltip("Текст кнопки.")]
        public string label = "Вариант";
        [Tooltip("Пояснение под кнопкой (можно {роли}).")]
        [TextArea(1, 3)] public string description;
        [Tooltip("Цвет карточки варианта. Авто: шанс ниже 70% или вариант подливает масла (злость/вражда/стресс к следующей съёмке, трэш) — красный, есть цена или шанс ниже 100% — золотой, иначе зелёный.")]
        public ChoiceAccent accent;

        [Tooltip("Когда вариант доступен. Невыполненное условие закрывает кнопку и показывает причину.")]
        public List<Condition> conditions = new List<Condition>();
        [Tooltip("Недоступный вариант не показывать вовсе (по умолчанию он виден, но закрыт).")]
        public bool hideIfUnavailable;

        [Tooltip("Цена в ЕБ (бюджет сезона). Не хватает — вариант закрыт.")]
        [Min(0)] public int costMoney;
        [Tooltip("Цена в УЕ выпуска. Не хватает — вариант закрыт.")]
        [Min(0)] public int costCash;

        [Tooltip("Шанс успеха, %. 100 — всегда успех. Меньше — при провале срабатывают «Если провал».")]
        public int chance = 100;

        [Tooltip("Что происходит при выборе (при успехе).")]
        public List<Effect> effects = new List<Effect>();
        [Tooltip("Текст результата (можно {роли}).")]
        [TextArea(2, 5)] public string resultText;
        [Tooltip("Сюжетные теги выпуска после выбора — их проверяют условия следующих комнат и событий.")]
        public List<string> resultTags = new List<string>();

        [Tooltip("Эффекты при провале (если шанс меньше 100).")]
        public List<Effect> failEffects = new List<Effect>();
        [TextArea(2, 5)] public string failText;
        public List<string> failTags = new List<string>();

        [Tooltip("Для команды: что вариант должен делать по таблице дизайна (игроку не показывается). "
                 + "Игрок видит последствия, собранные из «Эффектов» автоматически.")]
        [TextArea(1, 4)] public string designNote;
    }

    // Текстовое событие (GDD §26, §32): иллюстрация, текст и 2–3 выбора с последствиями.
    // Не создаёт footage — зритель его не видел.
    [CreateAssetMenu(menuName = "RealityDirector/Rooms/Event", fileName = "Event_")]
    public class EventRoomDefinition : RoomDefinition
    {
        [Header("Событие")]
        [Tooltip("Текст события на экране (можно {роли}). Поле «Описание» выше — короткая подпись на карте выпуска.")]
        [TextArea(4, 12)] public string body;
        [Tooltip("Сюжетные теги, которые выпуск получает при входе в событие.")]
        public List<string> eventTags = new List<string>();
        [Tooltip("Роли для текста: {ключ} заменяется именем участника.")]
        public List<EventRole> roles = new List<EventRole>();
        [Tooltip("2–3 варианта. Если все закрыты, игроку покажут «Уйти».")]
        public List<EventChoice> choices = new List<EventChoice>();

        public override RoomType Type => RoomType.Event;
    }
}
