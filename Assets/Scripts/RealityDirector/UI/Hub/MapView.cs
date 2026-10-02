using System;
using System.Collections.Generic;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Карта сезона: узлы-выпуски, связи, план съёмки и выбор следующей сцены.
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
        [Tooltip("Центр колонки 0 и шаг между колонками/строками, в пикселях доски.")]
        [SerializeField] Vector2 origin = new Vector2(-660f, 0f);
        [SerializeField] float layerStep = 265f;
        [SerializeField] float rowStep = 175f;
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
        [SerializeField] Button back;
        [SerializeField] Button random;

        public event Action<string> Select;
        public event Action Shoot;
        public event Action Back;
        public event Action Random;

        void Awake()
        {
            if (shoot != null)
                shoot.onClick.AddListener(() => Shoot?.Invoke());
            if (back != null)
                back.onClick.AddListener(() => Back?.Invoke());
            if (random != null)
                random.onClick.AddListener(() => Random?.Invoke());
        }

        public void Show(MapService map, MapNode selected, StatsModel statsModel, int episodeNumber, IList<string> tasks)
        {
            if (subtitle != null)
                subtitle.text = "Сезон 1  ·  Выпуск " + episodeNumber + " из " + Progression.SeasonLength;
            if (stats != null)
                stats.Show(statsModel);

            var focus = selected ?? map.CurrentChoice;
            if (goal != null)
                goal.text = focus != null && !string.IsNullOrEmpty(focus.goal)
                    ? focus.goal
                    : "Выбрать сцену выпуска и снять как можно больше хайлайтов.";

            DrawEdges(map);
            DrawNodes(map, selected);
            ShowPlan(map, tasks);
            ShowInfo(map, selected);
        }

        Vector2 PositionOf(MapNode node)
        {
            return origin + new Vector2(node.layer * layerStep, -node.row * rowStep) + node.offset;
        }

        void DrawNodes(MapService map, MapNode selected)
        {
            Clear(nodesRoot);
            if (nodePrefab == null)
                return;
            var nodes = map.Map.nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var view = Instantiate(nodePrefab, nodesRoot);
                view.name = "Node_" + node.id;
                ((RectTransform)view.transform).anchoredPosition = PositionOf(node);
                if (node.kind == MapNodeKind.Climax)
                    view.transform.localScale = Vector3.one * 1.2f;
                string id = node.id;
                view.Show(node, map.StateOf(node), map.LockReason(node), selected == node, () => Select?.Invoke(id));
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

        void ShowPlan(MapService map, IList<string> tasks)
        {
            if (plan == null)
                return;
            var body = "СЪЁМОЧНЫЙ ПЛАН";
            var nodes = map.Map.nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (map.StateOf(nodes[i]) == MapNodeState.Done)
                    body += "\n●  " + Capital(nodes[i].title);
            }

            if (tasks != null)
            {
                for (int i = 0; i < tasks.Count; i++)
                    body += "\n○  " + tasks[i];
            }

            plan.text = body;
        }

        void ShowInfo(MapService map, MapNode selected)
        {
            bool can = map.CanShoot(selected);
            if (selected == null)
            {
                if (infoTitle != null)
                    infoTitle.text = "Выберите следующую сцену";
                if (infoBody != null)
                    infoBody.text = "Каждая сцена влияет на тон сезона и бюджет. Пройденный путь не переснять.";
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
                    body += "\nУже снято.";
                else if (state == MapNodeState.Skipped)
                    body += "\nЭтот путь уже не пройти.";
                if (infoTitle != null)
                    infoTitle.text = Capital(selected.title) + "  ·  " + selected.subtitle;
                if (infoBody != null)
                    infoBody.text = body;
            }

            if (shoot != null)
                shoot.interactable = can;
            if (shootLabel != null)
                shootLabel.text = can ? "СНИМАТЬ" : selected == null ? "ВЫБОР" : "НЕДОСТУПНО";
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
