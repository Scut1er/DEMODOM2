using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Генерация карты выпуска в духе Slay the Spire:
    // 1) прокладываем N путей по сетке (ряд → ряд, сдвиг на дорожку вверх/вниз/прямо, без пересечений);
    // 2) все пути сходятся в монтаж;
    // 3) раздаём типы комнат по весам и правилам, затем конкретные комнаты из пула по весу и условиям.
    // Готовая карта сохраняется в EpisodeState, поэтому «Продолжить» не перегенерирует её.
    public static class MapGenerator
    {
        public const string MontageId = "montage";

        public static MapGraph Generate(RoomCatalog catalog, int seed, RuleContext ctx)
        {
            var config = catalog.Config;
            var rng = new System.Random(seed);
            int floors = Mathf.Max(2, config.floors);
            int lanes = Mathf.Max(1, config.lanes);
            int paths = Mathf.Max(1, config.paths);
            int last = floors - 2; // последний обычный ряд; floors-1 — монтаж

            var used = new bool[floors - 1, lanes];
            var edges = new List<Vector3Int>(); // x = ряд, y = дорожка откуда, z = дорожка куда

            // Один старт посередине — все пути расходятся из него.
            int start = (lanes - 1) / 2;
            int firstBranch = -1;
            for (int p = 0; p < paths; p++)
            {
                int lane = start;
                used[0, lane] = true;

                for (int f = 0; f < last; f++)
                {
                    // второй путь сразу уходит в другую сторону, чтобы со старта была развилка
                    int avoid = p == 1 && f == 0 ? firstBranch : -1;
                    int next = Step(rng, edges, f, lane, lanes, avoid);
                    if (p == 0 && f == 0)
                        firstBranch = next;
                    if (!edges.Contains(new Vector3Int(f, lane, next)))
                        edges.Add(new Vector3Int(f, lane, next));
                    lane = next;
                    used[f + 1, lane] = true;
                }
            }

            var graph = new MapGraph { Layers = floors, Lanes = lanes };
            var byCell = new MapNode[floors - 1, lanes];
            for (int f = 0; f <= last; f++)
            {
                for (int l = 0; l < lanes; l++)
                {
                    if (!used[f, l])
                        continue;
                    var node = new MapNode { id = "f" + f + "l" + l, layer = f, row = f == 0 ? 0f : Row(l, lanes) };
                    if (f > 0)
                        node.offset = new Vector2((float)(rng.NextDouble() - 0.5) * 24f, (float)(rng.NextDouble() - 0.5) * 24f);
                    byCell[f, l] = node;
                    graph.nodes.Add(node);
                }
            }

            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                var from = byCell[e.x, e.y];
                string to = byCell[e.x + 1, e.z].id;
                if (!from.next.Contains(to))
                    from.next.Add(to);
            }

            // Последний ряд — всегда монтаж.
            var montage = new MapNode { id = MontageId, type = RoomType.Montage, layer = floors - 1, row = 0f };
            SetRoom(montage, config.montage != null ? config.montage : First(catalog.Available(RoomType.Montage, ctx)) ?? catalog.Placeholder(RoomType.Montage));
            graph.nodes.Add(montage);
            for (int l = 0; l < lanes; l++)
            {
                if (used[last, l])
                    byCell[last, l].next.Add(MontageId);
            }

            AssignRooms(catalog, ctx, rng, graph, byCell, floors, lanes);
            return graph;
        }

        // Следующая дорожка: -1/0/+1 от текущей, без пересечения уже проложенных рёбер этого ряда.
        static int Step(System.Random rng, List<Vector3Int> edges, int floor, int lane, int lanes, int avoid = -1)
        {
            var options = new List<int>(3);
            for (int d = -1; d <= 1; d++)
            {
                int to = lane + d;
                if (to >= 0 && to < lanes)
                    options.Add(to);
            }

            for (int i = options.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (options[i], options[j]) = (options[j], options[i]);
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] != avoid && !Crosses(edges, floor, lane, options[i]))
                    return options[i];
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (!Crosses(edges, floor, lane, options[i]))
                    return options[i];
            }

            return lane;
        }

        static bool Crosses(List<Vector3Int> edges, int floor, int from, int to)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                if (e.x != floor)
                    continue;
                if ((e.y - from) * (e.z - to) < 0)
                    return true;
            }

            return false;
        }

        static float Row(int lane, int lanes)
        {
            if (lanes <= 1)
                return 0f;
            float half = (lanes - 1) * 0.5f;
            return (lane - half) / half;
        }

        static void AssignRooms(RoomCatalog catalog, RuleContext ctx, System.Random rng, MapGraph graph, MapNode[,] byCell, int floors, int lanes)
        {
            var config = catalog.Config;
            var pools = new Dictionary<RoomType, List<RoomDefinition>>
            {
                [RoomType.Situation] = catalog.Available(RoomType.Situation, ctx),
                [RoomType.Event] = catalog.Available(RoomType.Event, ctx),
                [RoomType.Marketing] = catalog.Available(RoomType.Marketing, ctx)
            };

            var parents = new Dictionary<string, List<MapNode>>();
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                var n = graph.nodes[i];
                for (int j = 0; j < n.next.Count; j++)
                {
                    if (!parents.TryGetValue(n.next[j], out var list))
                        parents[n.next[j]] = list = new List<MapNode>();
                    list.Add(n);
                }
            }

            // Комнаты, которые уже встречались на любом пути до узла, — для uniquePerEpisode.
            var ancestors = new Dictionary<string, HashSet<string>>();
            int last = floors - 2;
            for (int f = 0; f <= last; f++)
            {
                for (int l = 0; l < lanes; l++)
                {
                    var node = byCell[f, l];
                    if (node == null)
                        continue;
                    parents.TryGetValue(node.id, out var from);
                    var seen = new HashSet<string>();
                    if (from != null)
                    {
                        for (int i = 0; i < from.Count; i++)
                        {
                            seen.Add(from[i].roomId);
                            if (ancestors.TryGetValue(from[i].id, out var up))
                                seen.UnionWith(up);
                        }
                    }

                    ancestors[node.id] = seen;

                    RoomType type;
                    if (f == 0)
                        type = RoomType.Situation;
                    else if (f == config.guaranteedMarketingFloor && pools[RoomType.Marketing].Count > 0)
                        type = RoomType.Marketing;
                    else if (f < config.minSpecialFloor || (config.situationBeforeMontage && f == last))
                        type = RoomType.Situation;
                    else
                        type = PickType(config, rng, from, pools);

                    node.type = type;
                    var room = f == 0 && config.opening != null
                        ? config.opening
                        : PickRoom(rng, pools[type], from, seen) ?? catalog.Placeholder(type);
                    SetRoom(node, room);
                }
            }
        }

        static RoomType PickType(EpisodeMapConfig config, System.Random rng, List<MapNode> parents, Dictionary<RoomType, List<RoomDefinition>> pools)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                var type = Weighted(rng, config, pools);
                if (type == RoomType.Situation || !config.noRepeatSpecial || parents == null)
                    return type;
                bool repeat = false;
                for (int i = 0; i < parents.Count; i++)
                    repeat |= parents[i].type == type;
                if (!repeat)
                    return type;
            }

            return RoomType.Situation;
        }

        // Тип без единой подходящей комнаты не выпадает. Монтаж в средних рядах не бывает.
        static RoomType Weighted(System.Random rng, EpisodeMapConfig c, Dictionary<RoomType, List<RoomDefinition>> pools)
        {
            float s = Mathf.Max(0f, c.situationWeight);
            float e = pools[RoomType.Event].Count > 0 ? Mathf.Max(0f, c.eventWeight) : 0f;
            float m = pools[RoomType.Marketing].Count > 0 ? Mathf.Max(0f, c.marketingWeight) : 0f;
            float total = s + e + m;
            if (total <= 0f)
                return RoomType.Situation;
            float r = (float)rng.NextDouble() * total;
            if ((r -= s) < 0f) return RoomType.Situation;
            if ((r -= e) < 0f) return RoomType.Event;
            return RoomType.Marketing;
        }

        // По весу; сначала без повтора на пути (unique) и без повтора родителя, потом правила ослабляются.
        static RoomDefinition PickRoom(System.Random rng, List<RoomDefinition> pool, List<MapNode> parents, HashSet<string> seen)
        {
            if (pool == null || pool.Count == 0)
                return null;
            var strict = new List<RoomDefinition>();
            var loose = new List<RoomDefinition>();
            for (int i = 0; i < pool.Count; i++)
            {
                var r = pool[i];
                bool parentSame = false;
                if (parents != null)
                {
                    for (int j = 0; j < parents.Count; j++)
                        parentSame |= parents[j].roomId == r.Id;
                }

                if (parentSame)
                    continue;
                loose.Add(r);
                if (!(r.uniquePerEpisode && seen.Contains(r.Id)))
                    strict.Add(r);
            }

            var from = strict.Count > 0 ? strict : loose.Count > 0 ? loose : pool;
            return WeightedRoom(rng, from);
        }

        static RoomDefinition WeightedRoom(System.Random rng, List<RoomDefinition> rooms)
        {
            float total = 0f;
            for (int i = 0; i < rooms.Count; i++)
                total += Mathf.Max(0f, rooms[i].weight);
            if (total <= 0f)
                return rooms[rng.Next(rooms.Count)];
            float r = (float)rng.NextDouble() * total;
            for (int i = 0; i < rooms.Count; i++)
            {
                r -= Mathf.Max(0f, rooms[i].weight);
                if (r < 0f)
                    return rooms[i];
            }

            return rooms[rooms.Count - 1];
        }

        static RoomDefinition First(List<RoomDefinition> rooms)
        {
            return rooms.Count > 0 ? rooms[0] : null;
        }

        static void SetRoom(MapNode node, RoomDefinition room)
        {
            node.room = room;
            node.roomId = room != null ? room.Id : "";
        }
    }
}
