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
            DressChrome();
            DressHover();
        }

        // Хаб — диспетчерская канала: арт за экраном, станции в ряд, панели в рамках пака, старт — главный.
        void DressChrome()
        {
            var screen = (RectTransform)transform;
            UiKit.Backdrop(screen, "Art/Intro/bg/scene_5", new Rect(0f, 0.3f, 1f, 0.7f), 0.5f);
            var hint = transform.Find("BackgroundHint");
            if (hint != null)
                hint.gameObject.SetActive(false);
            var flat = transform.Find("Background")?.GetComponent<Image>();
            if (flat != null)
                flat.enabled = false;
            var self = GetComponent<Image>();
            if (self != null)
                self.color = UiKit.Ink;

            UiKit.DressSolid(transform.Find("TitlePanel")?.GetComponent<Image>(), UiKit.Frame.Dialog);
            UiKit.DressSolid(transform.Find("TasksPanel")?.GetComponent<Image>(), UiKit.Frame.Dialog);
            UiKit.DressSolid(transform.Find("ZonePanel")?.GetComponent<Image>(), UiKit.Frame.Dialog);
            UiKit.DressSolid(transform.Find("RosterPanel")?.GetComponent<Image>(), UiKit.Frame.Dialog);

            // Станции — ряд карточек в свободной части экрана (правую треть держат панели).
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] == null)
                    continue;
                int slot = zones[i].Track == CrewTrack.Cast ? 0 : zones[i].Track == CrewTrack.Writers ? 1 : 2;
                UiKit.Place((RectTransform)zones[i].transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(48f + slot * (ZoneButtonView.CardSize.x + 58f), -470f), ZoneButtonView.CardSize);
            }

            var tasksRt = tasksPanel != null ? tasksPanel.transform as RectTransform : null;
            if (tasksRt != null)
                UiKit.Place(tasksRt, Vector2.zero, Vector2.zero, new Vector2(20f, 150f), new Vector2(430f, 150f));
            if (tasks != null)
            {
                var tasksText = tasks.rectTransform;
                tasksText.offsetMin = new Vector2(28f, 20f);
                tasksText.offsetMax = new Vector2(-28f, -20f);
                tasks.horizontalOverflow = HorizontalWrapMode.Wrap;
                tasks.verticalOverflow = VerticalWrapMode.Truncate;
            }

            if (start != null)
            {
                UiKit.Primary(start, 26);
                UiKit.Place((RectTransform)start.transform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(810f, 78f), new Vector2(560f, 92f));
                UiKit.Pulse(start);
                var clap = UiKit.Img("Clap", start.transform, UiKit.Icon("icon_clapperboard"), UiKit.Paper);
                clap.preserveAspect = true;
                UiKit.Place(clap.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(44f, 2f), new Vector2(44f, 44f));
            }

            if (startCaption != null)
            {
                UiKit.Place(startCaption.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(810f, 18f), new Vector2(700f, 26f));
                startCaption.color = UiKit.Muted;
                UiKit.Shadow(startCaption);
            }

            UiKit.Secondary(deckButton);
            UiKit.Secondary(shopButton);
            UiKit.Secondary(settingsButton);
            UiKit.Secondary(menuButton);
            if (roster != null)
            {
                var invite = roster.transform.Find("Invite");
                UiKit.Secondary(invite != null ? invite.GetComponent<Button>() : null);
                FitInvite(invite as RectTransform);
            }
            if (detail != null)
                UiKit.Primary(detail.transform.Find("Upgrade")?.GetComponent<Button>(), 20);
            if (stats != null)
                stats.Dress();
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
                zones[i].Show(info, zones[i].Track == _selected && !_lockZones);
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
                shopButton.gameObject.SetActive(!_first);
            if (startCaption != null)
                startCaption.text = "Выпуск " + prep.episodeNumber + "  ·  дальше карта выпуска";

            if (deck != null && deck.IsOpen)
                deck.Show(prep);
            ApplyFirst();
        }

        // ---------- Первый выпуск: студия закрыта ----------

        // До первого эфира хаб показывает только старт съёмок и настройки, а вместо станций — как пройдёт выпуск.
        // Механика апгрейдов не трогается: станции, колода и магазин просто откроются после эфира.
        bool _first;
        GameObject _firstNote;

        public bool FirstEpisode => _first;

        public void SetFirstEpisode(bool first)
        {
            _first = first;
            if (first)
                _selected = null;
            ApplyFirst();
        }

        void ApplyFirst()
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null)
                    zones[i].gameObject.SetActive(!_first);
            }

            var zonePanel = transform.Find("ZonePanel");
            if (zonePanel != null && _first)
                zonePanel.gameObject.SetActive(false);
            var rosterPanel = transform.Find("RosterPanel");
            if (rosterPanel != null)
                rosterPanel.gameObject.SetActive(!_first);
            if (detail != null && _first)
                detail.gameObject.SetActive(false);
            if (deckButton != null)
                deckButton.gameObject.SetActive(!_first);
            if (shopButton != null)
                shopButton.gameObject.SetActive(!_first);
            if (menuButton != null)
                menuButton.gameObject.SetActive(!_first);
            if (_first && _firstNote == null)
                BuildFirstNote();
            if (_firstNote != null)
                _firstNote.SetActive(_first);
        }

        void BuildFirstNote()
        {
            var card = UiKit.Img("FirstEpisode", transform, null, Color.white);
            UiKit.DressSolid(card, UiKit.Frame.Dialog);
            var rt = card.rectTransform;
            UiKit.Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -330f), new Vector2(1136f, 470f));
            _firstNote = card.gameObject;
            _firstNote.transform.SetSiblingIndex(Mathf.Max(0, (start != null ? start.transform.GetSiblingIndex() : 1) - 1));

            var title = UiKit.Txt("Title", rt, "ПЕРВЫЙ ВЫПУСК", 34, UiKit.Gold, TextAnchor.UpperLeft, UiKit.Body);
            title.fontStyle = FontStyle.Bold;
            UiKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(900f, 46f));
            var lead = UiKit.Txt("Lead", rt, "Студия пока закрыта — сначала сними выпуск. Вот как он пройдёт:", 20, UiKit.Muted, TextAnchor.UpperLeft, UiKit.Body);
            UiKit.Place(lead.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -80f), new Vector2(1050f, 30f));

            string[] heads = { "КАСТ", "КАРТА ВЫПУСКА", "МОНТАЖ И ЭФИР" };
            string[] bodies =
            {
                "Из 5 кандидатов выбери 2. Видна их черта и как они реагируют. Скрытая черта всплывёт на съёмке.",
                "Иди по комнатам: съёмки, событие, маркетинг. На съёмке карты меняют людей — снимай, что из этого вышло.",
                "Собери 3 лучших кадра в историю. Зрители HellTube оценят — и платят за это."
            };
            string[] icons = { "icon_heart", "icon_camera", "icon_clapperboard" };
            for (int i = 0; i < 3; i++)
            {
                var step = UiKit.Img("Step" + i, rt, null, Color.white);
                UiKit.DressSolid(step, UiKit.Frame.Dark);
                UiKit.Place(step.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f + i * 356f, -130f), new Vector2(336f, 228f));
                var num = UiKit.Txt("N", step.rectTransform, (i + 1).ToString(), 44, UiKit.Ember, TextAnchor.UpperLeft, UiKit.Body);
                num.fontStyle = FontStyle.Bold;
                UiKit.Place(num.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -12f), new Vector2(60f, 56f));
                var icon = UiKit.Img("Icon", step.rectTransform, UiKit.Icon(icons[i]), Color.white);
                icon.preserveAspect = true;
                UiKit.Place(icon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -16f), new Vector2(46f, 46f));
                var head = UiKit.Txt("Head", step.rectTransform, heads[i], 21, UiKit.Paper, TextAnchor.UpperLeft, UiKit.Body);
                head.fontStyle = FontStyle.Bold;
                UiKit.Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -74f), new Vector2(300f, 30f));
                var body = UiKit.Txt("Body", step.rectTransform, bodies[i], 17, UiKit.Muted, TextAnchor.UpperLeft, UiKit.Body);
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
                UiKit.Place(body.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -108f), new Vector2(298f, 112f));
            }

            var lockLine = UiKit.Txt("Locked", rt, "После первого эфира откроются: кастинг, сценарная, съёмочная, колода и магазин карт.", 18, UiKit.GoldDim, TextAnchor.UpperLeft, UiKit.Body);
            UiKit.Place(lockLine.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -386f), new Vector2(1050f, 30f));
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

        public RectTransform StatsFocus()
        {
            return stats != null ? stats.transform as RectTransform : null;
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
            // Настройки доступны всегда, кроме момента, когда говорит босс.
            if (settingsButton != null)
                settingsButton.interactable = !talking;
            if (menuButton != null)
                menuButton.interactable = chrome;
            if (start != null)
                start.interactable = canStart;
            if (deck != null)
                deck.SetLocked(teach && talking);
            LockExtras(teach);
        }

        void FitInvite(RectTransform button)
        {
            if (button == null)
                return;
            var label = button.GetComponentInChildren<Text>();
            if (label == null)
                return;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 11;
            label.resizeTextMaxSize = Mathf.Max(14, label.fontSize);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.rectTransform.offsetMin = new Vector2(16f, 6f);
            label.rectTransform.offsetMax = new Vector2(-16f, -6f);
        }

        void LockExtras(bool teach)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null)
                    continue;
                bool invite = roster != null && button.name == "Invite" && button.transform.IsChildOf(roster.transform);
                var label = button.GetComponentInChildren<Text>(true);
                string text = label != null ? label.text : "";
                bool random = text.IndexOf("Случайн", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!invite && !random)
                    continue;
                button.interactable = !teach;
            }
        }
    }
}
