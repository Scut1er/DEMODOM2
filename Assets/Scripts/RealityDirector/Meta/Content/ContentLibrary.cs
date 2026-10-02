using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Весь контент лежит в Resources/Content (любые подпапки). Положили ассет в папку — он в игре.
    public static class ContentLibrary
    {
        public const string Root = "Content";

        static readonly Dictionary<Type, IList> Cache = new Dictionary<Type, IList>();

        // Сброс кэша: в начале игры и в редакторе после правок контента (предпросмотр карты).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            Cache.Clear();
        }

        public static IReadOnlyList<T> All<T>() where T : ContentDefinition
        {
            if (Cache.TryGetValue(typeof(T), out var cached))
                return (List<T>)cached;

            var list = new List<T>(Resources.LoadAll<T>(Root));
            var seen = new HashSet<string>();
            for (int i = 0; i < list.Count; i++)
            {
                if (!seen.Add(list[i].Id))
                    Debug.LogWarning("Content: повтор id '" + list[i].Id + "' (" + list[i].name + "). Id должен быть уникальным.", list[i]);
            }

            Cache[typeof(T)] = list;
            return list;
        }

        public static T Find<T>(string id) where T : ContentDefinition
        {
            if (string.IsNullOrEmpty(id))
                return null;
            var all = All<T>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Id == id)
                    return all[i];
            }

            return null;
        }
    }
}
