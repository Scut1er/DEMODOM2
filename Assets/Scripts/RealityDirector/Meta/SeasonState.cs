using System.Collections.Generic;
using RealityDirector.Capture;
using RealityDirector.Core;

namespace RealityDirector.Meta
{
    public class ViewerTask
    {
        public ViewerWishId id;
        public string label;
    }

    public class SeasonState
    {
        public int money;
        public int episodeIndex;
        public int castLevel = 1;
        public int operatorLevel = 1;
        public int writerLevel = 1;
        public readonly List<ViewerTask> tasks = new List<ViewerTask>();
        // Коллекция сезона (рабочая колода). Карты не сгорают: каждая съёмка тасует её заново.
        public readonly List<string> owned = new List<string>();
        // «Использовано» текущей съёмки (GDD §15–16). Чистится в начале и в конце каждой съёмки.
        public readonly HashSet<string> played = new HashSet<string>();
        // Устарело: ручной выбор карт перед съёмкой. Остаётся ради старых сейвов.
        public readonly List<string> picked = new List<string>();
        // «Библиотека» текущей съёмки: перетасованная колода, сверху — следующая карта добора.
        public readonly List<string> library = new List<string>();
        public float ratingSum;
        public int rated;

        // Сезон и карьера.
        public int seasonNumber = 1;
        public string producerName = "";
        // Сколько выпусков в сезоне (из SeasonConfig).
        public int seasonLength;
        // Долгие флаги сезона (условия и эффекты контента).
        public readonly List<string> flags = new List<string>();
        public readonly List<string> castPick = new List<string>();
        // Карты «держим в запасе» (Retain / Protect): не сбрасываются эффектами колоды и приходят в руку следующей съёмки первыми.
        public readonly List<string> retained = new List<string>();
        public int sponsorReputation = 25;
        public bool wantsTutorial;
        public int tutorialBeat;
        public readonly List<string> hand = new List<string>();
        public bool embarked;
        public string roomNodeId;
        public string sceneTitle;
        public string sceneId;
        // Текущий выпуск: от выхода из хаба до эфира. null — игрок в хабе между выпусками.
        public EpisodeState episode;
        // Устарело: квартира (PitchFlow) ещё увеличивает это поле. Мета смотрит на episode.step.
        public int step;

        public void Reset(IList<string> starters)
        {
            money = 0;
            episodeIndex = 0;
            castLevel = 1;
            operatorLevel = 1;
            writerLevel = 1;
            tasks.Clear();
            owned.Clear();
            played.Clear();
            picked.Clear();
            library.Clear();
            step = 0;
            ratingSum = 0;
            rated = 0;
            seasonNumber = 1;
            producerName = "";
            seasonLength = 0;
            flags.Clear();
            castPick.Clear();
            retained.Clear();
            sponsorReputation = 25;
            wantsTutorial = false;
            tutorialBeat = 0;
            hand.Clear();
            embarked = false;
            roomNodeId = null;
            sceneTitle = null;
            sceneId = null;
            episode = null;
            if (starters == null)
                return;
            for (int i = 0; i < starters.Count; i++)
            {
                if (!owned.Contains(starters[i]))
                    owned.Add(starters[i]);
            }
        }

        public bool HasTask(ViewerWishId id)
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].id == id)
                    return true;
            }

            return false;
        }

        public bool Accept(ViewerWishId id, string label)
        {
            if (id == ViewerWishId.None || HasTask(id) || tasks.Count >= 4)
                return false;
            tasks.Add(new ViewerTask { id = id, label = string.IsNullOrEmpty(label) ? id.ToString() : label });
            return true;
        }

        public bool Resolve(IReadOnlyList<CapturedMoment> moments, SeasonTone tone)
        {
            bool met = false;
            for (int i = tasks.Count - 1; i >= 0; i--)
            {
                if (!Progression.WishMet(tasks[i].id, moments, tone))
                    continue;
                met = true;
                tasks.RemoveAt(i);
            }

            return met;
        }

        public bool HasFlag(string key)
        {
            return !string.IsNullOrEmpty(key) && flags.Contains(key);
        }

        public void SetFlag(string key)
        {
            if (!string.IsNullOrEmpty(key) && !flags.Contains(key))
                flags.Add(key);
        }

        public int Level(CrewTrack track)
        {
            switch (track)
            {
                case CrewTrack.Cast: return castLevel;
                case CrewTrack.Operators: return operatorLevel;
                default: return writerLevel;
            }
        }

        public bool Owns(string id)
        {
            return owned.Contains(id);
        }

        public bool IsPlayed(string id)
        {
            return played.Contains(id);
        }

        // Верхняя карта библиотеки. Пусто — null: рука просто становится меньше.
        public string Draw()
        {
            if (library.Count == 0)
                return null;
            string id = library[0];
            library.RemoveAt(0);
            return id;
        }

        // Съёмка закончилась: «Использовано» и библиотека пустеют, в следующей съёмке колода снова целиком.
        public void EndSituation()
        {
            played.Clear();
            library.Clear();
            hand.Clear();
        }
    }
}
