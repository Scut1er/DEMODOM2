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

    // Участники для экрана хаба. Пока в квартире живут двое — остальных добавим вместе с артом.
    public static class CastRoster
    {
        public const int MaxSeats = 6;

        public static int Seats(int castLevel)
        {
            return Mathf.Clamp(castLevel + 1, 2, MaxSeats);
        }

        public static CastMember[] All()
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
