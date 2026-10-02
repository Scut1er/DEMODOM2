using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

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

    // Правила карты сезона: что пройдено, куда можно пойти, что закрыто.
    // Шаг (state.step) = номер текущего ряда. route[i] — узел, выбранный в ряду i.
    public class MapService
    {
        readonly SeasonState _state;
        readonly SeasonTone _tone;
        readonly MapGraph _map;

        public MapGraph Map => _map;
        public int Step => _state.step;

        public MapService(SeasonState state, SeasonTone tone, MapGraph map)
        {
            _state = state;
            _tone = tone;
            _map = map;
        }

        // Узел текущего ряда, уже выбранный, но ещё не пройденный (например, съёмка не досмотрена).
        public MapNode CurrentChoice =>
            _state.route.Count > _state.step ? _map.Find(_state.route[_state.step]) : null;

        public MapNodeState StateOf(MapNode node)
        {
            int index = _state.route.IndexOf(node.id);
            if (index >= 0)
                return index < _state.step ? MapNodeState.Done : MapNodeState.Current;
            if (node.layer < _state.step)
                return MapNodeState.Skipped;
            if (node.layer > _state.step)
                return MapNodeState.Future;
            if (CurrentChoice != null || !Reachable(node))
                return MapNodeState.Skipped;
            if (LockReason(node) == null || NoOpenNodes())
                return MapNodeState.Available;
            return MapNodeState.Locked;
        }

        public string LockReason(MapNode node)
        {
            if (node.lockLevel <= 0)
                return null;
            if (Level(node.lockTrack) >= node.lockLevel)
                return null;
            return "Нужно: " + TrackName(node.lockTrack) + " ур. " + node.lockLevel;
        }

        public MapEdgeState EdgeState(MapNode from, MapNode to)
        {
            int a = _state.route.IndexOf(from.id);
            int b = _state.route.IndexOf(to.id);
            if (a >= 0 && b == a + 1)
                return MapEdgeState.Route;
            if (a >= 0 && a == _state.step - 1)
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

        // Фиксирует выбор и применяет эффект узла. Повторный выбор того же узла эффект не дублирует.
        public bool Choose(MapNode node)
        {
            if (node == null)
                return false;
            var current = CurrentChoice;
            if (current != null)
                return current == node;
            if (StateOf(node) != MapNodeState.Available)
                return false;

            while (_state.route.Count < _state.step)
                _state.route.Add("");
            _state.route.Add(node.id);
            _state.money = Mathf.Max(0, _state.money + node.budget);
            if (node.toneGain > 0 && _tone != null)
                _tone.Add(node.mood, node.toneGain);
            return true;
        }

        // Закрывает текущий шаг (магазин, событие, монтаж). Съёмку закрывает квартира после отзывов.
        public void CompleteStep()
        {
            if (CurrentChoice != null)
                _state.step++;
        }

        public bool SeasonDone => _state.step >= _map.Layers;

        public string EffectText(MapNode node)
        {
            var parts = new List<string>();
            if (node.budget != 0)
                parts.Add("бюджет " + (node.budget > 0 ? "+" : "") + node.budget + " кр");
            if (node.toneGain > 0)
                parts.Add(MoodStyle.Paint(MoodStyle.Short(node.mood) + " +" + node.toneGain, node.mood));
            return parts.Count == 0 ? "" : string.Join("   ·   ", parts);
        }

        bool Reachable(MapNode node)
        {
            if (_state.step <= 0)
                return node.layer == 0;
            int prevIndex = _state.step - 1;
            var prev = prevIndex < _state.route.Count ? _map.Find(_state.route[prevIndex]) : null;
            // Нет записи о прошлом шаге (старый сейв / старт из квартиры) — открыт весь ряд.
            return prev == null || prev.next.Contains(node.id);
        }

        // Защита от тупика: если все достижимые узлы закрыты, открываем их.
        bool NoOpenNodes()
        {
            for (int i = 0; i < _map.nodes.Count; i++)
            {
                var n = _map.nodes[i];
                if (n.layer == _state.step && Reachable(n) && LockReason(n) == null)
                    return false;
            }

            return true;
        }

        int Level(CrewTrack track)
        {
            switch (track)
            {
                case CrewTrack.Cast: return _state.castLevel;
                case CrewTrack.Operators: return _state.operatorLevel;
                default: return _state.writerLevel;
            }
        }

        public static string TrackName(CrewTrack track)
        {
            switch (track)
            {
                case CrewTrack.Cast: return "Кастинг";
                case CrewTrack.Operators: return "Съёмочная";
                default: return "Сценарная";
            }
        }

        public static string TypeName(MapNodeType type)
        {
            switch (type)
            {
                case MapNodeType.Shop: return "магазин";
                case MapNodeType.RandomEvent: return "случайное событие";
                case MapNodeType.Editing: return "монтаж";
                default: return "съёмка";
            }
        }
    }
}
