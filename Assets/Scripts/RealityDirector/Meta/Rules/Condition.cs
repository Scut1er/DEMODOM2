using System;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Что проверяет условие. Новые значения — только в конец (сериализуются числом).
    public enum ConditionType
    {
        EpisodeAtLeast,   // value — номер выпуска (с 1)
        EpisodeAtMost,    // value — номер выпуска (с 1)
        BudgetAtLeast,    // value — кредиты
        CastAtLeast,      // value — участников в выпуске
        CastAtMost,       // value — участников в выпуске
        CastHasActor,     // key — id участника
        EpisodeFlag,      // key — флаг выпуска
        SeasonFlag,       // key — флаг сезона
        NarrativeTag,     // key — сюжетный тег выпуска
        RoomVisited,      // key — id комнаты (в этом выпуске)
        CrewLevelAtLeast, // key — Cast / Operators / Writers, value — уровень
        ContractActive,   // key — id спонсорского оффера
        ToneAtLeast,      // mood, value — очки тона сезона
        CashAtLeast       // value — нал выпуска
    }

    [Serializable]
    public class Condition
    {
        public ConditionType type;
        [Tooltip("Флаг, тег, id комнаты/участника/оффера или ветка команды (Cast / Operators / Writers) — зависит от типа.")]
        public string key;
        [Tooltip("Число для сравнения: номер выпуска, бюджет, размер каста, уровень, очки тона.")]
        public int value;
        [Tooltip("Только для ToneAtLeast.")]
        public ShowMood mood;
        [Tooltip("Инвертировать: условие выполнено, если проверка НЕ прошла (например, «флага нет»).")]
        public bool not;
        [Tooltip("Текст для игрока, если условие не выполнено. Пусто — сгенерируется.")]
        public string failText;
    }
}
