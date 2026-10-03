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
        public string secretHidden;
        public string secretKnown;
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
            var members = new List<CastMember>();
            for (int i = 0; i < defs.Count; i++)
                members.Add(From(defs[i]));
            var extra = BuiltIn();
            for (int i = 0; i < extra.Length; i++)
            {
                bool have = false;
                for (int j = 0; j < members.Count; j++)
                {
                    if (members[j].id == extra[i].id)
                        have = true;
                }

                if (!have)
                    members.Add(extra[i]);
            }

            return members.ToArray();
        }

        static CastMember From(ActorDefinition def)
        {
            var traits = new List<string>();
            if (def.visibleTraits != null)
            {
                for (int i = 0; i < def.visibleTraits.Count; i++)
                {
                    if (!string.IsNullOrEmpty(def.visibleTraits[i]) && !def.visibleTraits[i].Contains("???"))
                        traits.Add(def.visibleTraits[i]);
                }
            }

            return new CastMember
            {
                id = def.Id,
                name = def.displayName,
                traits = traits.ToArray(),
                portrait = def.portrait != null ? def.portrait
                    : GameArt.HeadByPrefix(def.artPrefix, Face.Neutral) ?? GameArt.Head(def.Id, Face.Neutral) ?? IllustratedArt.PersonKind,
                secretHidden = string.IsNullOrEmpty(def.hiddenTraitLabel) ? "" : def.hiddenTraitLabel,
                secretKnown = SecretName(def.hiddenTrait)
            };
        }

        static string SecretName(NPC.HiddenTrait trait)
        {
            switch (trait)
            {
                case NPC.HiddenTrait.Prankster: return "пранкер";
                case NPC.HiddenTrait.Kleptomaniac: return "клептоман";
                case NPC.HiddenTrait.Singer: return "поёт";
                default: return "";
            }
        }

        static CastMember[] BuiltIn()
        {
            return new[]
            {
                new CastMember
                {
                    id = "npc_zloi",
                    name = "Злой",
                    traits = new[] { "агрессивный" },
                    portrait = GameArt.Head("npc_zloi", Face.Happy) ?? IllustratedArt.PersonAngry,
                    secretHidden = "скрытая черта: ???",
                    secretKnown = "пранкер"
                },
                new CastMember
                {
                    id = "npc_dobryak",
                    name = "Добряк",
                    traits = new[] { "паникер" },
                    portrait = GameArt.Head("npc_dobryak", Face.Happy) ?? IllustratedArt.PersonKind,
                    secretHidden = "скрытая черта: ???",
                    secretKnown = "клептоман"
                },
                new CastMember
                {
                    id = "npc_kira",
                    name = "Кира",
                    traits = new[] { "ревнивая" },
                    portrait = GameArt.Head("npc_kira", Face.Neutral) ?? IllustratedArt.PersonAngry,
                    secretHidden = "",
                    secretKnown = ""
                },
                new CastMember
                {
                    id = "npc_max",
                    name = "Макс",
                    traits = new[] { "тщеславный" },
                    portrait = GameArt.Head("npc_max", Face.Neutral) ?? IllustratedArt.PersonKind,
                    secretHidden = "скрытая черта: ???",
                    secretKnown = "поёт"
                },
                new CastMember
                {
                    id = "npc_lyusya",
                    name = "Люся",
                    traits = new[] { "застенчивая" },
                    portrait = IllustratedArt.PersonKind,
                    secretHidden = "",
                    secretKnown = ""
                }
            };
        }
    }
}
