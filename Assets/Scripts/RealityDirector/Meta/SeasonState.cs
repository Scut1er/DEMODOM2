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
        public readonly List<string> owned = new List<string>();
        public readonly HashSet<string> played = new HashSet<string>();
        public readonly List<string> picked = new List<string>();
        // Путь по карте сезона: id узла на каждую серию.
        public readonly List<string> route = new List<string>();
        public int ratingSum;
        public int rated;

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
            route.Clear();
            ratingSum = 0;
            rated = 0;
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

        public void Accept(ViewerWishId id, string label)
        {
            if (id == ViewerWishId.None || HasTask(id) || tasks.Count >= 4)
                return;
            tasks.Add(new ViewerTask { id = id, label = label });
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

        public bool Owns(string id)
        {
            return owned.Contains(id);
        }

        public bool IsPlayed(string id)
        {
            return played.Contains(id);
        }

        public int UnplayedCount()
        {
            int n = 0;
            for (int i = 0; i < owned.Count; i++)
            {
                if (!played.Contains(owned[i]))
                    n++;
            }

            return n;
        }
    }
}
