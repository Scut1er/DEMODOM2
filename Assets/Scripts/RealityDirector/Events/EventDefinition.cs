using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.NPC;
using UnityEngine;

namespace RealityDirector.Events
{
    // Значения сериализуются числом — новые только в конец.
    public enum TargetType
    {
        Object,
        Actor,
        Global,
        ActorPair,
        Zone,
        CardInHand,
        UsedCard
    }

    // Карта продюсера. Поля по GDD §30 (CardDefinition); типы данных — в CardData.cs.
    [CreateAssetMenu(menuName = "RealityDirector/Event", fileName = "Event")]
    public class EventDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string hint;
        [Tooltip("Provocation, Environment, Social, Confession, Reveal, Comedy, Sponsor. Пусто — карта открыта сразу.")]
        public string category;
        public TargetType targetType;
        public string requiredObjectId;
        public Color cardColor = Color.white;
        public Sprite cardArt;
        public List<string> tags = new List<string>();
        public List<ShowMood> moods = new List<ShowMood>();
        public float rageSeconds;
        public bool ignite;
        [Tooltip("HellToken в долларах ($0.75), чтобы сыграть карту на съёмке.")]
        public float cost;
        public int price;
        [Tooltip("Цена в магазине выпуска (УЕ). 0 — в этот магазин не попадает. Карта живёт только до эфира выпуска.")]
        public int runPrice;
        [Tooltip("Продакт-плейсмент: сыгранная карта даёт ЕБ в конце сцены и режет отзывы.")]
        public bool sponsor;
        public int sponsorPay;
        public int sponsorScoreHit;
        public bool starter;
        public bool limitTrait;
        public TraitId targetTrait;

        [Header("Паспорт карты")]
        [Tooltip("Выключенной карты нет в игре: ни в колоде, ни в магазинах.")]
        public CardStatus status;
        public CardTier tier;
        public CardRarity rarity;

        [Header("Цель")]
        [Tooltip("Дополнительные условия на цель.")]
        public List<TargetFilter> targetFilters = new List<TargetFilter>();

        [Header("Текст")]
        [Tooltip("Полное описание для игрока.")]
        [TextArea(2, 5)] public string description;

        [Header("Эффекты")]
        public List<CardEffect> effects = new List<CardEffect>();
        public List<DiceEffect> diceEffects = new List<DiceEffect>();

        [Header("Окружение")]
        [Tooltip("Префаб, который карта ставит в мир (environment card).")]
        public GameObject environmentPrefab;
        [Tooltip("Id объекта окружения из таблицы (AlcoholCrate) — пока префаба нет.")]
        public string environmentId;
        public EnvironmentLifetime environmentLifetime;
        [Min(0f)] public float environmentSeconds = 30f;
        public AuraDefinition aura = new AuraDefinition();

        [Header("Footage и спонсор")]
        public FootageInfluence footage = new FootageInfluence();
        [Tooltip("Бренд спонсора (для контрактов и HellTube).")]
        public string sponsorId;

        [Header("Жизненный цикл")]
        public CardLifetime lifetime;
        public List<SpecialRule> specialRules = new List<SpecialRule>();

        [Header("Улучшения")]
        [Tooltip("Карта Tier II — отдельный ассет со своими значениями.")]
        public EventDefinition upgradeTier2;
        [Tooltip("Бюджет крафта: 3 × эта карта + ЕБ → Tier II.")]
        [Min(0)] public int craftBudgetTier2;
        public EventDefinition upgradeTier3;
        [Min(0)] public int craftBudgetTier3;

        [Header("Для команды")]
        [Tooltip("Зачем эта карта в игре, какую ситуацию должна создавать.")]
        [TextArea(2, 6)] public string designIntent;

        [Tooltip("Колонки таблицы дизайна как есть (импорт из .xlsx).")]
        public CardSheetSpec sheet = new CardSheetSpec();

        // Как карта наводится в квартире сейчас: пара — клик по одному участнику, зона и карты колоды — сразу.
        public TargetType PlayTarget
        {
            get
            {
                switch (targetType)
                {
                    case TargetType.ActorPair: return TargetType.Actor;
                    case TargetType.Zone:
                    case TargetType.CardInHand:
                    case TargetType.UsedCard: return TargetType.Global;
                    default: return targetType;
                }
            }
        }
    }
}
