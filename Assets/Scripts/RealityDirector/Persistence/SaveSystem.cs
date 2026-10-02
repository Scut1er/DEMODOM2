using System;
using System.Collections.Generic;
using System.IO;
using RealityDirector.Core;
using RealityDirector.Meta;
using UnityEngine;

namespace RealityDirector.Persistence
{
    [Serializable]
    public class SavedTask
    {
        public ViewerWishId id;
        public string label;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int money;
        public int episodeIndex;
        public int castLevel = 1;
        public int operatorLevel = 1;
        public int writerLevel = 1;
        public List<string> owned = new List<string>();
        public List<string> played = new List<string>();
        public List<string> picked = new List<string>();
        public List<SavedTask> tasks = new List<SavedTask>();
        public List<string> route = new List<string>();
        public int step;
        public int mapSeed;
        public int mapFloors;
        public int ratingSum;
        public int rated;
        public int drama;
        public int trash;
        public int family;
    }

    public static class SaveSystem
    {
        const int Version = 1;

        static string FilePath => Path.Combine(Application.persistentDataPath, "season.json");

        public static bool HasSave => File.Exists(FilePath);

        public static void Save(SeasonState state, SeasonTone tone)
        {
            var data = new SaveData
            {
                version = Version,
                money = state.money,
                episodeIndex = state.episodeIndex,
                castLevel = state.castLevel,
                operatorLevel = state.operatorLevel,
                writerLevel = state.writerLevel,
                owned = new List<string>(state.owned),
                played = new List<string>(state.played),
                picked = new List<string>(state.picked),
                route = new List<string>(state.route),
                step = state.step,
                mapSeed = state.mapSeed,
                mapFloors = state.mapFloors,
                ratingSum = state.ratingSum,
                rated = state.rated,
                drama = tone.Drama,
                trash = tone.Trash,
                family = tone.Family
            };
            for (int i = 0; i < state.tasks.Count; i++)
                data.tasks.Add(new SavedTask { id = state.tasks[i].id, label = state.tasks[i].label });

            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save failed: " + e.Message);
            }
        }

        public static bool Load(SeasonState state, SeasonTone tone)
        {
            if (!HasSave)
                return false;

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save unreadable: " + e.Message);
                return false;
            }

            if (data == null || data.version != Version)
                return false;

            state.Reset(data.owned);
            state.money = data.money;
            state.episodeIndex = data.episodeIndex;
            state.castLevel = data.castLevel;
            state.operatorLevel = data.operatorLevel;
            state.writerLevel = data.writerLevel;
            state.played.UnionWith(data.played);
            state.picked.AddRange(data.picked);
            state.route.AddRange(data.route);
            state.step = data.step;
            state.mapSeed = data.mapSeed;
            state.mapFloors = data.mapFloors;
            state.ratingSum = data.ratingSum;
            state.rated = data.rated;
            for (int i = 0; i < data.tasks.Count; i++)
                state.tasks.Add(new ViewerTask { id = data.tasks[i].id, label = data.tasks[i].label });

            tone.Reset();
            tone.Drama = data.drama;
            tone.Trash = data.trash;
            tone.Family = data.family;
            return true;
        }

        public static void Delete()
        {
            if (HasSave)
                File.Delete(FilePath);
        }
    }
}
