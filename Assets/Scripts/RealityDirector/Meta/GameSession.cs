using System.Collections.Generic;
using RealityDirector.Capture;
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
        public static string SceneId;
        // Серию запустили с карты (рука могла быть и пустой). Нет — квартиру открыли напрямую из редактора.
        public static bool Embarked;
        // Файл пишется только после подписи контракта. До этого «Продолжить» смотрит на прошлый сейв.
        public static bool Committed { get; private set; }
        // Узел карты выпуска, ради которого загрузили сцену съёмки. Хаб закрывает его, когда сцена вернулась.
        public static string RoomNodeId;
        // Квартира закрыла сцену — хаб открывает карту, а не меню продакшена.
        public static bool ReturnToMap;
        // Игрок сам вышел со съёмки. Комнату не закрываем, хаб не прыгает обратно на карту.
        public static bool ExitToHub;

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
            SceneId = null;
            Embarked = false;
            Committed = false;
            RoomNodeId = null;
            ReturnToMap = false;
            ExitToHub = false;
            Hand.Clear();
            FootageReel.ReleaseAll();
            Application.quitting -= Save;
            Application.quitting += Save;
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
            SceneTitle = null;
            SceneId = null;
            Embarked = false;
            Committed = false;
            RoomNodeId = null;
            Hand.Clear();
            FootageReel.ReleaseAll();
        }

        public static void Commit()
        {
            Committed = true;
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
            if (state.hand != null)
                Hand.AddRange(state.hand);
            Embarked = state.embarked;
            RoomNodeId = string.IsNullOrEmpty(state.roomNodeId) ? null : state.roomNodeId;
            SceneTitle = state.sceneTitle;
            SceneId = state.sceneId;
            Committed = true;
            return true;
        }

        public static void Save()
        {
            if (!Active || !Committed)
                return;
            State.hand.Clear();
            State.hand.AddRange(Hand);
            State.embarked = Embarked;
            State.roomNodeId = RoomNodeId;
            State.sceneTitle = SceneTitle;
            State.sceneId = SceneId;
            SaveSystem.Save(State, Tone);
        }

        public static void EndSeason()
        {
            Committed = false;
            SaveSystem.Delete();
        }
    }
}
