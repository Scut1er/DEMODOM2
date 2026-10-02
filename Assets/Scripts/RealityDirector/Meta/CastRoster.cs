using System.Collections.Generic;
using RealityDirector.Util;
using UnityEngine;

namespace RealityDirector.Meta
{
    public class CastMember
    {
        public string id;
        public string name;
        public string[] traits;
        public Sprite portrait;
    }

    // Участники для хаба: из ассетов Resources/Content/Characters (ActorDefinition).
    // Ассетов нет — двое встроенных из квартиры.
    public static class CastRoster
    {
        public const int MaxSeats = 6;

        public static int Seats(int castLevel)
        {
            return Mathf.Clamp(castLevel + 1, 2, MaxSeats);
        }

        public static CastMember[] All()
        {
            var defs = new List<ActorDefinition>();
            var all = ContentLibrary.All<ActorDefinition>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].available)
                    defs.Add(all[i]);
            }

            if (defs.Count == 0)
                return BuiltIn();

            defs.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.Id, b.Id));
            var members = new CastMember[defs.Count];
            for (int i = 0; i < defs.Count; i++)
                members[i] = From(defs[i]);
            return members;
        }

        static CastMember From(ActorDefinition def)
        {
            var traits = new List<string>(def.visibleTraits);
            if (!string.IsNullOrEmpty(def.hiddenTraitLabel))
                traits.Add(def.hiddenTraitLabel);
            return new CastMember
            {
                id = def.Id,
                name = def.displayName,
                traits = traits.ToArray(),
                portrait = def.portrait != null ? def.portrait
                    : GameArt.HeadByPrefix(def.artPrefix, Face.Happy) ?? GameArt.Head(def.Id, Face.Happy) ?? IllustratedArt.PersonKind
            };
        }

        static CastMember[] BuiltIn()
        {
            return new[]
            {
                new CastMember
                {
                    id = "npc_zloi",
                    name = "Злой",
                    traits = new[] { "агрессивный", "скрытая черта: ???" },
                    portrait = GameArt.Head("npc_zloi", Face.Happy) ?? IllustratedArt.PersonAngry
                },
                new CastMember
                {
                    id = "npc_dobryak",
                    name = "Добряк",
                    traits = new[] { "сентиментальный", "скрытая черта: ???" },
                    portrait = GameArt.Head("npc_dobryak", Face.Happy) ?? IllustratedArt.PersonKind
                }
            };
        }
    }
}
