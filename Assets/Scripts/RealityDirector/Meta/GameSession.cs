using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Persistence;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Живёт между сценами: хаб пишет, квартира читает.
    public static class GameSession
    {
        public static SeasonState State { get; private set; }
        public static SeasonTone Tone { get; private set; }
        public static readonly List<string> Hand = new List<string>();
        public static string SceneTitle;
        // Серию запустили с карты (рука могла быть и пустой). Нет — квартиру открыли напрямую из редактора.
        public static bool Embarked;
        // Узел карты выпуска, ради которого загрузили сцену съёмки. Хаб закрывает его, когда сцена вернулась.
        public static string RoomNodeId;
        // Квартира закрыла сцену — хаб открывает карту, а не меню продакшена.
        public static bool ReturnToMap;

        public static bool Active => State != null;
        public static bool InEpisode => Active && State.episode != null;
        // Сезон снят, когда вышли все выпуски.
        public static bool SeasonOver => Active && State.episode == null && State.seasonLength > 0
                                         && State.episodeIndex >= State.seasonLength;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Clear()
        {
            State = null;
            Tone = null;
            SceneTitle = null;
            Embarked = false;
            RoomNodeId = null;
            ReturnToMap = false;
            Hand.Clear();
        }

        public static void NewSeason(IList<string> starters, SeasonConfig config = null)
        {
            State = new SeasonState();
            State.Reset(starters);
            if (config != null)
            {
                State.seasonLength = config.episodes;
                State.money = config.startingBudget;
            }

            Tone = new SeasonTone();
            RoomNodeId = null;
            Hand.Clear();
            Save();
        }

        public static bool Continue()
        {
            var state = new SeasonState();
            var tone = new SeasonTone();
            if (!SaveSystem.Load(state, tone))
                return false;
            State = state;
            Tone = tone;
            RoomNodeId = null;
            Hand.Clear();
            return true;
        }

        public static void Save()
        {
            if (Active)
                SaveSystem.Save(State, Tone);
        }

        public static void EndSeason()
        {
            SaveSystem.Delete();
        }
    }
}
