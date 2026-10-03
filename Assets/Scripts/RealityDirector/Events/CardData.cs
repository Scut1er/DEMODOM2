using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Events
{
    // Данные карты продюсера по GDD (§15–§30). Значения enum сериализуются числом — новые только в конец.

    public enum CardStatus
    {
        [InspectorName("Черновик")] Draft,
        [InspectorName("На тесте")] Testing,
        [InspectorName("Готова")] Ready,
        [InspectorName("Выключена — нет в игре")] Disabled
    }

    public enum CardTier
    {
        [InspectorName("I")] I,
        [InspectorName("II")] II,
        [InspectorName("III")] III
    }

    public enum CardRarity
    {
        [InspectorName("Обычная")] Common,
        [InspectorName("Необычная")] Uncommon,
        [InspectorName("Редкая")] Rare,
        [InspectorName("Легендарная")] Legendary,
        [InspectorName("Спонсорская")] Sponsor
    }

    // Эмоции и SelfControl актёра (GDD §6).
    public enum ActorStat
    {
        [InspectorName("Стресс")] Stress,
        [InspectorName("Злость")] Anger,
        [InspectorName("Влечение")] Attraction,
        [InspectorName("Грусть")] Sadness,
        [InspectorName("Уверенность")] Confidence,
        [InspectorName("Самоконтроль")] SelfControl
    }

    public enum RelationshipAxis
    {
        [InspectorName("Доверие")] Trust,
        [InspectorName("Влечение")] Attraction,
        [InspectorName("Вражда")] Hostility
    }

    // ---------- Фильтры цели ----------

    public enum TargetFilterType
    {
        [InspectorName("Актёр: есть черта")] ActorHasTrait,
        [InspectorName("Актёр: нет черты")] ActorLacksTrait,
        [InspectorName("Актёр: эмоция не меньше")] ActorStatAtLeast,
        [InspectorName("Актёр: эмоция не больше")] ActorStatAtMost,
        [InspectorName("Актёр: есть состояние")] ActorHasState,
        [InspectorName("Актёр: нет состояния")] ActorLacksState,
        [InspectorName("Объект: есть тег")] ObjectHasTag,
        [InspectorName("Рядом с тегом окружения")] NearEnvironmentTag
    }

    [Serializable]
    public class TargetFilter
    {
        public TargetFilterType type;
        [Tooltip("Черта, состояние или тег — зависит от типа.")]
        public string key;
        public ActorStat stat;
        public int value;
    }

    // ---------- Эффекты / правила ----------

    public enum CardEffectType
    {
        [InspectorName("Изменить эмоцию")] ChangeStat,
        [InspectorName("Изменить отношения")] ChangeRelationship,
        [InspectorName("Дать временное состояние")] AddState,
        [InspectorName("Снять временное состояние")] RemoveState,
        [InspectorName("Создать событие (теги)")] WorldEvent,
        [InspectorName("Позвать / переместить актёра")] MoveActor,
        [InspectorName("Колода: вернуть карту из руки в библиотеку")] ReturnHandCardToLibrary,
        [InspectorName("Колода: взять случайную")] DrawRandom,
        [InspectorName("Колода: найти в библиотеке")] SearchLibrary,
        [InspectorName("Колода: вернуть использованную")] RecoverUsedCard,
        [InspectorName("Колода: защитить от сброса")] ProtectCard,
        [InspectorName("Колода: удержать в руке")] RetainCard,
        [InspectorName("Колода: снизить цену")] ReduceCost,
        [InspectorName("Колода: повысить цену")] IncreaseCost,
        [InspectorName("Колода: усилить следующую карту")] ModifyNextCard,
        [InspectorName("Колода: повторить эффект")] DuplicateEffect,
        [InspectorName("Колода: сбросить карту из руки в использованные")] MoveHandCardToUsed,
        [InspectorName("Колода: посмотреть случайные из библиотеки")] PeekLibrary,
        [InspectorName("Колода: одну из просмотренных — в руку")] ChooseOneToHand,
        [InspectorName("Колода: выбранную карту — в руку")] TakeSelectedIntoHand,
        [InspectorName("Изменить сильнейшую негативную эмоцию")] ChangeHighestNegative,
        [InspectorName("Окружение: поставить объект")] SpawnObject,
        [InspectorName("Окружение: аура объекта")] CreateAura,
        [InspectorName("Окружение: зона-ловушка (при входе)")] TriggerZone,
        [InspectorName("Окружение: NPC могут взаимодействовать")] NpcInteraction,
        [InspectorName("Поведение: изменить веса")] BehaviourWeights,
        [InspectorName("Контекст: добавить")] AddContext,
        [InspectorName("Контекст: снизить публичность")] LowerPublicContext,
        [InspectorName("Комната: запереть выход")] LockExit,
        [InspectorName("Комната: тише, меньше вмешательств")] QuietRoom,
        [InspectorName("Спонсор: кадр с объектом — бренду")] SponsorVisibility,
        [InspectorName("Проверка кубиком")] Check,
        [InspectorName("Раскрыть секрет")] RevealSecret,
        [InspectorName("Позвать актёра (бывший, третий)")] InviteActor,
        [InspectorName("Прервать слабую цепочку")] InterruptChain,
        [InspectorName("Случайный исход из таблицы")] RandomOutcome,
        [InspectorName("Актёры меняют позицию")] ForceMovement,
        [InspectorName("Кандидат на событие")] EventCandidate,
        [InspectorName("Съёмка: бонус следующему кадру")] NextCaptureBonus
    }

    public enum EffectReceiver
    {
        [InspectorName("Цель карты")] Target,
        [InspectorName("Все в комнате")] AllInRoom,
        [InspectorName("Свидетели")] Witnesses,
        [InspectorName("Случайный актёр")] RandomActor
    }

    // Сколько живёт временное состояние (GDD §9).
    public enum StateDuration
    {
        [InspectorName("По таймеру")] Timed,
        [InspectorName("Пока не сработает")] UntilConsumed,
        [InspectorName("До конца съёмки")] UntilEndOfSituation,
        [InspectorName("До конца выпуска")] UntilEpisodeEnd
    }

    [Serializable]
    public class CardEffect
    {
        public CardEffectType type;
        public EffectReceiver receiver;
        public ActorStat stat;
        public RelationshipAxis axis;
        [Tooltip("Сила: очки эмоции/отношений, число карт, $ цены.")]
        public float amount;
        [Tooltip("Состояние, теги события (через запятую), id актёра/комнаты — зависит от типа.")]
        public string key;
        public StateDuration duration;
        [Tooltip("Секунд — для «По таймеру».")]
        public float seconds;
        [Tooltip("Срабатывает, только если у цели есть эта черта / состояние / контекст. Пусто — всегда.")]
        public string onlyIf;
    }

    // ---------- Dice ----------

    public enum DieSize
    {
        [InspectorName("d4")] D4,
        [InspectorName("d6")] D6,
        [InspectorName("d8")] D8,
        [InspectorName("d10")] D10,
        [InspectorName("d12")] D12
    }

    public enum DiceRoll
    {
        [InspectorName("Обычный")] Normal,
        [InspectorName("Преимущество (2 кубика, лучший)")] Advantage,
        [InspectorName("Помеха (2 кубика, худший)")] Disadvantage
    }

    public enum StepUpCondition
    {
        [InspectorName("Рядом тег окружения")] NearEnvironmentTag,
        [InspectorName("У цели черта")] TargetHasTrait,
        [InspectorName("У цели состояние")] TargetHasState,
        [InspectorName("Эмоция цели не меньше")] TargetStatAtLeast
    }

    [Serializable]
    public class DiceStepUp
    {
        public StepUpCondition condition;
        [Tooltip("Тег, черта или состояние.")]
        public string key;
        public ActorStat stat;
        public int value;
        [Tooltip("На сколько ступеней растёт кубик: d6 → d8 = 1.")]
        public int steps = 1;
    }

    // Что меняет бросок.
    public enum DiceSubject
    {
        [InspectorName("Эмоция")] Stat,
        [InspectorName("Отношения")] Relationship,
        [InspectorName("Проверка / сила события")] Check
    }

    [Serializable]
    public class DiceEffect
    {
        public DiceSubject subject;
        public ActorStat stat;
        public RelationshipAxis axis;
        [Tooltip("Для проверки: что бросаем — Reveal, Performance, Outcome, Interaction...")]
        public string check;
        [Tooltip("Бросок снижает значение (Stress −1d6).")]
        public bool lower;
        [Tooltip("Только если у цели черта / состояние / контекст. Пусто — всегда.")]
        public string onlyIf;
        public DieSize die = DieSize.D6;
        [Min(1)] public int count = 1;
        public DiceRoll roll;
        [Tooltip("Прибавка к броску.")]
        public int bonus;
        public List<DiceStepUp> stepUps = new List<DiceStepUp>();
    }

    // ---------- Окружение и аура ----------

    public enum EnvironmentLifetime
    {
        [InspectorName("До конца съёмки")] UntilSituationEnd,
        [InspectorName("Секунд")] Seconds,
        [InspectorName("До конца выпуска")] UntilEpisodeEnd
    }

    [Serializable]
    public class AuraStatModifier
    {
        public ActorStat stat;
        [Tooltip("Изменение в секунду, пока актёр в радиусе.")]
        public float perSecond;
    }

    [Serializable]
    public class AuraDiceModifier
    {
        public ActorStat stat;
        [Tooltip("Ступени кубика для бросков этой эмоции в радиусе.")]
        public int steps = 1;
    }

    [Serializable]
    public class BehaviourWeight
    {
        [Tooltip("Поведение: Shout, Confront, ThrowObject, LeaveRoom, Cry...")]
        public string behaviour;
        public float multiplier = 1f;
    }

    [Serializable]
    public class AuraDefinition
    {
        public bool enabled;
        public float radius = 3f;
        public List<string> tags = new List<string>();
        public List<AuraStatModifier> actorModifiers = new List<AuraStatModifier>();
        public List<AuraDiceModifier> diceModifiers = new List<AuraDiceModifier>();
        public List<BehaviourWeight> behaviourWeights = new List<BehaviourWeight>();
    }

    [Serializable]
    public class FootageInfluence
    {
        [Tooltip("Теги, которые получают кадры с этой картой или её объектом.")]
        public List<string> tags = new List<string>();
        [Tooltip("Прибавка к ценности кадра.")]
        public int valueBonus;
        [Tooltip("Коммерческая ценность (для спонсорских целей).")]
        public int commercialValue;
        [Tooltip("Прибавка к техническому качеству кадра.")]
        public int technicalQuality;
    }

    // Колонки таблицы дизайна (.xlsx) как они есть. Структурные поля карты заполняются из них при импорте;
    // текст остаётся спецификацией для кора там, где структура пока не всё выражает.
    [Serializable]
    public class CardSheetSpec
    {
        [TextArea(1, 3)] public string targetFilters;
        [TextArea(1, 3)] public string effects;
        [TextArea(1, 3)] public string dice;
        [TextArea(1, 3)] public string aura;
        [TextArea(1, 3)] public string footage;
        [TextArea(1, 3)] public string lifecycle;
        [TextArea(1, 3)] public string upgradeTier2;
        [TextArea(1, 3)] public string upgradeTier3;
    }

    // HellToken — доллары с центами: $0.75, $2.
    public static class HellToken
    {
        public static string Format(float value)
        {
            var c = System.Globalization.CultureInfo.InvariantCulture;
            return "$" + (Mathf.Approximately(value, Mathf.Round(value)) ? Mathf.RoundToInt(value).ToString(c) : value.ToString("0.00", c));
        }
    }

    // ---------- Жизненный цикл ----------

    public enum CardLifetime
    {
        [InspectorName("Постоянная: снова в следующей съёмке")] Permanent,
        [InspectorName("Одна на выпуск")] OncePerEpisode,
        [InspectorName("Сгорает после розыгрыша")] Consumed,
        [InspectorName("Временная: до эфира")] UntilBroadcast
    }

    public enum SpecialRule
    {
        [InspectorName("Удерживается в руке")] Retain,
        [InspectorName("Всегда в стартовой руке")] Innate,
        [InspectorName("Не теряется при катастрофе")] CannotBeLost,
        [InspectorName("Сгорает, если не сыграна до конца съёмки")] Ethereal,
        [InspectorName("Нельзя сыграть (проклятие)")] Unplayable,
        [InspectorName("Занимает Production Slot")] UsesProductionSlot
    }

    public static class Dice
    {
        public static int Faces(DieSize die)
        {
            return 4 + (int)die * 2;
        }

        public static DieSize Step(DieSize die, int steps)
        {
            return (DieSize)Mathf.Clamp((int)die + steps, 0, (int)DieSize.D12);
        }
    }
}
