using System;
using System.Collections.Generic;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Продакшн-хаб: фон с зонами, панель зоны, участники, показатели, старт съёмки.
    public class HubView : MonoBehaviour
    {
        [Header("Шапка")]
        [SerializeField] Text subtitle;
        [SerializeField] StatsView stats;

        [Header("Зоны и панель")]
        [SerializeField] ZoneButtonView[] zones = new ZoneButtonView[3];
        [SerializeField] CrewDetailView detail;
        [SerializeField] RosterView roster;

        [Header("Задачи")]
        [SerializeField] GameObject tasksPanel;
        [SerializeField] Text tasks;

        [Header("Низ")]
        [SerializeField] Button start;
        [SerializeField] Text startCaption;
        [SerializeField] Button deckButton;
        [SerializeField] Text deckButtonLabel;
        [SerializeField] Button shopButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button menuButton;

        [SerializeField] DeckPanelView deck;

        public event Action<CrewTrack> Upgrade;
        public event Action<string> Toggle;
        public event Action<string> Buy;
        public event Action StartShoot;
        public event Action OpenSettings;
        public event Action Menu;
        public event Action<int> OpenDeck;
        public event Action CloseDeck;

        // Пусто, пока игрок сам не выбрал зону: при входе в хаб панель зоны закрыта.
        CrewTrack? _selected;
        bool _lockZones;
        Button[] _hover;
        Vector3[] _hoverRest;
        Canvas _canvas;

        public CrewTrack? Selected => _selected;

        public void ClearSelection()
        {
            _selected = null;
        }
        public DeckPanelView Deck => deck;

        void Awake()
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null)
                    zones[i].Clicked += t =>
                    {
                        // Повторный клик по той же зоне закрывает панель.
                        _selected = _selected == t ? (CrewTrack?)null : t;
                        Select?.Invoke();
                    };
            }

            if (detail != null)
                detail.Upgrade += t => Upgrade?.Invoke(t);
            if (roster != null)
                roster.Invite += () =>
                {
                    // Повторное нажатие закрывает панель кастинга.
                    _selected = _selected == CrewTrack.Cast ? (CrewTrack?)null : CrewTrack.Cast;
                    Select?.Invoke();
                };
            if (deck != null)
            {
                deck.Toggle += id => Toggle?.Invoke(id);
                deck.Buy += id => Buy?.Invoke(id);
                deck.Close += () => CloseDeck?.Invoke();
            }

            if (start != null)
                start.onClick.AddListener(() => StartShoot?.Invoke());
            if (deckButton != null)
                deckButton.onClick.AddListener(() => OpenDeck?.Invoke(DeckPanelView.DeckTab));
            if (shopButton != null)
                shopButton.onClick.AddListener(() => OpenDeck?.Invoke(DeckPanelView.ShopTab));
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => OpenSettings?.Invoke());
            if (menuButton != null)
                menuButton.onClick.AddListener(() => Menu?.Invoke());
            DressHover();
        }

        void LateUpdate()
        {
            if (_hover == null)
                return;
            var mouse = Mouse.current;
            if (mouse == null)
                return;
            Vector2 pos = mouse.position.ReadValue();
            Camera cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
            float t = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
            for (int i = 0; i < _hover.Length; i++)
            {
                var button = _hover[i];
                if (button == null)
                    continue;
                var rt = button.transform as RectTransform;
                bool hot = button.interactable && button.gameObject.activeInHierarchy && rt != null
                    && RectTransformUtility.RectangleContainsScreenPoint(rt, pos, cam);
                var target = hot ? _hoverRest[i] * 1.045f : _hoverRest[i];
                button.transform.localScale = Vector3.Lerp(button.transform.localScale, target, t);
            }
        }

        void DressHover()
        {
            _canvas = GetComponentInParent<Canvas>();
            _hover = GetComponentsInChildren<Button>(true);
            _hoverRest = new Vector3[_hover.Length];
            for (int i = 0; i < _hover.Length; i++)
            {
                _hoverRest[i] = _hover[i].transform.localScale;
                var colors = _hover[i].colors;
                var n = colors.normalColor;
                colors.highlightedColor = new Color(
                    Mathf.Clamp01(n.r + 0.14f),
                    Mathf.Clamp01(n.g + 0.14f),
                    Mathf.Clamp01(n.b + 0.14f),
                    n.a);
                colors.fadeDuration = 0.08f;
                _hover[i].colors = colors;
            }
        }

        // Выбор зоны — перерисовка целиком, её делает HubFlow.
        public event Action Select;

        public void Show(PrepModel prep, StatsModel statsModel, CrewInfo[] crew, CastMember[] members, int castLevel, IList<string> taskLines)
        {
            if (subtitle != null)
                subtitle.text = "Сезон 1  ·  Выпуск " + prep.episodeNumber + "  ·  Продакшн-хаб";
            if (stats != null)
                stats.Show(statsModel);

            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] == null)
                    continue;
                var info = crew[(int)zones[i].Track];
                zones[i].Show(info.level, zones[i].Track == _selected && !_lockZones);
            }

            if (detail != null)
            {
                detail.gameObject.SetActive(_selected.HasValue);
                if (_selected.HasValue)
                    detail.Show(crew[(int)_selected.Value]);
            }
            if (roster != null)
                roster.Show(members, castLevel);

            bool hasTasks = taskLines != null && taskLines.Count > 0;
            if (tasksPanel != null)
                tasksPanel.SetActive(hasTasks);
            if (tasks != null)
            {
                var body = "ЗАДАЧИ ЗРИТЕЛЕЙ";
                if (hasTasks)
                {
                    for (int i = 0; i < taskLines.Count; i++)
                        body += "\n★  " + taskLines[i];
                }

                tasks.text = body;
            }

            if (deckButtonLabel != null)
                deckButtonLabel.text = "Колода  ·  " + prep.available;
            if (shopButton != null)
                shopButton.gameObject.SetActive(true);
            if (startCaption != null)
                startCaption.text = "Выпуск " + prep.episodeNumber + "  ·  дальше карта эпизода";

            if (deck != null && deck.IsOpen)
                deck.Show(prep);
        }

        public void SetStartCaption(string text)
        {
            if (startCaption != null)
                startCaption.text = text;
        }

        public void SetSubtitle(string text)
        {
            if (subtitle != null)
                subtitle.text = text;
        }

        public RectTransform ZoneFocus(CrewTrack track)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null && zones[i].Track == track)
                    return zones[i].transform as RectTransform;
            }

            return null;
        }

        public RectTransform DeckFocus()
        {
            return deckButton != null ? deckButton.transform as RectTransform : null;
        }

        public RectTransform ShopFocus()
        {
            return shopButton != null ? shopButton.transform as RectTransform : null;
        }

        public RectTransform StartFocus()
        {
            return start != null ? start.transform as RectTransform : null;
        }

        // Обучение: пока босс говорит — ничего. После речи на хабе золотая только кнопка старта.
        public void ApplyTutorial(bool teach, bool talking, bool startReady)
        {
            bool chrome = !teach;
            bool canStart = !teach || (startReady && !talking);
            _lockZones = teach;
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] == null)
                    continue;
                zones[i].SetEnabled(!teach && !talking);
                zones[i].SetChosen(!_lockZones && zones[i].Track == _selected);
            }

            if (detail != null)
                detail.SetUpgradeEnabled(chrome && !talking);
            if (deckButton != null)
                deckButton.interactable = chrome && !talking;
            if (shopButton != null)
                shopButton.interactable = chrome && !talking;
            if (settingsButton != null)
                settingsButton.interactable = chrome;
            if (menuButton != null)
                menuButton.interactable = chrome;
            if (start != null)
                start.interactable = canStart;
            if (deck != null)
                deck.SetLocked(teach && talking);
        }
    }
}
