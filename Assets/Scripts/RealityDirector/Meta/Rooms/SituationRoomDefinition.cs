using System;
using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    public enum HouseRoom
    {
        Any,
        Living,
        Kitchen,
        Bedroom,
        Bathroom
    }

    // Кто как начинает съёмку: где стоит, с какими эмоциями и состояниями.
    [Serializable]
    public class SituationActorSetup
    {
        [Tooltip("Кто: номер в касте по порядку выбора (0 — первый взятый). -1 — все участники.")]
        public int castSlot = -1;
        [Tooltip("Где стоит в начале съёмки. Any — на обычном месте.")]
        public HouseRoom room;
        [Header("Добавить к эмоциям в начале (можно минус)")]
        public int stress;
        public int anger;
        public int sadness;
        public int attraction;
        public int confidence;
        public int selfControl;
        [Tooltip("Временные состояния с начала съёмки: Drunk, Suspicious, Spotlit, NextNegativeEventAmplified…")]
        public List<string> states = new List<string>();
    }

    // Объект, который уже стоит в квартире, когда съёмка начинается (декорация, не занимает Production Slot).
    [Serializable]
    public class SituationProp
    {
        [Tooltip("Id карты окружения: объект встаёт с её аурой и действиями людей (CARD_ENV_001 — алкоголь).")]
        public string cardId;
        public HouseRoom room = HouseRoom.Living;
    }

    // Съёмка: основной геймплей (кор). Здесь — то, что нужно мете для карты и запуска сцены,
    // и постановка: чем эта съёмка отличается от других на той же квартире.
    [CreateAssetMenu(menuName = "RealityDirector/Rooms/Situation", fileName = "Situation_")]
    public class SituationRoomDefinition : RoomDefinition
    {
        [Header("Съёмка")]
        [Tooltip("Сцена, которая загружается при входе.")]
        public string scene = SceneFlow.Episode;
        [Tooltip("Роли для этой съёмки. Не имена: «инициатор — вспыльчивый».")]
        [TextArea(1, 3)] public string roleBrief;

        [Header("Постановка (задача съёмки — поле «Цель» выше)")]
        [Tooltip("Где стоят участники в начале, с какими эмоциями и состояниями.")]
        public List<SituationActorSetup> actors = new List<SituationActorSetup>();
        [Tooltip("Вражда первых двух участников в начале (0–100, прибавляется).")]
        public int hostility;
        [Tooltip("Доверие первых двух участников в начале (прибавляется, гасит вражду).")]
        public int trust;
        [Tooltip("События на старте (теги WorldEvent): участники реагируют по своим чертам. Conflict, Flirt, Rumour, Public…")]
        public List<string> startEvents = new List<string>();
        [Tooltip("Объекты, которые уже стоят в квартире.")]
        public List<SituationProp> props = new List<SituationProp>();
        [Tooltip("Спальня открыта с начала (иначе заколочена — откроет только карта).")]
        public bool openBedroom;
        [Tooltip("Ванная открыта с начала.")]
        public bool openBathroom;
        [Tooltip("Комнаты «без камер» (Private): меньше публичного давления, признания вероятнее, стеснительные расслабляются.")]
        public List<HouseRoom> privateRooms = new List<HouseRoom>();
        [Tooltip("Комнаты под давлением (Pressure): стресс растёт.")]
        public List<HouseRoom> pressureRooms = new List<HouseRoom>();

        [Header("Карты и бюджет")]
        [Tooltip("Только эти категории карт можно играть в этой съёмке. Пусто — все.")]
        public List<string> allowedCategories = new List<string>();
        [Tooltip("Эти категории карт в этой съёмке не играются.")]
        public List<string> blockedCategories = new List<string>();
        [Tooltip("HellToken на эту съёмку ($). 0 — как в SeasonConfig.")]
        [Min(0f)] public float hellTokenBudget;

        public override RoomType Type => RoomType.Situation;

        public bool Allows(string category)
        {
            if (blockedCategories != null && blockedCategories.Contains(category))
                return false;
            return allowedCategories == null || allowedCategories.Count == 0 || allowedCategories.Contains(category);
        }

        public bool HasSetup => (actors != null && actors.Count > 0) || hostility != 0 || trust != 0
                                || (startEvents != null && startEvents.Count > 0) || (props != null && props.Count > 0)
                                || openBedroom || openBathroom || (privateRooms != null && privateRooms.Count > 0)
                                || (pressureRooms != null && pressureRooms.Count > 0);
    }
}
