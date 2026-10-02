using System.Collections.Generic;

namespace RealityDirector.Meta
{
    public enum MapNodeState
    {
        Future,
        Available,
        Locked,
        Current,
        Done,
        Skipped
    }

    public enum MapEdgeState
    {
        Idle,
        Open,
        Route
    }

    // Правила карты выпуска: что пройдено, куда можно пойти, что закрыто.
    // Шаг (episode.step) = номер текущего ряда. route[i] — узел, выбранный в ряду i.
    public class MapService
    {
        readonly EpisodeState _episode;
        readonly RuleContext _ctx;
        readonly MapGraph _map;

        public MapGraph Map => _map;
        public EpisodeState Episode => _episode;
        public int Step => _episode.step;

        public MapService(RuleContext ctx, MapGraph map)
        {
            _ctx = ctx;
            _episode = ctx.episode;
            _map = map;
        }

        // Узел текущего ряда, уже выбранный, но ещё не пройденный (например, съёмка не досмотрена).
        public MapNode CurrentChoice =>
            _episode.route.Count > _episode.step ? _map.Find(_episode.route[_episode.step]) : null;

        public MapNodeState StateOf(MapNode node)
        {
            int index = _episode.route.IndexOf(node.id);
            if (index >= 0)
                return index < _episode.step ? MapNodeState.Done : MapNodeState.Current;
            if (node.layer < _episode.step)
                return MapNodeState.Skipped;
            if (node.layer > _episode.step)
                return MapNodeState.Future;
            if (CurrentChoice != null || !Reachable(node))
                return MapNodeState.Skipped;
            if (LockReason(node) == null || NoOpenNodes())
                return MapNodeState.Available;
            return MapNodeState.Locked;
        }

        // Условия комнаты проверяются ещё раз на входе: флаги из прошлых комнат могут её закрыть.
        public string LockReason(MapNode node)
        {
            if (node.room == null)
                return null;
            return Rules.FailText(Rules.FirstFailed(node.room.conditions, _ctx));
        }

        public MapEdgeState EdgeState(MapNode from, MapNode to)
        {
            int a = _episode.route.IndexOf(from.id);
            int b = _episode.route.IndexOf(to.id);
            if (a >= 0 && b == a + 1)
                return MapEdgeState.Route;
            if (a >= 0 && a == _episode.step - 1)
            {
                var s = StateOf(to);
                if (s == MapNodeState.Available || s == MapNodeState.Current)
                    return MapEdgeState.Open;
            }

            return MapEdgeState.Idle;
        }

        public List<MapNode> Available()
        {
            var list = new List<MapNode>();
            for (int i = 0; i < _map.nodes.Count; i++)
            {
                if (StateOf(_map.nodes[i]) == MapNodeState.Available)
                    list.Add(_map.nodes[i]);
            }

            return list;
        }

        public bool CanEnter(MapNode node)
        {
            if (node == null)
                return false;
            var s = StateOf(node);
            return s == MapNodeState.Available || s == MapNodeState.Current;
        }

        // Фиксирует выбор, пишет историю и применяет эффекты входа. Повторный выбор того же узла их не дублирует.
        public bool Choose(MapNode node)
        {
            if (node == null)
                return false;
            var current = CurrentChoice;
            if (current != null)
                return current == node;
            if (StateOf(node) != MapNodeState.Available)
                return false;

            while (_episode.route.Count < _episode.step)
                _episode.route.Add("");
            _episode.route.Add(node.id);
            _episode.history.Add(new RoomVisit { step = _episode.step, nodeId = node.id, roomId = node.roomId, type = node.type });
            if (node.room != null)
                Rules.Apply(node.room.onEnter, _ctx);
            return true;
        }

        // Закрывает текущую комнату.
        public void CompleteStep()
        {
            if (CurrentChoice != null)
                _episode.step++;
        }

        public bool EpisodeDone => _episode.Finished;

        public string EffectText(MapNode node)
        {
            return node.room != null ? Rules.Describe(node.room.onEnter) : "";
        }

        bool Reachable(MapNode node)
        {
            if (_episode.step <= 0)
                return node.layer == 0;
            int prevIndex = _episode.step - 1;
            var prev = prevIndex < _episode.route.Count ? _map.Find(_episode.route[prevIndex]) : null;
            // Нет записи о прошлом шаге — открыт весь ряд.
            return prev == null || prev.next.Contains(node.id);
        }

        // Защита от тупика: если все достижимые узлы закрыты, открываем их.
        bool NoOpenNodes()
        {
            for (int i = 0; i < _map.nodes.Count; i++)
            {
                var n = _map.nodes[i];
                if (n.layer == _episode.step && Reachable(n) && LockReason(n) == null)
                    return false;
            }

            return true;
        }

        public static string TypeName(RoomType type)
        {
            switch (type)
            {
                case RoomType.Marketing: return "маркетинг";
                case RoomType.Event: return "событие";
                case RoomType.Montage: return "монтаж";
                default: return "съёмка";
            }
        }
    }
}
