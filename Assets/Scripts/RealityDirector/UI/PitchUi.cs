using System;
using System.Collections;
using System.Collections.Generic;
using RealityDirector.Capture;
using RealityDirector.Core;
using RealityDirector.Events;
using RealityDirector.Meta;
using RealityDirector.Util;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI
{
    public class PitchUi : MonoBehaviour
    {
        static readonly Color Ink = new Color(0.06f, 0.05f, 0.07f, 0.94f);
        static readonly Color Paper = new Color(0.96f, 0.93f, 0.88f, 1f);
        static readonly Color Muted = new Color(0.62f, 0.56f, 0.52f, 1f);
        static readonly Color Accent = new Color(0.93f, 0.33f, 0.22f, 1f);
        static readonly Color Good = new Color(0.49f, 0.86f, 0.62f, 1f);
        static readonly Color PanelColor = new Color(0.13f, 0.11f, 0.14f, 1f);
        static readonly Color Hud = new Color(0.08f, 0.07f, 0.09f, 0.82f);

        Font _font;
        RectTransform _canvasRect;
        GameObject _tagsRoot;
        GameObject _hud;
        GameObject _intro;
        GameObject _feedback;
        GameObject _vision;
        GameObject _captureBanner;
        Transform _cardBar;
        Text _hint;
        Text _toast;
        Text _camLabel;
        Image _flash;
        float _toastUntil;
        int _slotEpoch;
        readonly List<Tag> _tags = new List<Tag>();
        readonly List<Bubble> _bubbles = new List<Bubble>();
        readonly List<Card> _cards = new List<Card>();
        readonly Slot[] _slots = new Slot[5];
        readonly Text[] _reviewAuthors = new Text[3];
        readonly Text[] _reviewBodies = new Text[3];
        readonly Text[] _reviewScores = new Text[3];
        readonly Button[] _stars = new Button[3];
        readonly Text[] _starLabels = new Text[3];
        Text _scoreText;
        Text _wishText;
        Text _payText;
        GameObject _tasksRoot;
        Text _tasksBody;
        bool _taskTaken;
        int _offerRow = -1;

        public bool TaskTaken => _taskTaken;
        Text _episodeTitle;
        Coroutine _flashRoutine;
        GameObject _toneRoot;
        Text _toneLead;
        readonly ToneRow[] _toneRows = new ToneRow[3];

        class Tag
        {
            public Transform Target;
            public RectTransform Rect;
            public Text Text;
            public Func<string> Pull;
            public Vector2 Offset;
        }

        class Bubble
        {
            public Transform Target;
            public RectTransform Rect;
            public Text Text;
            public Func<string> Pull;
            public Func<bool> Thought;
            public Vector2 Offset;
            public string Shown;
            public bool WasThought;
            public GameObject Speech;
            public GameObject ThoughtCloud;
            public RectTransform Plate;
            public RectTransform Tail;
            public RectTransform Fill;
            public RectTransform[] Lumps;
            public RectTransform[] Puffs;
        }

        class Card
        {
            public EventDefinition Def;
            public Image Frame;
            public Text Status;
            public RectTransform Root;
            public Color Base;
            public bool Used;
        }

        class Slot
        {
            public GameObject Root;
            public RectTransform Well;
            public Text Placeholder;
        }

        class ToneRow
        {
            public ShowMood Mood;
            public RectTransform Fill;
            public Text Value;
            public Text Delta;
            public float DeltaUntil;
        }

        public static PitchUi Build()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var go = new GameObject("PitchUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var ui = go.AddComponent<PitchUi>();
            ui._font = font;
            ui._canvasRect = go.GetComponent<RectTransform>();
            ui.Construct();
            return ui;
        }

        public void AddTag(Transform target, Func<string> pull, Color color, Vector2 offset, int size, bool plate)
        {
            var root = new GameObject("tag", typeof(RectTransform));
            root.transform.SetParent(_tagsRoot.transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 56f);
            if (plate)
            {
                var bg = root.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.45f);
                bg.raycastTarget = false;
            }

            var text = MakeText(root.transform, "", size, color, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.raycastTarget = false;
            _tags.Add(new Tag
            {
                Target = target,
                Rect = rect,
                Text = text,
                Pull = pull,
                Offset = offset
            });
        }

        public void AddBubble(Transform target, Func<string> pull, Func<bool> thought, Vector2 offset)
        {
            var root = new GameObject("bubble", typeof(RectTransform));
            root.transform.SetParent(_tagsRoot.transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(120f, 64f);

            var speech = new GameObject("speech", typeof(RectTransform));
            speech.transform.SetParent(root.transform, false);
            Stretch(speech.GetComponent<RectTransform>());
            var plate = Shape(speech.transform, BubbleRound(), new Vector2(0f, 40f), new Vector2(120f, 48f), true);
            plate.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            plate.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            plate.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var tail = Shape(speech.transform, BubbleTail(), Vector2.zero, new Vector2(22f, 16f), false);
            tail.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            tail.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            tail.rectTransform.pivot = new Vector2(0.5f, 0f);

            var cloud = new GameObject("thought", typeof(RectTransform));
            cloud.transform.SetParent(root.transform, false);
            Stretch(cloud.GetComponent<RectTransform>());
            var lumps = new[]
            {
                Bump(cloud.transform, 36f),
                Bump(cloud.transform, 42f),
                Bump(cloud.transform, 34f)
            };
            for (int i = 0; i < lumps.Length; i++)
                lumps[i].gameObject.SetActive(false);
            var fill = Shape(cloud.transform, BubbleCloud(), new Vector2(0f, 48f), new Vector2(120f, 48f), false);
            fill.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            fill.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            fill.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var puffs = new[]
            {
                Puff(cloud.transform, 16f),
                Puff(cloud.transform, 11f),
                Puff(cloud.transform, 7f)
            };

            var label = MakeText(root.transform, "", 22, Color.black, TextAnchor.MiddleCenter);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(80f, 28f);
            label.fontStyle = FontStyle.Bold;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;

            speech.SetActive(false);
            cloud.SetActive(false);
            root.SetActive(false);
            _bubbles.Add(new Bubble
            {
                Target = target,
                Rect = rect,
                Text = label,
                Pull = pull,
                Thought = thought,
                Offset = offset,
                Speech = speech,
                ThoughtCloud = cloud,
                Plate = plate.rectTransform,
                Tail = tail.rectTransform,
                Fill = fill.rectTransform,
                Lumps = lumps,
                Puffs = puffs
            });
        }

        void LayoutBubble(Bubble bubble, string value, bool thought)
        {
            bubble.Shown = value;
            bubble.WasThought = thought;
            bubble.Speech.SetActive(!thought);
            bubble.ThoughtCloud.SetActive(thought);
            bubble.Text.text = value;
            bubble.Text.horizontalOverflow = HorizontalWrapMode.Overflow;
            float inner = Mathf.Clamp(bubble.Text.preferredWidth, 24f, 200f);
            bubble.Text.horizontalOverflow = HorizontalWrapMode.Wrap;
            bubble.Text.rectTransform.sizeDelta = new Vector2(inner, 160f);
            float line = Mathf.Max(22f, bubble.Text.preferredHeight);
            float bodyW = inner + (thought ? 56f : 36f);
            float bodyH = line + (thought ? 40f : 22f);
            float tail = thought ? 22f : 14f;
            bubble.Rect.sizeDelta = new Vector2(bodyW, bodyH + tail);

            var label = bubble.Text.rectTransform;
            label.sizeDelta = new Vector2(inner, line);
            label.anchoredPosition = new Vector2(0f, tail + bodyH * 0.5f);

            if (thought)
            {
                float cy = tail + bodyH * 0.5f;
                bubble.Fill.sizeDelta = new Vector2(bodyW, bodyH);
                bubble.Fill.anchoredPosition = new Vector2(0f, cy);
                bubble.Puffs[0].sizeDelta = new Vector2(11f, 11f);
                bubble.Puffs[1].sizeDelta = new Vector2(8f, 8f);
                bubble.Puffs[2].sizeDelta = new Vector2(5f, 5f);
                bubble.Puffs[0].anchoredPosition = new Vector2(8f, 11f);
                bubble.Puffs[1].anchoredPosition = new Vector2(16f, 5f);
                bubble.Puffs[2].anchoredPosition = new Vector2(22f, 0f);
            }
            else
            {
                bubble.Plate.sizeDelta = new Vector2(bodyW, bodyH);
                bubble.Plate.anchoredPosition = new Vector2(0f, tail + bodyH * 0.5f);
                bubble.Tail.anchoredPosition = Vector2.zero;
            }
        }

        static Image Shape(Transform parent, Sprite sprite, Vector2 pos, Vector2 size, bool sliced)
        {
            var go = new GameObject("shape", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return image;
        }

        static RectTransform Bump(Transform parent, float size)
        {
            var image = Shape(parent, BubbleRound(), Vector2.zero, new Vector2(size, size), false);
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        static void PlaceLump(RectTransform rect, float x, float y, float size)
        {
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(size, size);
        }

        static RectTransform Puff(Transform parent, float size)
        {
            var image = Shape(parent, BubbleRound(), Vector2.zero, new Vector2(size, size), false);
            image.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            image.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            image.rectTransform.pivot = new Vector2(0.5f, 0f);
            return image.rectTransform;
        }

        public void SetTagsVisible(bool on)
        {
            _tagsRoot.SetActive(on);
        }

        public void ClearHand()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Root != null)
                    Destroy(_cards[i].Root.gameObject);
            }

            _cards.Clear();
        }

        public void BindCards(EventDefinition[] hand, Action<EventDefinition> onCard)
        {
            for (int i = 0; i < hand.Length; i++)
            {
                int index = i;
                var def = hand[i];
                var card = MakeCard(index, def, () => onCard(def));
                _cards.Add(card);
            }
        }

        public void SetArmed(EventDefinition def)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                bool on = def != null && _cards[i].Def == def;
                var color = _cards[i].Base;
                if (on)
                    color = new Color(0.95f, 0.78f, 0.32f, 1f);
                else if (_cards[i].Used)
                    color *= 0.72f;
                _cards[i].Frame.color = color;
                _cards[i].Root.localScale = on ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
            }
        }

        public void MarkUsed(string id)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Def.id != id)
                    continue;
                _cards[i].Used = true;
                _cards[i].Status.gameObject.SetActive(true);
            }
        }

        public void ClearUsed()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].Used = false;
                _cards[i].Status.gameObject.SetActive(false);
                _cards[i].Frame.color = _cards[i].Base;
                _cards[i].Root.localScale = Vector3.one;
            }
        }

        public void SetCaptureCapacity(int capacity)
        {
            capacity = Mathf.Clamp(capacity, 1, _slots.Length);
            for (int i = 0; i < _slots.Length; i++)
            {
                bool on = i < capacity;
                _slots[i].Root.SetActive(on);
                if (!on)
                    continue;
                var rect = _slots[i].Root.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(-14f - (capacity - 1 - i) * 160f, -132f);
            }
        }

        public void SetEpisodeTitle(string text)
        {
            if (_episodeTitle != null)
                _episodeTitle.text = text;
        }

        public void SetSlots(IReadOnlyList<CapturedMoment> moments, int capacity)
        {
            if (moments == null || moments.Count == 0)
                ClearSlots();
        }

        public void ClearSlots()
        {
            _slotEpoch++;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == "Polaroid")
                    DestroyPolaroid(child.gameObject);
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Placeholder.gameObject.SetActive(true);
                for (int c = _slots[i].Well.childCount - 1; c >= 0; c--)
                    DestroyPolaroid(_slots[i].Well.GetChild(c).gameObject);
            }
        }

        public void FlyPhoto(Texture2D photo, int slot, Vector2 screen, bool framed)
        {
            if (photo == null || slot < 0 || slot >= _slots.Length)
                return;
            StartCoroutine(FlyRoutine(photo, slot, screen, framed, _slotEpoch));
        }

        public void SetCaptureMode(bool on)
        {
            _captureBanner.SetActive(on);
            _camLabel.text = on ? "КАМЕРА ВКЛ" : "КАМЕРА  C";
        }

        public void SetHint(string text)
        {
            _hint.text = text;
        }

        public void Toast(string text)
        {
            _toast.text = text;
            _toastUntil = Time.unscaledTime + 1.5f;
            _toast.gameObject.SetActive(true);
        }

        public void ShowIntro()
        {
            _intro.SetActive(true);
            _feedback.SetActive(false);
            _vision.SetActive(false);
            _hud.SetActive(false);
            if (_toneRoot != null)
                _toneRoot.SetActive(false);
            SetTagsVisible(false);
            SetCaptureMode(false);
            if (_tasksRoot != null)
                _tasksRoot.SetActive(false);
        }

        public void ShowPlay()
        {
            _intro.SetActive(false);
            _feedback.SetActive(false);
            _vision.SetActive(false);
            _hud.SetActive(true);
            SetTagsVisible(true);
            _toneRoot.SetActive(true);
            _toneRoot.transform.SetAsLastSibling();
        }

        public void ShowFeedback(FeedbackResult result, Action onNext)
        {
            _hud.SetActive(false);
            SetTagsVisible(false);
            _feedback.SetActive(true);
            _toneRoot.SetActive(true);
            _toneRoot.transform.SetAsLastSibling();
            SetCaptureMode(false);
            _taskTaken = false;
            _offerRow = -1;
            for (int i = 0; i < 3; i++)
            {
                var review = result.reviews[i];
                bool offer = review.offer;
                _reviewAuthors[i].text = offer ? "★  " + review.author : review.author;
                _reviewBodies[i].text = review.body;
                _reviewScores[i].text = review.score + "/10";
                _stars[i].gameObject.SetActive(offer);
                if (!offer)
                    continue;
                _offerRow = i;
                _starLabels[i].text = "☆ взять";
                _starLabels[i].color = Paper;
                _stars[i].image.color = new Color(0.22f, 0.2f, 0.24f, 1f);
            }

            _scoreText.text = result.score + "/10";
            _scoreText.color = result.score >= 7 ? Good : Accent;
            _wishText.text = "";
            _payText.text = result.payLine ?? "";
            _feedbackNext = onNext;
        }

        public void SetTasks(IList<string> lines, bool visible)
        {
            bool show = visible && lines != null && lines.Count > 0;
            _tasksRoot.SetActive(show);
            if (!show)
                return;
            var body = "ЗАДАЧИ";
            for (int i = 0; i < lines.Count; i++)
                body += "\n★  " + lines[i];
            _tasksBody.text = body;
            _tasksRoot.transform.SetAsLastSibling();
        }

        void ToggleTask()
        {
            if (_offerRow < 0)
                return;
            _taskTaken = !_taskTaken;
            _starLabels[_offerRow].text = _taskTaken ? "★ в задачах" : "☆ взять";
            _starLabels[_offerRow].color = _taskTaken ? Ink : Paper;
            _stars[_offerRow].image.color = _taskTaken
                ? new Color(0.93f, 0.76f, 0.28f, 1f)
                : new Color(0.22f, 0.2f, 0.24f, 1f);
        }

        public void ShowVision()
        {
            _feedback.SetActive(false);
            _vision.SetActive(true);
            _toneRoot.SetActive(false);
        }

        public void Pulse(Color color)
        {
            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FadeFlash(color, 0.22f));
        }

        Action _feedbackNext;
        Action _onStart;
        Action _onEnd;
        Action _onCamera;
        Action _onReplay;

        public void BindFlow(Action onStart, Action onEnd, Action onCamera, Action onReplay)
        {
            _onStart = onStart;
            _onEnd = onEnd;
            _onCamera = onCamera;
            _onReplay = onReplay;
        }

        void Construct()
        {
            _tagsRoot = NewRect("Tags", transform);
            _hud = NewRect("Hud", transform);
            BuildHud();
            _flash = Panel("Flash", transform, Color.white);
            Stretch(_flash.rectTransform);
            _flash.raycastTarget = false;
            _flash.gameObject.SetActive(false);

            _intro = BuildIntro();
            _feedback = BuildFeedback();
            _vision = BuildVision();
            BuildTone();
            BuildTasks();
            ShowIntro();
        }

        void BuildHud()
        {
            var top = Panel("top", _hud.transform, Hud);
            var topRect = top.rectTransform;
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.sizeDelta = new Vector2(0f, 118f);
            topRect.anchoredPosition = Vector2.zero;

            _episodeTitle = MakeText(top.transform, "СЕРИЯ 1\nты режиссёр, не участник", 22, Paper, TextAnchor.UpperLeft);
            var title = _episodeTitle;
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(28f, -16f);
            titleRect.sizeDelta = new Vector2(520f, 80f);

            for (int i = 0; i < _slots.Length; i++)
            {
                var slotImg = Panel("slot" + i, _hud.transform, new Color(0.1f, 0.09f, 0.08f, 0.92f));
                var slotRect = slotImg.rectTransform;
                slotRect.anchorMin = new Vector2(1f, 1f);
                slotRect.anchorMax = new Vector2(1f, 1f);
                slotRect.pivot = new Vector2(1f, 1f);
                slotRect.sizeDelta = new Vector2(148f, 200f);
                slotRect.anchoredPosition = new Vector2(-14f - (_slots.Length - 1 - i) * 160f, -132f);
                var placeholder = MakeText(slotImg.transform, "СЛОТ " + (i + 1) + "\nпусто", 18, Muted, TextAnchor.MiddleCenter);
                Stretch(placeholder.rectTransform);
                var well = NewRect("well", slotImg.transform);
                _slots[i] = new Slot { Root = slotImg.gameObject, Well = well.GetComponent<RectTransform>(), Placeholder = placeholder };
            }

            var cam = MakeButton(_hud.transform, "КАМЕРА  C", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(168f, -132f), new Vector2(180f, 48f), Accent, () => _onCamera?.Invoke());
            _camLabel = cam.GetComponentInChildren<Text>();

            MakeButton(_hud.transform, "КОНЕЦ СЕРИИ", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(364f, -132f), new Vector2(210f, 48f), new Color(0.22f, 0.2f, 0.24f, 1f), () => _onEnd?.Invoke());

            _captureBanner = Panel("banner", _hud.transform, new Color(0.95f, 0.82f, 0.35f, 0.95f)).gameObject;
            var bannerRect = _captureBanner.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 1f);
            bannerRect.anchorMax = new Vector2(0.5f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.sizeDelta = new Vector2(460f, 42f);
            bannerRect.anchoredPosition = new Vector2(0f, -132f);
            var bannerText = MakeText(_captureBanner.transform, "SPACE  —  СНЯТЬ", 22, Ink, TextAnchor.MiddleCenter);
            Stretch(bannerText.rectTransform);
            _captureBanner.SetActive(false);

            var bar = Panel("bar", _hud.transform, Hud);
            _cardBar = bar.transform;
            var barRect = bar.rectTransform;
            barRect.anchorMin = new Vector2(0.5f, 0f);
            barRect.anchorMax = new Vector2(0.5f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.sizeDelta = new Vector2(980f, 332f);
            barRect.anchoredPosition = new Vector2(0f, 16f);
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            _hint = MakeText(_hud.transform, "", 22, Paper, TextAnchor.MiddleCenter);
            var hintRect = _hint.rectTransform;
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(1100f, 36f);
            hintRect.anchoredPosition = new Vector2(0f, 356f);

            _toast = MakeText(_hud.transform, "", 22, new Color(1f, 0.82f, 0.45f, 1f), TextAnchor.MiddleCenter);
            var toastRect = _toast.rectTransform;
            toastRect.anchorMin = new Vector2(0.5f, 0f);
            toastRect.anchorMax = new Vector2(0.5f, 0f);
            toastRect.pivot = new Vector2(0.5f, 0f);
            toastRect.sizeDelta = new Vector2(900f, 32f);
            toastRect.anchoredPosition = new Vector2(0f, 392f);
            _toast.gameObject.SetActive(false);
            SetCaptureCapacity(1);
        }

        Card MakeCard(int index, EventDefinition def, Action onClick)
        {
            var frame = Panel("card" + index, _cardBar, def.cardColor);
            var element = frame.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 176f;
            element.preferredHeight = 308f;
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.onClick.AddListener(() =>
            {
                Sfx.Play(Cue.Click, 0.3f);
                onClick();
            });

            // Рамка художника по тону карты: окно внутри прозрачное, под ним тёмная подложка.
            var frameArt = def.moods != null && def.moods.Count > 0 ? GameArt.CardFrame(def.moods[0]) : null;
            var inner = Panel("inner", frame.transform, new Color(0.95f, 0.91f, 0.84f, 1f));
            var innerRect = inner.rectTransform;
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(8f, 8f);
            innerRect.offsetMax = new Vector2(-8f, -8f);
            inner.raycastTarget = false;
            if (frameArt != null)
            {
                frame.sprite = frameArt;
                frame.color = Color.white;
                inner.color = new Color(0.07f, 0.05f, 0.08f, 0.96f);
                innerRect.offsetMin = new Vector2(18f, 60f);
                innerRect.offsetMax = new Vector2(-18f, -36f);
                inner.transform.SetAsFirstSibling();
            }

            var badge = Panel("badge", frame.transform, new Color(0.14f, 0.09f, 0.08f, 1f));
            var badgeRect = badge.rectTransform;
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 1f);
            badgeRect.anchoredPosition = new Vector2(12f, -12f);
            badgeRect.sizeDelta = new Vector2(32f, 32f);
            badge.raycastTarget = false;
            var number = MakeText(badge.transform, (index + 1).ToString(), 18, Paper, TextAnchor.MiddleCenter);
            Stretch(number.rectTransform);

            int moodCount = def.moods != null ? def.moods.Count : 0;
            var title = MakeText(inner.transform, def.displayName.ToUpperInvariant(), 18, new Color(0.18f, 0.12f, 0.1f), TextAnchor.MiddleCenter);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(moodCount > 0 ? -6f : 0f, -6f);
            titleRect.sizeDelta = new Vector2(moodCount > 0 ? -28f : -8f, 36f);
            StampMoods(frame.transform, def);

            var art = Panel("art", inner.transform, new Color(0.9f, 0.86f, 0.78f, 1f));
            var artRect = art.rectTransform;
            artRect.anchorMin = new Vector2(0f, 1f);
            artRect.anchorMax = new Vector2(1f, 1f);
            artRect.pivot = new Vector2(0.5f, 1f);
            artRect.anchoredPosition = new Vector2(0f, -44f);
            artRect.sizeDelta = new Vector2(-16f, 150f);
            art.raycastTarget = false;
            art.preserveAspect = true;
            art.color = Color.white;
            if (def.cardArt != null)
                art.sprite = def.cardArt;

            var body = MakeText(inner.transform, def.hint, 15, new Color(0.35f, 0.26f, 0.2f), TextAnchor.MiddleCenter);
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 0f);
            bodyRect.pivot = new Vector2(0.5f, 0f);
            bodyRect.anchoredPosition = new Vector2(0f, 8f);
            bodyRect.sizeDelta = new Vector2(-12f, 64f);
            if (frameArt != null)
            {
                title.color = Paper;
                body.color = new Color(0.82f, 0.76f, 0.7f, 1f);
            }

            var status = MakeText(frame.transform, "сыграно", 16, new Color(0.55f, 0.32f, 0.08f), TextAnchor.MiddleCenter);
            var statusRect = status.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0f);
            statusRect.anchoredPosition = new Vector2(0f, 14f);
            statusRect.sizeDelta = new Vector2(-16f, 24f);
            status.gameObject.SetActive(false);

            return new Card
            {
                Def = def,
                Frame = frame,
                Status = status,
                Root = frame.rectTransform,
                Base = frameArt != null ? Color.white : def.cardColor
            };
        }

        void StampMoods(Transform frame, EventDefinition def)
        {
            if (def.moods == null)
                return;

            int count = def.moods.Count > 2 ? 2 : def.moods.Count;
            for (int i = 0; i < count; i++)
            {
                var stamp = Panel("mood", frame, Color.white);
                var rect = stamp.rectTransform;
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.sizeDelta = new Vector2(26f, 26f);
                rect.anchoredPosition = new Vector2(-8f - (count - 1 - i) * 30f, -8f);
                stamp.sprite = MoodIcon(def.moods[i]);
                stamp.preserveAspect = true;
                stamp.raycastTarget = false;
            }
        }

        public void RefreshTone(SeasonTone tone)
        {
            if (tone == null)
                return;

            for (int i = 0; i < _toneRows.Length; i++)
            {
                var row = _toneRows[i];
                float k = Mathf.Clamp01(tone.Get(row.Mood) / (float)SeasonTone.Cap);
                row.Fill.sizeDelta = new Vector2(156f * k, 0f);
                row.Value.text = tone.Get(row.Mood).ToString();
            }

            if (tone.Total <= 0)
                _toneLead.text = "тон ещё не выбран";
            else if (tone.TryLead(out ShowMood lead))
                _toneLead.text = "ведёт " + MoodStyle.Paint(MoodStyle.Full(lead), lead);
            else
                _toneLead.text = "тон на распутье";
        }

        public void FlashTone(ShowMood mood, int delta)
        {
            if (delta == 0)
                return;

            for (int i = 0; i < _toneRows.Length; i++)
            {
                if (_toneRows[i].Mood != mood)
                    continue;
                bool up = delta > 0;
                _toneRows[i].Delta.text = up ? "+" + delta : delta.ToString();
                _toneRows[i].Delta.color = up ? MoodStyle.ColorOf(mood) : new Color(0.22f, 0.14f, 0.12f, 1f);
                _toneRows[i].Delta.gameObject.SetActive(true);
                _toneRows[i].DeltaUntil = Time.unscaledTime + 1.15f;
                if (up)
                    Sfx.Play(Cue.Tick, 0.22f, mood == ShowMood.Drama ? 1.15f : mood == ShowMood.Trash ? 0.82f : 1f);
            }
        }

        void BuildTone()
        {
            var plate = Panel("Tone", transform, Hud);
            _toneRoot = plate.gameObject;
            var rect = plate.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-12f, -6f);
            rect.sizeDelta = new Vector2(400f, 118f);
            plate.raycastTarget = false;

            ShowMood[] moods = { ShowMood.Drama, ShowMood.Trash, ShowMood.Family };
            for (int i = 0; i < moods.Length; i++)
            {
                ShowMood mood = moods[i];
                var row = NewRect("tone" + i, plate.transform);
                var rowRect = row.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.sizeDelta = new Vector2(-20f, 26f);
                rowRect.anchoredPosition = new Vector2(0f, -6f - i * 28f);

                var icon = Panel("icon", row.transform, Color.white);
                var iconRect = icon.rectTransform;
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = new Vector2(22f, 22f);
                icon.sprite = MoodIcon(mood);
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                var label = MakeText(row.transform, MoodStyle.Short(mood).ToUpperInvariant(), 15, MoodStyle.ColorOf(mood), TextAnchor.MiddleLeft);
                var labelRect = label.rectTransform;
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(0f, 1f);
                labelRect.pivot = new Vector2(0f, 0.5f);
                labelRect.anchoredPosition = new Vector2(28f, 0f);
                labelRect.sizeDelta = new Vector2(78f, 0f);

                var track = Panel("track", row.transform, new Color(0f, 0f, 0f, 0.45f));
                var trackRect = track.rectTransform;
                trackRect.anchorMin = new Vector2(0f, 0.5f);
                trackRect.anchorMax = new Vector2(0f, 0.5f);
                trackRect.pivot = new Vector2(0f, 0.5f);
                trackRect.anchoredPosition = new Vector2(112f, 0f);
                trackRect.sizeDelta = new Vector2(156f, 10f);
                track.raycastTarget = false;

                var fill = Panel("fill", track.transform, MoodStyle.ColorOf(mood));
                var fillRect = fill.rectTransform;
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(0f, 1f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.anchoredPosition = Vector2.zero;
                fillRect.sizeDelta = new Vector2(0f, 0f);
                fill.raycastTarget = false;

                var value = MakeText(row.transform, "0", 15, Paper, TextAnchor.MiddleRight);
                var valueRect = value.rectTransform;
                valueRect.anchorMin = new Vector2(1f, 0f);
                valueRect.anchorMax = new Vector2(1f, 1f);
                valueRect.pivot = new Vector2(1f, 0.5f);
                valueRect.anchoredPosition = new Vector2(-30f, 0f);
                valueRect.sizeDelta = new Vector2(36f, 0f);

                var delta = MakeText(row.transform, "", 14, MoodStyle.ColorOf(mood), TextAnchor.MiddleRight);
                var deltaRect = delta.rectTransform;
                deltaRect.anchorMin = new Vector2(1f, 0f);
                deltaRect.anchorMax = new Vector2(1f, 1f);
                deltaRect.pivot = new Vector2(1f, 0.5f);
                deltaRect.anchoredPosition = Vector2.zero;
                deltaRect.sizeDelta = new Vector2(32f, 0f);
                delta.gameObject.SetActive(false);

                _toneRows[i] = new ToneRow
                {
                    Mood = mood,
                    Fill = fillRect,
                    Value = value,
                    Delta = delta
                };
            }

            _toneLead = MakeText(plate.transform, "тон ещё не выбран", 13, Muted, TextAnchor.MiddleLeft);
            var leadRect = _toneLead.rectTransform;
            leadRect.anchorMin = new Vector2(0f, 0f);
            leadRect.anchorMax = new Vector2(1f, 0f);
            leadRect.pivot = new Vector2(0f, 0f);
            leadRect.anchoredPosition = new Vector2(10f, 4f);
            leadRect.sizeDelta = new Vector2(-20f, 18f);
        }

        void BuildTasks()
        {
            var plate = Panel("Tasks", transform, new Color(0.06f, 0.05f, 0.07f, 0.55f));
            _tasksRoot = plate.gameObject;
            var rect = plate.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(16f, -20f);
            rect.sizeDelta = new Vector2(280f, 240f);
            plate.raycastTarget = false;
            var group = plate.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0.72f;
            group.blocksRaycasts = false;
            group.interactable = false;

            _tasksBody = MakeText(plate.transform, "ЗАДАЧИ", 18, Paper, TextAnchor.UpperLeft);
            var body = _tasksBody.rectTransform;
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(16f, 14f);
            body.offsetMax = new Vector2(-14f, -14f);
            _tasksRoot.SetActive(false);
        }

        static Sprite MoodIcon(ShowMood mood)
        {
            switch (mood)
            {
                case ShowMood.Drama: return IllustratedArt.IconTear;
                case ShowMood.Trash: return IllustratedArt.IconDevil;
                default: return IllustratedArt.IconFamily;
            }
        }

        GameObject BuildIntro()
        {
            var panel = Panel("Intro", transform, Ink).gameObject;
            Stretch(panel.GetComponent<RectTransform>());
            var title = MakeText(panel.transform, "Ты — режиссёр реалити.", 64, Paper, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1400f, 90f));
            var sub = MakeText(panel.transform, "Создай драму. Сними хайлайт.", 36, Accent, TextAnchor.MiddleCenter);
            Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1200f, 60f));
            var help = MakeText(panel.transform,
                "хаб: карты в серию, магазин, команда  ·  съёмка  ·  фидбек  ·  6 серий",
                22, Muted, TextAnchor.MiddleCenter);
            Place(help.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(1400f, 40f));
            MakeButton(panel.transform, "НАЧАТЬ", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -160f), new Vector2(280f, 64f), Accent, () => _onStart?.Invoke());
            return panel;
        }

        GameObject BuildFeedback()
        {
            var panel = Panel("Feedback", transform, Ink).gameObject;
            Stretch(panel.GetComponent<RectTransform>());
            var title = MakeText(panel.transform, "РЕАКЦИЯ ЗРИТЕЛЕЙ", 28, Muted, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(800f, 40f));

            for (int i = 0; i < 3; i++)
            {
                var row = Panel("review" + i, panel.transform, PanelColor);
                var rowRect = row.rectTransform;
                rowRect.anchorMin = new Vector2(0.5f, 1f);
                rowRect.anchorMax = new Vector2(0.5f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.sizeDelta = new Vector2(980f, 110f);
                rowRect.anchoredPosition = new Vector2(0f, -110f - i * 124f);

                _reviewAuthors[i] = MakeText(row.transform, "", 22, Accent, TextAnchor.UpperLeft);
                var authorRect = _reviewAuthors[i].rectTransform;
                authorRect.anchorMin = new Vector2(0f, 1f);
                authorRect.anchorMax = new Vector2(0f, 1f);
                authorRect.pivot = new Vector2(0f, 1f);
                authorRect.anchoredPosition = new Vector2(22f, -12f);
                authorRect.sizeDelta = new Vector2(400f, 32f);

                var star = MakeButton(row.transform, "☆ взять", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(520f, -10f), new Vector2(168f, 34f), new Color(0.22f, 0.2f, 0.24f, 1f), ToggleTask);
                star.gameObject.SetActive(false);
                _stars[i] = star;
                _starLabels[i] = star.GetComponentInChildren<Text>();

                _reviewScores[i] = MakeText(row.transform, "", 22, Good, TextAnchor.UpperRight);
                var scoreRect = _reviewScores[i].rectTransform;
                scoreRect.anchorMin = new Vector2(1f, 1f);
                scoreRect.anchorMax = new Vector2(1f, 1f);
                scoreRect.pivot = new Vector2(1f, 1f);
                scoreRect.anchoredPosition = new Vector2(-22f, -12f);
                scoreRect.sizeDelta = new Vector2(120f, 32f);

                _reviewBodies[i] = MakeText(row.transform, "", 26, Paper, TextAnchor.UpperLeft);
                var bodyRect = _reviewBodies[i].rectTransform;
                bodyRect.anchorMin = new Vector2(0f, 0f);
                bodyRect.anchorMax = new Vector2(1f, 1f);
                bodyRect.offsetMin = new Vector2(22f, 12f);
                bodyRect.offsetMax = new Vector2(-22f, -46f);
            }

            _scoreText = MakeText(panel.transform, "7/10", 72, Good, TextAnchor.MiddleCenter);
            Place(_scoreText.rectTransform, new Vector2(0.5f, 0f), new Vector2(-160f, 150f), new Vector2(320f, 90f));
            var scoreCaption = MakeText(panel.transform, "оценка серии", 18, Muted, TextAnchor.MiddleCenter);
            Place(scoreCaption.rectTransform, new Vector2(0.5f, 0f), new Vector2(-160f, 108f), new Vector2(320f, 28f));

            _wishText = MakeText(panel.transform, "", 20, Muted, TextAnchor.MiddleLeft);
            Place(_wishText.rectTransform, new Vector2(0.5f, 0f), new Vector2(180f, 168f), new Vector2(640f, 70f));
            _payText = MakeText(panel.transform, "", 22, new Color(0.95f, 0.82f, 0.45f, 1f), TextAnchor.MiddleLeft);
            Place(_payText.rectTransform, new Vector2(0.5f, 0f), new Vector2(180f, 108f), new Vector2(640f, 36f));

            MakeButton(panel.transform, "ДАЛЬШЕ", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 42f), new Vector2(320f, 58f), Accent, () => _feedbackNext?.Invoke());
            return panel;
        }

        GameObject BuildVision()
        {
            var panel = Panel("Vision", transform, new Color(0.05f, 0.045f, 0.06f, 0.97f)).gameObject;
            Stretch(panel.GetComponent<RectTransform>());
            var title = MakeText(panel.transform, "ДАЛЬШЕ НА ДЖЕМЕ", 40, Paper, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(900f, 52f));
            var sub = MakeText(panel.transform, "неиграбельный макет  ·  этого ещё нет в коде", 20, Accent, TextAnchor.MiddleCenter);
            Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(900f, 30f));

            var draft = BuildColumn(panel.transform, -560f, "ДРАФТ",
                "3 карты на стол · берёшь 2 из 3",
                "");
            AddPortrait(draft.transform, "ПОДЖОГ", "взять", IllustratedArt.IconFire, -155f);
            AddPortrait(draft.transform, "РАЗОЗЛИТЬ", "взять", IllustratedArt.IconAnger, 0f);
            AddPortrait(draft.transform, "НЕТ ВОДЫ", "мимо", IllustratedArt.IconWater, 155f);

            var crew = BuildColumn(panel.transform, 0f, "КОМАНДА",
                "уровни 1–5, старт с 1",
                "");
            AddPortrait(crew.transform, "УЧАСТНИКИ", "ур. 1\n2 человека\nапгрейд 70", IllustratedArt.IconCast, -155f);
            AddPortrait(crew.transform, "ОПЕРАТОРЫ", "ур. 1\n1 слот кадра\nапгрейд 90", IllustratedArt.IconCamera, 0f);
            AddPortrait(crew.transform, "СЦЕНАРИСТЫ", "ур. 1\n2 розыгрыша\nапгрейд 90", IllustratedArt.IconPen, 155f);

            BuildColumn(panel.transform, 560f, "МАГАЗИН",
                "ивенты 100–180",
                "Поджог — уже есть\nСлух о треугольнике — 140\nКрыса — 160\nВыключить wifi — 120\nИсповедь — 180\n\nкаталог 15–20 карт");

            var foot = MakeText(panel.transform,
                "Сезон на 6 серий  ·  деньги с оценки  ·  желание зрителей даёт бонус  ·  save между сериями",
                18, Muted, TextAnchor.MiddleCenter);
            Place(foot.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 88f), new Vector2(1400f, 30f));
            MakeButton(panel.transform, "ПРОГНАТЬ ДЕМО ЕЩЁ РАЗ", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 28f), new Vector2(420f, 52f), new Color(0.22f, 0.2f, 0.24f, 1f), () => _onReplay?.Invoke());
            return panel;
        }

        Image BuildColumn(Transform parent, float x, string title, string caption, string body)
        {
            var col = Panel(title, parent, PanelColor);
            var rect = col.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(500f, 640f);
            rect.anchoredPosition = new Vector2(x, 10f);

            var header = MakeText(col.transform, title, 28, Accent, TextAnchor.UpperLeft);
            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.anchoredPosition = new Vector2(24f, -20f);
            headerRect.sizeDelta = new Vector2(-48f, 40f);

            var cap = MakeText(col.transform, caption, 18, Muted, TextAnchor.UpperLeft);
            var capRect = cap.rectTransform;
            capRect.anchorMin = new Vector2(0f, 1f);
            capRect.anchorMax = new Vector2(1f, 1f);
            capRect.pivot = new Vector2(0f, 1f);
            capRect.anchoredPosition = new Vector2(24f, -68f);
            capRect.sizeDelta = new Vector2(-48f, 70f);

            var text = MakeText(col.transform, body, 24, Paper, TextAnchor.UpperLeft);
            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(24f, 24f);
            textRect.offsetMax = new Vector2(-24f, -150f);
            return col;
        }

        void AddPortrait(Transform parent, string title, string body, Sprite art, float x)
        {
            var frame = Panel(title, parent, new Color(0.42f, 0.28f, 0.16f, 1f));
            var rect = frame.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -150f);
            rect.sizeDelta = new Vector2(148f, 280f);
            frame.raycastTarget = false;

            var inner = Panel("inner", frame.transform, new Color(0.95f, 0.91f, 0.84f, 1f));
            var innerRect = inner.rectTransform;
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(6f, 6f);
            innerRect.offsetMax = new Vector2(-6f, -6f);
            inner.raycastTarget = false;

            var picture = Panel("art", inner.transform, new Color(0.9f, 0.86f, 0.78f, 1f));
            var pictureRect = picture.rectTransform;
            pictureRect.anchorMin = new Vector2(0f, 1f);
            pictureRect.anchorMax = new Vector2(1f, 1f);
            pictureRect.pivot = new Vector2(0.5f, 1f);
            pictureRect.anchoredPosition = new Vector2(0f, -8f);
            pictureRect.sizeDelta = new Vector2(-12f, 120f);
            picture.raycastTarget = false;
            picture.preserveAspect = true;
            picture.color = Color.white;
            picture.sprite = art;

            var header = MakeText(inner.transform, title, 14, new Color(0.18f, 0.12f, 0.1f), TextAnchor.MiddleCenter);
            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -132f);
            headerRect.sizeDelta = new Vector2(-8f, 28f);

            var text = MakeText(inner.transform, body, 13, new Color(0.35f, 0.26f, 0.2f), TextAnchor.UpperCenter);
            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(6f, 8f);
            textRect.offsetMax = new Vector2(-6f, -162f);
        }

        void LateUpdate()
        {
            if (_toast.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
                _toast.gameObject.SetActive(false);

            for (int i = 0; i < _toneRows.Length; i++)
            {
                var row = _toneRows[i];
                if (row == null || !row.Delta.gameObject.activeSelf)
                    continue;
                if (Time.unscaledTime > row.DeltaUntil)
                    row.Delta.gameObject.SetActive(false);
            }

            if (!_tagsRoot.activeSelf || Camera.main == null)
                return;

            for (int i = 0; i < _tags.Count; i++)
            {
                var tag = _tags[i];
                if (tag.Target == null)
                    continue;
                string value = tag.Pull != null ? tag.Pull() : "";
                bool show = !string.IsNullOrEmpty(value);
                tag.Rect.gameObject.SetActive(show);
                if (!show)
                    continue;
                tag.Text.text = value;
                Vector3 screen = Camera.main.WorldToScreenPoint(tag.Target.position);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out var local))
                    tag.Rect.anchoredPosition = local + tag.Offset;
            }

            for (int i = 0; i < _bubbles.Count; i++)
            {
                var bubble = _bubbles[i];
                if (bubble.Target == null)
                    continue;
                string value = bubble.Pull != null ? bubble.Pull() : "";
                bool show = !string.IsNullOrEmpty(value);
                bubble.Rect.gameObject.SetActive(show);
                if (!show)
                    continue;
                bool thought = bubble.Thought != null && bubble.Thought();
                bubble.Speech.SetActive(!thought);
                bubble.ThoughtCloud.SetActive(thought);
                if (value != bubble.Shown || thought != bubble.WasThought)
                    LayoutBubble(bubble, value, thought);
                Vector3 screen = Camera.main.WorldToScreenPoint(bubble.Target.position);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out var local))
                    bubble.Rect.anchoredPosition = local + bubble.Offset;
            }
        }

        IEnumerator FlyRoutine(Texture2D photo, int slot, Vector2 screen, bool framed, int epoch)
        {
            _slots[slot].Placeholder.gameObject.SetActive(false);
            var polaroid = new GameObject("Polaroid", typeof(RectTransform), typeof(Image));
            polaroid.transform.SetParent(transform, false);
            var rt = polaroid.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(260f, 320f);
            var bg = polaroid.GetComponent<Image>();
            bg.color = new Color(0.97f, 0.96f, 0.93f, 1f);
            bg.raycastTarget = false;

            if (framed)
            {
                var ring = new GameObject("ring", typeof(RectTransform), typeof(Image));
                ring.transform.SetParent(polaroid.transform, false);
                var ringRt = ring.GetComponent<RectTransform>();
                ringRt.anchorMin = Vector2.zero;
                ringRt.anchorMax = Vector2.one;
                ringRt.offsetMin = new Vector2(6f, 6f);
                ringRt.offsetMax = new Vector2(-6f, -6f);
                var ringImg = ring.GetComponent<Image>();
                ringImg.color = new Color(0.93f, 0.76f, 0.28f, 1f);
                ringImg.raycastTarget = false;
            }

            var shotGo = new GameObject("shot", typeof(RectTransform), typeof(Image));
            shotGo.transform.SetParent(polaroid.transform, false);
            var shotRt = shotGo.GetComponent<RectTransform>();
            shotRt.anchorMin = Vector2.zero;
            shotRt.anchorMax = Vector2.one;
            shotRt.offsetMin = new Vector2(12f, 12f);
            shotRt.offsetMax = new Vector2(-12f, -12f);
            var shot = shotGo.GetComponent<Image>();
            shot.raycastTarget = false;
            shot.preserveAspect = false;
            shot.sprite = Sprite.Create(photo, new Rect(0f, 0f, photo.width, photo.height), new Vector2(0.5f, 0.5f), 100f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out var start);
            var slotScreen = RectTransformUtility.WorldToScreenPoint(null, _slots[slot].Well.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, slotScreen, null, out var end);
            float spin = slot == 0 ? -18f : 16f;
            float t = 0f;
            const float dur = 0.55f;
            while (t < dur)
            {
                if (epoch != _slotEpoch || rt == null)
                    yield break;
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float e = 1f - (1f - k) * (1f - k);
                var pos = Vector2.Lerp(start, end, e);
                pos.y += Mathf.Sin(k * Mathf.PI) * 110f;
                rt.anchoredPosition = pos;
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(spin, slot == 0 ? -3f : 4f, e));
                float s = Mathf.Lerp(1.05f, 0.62f, e);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            if (epoch != _slotEpoch || rt == null)
                yield break;

            rt.SetParent(_slots[slot].Well, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-8f, -8f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        static void DestroyPolaroid(GameObject go)
        {
            if (go == null)
                return;
            var shot = go.transform.Find("shot");
            if (shot != null)
            {
                var image = shot.GetComponent<Image>();
                if (image != null && image.sprite != null)
                {
                    var sprite = image.sprite;
                    var tex = sprite.texture;
                    image.sprite = null;
                    UnityEngine.Object.Destroy(sprite);
                    if (tex != null)
                        UnityEngine.Object.Destroy(tex);
                }
            }

            UnityEngine.Object.Destroy(go);
        }

        IEnumerator FadeFlash(Color color, float duration)
        {
            _flash.gameObject.SetActive(true);
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var c = color;
                c.a = color.a * (1f - t / duration);
                _flash.color = c;
                yield return null;
            }

            _flash.gameObject.SetActive(false);
            _flashRoutine = null;
        }

        Image Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        GameObject NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            return go;
        }

        Text MakeText(Transform parent, string value, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject("text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        Button MakeButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, Color color, Action onClick)
        {
            var image = Panel(label, parent, color);
            var rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.onClick.AddListener(() =>
            {
                Sfx.Play(Cue.Click, 0.35f);
                onClick();
            });
            var text = MakeText(image.transform, label, 20, Paper, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        static Sprite _bubbleRound;
        static Sprite _bubbleTail;
        static Sprite _bubbleCloud;

        static Sprite BubbleRound()
        {
            if (_bubbleRound != null)
                return _bubbleRound;
            const int n = 64;
            const float radius = 22f;
            const float outline = 4f;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = Stadium(x + 0.5f, y + 0.5f, n, n, radius);
                    px[y * n + x] = d > 0.8f
                        ? new Color32(0, 0, 0, 0)
                        : d > -outline
                            ? new Color32(0, 0, 0, 255)
                            : new Color32(255, 255, 255, 255);
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            _bubbleRound = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
            _bubbleRound.hideFlags = HideFlags.HideAndDontSave;
            return _bubbleRound;
        }

        static Sprite BubbleCloud()
        {
            if (_bubbleCloud != null)
                return _bubbleCloud;
            const int w = 256;
            const int h = 168;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Bilinear;
            var blobs = new[]
            {
                new Vector3(78f, 78f, 52f),
                new Vector3(128f, 96f, 62f),
                new Vector3(180f, 78f, 50f),
                new Vector3(128f, 62f, 46f)
            };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = 999f;
                    for (int i = 0; i < blobs.Length; i++)
                    {
                        float dx = x + 0.5f - blobs[i].x;
                        float dy = y + 0.5f - blobs[i].y;
                        d = Mathf.Min(d, Mathf.Sqrt(dx * dx + dy * dy) - blobs[i].z);
                    }

                    px[y * w + x] = d > 1.2f
                        ? new Color32(0, 0, 0, 0)
                        : d > -5f
                            ? new Color32(0, 0, 0, 255)
                            : new Color32(255, 255, 255, 255);
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            _bubbleCloud = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            _bubbleCloud.hideFlags = HideFlags.HideAndDontSave;
            return _bubbleCloud;
        }

        static Sprite BubbleTail()
        {
            if (_bubbleTail != null)
                return _bubbleTail;
            const int w = 32;
            const int h = 22;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                    px[y * w + x] = TailColor(x + 0.5f, y + 0.5f);
            }

            tex.SetPixels32(px);
            tex.Apply();
            _bubbleTail = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 100f);
            _bubbleTail.hideFlags = HideFlags.HideAndDontSave;
            return _bubbleTail;
        }

        static Color32 TailColor(float x, float y)
        {
            bool ink = InTail(x, y, 3f, 20f, 29f, 3f);
            bool paper = InTail(x, y, 6f, 20f, 26f, 6f);
            if (paper)
                return new Color32(255, 255, 255, 255);
            if (ink)
                return new Color32(0, 0, 0, 255);
            return new Color32(0, 0, 0, 0);
        }

        static bool InTail(float x, float y, float left, float top, float right, float tip)
        {
            if (y < tip || y > top)
                return false;
            float t = (y - tip) / (top - tip);
            float min = Mathf.Lerp(16f, left, t);
            float max = Mathf.Lerp(16f, right, t);
            return x >= min && x <= max;
        }

        static float Stadium(float x, float y, float w, float h, float r)
        {
            float dx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - r);
            float dy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - r);
            float ax = Mathf.Max(dx, 0f);
            float ay = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
        }
    }
}
