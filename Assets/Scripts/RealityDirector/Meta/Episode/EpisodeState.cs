using System;
using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Один отснятый момент. Общая библиотека выпуска: все съёмки складывают сюда, монтаж выбирает отсюда.
    [Serializable]
    public class FootageEntry
    {
        public string id;
        [Tooltip("Комната (id контента) и узел карты, где снято.")]
        public string roomId;
        public string nodeId;
        public List<string> actorIds = new List<string>();
        public List<string> objectIds = new List<string>();
        public List<string> tags = new List<string>();
        [Tooltip("Кто что делал в кадре: «тег|имя». HellTube называет только тех, кто попал в эфир.")]
        public List<string> cues = new List<string>();
        public ShowMood mood;
        public int quality;
        public int value;
        public float time;
        public string description;
        public string plotline;
        public int exposed;
        [Tooltip("Ссылка на видео/скриншот клипа — формат задаёт кор (Стефан).")]
        public string clipRef;
        public bool failed;
    }

    public enum ContractStatus
    {
        Active,
        Fulfilled,
        Failed
    }

    [Serializable]
    public class SponsorContractState
    {
        public string offerId;
        public string brandId;
        public ContractStatus status;
        public string grantedCardId;
        public bool cardWasPlayed;
        public List<string> matchingFootageIds = new List<string>();
        public bool footageWasAired;
        public int payout;
        public int scoreHit;
    }

    [Serializable]
    public class ActorRuntime
    {
        public string actorId;
        public int stress;
        public int anger;
        public int sadness;
        public int attraction;
        public int confidence = 40;
        public int selfControl = 50;
        public int hostility;
        public string memory;
        public bool seeded;
    }

    [Serializable]
    public class Modifier
    {
        public string key;
        public int value;
        public string sourceId;
    }

    [Serializable]
    public class RoomVisit
    {
        public int step;
        public string nodeId;
        public string roomId;
        public RoomType type;
    }

    // Состояние одного выпуска. Живёт от выхода из хаба до эфира и не сбрасывается между комнатами.
    // Хранит только id и данные — сейв пишется как есть.
    [Serializable]
    public class EpisodeState
    {
        public int index;
        public List<string> cast = new List<string>();
        public int budgetAtStart;
        [Tooltip("Нал выпуска. Тратится в магазине на карте, в хаб не переносится.")]
        public int cash;
        public const float HellCap = 10f;
        [Tooltip("HellToken съёмки ($). Тратится на cost карт. Новый заход в комнату заливает пул заново.")]
        public float hell;
        public float hellMax;
        public string hellRoom;
        public const int DefaultHandSize = 5;
        [Tooltip("Карт в руке на съёмке (из SeasonConfig).")]
        public int handSize = DefaultHandSize;
        [Tooltip("Production Slots на съёмку (из SeasonConfig).")]
        public int productionSlots = 3;
        public string setRoom;
        public bool setOnFire;
        public bool setBathOpen;
        public bool setBedOpen;
        [Tooltip("Сыгранные спонсорские карты, которым ещё нужен кадр.")]
        public List<string> pendingSponsors = new List<string>();
        [Tooltip("Больше не режет библиотеку. Слоты считаются на сцену.")]
        public int footageLimit = 5;
        public List<string> finalCut = new List<string>();
        public bool settled;
        public int settledPay;
        public int settledSponsor;
        public string settledLine;
        [TextArea(1, 3)] public string roleBrief;

        [Header("Карта выпуска")]
        public int mapSeed;
        public int mapLayers;
        public int mapLanes;
        public List<MapNode> nodes = new List<MapNode>();
        public int step;
        public List<string> route = new List<string>();
        public List<RoomVisit> history = new List<RoomVisit>();

        [Header("Накопленное за выпуск")]
        public List<FootageEntry> footage = new List<FootageEntry>();
        public List<string> tempCards = new List<string>();
        public List<SponsorContractState> contracts = new List<SponsorContractState>();
        public List<string> flags = new List<string>();
        public List<string> narrativeTags = new List<string>();
        public List<Modifier> nextRoomModifiers = new List<Modifier>();
        public List<Modifier> broadcastModifiers = new List<Modifier>();
        public List<ActorRuntime> actors = new List<ActorRuntime>();

        public void OpenHell(string roomId)
        {
            if (hellMax <= 0)
                hellMax = HellCap;
            string room = roomId ?? "";
            if (hellRoom != room)
            {
                hellRoom = room;
                hell = hellMax;
            }
        }

        public bool SpendHell(float cost)
        {
            if (cost <= 0f)
                return true;
            if (hell + 0.001f < cost)
                return false;
            hell = Mathf.Max(0f, hell - cost);
            return true;
        }

        public void EnsureLists()
        {
            if (pendingSponsors == null)
                pendingSponsors = new List<string>();
            if (finalCut == null)
                finalCut = new List<string>();
            if (contracts == null)
                contracts = new List<SponsorContractState>();
            if (footage == null)
                footage = new List<FootageEntry>();
            if (actors == null)
                actors = new List<ActorRuntime>();
            if (nextRoomModifiers == null)
                nextRoomModifiers = new List<Modifier>();
            for (int i = 0; i < contracts.Count; i++)
            {
                if (contracts[i].matchingFootageIds == null)
                    contracts[i].matchingFootageIds = new List<string>();
            }
        }

        public int Number => index + 1;
        public bool Finished => mapLayers > 0 && step >= mapLayers;

        public bool HasFlag(string key)
        {
            return !string.IsNullOrEmpty(key) && flags.Contains(key);
        }

        public void SetFlag(string key)
        {
            if (!string.IsNullOrEmpty(key) && !flags.Contains(key))
                flags.Add(key);
        }

        public bool HasTag(string key)
        {
            return !string.IsNullOrEmpty(key) && narrativeTags.Contains(key);
        }

        public void AddTag(string key)
        {
            if (!string.IsNullOrEmpty(key) && !narrativeTags.Contains(key))
                narrativeTags.Add(key);
        }

        public bool Visited(string roomId)
        {
            for (int i = 0; i < history.Count; i++)
            {
                if (history[i].roomId == roomId)
                    return true;
            }

            return false;
        }

        public bool ContractActive(string offerId)
        {
            for (int i = 0; i < contracts.Count; i++)
            {
                if (contracts[i].offerId == offerId && contracts[i].status == ContractStatus.Active)
                    return true;
            }

            return false;
        }

        public void AddModifier(List<Modifier> list, string key, int value, string sourceId = null)
        {
            if (string.IsNullOrEmpty(key))
                return;
            list.Add(new Modifier { key = key, value = value, sourceId = sourceId });
        }

        public ActorRuntime Actor(string actorId)
        {
            EnsureLists();
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i].actorId == actorId)
                    return actors[i];
            }

            var created = new ActorRuntime { actorId = actorId, confidence = 40, selfControl = 50 };
            actors.Add(created);
            return created;
        }

        public static int Sum(List<Modifier> list, string key)
        {
            int total = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].key == key)
                    total += list[i].value;
            }

            return total;
        }
    }
}
