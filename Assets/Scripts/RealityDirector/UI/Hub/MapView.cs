using System;
using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Карта выпуска: комнаты, связи, план съёмки и выбор следующей комнаты.
    public class MapView : MonoBehaviour
    {
        [Header("Шапка")]
        [SerializeField] Text subtitle;
        [SerializeField] Text goal;
        [SerializeField] StatsView stats;

        [Header("Доска")]
        [SerializeField] RectTransform board;
        [SerializeField] RectTransform edgesRoot;
        [SerializeField] RectTransform nodesRoot;
        [SerializeField] MapNodeView nodePrefab;
        [Tooltip("Раскладывать узлы по размеру доски (под любое число рядов и дорожек).")]
        [SerializeField] bool autoFit = true;
        [SerializeField] Vector2 boardMargin = new Vector2(130f, 90f);
        [Tooltip("Ручная раскладка, если autoFit выключен: центр колонки 0 и шаги, в пикселях доски.")]
        [SerializeField] Vector2 origin = new Vector2(-660f, 0f);
        [SerializeField] float layerStep = 265f;
        [SerializeField] float rowStep = 175f;
        [Tooltip("Размер карточки узла в префабе — для уменьшения при плотной карте.")]
        [SerializeField] Vector2 nodeSize = new Vector2(220f, 150f);
        [SerializeField] float edgeWidth = 6f;
        [SerializeField] Color routeColor = new Color(0.98f, 0.8f, 0.3f, 1f);
        [SerializeField] Color openColor = new Color(0.98f, 0.38f, 0.62f, 1f);
        [SerializeField] Color idleColor = new Color(1f, 1f, 1f, 0.22f);

        [Header("План и инфо")]
        [SerializeField] Text plan;
        [SerializeField] Text infoTitle;
        [SerializeField] Text infoBody;
        [SerializeField] Button shoot;
        [SerializeField] Text shootLabel;
        [Tooltip("«← В хаб»: выпуск не прерывается, вернуться — кнопкой старта в хабе.")]
        [SerializeField] Button back;

        public event Action<string> Select;
        public event Action Shoot;
        public event Action Back;

        void Awake()
        {
            if (shoot != null)
                shoot.onClick.AddListener(() => Shoot?.Invoke());
            if (back != null)
            {
                back.onClick.AddListener(() => Back?.Invoke());
                var label = back.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.text = "←  В ХАБ";
            }

            Dress();
        }

        // Карта — доска продюсера в студии (пак EpisodeMap): панели в рамках, путь золотом, доступное — огнём.
        void Dress()
        {
            if (UiKit.Backdrop((RectTransform)transform, "Art/UI/EpisodeMap/map_background", new Rect(0f, 0f, 1f, 1f), 0.3f) == null)
                UiKit.Backdrop((RectTransform)transform, "Art/Intro/bg/scene_1", new Rect(0f, 0.3f, 1f, 0.7f), 0.72f);
            var self = GetComponent<Image>();
            if (self != null)
                self.color = UiKit.Ink;
            if (board != null)
            {
                var plate = board.GetComponent<Image>();
                UiKit.DressSolid(plate, UiKit.Frame.Dialog, 8f);
                var solid = plate != null ? plate.transform.Find("Solid")?.GetComponent<Image>() : null;
                // Доска с фона просвечивает: узлы лежат на ней, линии маршрута читаются.
                if (solid != null)
                    solid.color = new Color(0.06f, 0.03f, 0.05f, 0.42f);
            }

            UiKit.DressSolid(transform.Find("TitlePanel")?.GetComponent<Image>(), UiKit.Frame.Dialog);
            UiKit.DressSolid(transform.Find("PlanPanel")?.GetComponent<Image>(), UiKit.Frame.Dialog);
            UiKit.DressSolid(transform.Find("InfoPanel")?.GetComponent<Image>(), UiKit.Frame.Dialog);
            // Рамка пака толще прежней плашки — тексты отодвигаем от кромки.
            if (plan != null)
            {
                plan.rectTransform.offsetMin = new Vector2(30f, 14f);
                plan.rectTransform.offsetMax = new Vector2(-30f, -16f);
            }

            if (infoTitle != null)
                infoTitle.rectTransform.anchoredPosition = new Vector2(34f, -18f);
            if (infoBody != null)
                infoBody.rectTransform.anchoredPosition = new Vector2(34f, -54f);
            UiKit.Primary(shoot, 22);
            UiKit.Pulse(shoot);
            UiKit.Secondary(back);
            if (stats != null)
                stats.Dress();
            routeColor = UiKit.Gold;
            openColor = UiKit.Ember;
            idleColor = new Color(1f, 1f, 1f, 0.16f);
            edgeWidth = Mathf.Max(edgeWidth, 7f);
            var title = transform.Find("TitlePanel/Title")?.GetComponent<Text>();
            if (title != null && UiKit.Display != null)
            {
                title.font = UiKit.Display;
                UiKit.Shadow(title);
            }
        }

        readonly List<MapNodeView> _spawned = new List<MapNodeView>();
        readonly List<bool> _spawnedLive = new List<bool>();
        bool _shootCan;

        public RectTransform BoardFocus => board;
        public RectTransform EnterFocus => shoot != null ? shoot.transform as RectTransform : null;
        public RectTransform PlanFocus => plan != null ? plan.rectTransform : BoardFocus;
        public RectTransform InfoFocus => infoBody != null ? infoBody.rectTransform : BoardFocus;
        public RectTransform StatsFocus => stats != null ? stats.transform as RectTransform : null;
        readonly Dictionary<RoomType, RectTransform> _nodeFocus = new Dictionary<RoomType, RectTransform>();

        public RectTransform NodeFocus(RoomType type)
        {
            return _nodeFocus.TryGetValue(type, out var rect) ? rect : null;
        }

        public void Show(MapService map, MapNode selected, StatsModel statsModel, int episodeNumber, int seasonLength, IList<string> tasks)
        {
            if (back != null)
                back.gameObject.SetActive(true);
            if (subtitle != null)
                subtitle.text = "Выпуск " + episodeNumber + " из " + seasonLength + "  ·  сцена " + Mathf.Min(map.Step + 1, map.Map.Layers)
                                + " из " + map.Map.Layers + "  ·  в конце — монтаж";
            Layout(map.Map);
            if (stats != null)
                stats.Show(statsModel);

            var focus = selected ?? map.CurrentChoice;
            string goalText = focus != null && !string.IsNullOrEmpty(focus.goal)
                ? focus.goal
                : "Пройти выпуск до монтажа и набрать материала на сильный эфир.";
            if (goal != null)
            {
                goal.gameObject.SetActive(true);
                var header = goal.transform.parent != null ? goal.transform.parent.Find("GoalHeader") : null;
                if (header != null)
                    header.gameObject.SetActive(true);
                var titlePanel = goal.transform.parent as RectTransform;
                if (titlePanel != null && titlePanel.sizeDelta.y < 180f)
                    titlePanel.sizeDelta = new Vector2(titlePanel.sizeDelta.x, 180f);
                if (header is RectTransform headerRect)
                {
                    headerRect.anchoredPosition = new Vector2(24f, -98f);
                    headerRect.sizeDelta = new Vector2(472f, 22f);
                }

                var goalRect = goal.rectTransform;
                goalRect.anchoredPosition = new Vector2(24f, -120f);
                goalRect.sizeDelta = new Vector2(472f, 40f);
                goal.horizontalOverflow = HorizontalWrapMode.Wrap;
                goal.verticalOverflow = VerticalWrapMode.Truncate;
                goal.text = goalText;
            }

            DrawEdges(map);
            DrawNodes(map, selected);
            ShowPlan(goalText, tasks);
            ShowInfo(map, selected);
        }

        Vector2 _origin;
        float _layerStep;
        float _rowStep;
        float _nodeScale = 1f;

        // Подгоняет шаги сетки и размер карточек под доску.
        void Layout(MapGraph graph)
        {
            _origin = origin;
            _layerStep = layerStep;
            _rowStep = rowStep;
            _nodeScale = 1f;
            if (!autoFit || board == null || graph.Layers < 2)
                return;

            Vector2 size = board.rect.size;
            float width = Mathf.Max(100f, size.x - boardMargin.x * 2f);
            float height = Mathf.Max(100f, size.y - boardMargin.y * 2f);
            _layerStep = width / (graph.Layers - 1);
            _rowStep = height * 0.5f;
            _origin = new Vector2(-width * 0.5f, 0f);
            float laneGap = graph.Lanes > 1 ? height / (graph.Lanes - 1) : height;
            _nodeScale = Mathf.Clamp(Mathf.Min(_layerStep * 0.82f / nodeSize.x, laneGap * 0.88f / nodeSize.y), 0.45f, 1f);
        }

        Vector2 PositionOf(MapNode node)
        {
            return _origin + new Vector2(node.layer * _layerStep, -node.row * _rowStep) + node.offset * _nodeScale;
        }

        void DrawNodes(MapService map, MapNode selected)
        {
            Clear(nodesRoot);
            _spawned.Clear();
            _spawnedLive.Clear();
            _nodeFocus.Clear();
            if (nodePrefab == null)
                return;
            var nodes = map.Map.nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var view = Instantiate(nodePrefab, nodesRoot);
                view.name = "Node_" + node.id;
                ((RectTransform)view.transform).anchoredPosition = PositionOf(node);
                view.transform.localScale = Vector3.one * _nodeScale;
                string id = node.id;
                var state = map.StateOf(node);
                view.Show(node, state, map.LockReason(node), selected == node, () => Select?.Invoke(id));
                bool live = state == MapNodeState.Available || state == MapNodeState.Current;
                _spawned.Add(view);
                _spawnedLive.Add(live);
                if (!_nodeFocus.ContainsKey(node.type))
                    _nodeFocus[node.type] = (RectTransform)view.transform;
            }
        }

        void DrawEdges(MapService map)
        {
            Clear(edgesRoot);
            var nodes = map.Map.nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                var from = nodes[i];
                for (int j = 0; j < from.next.Count; j++)
                {
                    var to = map.Map.Find(from.next[j]);
                    if (to == null)
                        continue;
                    var state = map.EdgeState(from, to);
                    var color = state == MapEdgeState.Route ? routeColor : state == MapEdgeState.Open ? openColor : idleColor;
                    Line(PositionOf(from), PositionOf(to), color, state == MapEdgeState.Idle ? edgeWidth * 0.6f : edgeWidth);
                }
            }
        }

        void Line(Vector2 a, Vector2 b, Color color, float width)
        {
            var go = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(edgesRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Vector2 d = b - a;
            rect.anchoredPosition = (a + b) * 0.5f;
            rect.sizeDelta = new Vector2(d.magnitude, width);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        // Цель выпуска и задачи зрителей. Пройденный путь видно на самой карте.
        void ShowPlan(string goalText, IList<string> tasks)
        {
            if (plan == null)
                return;
            plan.gameObject.SetActive(true);
            plan.resizeTextForBestFit = false;
            plan.horizontalOverflow = HorizontalWrapMode.Wrap;
            plan.verticalOverflow = VerticalWrapMode.Truncate;
            var planRect = plan.rectTransform;
            planRect.offsetMin = new Vector2(28f, 16f);
            planRect.offsetMax = new Vector2(-28f, -16f);
            var planPanel = plan.transform.parent as RectTransform;
            if (planPanel != null && planPanel.sizeDelta.y < 148f)
                planPanel.sizeDelta = new Vector2(planPanel.sizeDelta.x, 148f);
            var body = "<color=#F2C94C><b>ЦЕЛЬ ВЫПУСКА</b></color>\n" + goalText;
            if (tasks != null && tasks.Count > 0)
            {
                body += "\n\n<color=#F2C94C><b>ЗАДАЧИ ЗРИТЕЛЕЙ</b></color>";
                for (int i = 0; i < tasks.Count; i++)
                    body += "\n○  " + tasks[i];
            }

            plan.text = body;
        }

        void ShowInfo(MapService map, MapNode selected)
        {
            bool can = map.CanEnter(selected);
            if (selected == null)
            {
                if (infoTitle != null)
                    infoTitle.text = "Выбери следующую комнату";
                if (infoBody != null)
                    infoBody.text = "Нажми на светящуюся комнату — здесь появится, что в ней будет. Комната двигает "
                        + MoodStyle.Paint("драму", ShowMood.Drama) + ", "
                        + MoodStyle.Paint("трэш", ShowMood.Trash) + " или "
                        + MoodStyle.Paint("семью", ShowMood.Family) + ".\n"
                        + "<color=#FF7329>●</color> можно идти   <color=#7DDB9E>✓</color> снято   <color=#9E8F85>□</color> впереди   <color=#5A5560>■</color> путь закрыт   ·   в конце — монтаж";
            }
            else
            {
                var state = map.StateOf(selected);
                string effect = map.EffectText(selected);
                string body = selected.description;
                if (!string.IsNullOrEmpty(effect))
                    body += "\n" + effect;
                string reason = map.LockReason(selected);
                if (state == MapNodeState.Locked && reason != null)
                    body += "\n" + reason;
                else if (state == MapNodeState.Future)
                    body += "\nОткроется позже.";
                else if (state == MapNodeState.Done)
                    body += selected.type == RoomType.Situation ? "\nУже снято." : "\nУже пройдено.";
                else if (state == MapNodeState.Skipped)
                    body += "\nЭтот путь уже не пройти.";
                if (infoTitle != null)
                    infoTitle.text = Capital(selected.title) + "  ·  " + selected.subtitle;
                if (infoBody != null)
                    infoBody.text = body;
            }

            _shootCan = can;
            if (shoot != null)
                shoot.interactable = can;
            if (shootLabel != null)
                shootLabel.text = !can ? (selected == null ? "ВЫБЕРИ КОМНАТУ" : "НЕДОСТУПНО") : ActionLabel(selected.type);
        }

        // Сообщение в инфо-панели поверх обычного текста (например, «событие пока не реализовано»).
        public void ApplyTutorial(bool teach, bool talking)
        {
            if (back != null)
                back.interactable = !teach;
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] == null)
                    continue;
                _spawned[i].SetEnabled(!teach || (!talking && _spawnedLive[i]));
            }

            if (shoot != null)
                shoot.interactable = teach && talking ? false : _shootCan;
        }

        public void Notice(string title, string body)
        {
            if (infoTitle != null)
                infoTitle.text = title;
            if (infoBody != null)
                infoBody.text = body;
        }

        static string ActionLabel(RoomType type)
        {
            switch (type)
            {
                case RoomType.Marketing: return "К СПОНСОРАМ";
                case RoomType.Event: return "ПРОЙТИ";
                case RoomType.Montage: return "МОНТАЖ";
                default: return "СНИМАТЬ";
            }
        }

        static string Capital(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return s.Substring(0, 1) + s.Substring(1).ToLowerInvariant();
        }

        static void Clear(RectTransform root)
        {
            if (root == null)
                return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var old = root.GetChild(i).gameObject;
                old.SetActive(false);
                Destroy(old);
            }
        }
    }
}
