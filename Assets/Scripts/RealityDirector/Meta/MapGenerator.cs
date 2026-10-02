using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Генерация карты сезона в духе Slay the Spire:
    // 1) прокладываем N путей по сетке (ряд → ряд, сдвиг на дорожку вверх/вниз/прямо, без пересечений);
    // 2) все пути сходятся в финал;
    // 3) раздаём типы узлов по весам и правилам, съёмкам — случайный «вкус».
    // Один и тот же seed + конфиг = одна и та же карта.
    public static class MapGenerator
    {
        public const string FinaleId = "finale";

        public static MapGraph Generate(SeasonMapConfig config, int seed)
        {
            var rng = new System.Random(seed);
            int floors = Mathf.Max(2, config.floors);
            int lanes = Mathf.Max(1, config.lanes);
            int paths = Mathf.Max(1, config.paths);
            int last = floors - 2; // последний обычный ряд; floors-1 — финал

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

            // Финал сезона — всегда монтаж.
            var finale = config.finale != null ? config.finale.Clone() : new MapNode();
            finale.type = MapNodeType.Editing;
            finale.id = FinaleId;
            finale.layer = floors - 1;
            finale.row = 0f;
            graph.nodes.Add(finale);
            for (int l = 0; l < lanes; l++)
            {
                if (used[last, l])
                    byCell[last, l].next.Add(FinaleId);
            }

            AssignTypes(config, rng, graph, byCell, floors, lanes);
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

        static void AssignTypes(SeasonMapConfig config, System.Random rng, MapGraph graph, MapNode[,] byCell, int floors, int lanes)
        {
            // родители узла — для правил «не повторять подряд» и разнообразия вкусов
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

            int last = floors - 2;
            for (int f = 0; f <= last; f++)
            {
                for (int l = 0; l < lanes; l++)
                {
                    var node = byCell[f, l];
                    if (node == null)
                        continue;
                    parents.TryGetValue(node.id, out var from);
                    MapNodeType type;
                    if (f == 0)
                        type = MapNodeType.Filming;
                    else if (f == config.guaranteedShopFloor)
                        type = MapNodeType.Shop;
                    else if (f < config.minSpecialFloor || (config.filmingBeforeFinale && f == last))
                        type = MapNodeType.Filming;
                    else
                        type = PickType(config, rng, from);

                    MapNode template = f == 0 && config.opening != null
                        ? config.opening
                        : TemplateFor(config, rng, type, from);
                    Fill(node, template, type);
                }
            }
        }

        static MapNodeType PickType(SeasonMapConfig config, System.Random rng, List<MapNode> parents)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                var type = Weighted(rng, config);
                if (type == MapNodeType.Filming || !config.noRepeatSpecial || parents == null)
                    return type;
                bool repeat = false;
                for (int i = 0; i < parents.Count; i++)
                {
                    if (parents[i].type == type && type == MapNodeType.Shop)
                        repeat = true;
                }

                if (!repeat)
                    return type;
            }

            return MapNodeType.Filming;
        }

        static MapNodeType Weighted(System.Random rng, SeasonMapConfig c)
        {
            // Монтаж в случайных рядах не бывает — это всегда финал.
            float total = Mathf.Max(0f, c.filmingWeight) + Mathf.Max(0f, c.randomEventWeight) + Mathf.Max(0f, c.shopWeight);
            if (total <= 0f)
                return MapNodeType.Filming;
            float r = (float)rng.NextDouble() * total;
            if ((r -= Mathf.Max(0f, c.filmingWeight)) < 0f) return MapNodeType.Filming;
            if ((r -= Mathf.Max(0f, c.randomEventWeight)) < 0f) return MapNodeType.RandomEvent;
            return MapNodeType.Shop;
        }

        static MapNode TemplateFor(SeasonMapConfig config, System.Random rng, MapNodeType type, List<MapNode> parents)
        {
            switch (type)
            {
                case MapNodeType.Shop: return config.shop;
                case MapNodeType.RandomEvent: return config.randomEvent;
                case MapNodeType.Editing: return config.finale;
            }

            var flavors = config.filmingFlavors;
            if (flavors == null || flavors.Count == 0)
                return config.opening;
            // пробуем не повторять вкус родителя
            MapNode pick = null;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                pick = WeightedFlavor(rng, flavors);
                bool same = false;
                if (parents != null)
                {
                    for (int i = 0; i < parents.Count; i++)
                        same |= parents[i].title == pick.title;
                }

                if (!same)
                    break;
            }

            return pick;
        }

        static MapNode WeightedFlavor(System.Random rng, List<MapNode> flavors)
        {
            float total = 0f;
            for (int i = 0; i < flavors.Count; i++)
                total += Mathf.Max(0f, flavors[i].weight);
            if (total <= 0f)
                return flavors[rng.Next(flavors.Count)];
            float r = (float)rng.NextDouble() * total;
            for (int i = 0; i < flavors.Count; i++)
            {
                r -= Mathf.Max(0f, flavors[i].weight);
                if (r < 0f)
                    return flavors[i];
            }

            return flavors[flavors.Count - 1];
        }

        // Копирует содержимое шаблона в узел, сохраняя позицию и связи.
        static void Fill(MapNode node, MapNode template, MapNodeType type)
        {
            if (template != null)
            {
                node.title = template.title;
                node.subtitle = template.subtitle;
                node.description = template.description;
                node.goal = template.goal;
                node.kind = template.kind;
                node.color = template.color;
                node.art = template.art;
                node.lockTrack = template.lockTrack;
                node.lockLevel = template.lockLevel;
                node.budget = template.budget;
                node.mood = template.mood;
                node.toneGain = template.toneGain;
            }

            node.type = type;
        }
    }
}
