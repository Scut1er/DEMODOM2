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

        public static bool Active => State != null;
        // Сезон снят, когда пройдены все ряды карты (последний — финал).
        public static bool SeasonOver => Active && State.mapFloors > 0 && State.step >= State.mapFloors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Clear()
        {
            State = null;
            Tone = null;
            SceneTitle = null;
            Embarked = false;
            Hand.Clear();
        }

        public static void NewSeason(IList<string> starters)
        {
            State = new SeasonState();
            State.Reset(starters);
            // Карту генерирует хаб по этому seed; 0 в конфиге — новая карта каждый сезон.
            State.mapSeed = Random.Range(1, int.MaxValue);
            Tone = new SeasonTone();
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
