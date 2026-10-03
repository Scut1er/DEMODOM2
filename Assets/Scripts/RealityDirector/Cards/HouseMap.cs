using UnityEngine;

namespace RealityDirector.Cards
{
    public enum Room
    {
        None,
        Living,
        Kitchen,
        Bedroom,
        Bathroom
    }

    // Геометрия квартиры для карт: комнаты, двери, куда можно поставить реквизит и куда идти.
    // Числа — из PitchFlow.BuildApartment (полы, стены и проёмы).
    public static class HouseMap
    {
        // Где можно стоять и ставить вещи (с отступом от стен и задней стены).
        static readonly Rect Living = Rect.MinMaxRect(-7.35f, 0.75f, -3.15f, 4.05f);
        static readonly Rect Kitchen = Rect.MinMaxRect(-2.45f, 0.75f, 2.45f, 4.05f);
        static readonly Rect Bedroom = Rect.MinMaxRect(3.2f, 0.75f, 7.6f, 4.05f);
        static readonly Rect Bathroom = Rect.MinMaxRect(-2.05f, 5.4f, 2.15f, 7.4f);

        public static Rect Walk(Room room)
        {
            switch (room)
            {
                case Room.Living: return Living;
                case Room.Kitchen: return Kitchen;
                case Room.Bedroom: return Bedroom;
                case Room.Bathroom: return Bathroom;
                default: return Kitchen;
            }
        }

        // Комната целиком (стены включительно) — для запертой двери.
        public static Rect Whole(Room room)
        {
            switch (room)
            {
                case Room.Living: return Rect.MinMaxRect(-7.6f, 0.35f, -2.95f, 4.6f);
                case Room.Kitchen: return Rect.MinMaxRect(-2.6f, 0.35f, 2.6f, 4.6f);
                case Room.Bedroom: return Rect.MinMaxRect(3.0f, 0.35f, 7.85f, 4.6f);
                case Room.Bathroom: return Rect.MinMaxRect(-2.3f, 4.95f, 2.4f, 8.0f);
                default: return Kitchen;
            }
        }

        public static Room At(Vector2 p, bool bedOpen, bool bathOpen)
        {
            if (p.y > 4.9f)
                return bathOpen && p.x > -2.4f && p.x < 2.5f && p.y < 8.2f ? Room.Bathroom : Room.None;
            if (p.y < 0.2f || p.y > 4.9f)
                return Room.None;
            if (p.x < -7.65f)
                return Room.None;
            if (p.x < -2.85f)
                return Room.Living;
            if (p.x < 2.75f)
                return Room.Kitchen;
            if (p.x < 7.9f)
                return bedOpen ? Room.Bedroom : Room.None;
            return Room.None;
        }

        public static string Name(Room room)
        {
            switch (room)
            {
                case Room.Living: return "гостиная";
                case Room.Kitchen: return "кухня";
                case Room.Bedroom: return "спальня";
                case Room.Bathroom: return "ванная";
                default: return "дом";
            }
        }

        public static Vector2 Center(Room room)
        {
            return Walk(room).center;
        }

        // Точка у двери изнутри комнаты — туда ломятся запертые.
        public static Vector2 Door(Room room)
        {
            switch (room)
            {
                case Room.Living: return new Vector2(-3.3f, 2.05f);
                case Room.Kitchen: return new Vector2(-2.3f, 2.05f);
                case Room.Bedroom: return new Vector2(3.35f, 2.05f);
                case Room.Bathroom: return new Vector2(0.05f, 5.5f);
                default: return new Vector2(0f, 2f);
            }
        }

        // Все проёмы комнаты — на них вешается замок.
        public static Vector2[] Doorways(Room room, bool bedOpen, bool bathOpen)
        {
            switch (room)
            {
                case Room.Living: return new[] { new Vector2(-2.85f, 2.05f) };
                case Room.Kitchen:
                {
                    var list = new System.Collections.Generic.List<Vector2> { new Vector2(-2.85f, 2.05f) };
                    if (bedOpen)
                        list.Add(new Vector2(2.75f, 2.05f));
                    if (bathOpen)
                        list.Add(new Vector2(0.05f, 5.0f));
                    return list.ToArray();
                }
                case Room.Bedroom: return new[] { new Vector2(2.75f, 2.05f) };
                case Room.Bathroom: return new[] { new Vector2(0.05f, 5.0f) };
                default: return new Vector2[0];
            }
        }

        public static Vector2 RandomPoint(Room room)
        {
            var r = Walk(room);
            return new Vector2(Random.Range(r.xMin + 0.3f, r.xMax - 0.3f), Random.Range(r.yMin + 0.2f, r.yMax - 0.5f));
        }

        public static Vector2 Clamp(Room room, Vector2 p)
        {
            var r = Walk(room);
            return new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax));
        }

        // Вход «с улицы» — гости заходят из прихожей слева.
        public static Vector2 FrontDoor => new Vector2(-7.9f, 1.55f);
    }
}
