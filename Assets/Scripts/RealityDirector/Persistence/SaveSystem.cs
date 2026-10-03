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
        public List<string> library = new List<string>();
        public List<SavedTask> tasks = new List<SavedTask>();
        public int step;
        public float ratingSum;
        public int rated;
        public int drama;
        public int trash;
        public int family;
        public int seasonNumber = 1;
        public string producerName = "";
        public List<string> castPick = new List<string>();
        public List<string> retained = new List<string>();
        public List<string> castCandidates = new List<string>();
        public int candidatesEpisode = -1;
        public int sponsorReputation = 25;
        public bool wantsTutorial;
        public int tutorialBeat;
        public List<string> hand = new List<string>();
        public bool embarked;
        public string roomNodeId;
        public string sceneTitle;
        public string sceneId;
        public int seasonLength;
        public List<string> flags = new List<string>();
        // JsonUtility не умеет null для вложенных классов — поэтому отдельный флаг.
        public bool hasEpisode;
        public EpisodeState episode;
    }

    public static class SaveSystem
    {
        // 2 — карта стала картой выпуска (EpisodeState). Старые сейвы не читаются.
        const int Version = 2;

        static string FilePath => Path.Combine(Application.persistentDataPath, "season.json");

        public static bool HasSave
        {
            get
            {
                if (!File.Exists(FilePath))
                    return false;
                try
                {
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                    return data != null && data.version == Version;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

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
                library = new List<string>(state.library),
                step = state.step,
                ratingSum = state.ratingSum,
                rated = state.rated,
                drama = tone.Drama,
                trash = tone.Trash,
                family = tone.Family,
                seasonNumber = state.seasonNumber,
                producerName = state.producerName,
                castPick = new List<string>(state.castPick),
                retained = new List<string>(state.retained),
                castCandidates = new List<string>(state.castCandidates),
                candidatesEpisode = state.candidatesEpisode,
                sponsorReputation = state.sponsorReputation,
                wantsTutorial = state.wantsTutorial,
                tutorialBeat = state.tutorialBeat,
                hand = new List<string>(state.hand),
                embarked = state.embarked,
                roomNodeId = state.roomNodeId,
                sceneTitle = state.sceneTitle,
                sceneId = state.sceneId,
                seasonLength = state.seasonLength,
                flags = new List<string>(state.flags),
                hasEpisode = state.episode != null,
                episode = state.episode
            };
            for (int i = 0; i < state.tasks.Count; i++)
                data.tasks.Add(new SavedTask { id = state.tasks[i].id, label = state.tasks[i].label });

            try
            {
                WriteAtomic(FilePath, JsonUtility.ToJson(data, true));
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
            if (data.played != null)
                state.played.UnionWith(data.played);
            if (data.picked != null)
                state.picked.AddRange(data.picked);
            state.step = data.step;
            state.seasonNumber = data.seasonNumber;
            state.producerName = data.producerName ?? "";
            if (data.castPick != null)
                state.castPick.AddRange(data.castPick);
            if (data.retained != null)
                state.retained.AddRange(data.retained);
            if (data.castCandidates != null)
                state.castCandidates.AddRange(data.castCandidates);
            state.candidatesEpisode = data.candidatesEpisode;
            state.sponsorReputation = data.sponsorReputation;
            state.wantsTutorial = data.wantsTutorial;
            state.tutorialBeat = data.tutorialBeat;
            if (data.hand != null)
                state.hand.AddRange(data.hand);
            state.embarked = data.embarked;
            if (data.library != null)
                state.library.AddRange(data.library);
            // «Использовано» и библиотека живут только внутри съёмки. Вне съёмки — пусто:
            // так и старые сейвы, где карты сгорали на весь сезон, получают колоду обратно.
            if (!state.embarked)
            {
                state.played.Clear();
                state.library.Clear();
            }

            state.picked.Clear();
            state.roomNodeId = data.roomNodeId;
            state.sceneTitle = data.sceneTitle;
            state.sceneId = data.sceneId;
            state.seasonLength = data.seasonLength;
            if (data.flags != null)
                state.flags.AddRange(data.flags);
            state.episode = data.hasEpisode ? data.episode : null;
            if (state.episode != null)
                state.episode.EnsureLists();
            state.ratingSum = data.ratingSum;
            state.rated = data.rated;
            if (data.tasks != null)
            {
                for (int i = 0; i < data.tasks.Count; i++)
                    state.tasks.Add(new ViewerTask { id = data.tasks[i].id, label = data.tasks[i].label });
            }

            tone.Reset();
            tone.Drama = data.drama;
            tone.Trash = data.trash;
            tone.Family = data.family;
            return true;
        }

        // Сначала во временный файл, потом подмена: падение посреди записи не портит прошлый сейв.
        static void WriteAtomic(string path, string text)
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, text);
            if (!File.Exists(path))
            {
                File.Move(tmp, path);
                return;
            }

            try
            {
                File.Replace(tmp, path, null);
            }
            catch (Exception)
            {
                // Файловая система без атомарной подмены — копия поверх и уборка.
                File.Copy(tmp, path, true);
                File.Delete(tmp);
            }
        }

        // Удаляет и сейв чужой версии, и недописанный временный файл.
        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
                if (File.Exists(FilePath + ".tmp"))
                    File.Delete(FilePath + ".tmp");
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save delete failed: " + e.Message);
            }
        }
    }
}
