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

        static Color ScoreColor(float score)
        {
            float t = Mathf.InverseLerp(1f, 10f, score);
            return Color.HSVToRGB(t * (120f / 360f), 0.78f, 0.95f);
        }
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
        Text _bannerText;
        Transform _cardBar;
        RectTransform _camRect;
        Button _camButton;
        Button _doneButton;
        Button _hubButton;
        bool _gated;
        bool _gateCards;
        string _gateCard;
        bool _gateCamera;
        bool _gateDone;
        bool _gateHub;
        RectTransform _doneRect;
        public RectTransform CardBarRect => _cardBar as RectTransform;
        RectTransform _aim;
        public RectTransform AimRect => _aim;
        public RectTransform CardRect(string id)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Def != null && _cards[i].Def.id == id && _cards[i].Root != null)
                    return _cards[i].Root;
            }

            return CardBarRect;
        }
        public RectTransform CameraRect => _camRect;
        public RectTransform DoneRect => _doneRect;
        RectTransform _hubRect;
        public RectTransform HubRect => _hubRect;
        public RectTransform[] SlotRects()
        {
            var list = new List<RectTransform>();
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Root != null)
                    list.Add(_slots[i].Root.transform as RectTransform);
            }

            return list.ToArray();
        }
        Text _hint;
        Text _toast;
        Text _camLabel;
        Image _endPlate;
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
        Text _tubeTitle;
        Text _metaLine;
        Text _commentHead;
        RectTransform _likeFill;
        Text _viewsText;
        Text _qualityText;
        Text _linkText;
        Text _bonusText;
        Text _cutList;
        Text _scoreText;
        Text _wishText;
        Text _payText;
        GameObject _player;
        RectTransform _likeTrack;
        readonly Image[] _frames = new Image[3];
        readonly Sprite[] _frameSprites = new Sprite[3];
        readonly GameObject[] _reviewRows = new GameObject[3];
        readonly Text[] _reviewLetters = new Text[3];
        GameObject _tasksRoot;
        Text _tasksBody;
        bool _taskTaken;
        int _offerRow = -1;

        public bool TaskTaken => _taskTaken;
        public int UsedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _cards.Count; i++)
                {
                    if (_cards[i].Used)
                        n++;
                }

                return n;
            }
        }
        Text _episodeTitle;
        Text _footage;
        Text _libraryCount;
        Text _usedCount;
        Text _handCount;
        Text _cash;
        RectTransform _hellFill;
        float _hell = 10f;
        bool _handLocked;
        Text _frameBody;
        GameObject _framePlate;
        Button _pauseButton;
        RectTransform _castRoot;
        readonly List<Text> _castMood = new List<Text>();
        readonly List<RectTransform> _stressFill = new List<RectTransform>();
        readonly List<RectTransform> _angerFill = new List<RectTransform>();
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
            public Button Button;
            public CanvasGroup Group;
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

        public void SetHandLocked(bool locked)
        {
            _handLocked = locked;
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                bool poor = Poor(card);
                if (card.Button != null)
                    card.Button.interactable = !locked && !card.Used && !poor;
                if (card.Group != null)
                    card.Group.alpha = (locked || poor) && !card.Used ? 0.45f : 1f;
            }

            ApplyGates();
        }

        bool Poor(Card card)
        {
            return card.Def != null && card.Def.cost > _hell + 0.001f;
        }

        // Урок: жива только та кнопка, которую босс только что потребовал.
        public void SetActionGates(bool cards, string onlyCard, bool camera, bool done, bool hub)
        {
            _gated = true;
            _gateCards = cards;
            _gateCard = onlyCard;
            _gateCamera = camera;
            _gateDone = done;
            _gateHub = hub;
            ApplyGates();
        }

        public void ClearActionGates()
        {
            if (!_gated)
                return;
            _gated = false;
            if (_camButton != null)
                _camButton.interactable = true;
            if (_doneButton != null)
                _doneButton.interactable = true;
            if (_hubButton != null)
                _hubButton.interactable = true;
            SetHandLocked(false);
        }

        void ApplyGates()
        {
            if (!_gated)
                return;
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card.Button == null)
                    continue;
                bool mine = string.IsNullOrEmpty(_gateCard) || (card.Def != null && card.Def.id == _gateCard);
                bool poor = Poor(card);
                card.Button.interactable = _gateCards && mine && !card.Used && !poor;
                if (card.Group != null && !card.Used)
                    card.Group.alpha = poor || _handLocked ? 0.45f : 1f;
            }

            if (_camButton != null)
                _camButton.interactable = _gateCamera;
            if (_doneButton != null)
                _doneButton.interactable = _gateDone;
            if (_hubButton != null)
                _hubButton.interactable = _gateHub;
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
                if (_cards[i].Def == null || _cards[i].Def.id != id)
                    continue;
                var card = _cards[i];
                _cards.RemoveAt(i);
                StartCoroutine(FlyOut(card));
                return;
            }
        }

        IEnumerator FlyOut(Card card)
        {
            if (card.Root == null)
                yield break;
            if (card.Button != null)
                card.Button.interactable = false;
            var rect = card.Root;
            rect.SetParent(transform, true);
            var element = rect.GetComponent<LayoutElement>();
            if (element != null)
                element.ignoreLayout = true;
            Vector2 start = rect.anchoredPosition;
            Vector3 scale = rect.localScale;
            float t = 0f;
            const float dur = 0.42f;
            while (t < dur && rect != null)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - (1f - Mathf.Clamp01(t / dur)) * (1f - Mathf.Clamp01(t / dur));
                rect.anchoredPosition = start + new Vector2(36f * k, 240f * k);
                rect.localScale = Vector3.Lerp(scale, scale * 0.7f, k);
                rect.localRotation = Quaternion.Euler(0f, 0f, -18f * k);
                if (card.Group != null)
                    card.Group.alpha = 1f - k;
                yield return null;
            }

            if (rect != null)
                Destroy(rect.gameObject);
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
            float span = (capacity - 1) * 58f;
            for (int i = 0; i < _slots.Length; i++)
            {
                bool on = i < capacity;
                _slots[i].Root.SetActive(on);
                if (!on)
                    continue;
                var rect = _slots[i].Root.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(-span * 0.5f + i * 58f, -52f);
            }

            SetFootage(0, capacity);
        }

        void BuildCastRow(int index)
        {
            var row = Panel("who" + index, _castRoot, new Color(0.1f, 0.08f, 0.12f, 1f));
            var rect = row.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 96f);
            rect.anchoredPosition = new Vector2(0f, -index * 102f);
            row.raycastTarget = false;

            var name = MakeText(row.transform, "", 18, Paper, TextAnchor.UpperLeft);
            var nameRect = name.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.anchoredPosition = new Vector2(12f, -8f);
            nameRect.sizeDelta = new Vector2(-20f, 24f);

            var trait = MakeText(row.transform, "", 13, Muted, TextAnchor.UpperLeft);
            var traitRect = trait.rectTransform;
            traitRect.anchorMin = new Vector2(0f, 1f);
            traitRect.anchorMax = new Vector2(1f, 1f);
            traitRect.pivot = new Vector2(0f, 1f);
            traitRect.anchoredPosition = new Vector2(12f, -32f);
            traitRect.sizeDelta = new Vector2(-20f, 18f);

            var mood = MakeText(row.transform, "", 13, new Color(0.95f, 0.82f, 0.45f, 1f), TextAnchor.UpperLeft);
            var moodRect = mood.rectTransform;
            moodRect.anchorMin = new Vector2(0f, 1f);
            moodRect.anchorMax = new Vector2(1f, 1f);
            moodRect.pivot = new Vector2(0f, 1f);
            moodRect.anchoredPosition = new Vector2(12f, -50f);
            moodRect.sizeDelta = new Vector2(-20f, 18f);
            _castMood.Add(mood);

            _stressFill.Add(Meter(row.transform, new Color(0.35f, 0.82f, 0.45f, 1f), -72f));
            _angerFill.Add(Meter(row.transform, new Color(0.86f, 0.28f, 0.24f, 1f), -82f));
        }

        RectTransform Meter(Transform parent, Color color, float y)
        {
            var track = Panel("track", parent, new Color(0f, 0f, 0f, 0.45f));
            var trackRect = track.rectTransform;
            trackRect.anchorMin = trackRect.anchorMax = new Vector2(0f, 1f);
            trackRect.pivot = new Vector2(0f, 1f);
            trackRect.anchoredPosition = new Vector2(12f, y);
            trackRect.sizeDelta = new Vector2(78f, 6f);
            track.raycastTarget = false;
            var fill = Panel("fill", track.transform, color);
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(0f, 6f);
            fill.raycastTarget = false;
            return fillRect;
        }

        public void SetEpisodeTitle(string text)
        {
            if (_episodeTitle != null)
                _episodeTitle.text = text;
        }

        public struct CastFace
        {
            public string name;
            public string trait;
            public string mood;
            public int stress;
            public int anger;
        }

        public void SetCast(CastFace[] faces)
        {
            if (_castRoot == null)
                return;
            int n = faces != null ? faces.Length : 0;
            if (_castRoot.childCount != n)
            {
                for (int i = _castRoot.childCount - 1; i >= 0; i--)
                    Destroy(_castRoot.GetChild(i).gameObject);
                _castMood.Clear();
                _stressFill.Clear();
                _angerFill.Clear();
                for (int i = 0; i < n; i++)
                    BuildCastRow(i);
            }

            for (int i = 0; i < n; i++)
            {
                var face = faces[i];
                var row = _castRoot.GetChild(i);
                var labels = row.GetComponentsInChildren<Text>();
                if (labels.Length > 0)
                    labels[0].text = face.name;
                if (labels.Length > 1)
                    labels[1].text = face.trait;
                if (i < _castMood.Count)
                    _castMood[i].text = face.mood;
                if (i < _stressFill.Count)
                    _stressFill[i].sizeDelta = new Vector2(78f * Mathf.Clamp01(face.stress / 100f), 6f);
                if (i < _angerFill.Count)
                    _angerFill[i].sizeDelta = new Vector2(78f * Mathf.Clamp01(face.anger / 100f), 6f);
            }
        }

        public void SetFrame(string body, bool on)
        {
            if (_framePlate == null)
                return;
            bool show = on && !string.IsNullOrEmpty(body);
            _framePlate.SetActive(show);
            if (show)
                _frameBody.text = "В кадре:\n" + body;
        }

        public void SetHandMeta(int hand, int handMax, int library, int used)
        {
            if (_handCount != null)
                _handCount.text = "РУКА  " + hand + " / " + handMax;
            if (_libraryCount != null)
                _libraryCount.text = library.ToString();
            if (_usedCount != null)
                _usedCount.text = used.ToString();
        }

        public void SetHell(float current, float max)
        {
            _hell = current;
            if (max < 1f)
                max = 1f;
            if (_cash != null)
                _cash.text = "HELL  " + HellToken.Format(current) + " / " + HellToken.Format(max);
            if (_hellFill != null)
                _hellFill.sizeDelta = new Vector2(160f * Mathf.Clamp01(current / max), 8f);
            if (!_gated)
                SetHandLocked(_handLocked);
            else
                ApplyGates();
        }

        public void SetFootage(int count, int capacity)
        {
            if (_footage != null)
                _footage.text = "ФУТАЖ  " + count + " / " + capacity;
        }

        public void SetPauseEnabled(bool on)
        {
            if (_pauseButton != null)
                _pauseButton.interactable = on;
        }

        public void SetPaused(bool on)
        {
            if (_pauseButton == null)
                return;
            var label = _pauseButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = on ? "ПАУЗА" : "ПАУЗА  ESC";
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

        public void PlaySlate(int scene, Action onBlack)
        {
            StartCoroutine(SlateRoutine(scene, onBlack));
        }

        IEnumerator SlateRoutine(int scene, Action onBlack)
        {
            var root = new GameObject("Slate", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(transform, false);
            var rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            var black = root.GetComponent<Image>();
            black.color = new Color(0f, 0f, 0f, 0f);
            black.raycastTarget = true;

            var board = new GameObject("board", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            board.transform.SetParent(root.transform, false);
            var boardRect = board.GetComponent<RectTransform>();
            boardRect.anchorMin = boardRect.anchorMax = new Vector2(0.5f, 0.5f);
            boardRect.pivot = new Vector2(0.5f, 0.5f);
            boardRect.sizeDelta = new Vector2(980f, 620f);
            var boardImg = board.GetComponent<Image>();
            boardImg.sprite = IllustratedArt.SlateBoard;
            boardImg.color = Color.white;
            boardImg.type = Image.Type.Simple;
            boardImg.raycastTarget = false;
            var group = board.GetComponent<CanvasGroup>();

            var stick = new GameObject("stick", typeof(RectTransform), typeof(Image));
            stick.transform.SetParent(board.transform, false);
            var stickRect = stick.GetComponent<RectTransform>();
            stickRect.anchorMin = stickRect.anchorMax = new Vector2(0f, 1f);
            stickRect.pivot = new Vector2(0f, 1f);
            stickRect.anchoredPosition = new Vector2(22f, -18f);
            stickRect.sizeDelta = new Vector2(936f, 150f);
            var stickImg = stick.GetComponent<Image>();
            stickImg.sprite = IllustratedArt.SlateStick;
            stickImg.color = Color.white;
            stickImg.raycastTarget = false;

            var caption = MakeText(board.transform, "СЦЕНА", 36, new Color(0.9f, 0.88f, 0.82f, 1f), TextAnchor.MiddleCenter);
            var capRect = caption.rectTransform;
            capRect.anchorMin = capRect.anchorMax = new Vector2(0.5f, 0.5f);
            capRect.sizeDelta = new Vector2(800f, 48f);
            capRect.anchoredPosition = new Vector2(0f, 20f);
            var number = MakeText(board.transform, scene.ToString(), 140, Color.white, TextAnchor.MiddleCenter);
            var numRect = number.rectTransform;
            numRect.anchorMin = numRect.anchorMax = new Vector2(0.5f, 0.5f);
            numRect.sizeDelta = new Vector2(800f, 170f);
            numRect.anchoredPosition = new Vector2(0f, -90f);

            stickRect.localRotation = Quaternion.Euler(0f, 0f, 38f);
            float t = 0f;
            const float drop = 0.5f;
            while (t < drop)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / drop), 3f);
                black.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.82f, k));
                boardRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(640f, 0f, k));
                boardRect.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, k);
                yield return null;
            }

            boardRect.anchoredPosition = Vector2.zero;
            boardRect.localScale = Vector3.one;
            black.color = new Color(0f, 0f, 0f, 0.82f);
            yield return new WaitForSecondsRealtime(0.14f);

            t = 0f;
            const float clap = 0.16f;
            bool hit = false;
            while (t < clap)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / clap);
                stickRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(38f, 0f, k * k));
                if (!hit && k > 0.86f)
                {
                    hit = true;
                    Sfx.Play(Cue.Slap, 1f, 0.66f);
                    boardRect.anchoredPosition = new Vector2(0f, -8f);
                }

                yield return null;
            }

            stickRect.localRotation = Quaternion.identity;
            t = 0f;
            while (t < 0.12f)
            {
                t += Time.unscaledDeltaTime;
                boardRect.anchoredPosition = Vector2.Lerp(new Vector2(0f, -8f), Vector2.zero, t / 0.12f);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.85f);
            t = 0f;
            const float fade = 2.4f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fade);
                float e = k * k * (3f - 2f * k);
                black.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.82f, 1f, e));
                group.alpha = 1f - e;
                yield return null;
            }

            black.color = Color.black;
            group.alpha = 0f;
            yield return new WaitForSecondsRealtime(0.55f);
            onBlack?.Invoke();
            root.transform.SetAsLastSibling();
            t = 0f;
            const float outFade = 1.15f;
            while (t < outFade)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / outFade);
                black.color = new Color(0f, 0f, 0f, 1f - k * k);
                yield return null;
            }

            Destroy(root);
        }

        public void SetWrapReady(bool on)
        {
            if (_endPlate == null)
                return;
            _endPlate.color = on
                ? new Color(0.86f, 0.62f, 0.16f, 1f)
                : new Color(0.22f, 0.2f, 0.24f, 1f);
        }

        public void SetCaptureMode(bool on)
        {
            _captureBanner.SetActive(on);
            _camLabel.text = on ? "КАМЕРА ВКЛ" : "КАМЕРА   C";
            if (_bannerText != null && on)
                _bannerText.text = "REC  00 / 03";
        }

        public void SetRecord(float seconds, bool on)
        {
            if (_bannerText == null || !on)
                return;
            _captureBanner.SetActive(true);
            int sec = Mathf.FloorToInt(seconds);
            _bannerText.text = "REC  " + sec.ToString("00") + " / 03";
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

        public void ShowFeedback(FeedbackResult result, IReadOnlyList<CapturedMoment> moments, string title, int pay, Action onNext)
        {
            _hud.SetActive(false);
            SetTagsVisible(false);
            if (_toneRoot != null)
                _toneRoot.SetActive(false);
            if (_tasksRoot != null)
                _tasksRoot.SetActive(false);
            _feedback.SetActive(true);
            _feedback.transform.SetAsLastSibling();
            SetCaptureMode(false);
            _taskTaken = false;
            _offerRow = -1;

            int views = Mathf.RoundToInt(8000f + result.score * 8000f);
            int likes = Mathf.RoundToInt(result.score * 10f);
            string rating = Comma(result.score) + " / 10";
            _tubeTitle.text = string.IsNullOrEmpty(title) ? "Серия" : title;
            _metaLine.text = Grouped(views) + " просмотров   ·   нравится " + likes + "%   ·   рейтинг " + rating;
            var likeSize = _likeFill.sizeDelta;
            likeSize.x = 260f * Mathf.Clamp01(result.score / 10f);
            _likeFill.sizeDelta = likeSize;

            int comments = result.reviews != null ? result.reviews.Count : 0;
            _commentHead.text = "КОММЕНТАРИИ   ·   " + comments;
            for (int i = 0; i < 3; i++)
            {
                bool has = result.reviews != null && i < result.reviews.Count;
                _reviewRows[i].SetActive(has);
                if (!has)
                    continue;
                var review = result.reviews[i];
                bool offer = review.offer;
                string author = review.author ?? "";
                _reviewLetters[i].text = author.Length > 0 ? author.Substring(0, 1) : "?";
                _reviewAuthors[i].text = offer ? "★  " + author + "   ·   заказ зрителей" : author;
                _reviewBodies[i].text = review.body;
                _reviewScores[i].gameObject.SetActive(!offer);
                _reviewScores[i].text = "+" + Grouped(Mathf.RoundToInt(review.score * review.score * 16f));
                _stars[i].gameObject.SetActive(offer);
                if (!offer)
                    continue;
                _offerRow = i;
                _starLabels[i].text = "☆ взять";
                _starLabels[i].color = Paper;
                _stars[i].image.color = new Color(0.16f, 0.14f, 0.18f, 1f);
            }

            FillFrames(moments);
            _viewsText.text = Grouped(views);
            _scoreText.text = rating;
            _qualityText.text = Comma(FrameQuality(moments)) + " / 10";
            _linkText.text = LinkPercent(moments) + "%";
            int bright = BrightCount(moments);
            _bonusText.text = "+0,0 (" + bright + ")";
            _payText.text = "+" + pay + " кр";
            _wishText.text = result.payLine ?? "";
            _cutList.text = CutList(moments);
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
        Action _onHub;
        Action _onPause;

        public void BindFlow(Action onStart, Action onEnd, Action onCamera, Action onReplay)
        {
            _onStart = onStart;
            _onEnd = onEnd;
            _onCamera = onCamera;
            _onReplay = onReplay;
        }

        public void BindExit(Action onHub)
        {
            _onHub = onHub;
        }

        public void BindPause(Action onPause)
        {
            _onPause = onPause;
        }

        void Construct()
        {
            _tagsRoot = NewRect("Tags", transform);
            _hud = NewRect("Hud", transform);
            var aim = NewRect("Aim", transform);
            _aim = aim.GetComponent<RectTransform>();
            _aim.anchorMin = _aim.anchorMax = new Vector2(0.5f, 0.5f);
            _aim.pivot = new Vector2(0.5f, 0.5f);
            _aim.sizeDelta = new Vector2(200f, 240f);
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
            var left = Panel("cast", _hud.transform, new Color(0.05f, 0.04f, 0.07f, 0.94f));
            var leftRect = left.rectTransform;
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0f, 1f);
            leftRect.pivot = new Vector2(0f, 0.5f);
            leftRect.offsetMin = new Vector2(0f, 332f);
            leftRect.offsetMax = new Vector2(268f, 0f);

            _episodeTitle = MakeText(left.transform, "СЕРИЯ 1\nты режиссёр, не участник", 20, Paper, TextAnchor.UpperLeft);
            var titleRect = _episodeTitle.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(16f, -14f);
            titleRect.sizeDelta = new Vector2(236f, 64f);

            var cam = MakeButton(left.transform, "КАМЕРА   C", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -86f), new Vector2(236f, 44f), Accent, () => _onCamera?.Invoke());
            _camButton = cam;
            _camRect = cam.transform as RectTransform;
            _camLabel = cam.GetComponentInChildren<Text>();

            var castGo = NewRect("rows", left.transform);
            _castRoot = castGo.GetComponent<RectTransform>();
            _castRoot.anchorMin = new Vector2(0f, 1f);
            _castRoot.anchorMax = new Vector2(1f, 1f);
            _castRoot.pivot = new Vector2(0.5f, 1f);
            _castRoot.anchoredPosition = new Vector2(0f, -142f);
            _castRoot.sizeDelta = new Vector2(-16f, 520f);

            _footage = MakeText(_hud.transform, "ФУТАЖ  0 / 1", 22, Paper, TextAnchor.MiddleCenter);
            var footRect = _footage.rectTransform;
            footRect.anchorMin = footRect.anchorMax = new Vector2(0.5f, 1f);
            footRect.pivot = new Vector2(0.5f, 1f);
            footRect.anchoredPosition = new Vector2(-40f, -14f);
            footRect.sizeDelta = new Vector2(280f, 36f);

            for (int i = 0; i < _slots.Length; i++)
            {
                var slotImg = Panel("slot" + i, _hud.transform, new Color(0.1f, 0.09f, 0.08f, 0.92f));
                var slotRect = slotImg.rectTransform;
                slotRect.anchorMin = slotRect.anchorMax = new Vector2(0.5f, 1f);
                slotRect.pivot = new Vector2(0.5f, 1f);
                slotRect.sizeDelta = new Vector2(52f, 36f);
                slotRect.anchoredPosition = new Vector2(-90f + i * 58f, -52f);
                var placeholder = MakeText(slotImg.transform, (i + 1).ToString(), 14, Muted, TextAnchor.MiddleCenter);
                Stretch(placeholder.rectTransform);
                var well = NewRect("well", slotImg.transform);
                _slots[i] = new Slot { Root = slotImg.gameObject, Well = well.GetComponent<RectTransform>(), Placeholder = placeholder };
            }

            var pause = MakeButton(_hud.transform, "ПАУЗА  ESC", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(210f, -18f), new Vector2(150f, 44f), new Color(0.16f, 0.14f, 0.18f, 1f), () => _onPause?.Invoke());
            _pauseButton = pause;

            var end = MakeButton(_hud.transform, "СНЯТО!", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(390f, -18f), new Vector2(160f, 44f), new Color(0.75f, 0.16f, 0.18f, 1f), () => _onEnd?.Invoke());
            _doneButton = end;
            var leave = MakeButton(_hud.transform, "ХАБ", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(560f, -18f), new Vector2(90f, 44f), new Color(0.16f, 0.14f, 0.18f, 1f), () => _onHub?.Invoke());
            _hubButton = leave;
            _hubRect = leave.transform as RectTransform;
            _endPlate = end.GetComponent<Image>();
            _doneRect = end.transform as RectTransform;
            var clap = new GameObject("clap", typeof(RectTransform), typeof(Image));
            clap.transform.SetParent(end.transform, false);
            var clapRect = clap.GetComponent<RectTransform>();
            clapRect.anchorMin = new Vector2(0f, 0.5f);
            clapRect.anchorMax = new Vector2(0f, 0.5f);
            clapRect.pivot = new Vector2(0f, 0.5f);
            clapRect.anchoredPosition = new Vector2(8f, 0f);
            clapRect.sizeDelta = new Vector2(32f, 32f);
            var clapImg = clap.GetComponent<Image>();
            clapImg.sprite = IllustratedArt.IconClap;
            clapImg.preserveAspect = true;
            clapImg.raycastTarget = false;
            var endLabel = end.GetComponentInChildren<Text>();
            endLabel.rectTransform.offsetMin = new Vector2(42f, 0f);
            endLabel.alignment = TextAnchor.MiddleLeft;

            _captureBanner = Panel("banner", _hud.transform, new Color(0.75f, 0.12f, 0.16f, 0.95f)).gameObject;
            var bannerRect = _captureBanner.GetComponent<RectTransform>();
            bannerRect.anchorMin = bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRect.pivot = new Vector2(0.5f, 0.5f);
            bannerRect.sizeDelta = new Vector2(220f, 36f);
            bannerRect.anchoredPosition = new Vector2(80f, -40f);
            _bannerText = MakeText(_captureBanner.transform, "REC", 18, Paper, TextAnchor.MiddleCenter);
            Stretch(_bannerText.rectTransform);
            _captureBanner.SetActive(false);

            var frame = Panel("frame", _hud.transform, new Color(0.06f, 0.05f, 0.08f, 0.88f));
            _framePlate = frame.gameObject;
            var frameRect = frame.rectTransform;
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.sizeDelta = new Vector2(280f, 160f);
            frameRect.anchoredPosition = new Vector2(40f, 80f);
            _frameBody = MakeText(frame.transform, "В кадре:", 16, Paper, TextAnchor.UpperLeft);
            var frameBody = _frameBody.rectTransform;
            frameBody.anchorMin = Vector2.zero;
            frameBody.anchorMax = Vector2.one;
            frameBody.offsetMin = new Vector2(14f, 10f);
            frameBody.offsetMax = new Vector2(-12f, -10f);
            _framePlate.SetActive(false);

            var deck = Panel("deck", _hud.transform, new Color(0.05f, 0.04f, 0.07f, 0.96f));
            var deckRect = deck.rectTransform;
            deckRect.anchorMin = new Vector2(0f, 0f);
            deckRect.anchorMax = new Vector2(1f, 0f);
            deckRect.pivot = new Vector2(0.5f, 0f);
            deckRect.sizeDelta = new Vector2(0f, 332f);

            _handCount = MakeText(deck.transform, "РУКА  0 / 0", 16, Paper, TextAnchor.MiddleCenter);
            var handRect = _handCount.rectTransform;
            handRect.anchorMin = handRect.anchorMax = new Vector2(0.5f, 1f);
            handRect.pivot = new Vector2(0.5f, 1f);
            handRect.anchoredPosition = new Vector2(0f, -6f);
            handRect.sizeDelta = new Vector2(240f, 24f);

            _cash = MakeText(deck.transform, "HELL  10 / 10", 16, new Color(0.95f, 0.82f, 0.28f, 1f), TextAnchor.MiddleRight);
            var cashRect = _cash.rectTransform;
            cashRect.anchorMin = cashRect.anchorMax = new Vector2(1f, 1f);
            cashRect.pivot = new Vector2(1f, 1f);
            cashRect.anchoredPosition = new Vector2(-24f, -4f);
            cashRect.sizeDelta = new Vector2(220f, 22f);
            var hellTrack = Panel("hell", deck.transform, new Color(0.15f, 0.1f, 0.08f, 1f));
            var hellRect = hellTrack.rectTransform;
            hellRect.anchorMin = hellRect.anchorMax = new Vector2(1f, 1f);
            hellRect.pivot = new Vector2(1f, 1f);
            hellRect.anchoredPosition = new Vector2(-24f, -28f);
            hellRect.sizeDelta = new Vector2(160f, 8f);
            hellTrack.raycastTarget = false;
            var hellFill = Panel("fill", hellTrack.transform, new Color(0.95f, 0.72f, 0.18f, 1f));
            _hellFill = hellFill.rectTransform;
            _hellFill.anchorMin = new Vector2(0f, 0.5f);
            _hellFill.anchorMax = new Vector2(0f, 0.5f);
            _hellFill.pivot = new Vector2(0f, 0.5f);
            _hellFill.anchoredPosition = Vector2.zero;
            _hellFill.sizeDelta = new Vector2(160f, 8f);
            hellFill.raycastTarget = false;

            var library = MakeText(deck.transform, "БИБЛИОТЕКА", 13, Muted, TextAnchor.UpperCenter);
            var libRect = library.rectTransform;
            libRect.anchorMin = libRect.anchorMax = new Vector2(0f, 1f);
            libRect.pivot = new Vector2(0f, 1f);
            libRect.anchoredPosition = new Vector2(16f, -36f);
            libRect.sizeDelta = new Vector2(120f, 20f);
            _libraryCount = MakeText(deck.transform, "0", 28, Paper, TextAnchor.MiddleCenter);
            var libNum = _libraryCount.rectTransform;
            libNum.anchorMin = libNum.anchorMax = new Vector2(0f, 1f);
            libNum.pivot = new Vector2(0f, 1f);
            libNum.anchoredPosition = new Vector2(16f, -58f);
            libNum.sizeDelta = new Vector2(120f, 40f);
            var libHint = MakeText(deck.transform, "карт осталось", 12, Muted, TextAnchor.UpperCenter);
            var libHintRect = libHint.rectTransform;
            libHintRect.anchorMin = libHintRect.anchorMax = new Vector2(0f, 1f);
            libHintRect.pivot = new Vector2(0f, 1f);
            libHintRect.anchoredPosition = new Vector2(16f, -100f);
            libHintRect.sizeDelta = new Vector2(120f, 18f);

            var used = MakeText(deck.transform, "ИСПОЛЬЗОВАНО", 13, Muted, TextAnchor.UpperCenter);
            var usedRect = used.rectTransform;
            usedRect.anchorMin = usedRect.anchorMax = new Vector2(1f, 1f);
            usedRect.pivot = new Vector2(1f, 1f);
            usedRect.anchoredPosition = new Vector2(-16f, -36f);
            usedRect.sizeDelta = new Vector2(130f, 20f);
            _usedCount = MakeText(deck.transform, "0", 28, Paper, TextAnchor.MiddleCenter);
            var usedNum = _usedCount.rectTransform;
            usedNum.anchorMin = usedNum.anchorMax = new Vector2(1f, 1f);
            usedNum.pivot = new Vector2(1f, 1f);
            usedNum.anchoredPosition = new Vector2(-16f, -58f);
            usedNum.sizeDelta = new Vector2(130f, 40f);

            var bar = Panel("bar", deck.transform, new Color(0f, 0f, 0f, 0f));
            _cardBar = bar.transform;
            var barRect = bar.rectTransform;
            barRect.anchorMin = Vector2.zero;
            barRect.anchorMax = Vector2.one;
            barRect.offsetMin = new Vector2(150f, 8f);
            barRect.offsetMax = new Vector2(-150f, -28f);
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
            hintRect.anchoredPosition = new Vector2(0f, 340f);

            _toast = MakeText(_hud.transform, "", 22, new Color(1f, 0.82f, 0.45f, 1f), TextAnchor.MiddleCenter);
            var toastRect = _toast.rectTransform;
            toastRect.anchorMin = new Vector2(0.5f, 0f);
            toastRect.anchorMax = new Vector2(0.5f, 0f);
            toastRect.pivot = new Vector2(0.5f, 0f);
            toastRect.sizeDelta = new Vector2(900f, 32f);
            toastRect.anchoredPosition = new Vector2(0f, 372f);
            _toast.gameObject.SetActive(false);
            SetCaptureCapacity(1);
        }

        Card MakeCard(int index, EventDefinition def, Action onClick)
        {
            var frame = Panel("card" + index, _cardBar, def.cardColor);
            var element = frame.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 176f;
            element.preferredHeight = 308f;
            var group = frame.gameObject.AddComponent<CanvasGroup>();
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

            var inner = Panel("inner", frame.transform, new Color(0.95f, 0.91f, 0.84f, 1f));
            var innerRect = inner.rectTransform;
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(8f, 8f);
            innerRect.offsetMax = new Vector2(-8f, -8f);
            inner.raycastTarget = false;


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

            var title = MakeText(inner.transform, def.displayName.ToUpperInvariant(), 18, new Color(0.18f, 0.12f, 0.1f), TextAnchor.MiddleCenter);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            // Название — своей строкой под номером и значками тона.
            titleRect.anchoredPosition = new Vector2(0f, -34f);
            // Длинные названия из таблицы карт: шрифт уменьшается, чтобы влезть в две строки.
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 10;
            title.resizeTextMaxSize = 18;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            titleRect.sizeDelta = new Vector2(-8f, 36f);
            StampMoods(frame.transform, def);

            var art = Panel("art", inner.transform, new Color(0.9f, 0.86f, 0.78f, 1f));
            var artRect = art.rectTransform;
            artRect.anchorMin = new Vector2(0f, 1f);
            artRect.anchorMax = new Vector2(1f, 1f);
            artRect.pivot = new Vector2(0.5f, 1f);
            artRect.anchoredPosition = new Vector2(0f, -72f);
            artRect.sizeDelta = new Vector2(-16f, 122f);
            art.raycastTarget = false;
            art.preserveAspect = true;
            art.color = Color.white;
            if (def.cardArt != null)
                art.sprite = def.cardArt;

            // Цена в HellToken — плашкой в углу арта (верхний угол карты заняли значки тона).
            var pill = Panel("cost", art.transform, new Color(0.14f, 0.09f, 0.08f, 0.92f));
            var pillRect = pill.rectTransform;
            pillRect.anchorMin = new Vector2(1f, 0f);
            pillRect.anchorMax = new Vector2(1f, 0f);
            pillRect.pivot = new Vector2(1f, 0f);
            pillRect.anchoredPosition = new Vector2(-4f, 4f);
            pillRect.sizeDelta = new Vector2(62f, 26f);
            pill.raycastTarget = false;
            var price = MakeText(pill.transform, HellToken.Format(def.cost), 18, new Color(0.95f, 0.82f, 0.28f, 1f), TextAnchor.MiddleCenter);
            Stretch(price.rectTransform);

            var body = MakeText(inner.transform, def.hint, 15, new Color(0.35f, 0.26f, 0.2f), TextAnchor.MiddleCenter);
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 0f);
            bodyRect.pivot = new Vector2(0.5f, 0f);
            bodyRect.anchoredPosition = new Vector2(0f, 8f);
            bodyRect.sizeDelta = new Vector2(-12f, 64f);

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
                Base = def.cardColor,
                Button = button,
                Group = group
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
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-16f, -140f);
            rect.sizeDelta = new Vector2(280f, 160f);
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
            var panel = Panel("Feedback", transform, new Color(0.07f, 0.055f, 0.08f, 1f)).gameObject;
            Stretch(panel.GetComponent<RectTransform>());
            BuildTubeChrome(panel.transform);
            return panel;
        }

        void BuildTubeChrome(Transform panel)
        {
            var bar = Panel("bar", panel, new Color(0.09f, 0.07f, 0.1f, 1f));
            var barRect = bar.rectTransform;
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.sizeDelta = new Vector2(0f, 52f);
            barRect.anchoredPosition = Vector2.zero;

            var flame = Panel("flame", bar.transform, Color.white);
            flame.sprite = IllustratedArt.IconFire;
            flame.preserveAspect = true;
            Pin(flame.rectTransform, 22f, 10f, 28f, 28f);
            var logo = MakeText(bar.transform, "HELLTUBE", 22, new Color(0.96f, 0.78f, 0.22f, 1f), TextAnchor.MiddleLeft);
            Pin(logo.rectTransform, 56f, 8f, 180f, 36f);
            var tagline = MakeText(bar.transform, "смотри, пока горишь", 14, Muted, TextAnchor.MiddleLeft);
            Pin(tagline.rectTransform, 250f, 10f, 240f, 32f);

            var search = Panel("search", bar.transform, new Color(0.14f, 0.11f, 0.16f, 1f));
            var searchRect = search.rectTransform;
            searchRect.anchorMin = searchRect.anchorMax = new Vector2(0.5f, 0.5f);
            searchRect.sizeDelta = new Vector2(460f, 32f);
            var searchText = MakeText(search.transform, "поиск по грехам", 14, new Color(0.45f, 0.4f, 0.42f, 1f), TextAnchor.MiddleLeft);
            var searchLabel = searchText.rectTransform;
            searchLabel.anchorMin = Vector2.zero;
            searchLabel.anchorMax = Vector2.one;
            searchLabel.offsetMin = new Vector2(16f, 0f);
            searchLabel.offsetMax = new Vector2(-12f, 0f);

            var channel = MakeText(bar.transform, "канал  ONLY WHAT MATTERS", 15, new Color(0.45f, 0.82f, 0.38f, 1f), TextAnchor.MiddleRight);
            var channelRect = channel.rectTransform;
            channelRect.anchorMin = channelRect.anchorMax = new Vector2(1f, 0.5f);
            channelRect.pivot = new Vector2(1f, 0.5f);
            channelRect.anchoredPosition = new Vector2(-22f, 0f);
            channelRect.sizeDelta = new Vector2(420f, 32f);

            var player = Panel("player", panel, new Color(0.02f, 0.02f, 0.025f, 1f));
            _player = player.gameObject;
            Pin(player.rectTransform, 28f, 72f, 1400f, 392f);
            float pad = 8f;
            float gap = 6f;
            float frameW = (1400f - pad * 2f - gap * 2f) / 3f;
            float frameH = 392f - pad * 2f;
            for (int i = 0; i < 3; i++)
            {
                var frame = Panel("frame" + i, player.transform, new Color(0.05f, 0.04f, 0.06f, 1f));
                Pin(frame.rectTransform, pad + i * (frameW + gap), pad, frameW, frameH);
                frame.preserveAspect = true;
                _frames[i] = frame;
            }

            _tubeTitle = MakeText(panel, "", 26, Paper, TextAnchor.MiddleLeft);
            Pin(_tubeTitle.rectTransform, 28f, 478f, 1380f, 40f);
            _metaLine = MakeText(panel, "", 16, Muted, TextAnchor.MiddleLeft);
            Pin(_metaLine.rectTransform, 28f, 518f, 1100f, 26f);
            var track = Panel("likes", panel, new Color(0.2f, 0.18f, 0.16f, 1f));
            _likeTrack = track.rectTransform;
            Pin(track.rectTransform, 28f, 550f, 260f, 6f);
            var fill = Panel("fill", track.transform, new Color(0.62f, 0.78f, 0.28f, 1f));
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(80f, 0f);
            _likeFill = fillRect;

            _commentHead = MakeText(panel, "КОММЕНТАРИИ", 16, Paper, TextAnchor.MiddleLeft);
            Pin(_commentHead.rectTransform, 28f, 572f, 400f, 28f);

            var nameColor = new Color(0.42f, 0.78f, 0.4f, 1f);
            var rowColor = new Color(0.1f, 0.085f, 0.12f, 1f);
            Color[] faces =
            {
                new Color(0.32f, 0.72f, 0.4f, 1f),
                new Color(0.78f, 0.62f, 0.22f, 1f),
                new Color(0.28f, 0.58f, 0.62f, 1f)
            };
            for (int i = 0; i < 3; i++)
            {
                var row = Panel("review" + i, panel, rowColor);
                Pin(row.rectTransform, 28f, 608f + i * 96f, 1400f, 88f);
                _reviewRows[i] = row.gameObject;

                var face = Panel("face", row.transform, faces[i]);
                face.sprite = Disc();
                Pin(face.rectTransform, 14f, 22f, 40f, 40f);
                _reviewLetters[i] = MakeText(face.transform, "", 18, new Color(0.08f, 0.06f, 0.07f, 1f), TextAnchor.MiddleCenter);
                Stretch(_reviewLetters[i].rectTransform);

                _reviewAuthors[i] = MakeText(row.transform, "", 16, nameColor, TextAnchor.MiddleLeft);
                Pin(_reviewAuthors[i].rectTransform, 68f, 10f, 980f, 26f);
                _reviewBodies[i] = MakeText(row.transform, "", 18, Paper, TextAnchor.UpperLeft);
                Pin(_reviewBodies[i].rectTransform, 68f, 36f, 1080f, 44f);

                _reviewScores[i] = MakeText(row.transform, "", 15, Muted, TextAnchor.MiddleRight);
                var likeRect = _reviewScores[i].rectTransform;
                likeRect.anchorMin = likeRect.anchorMax = new Vector2(1f, 0.5f);
                likeRect.pivot = new Vector2(1f, 0.5f);
                likeRect.anchoredPosition = new Vector2(-18f, 0f);
                likeRect.sizeDelta = new Vector2(90f, 28f);

                var star = MakeButton(row.transform, "☆ взять", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-14f, 0f), new Vector2(150f, 34f), new Color(0.16f, 0.14f, 0.18f, 1f), ToggleTask);
                star.gameObject.SetActive(false);
                _stars[i] = star;
                _starLabels[i] = star.GetComponentInChildren<Text>();
            }

            var side = Panel("side", panel, new Color(0.085f, 0.07f, 0.1f, 1f));
            Pin(side.rectTransform, 1452f, 72f, 440f, 984f);
            var head = MakeText(side.transform, "ИТОГИ ЭФИРА", 22, new Color(0.93f, 0.34f, 0.28f, 1f), TextAnchor.MiddleLeft);
            Pin(head.rectTransform, 22f, 18f, 390f, 36f);
            _viewsText = SideStat(side.transform, "Просмотры", 68f);
            _scoreText = SideStat(side.transform, "Рейтинг", 106f);
            _qualityText = SideStat(side.transform, "Качество кадров", 144f);
            _linkText = SideStat(side.transform, "Связность монтажа", 182f);
            _bonusText = SideStat(side.transform, "Бонус за яркие моменты", 220f);
            _payText = SideStat(side.transform, "Доход", 258f);
            _payText.color = new Color(0.95f, 0.72f, 0.28f, 1f);
            _wishText = MakeText(side.transform, "", 13, Muted, TextAnchor.MiddleLeft);
            Pin(_wishText.rectTransform, 22f, 292f, 396f, 22f);
            var cut = MakeText(side.transform, "МОНТАЖ", 14, Muted, TextAnchor.MiddleLeft);
            Pin(cut.rectTransform, 22f, 330f, 390f, 24f);
            _cutList = MakeText(side.transform, "", 16, Paper, TextAnchor.UpperLeft);
            Pin(_cutList.rectTransform, 22f, 360f, 396f, 500f);

            var next = MakeButton(side.transform, "ДАЛЬШЕ (Space)", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(-36f, 52f), new Color(0.93f, 0.36f, 0.28f, 1f), () => _feedbackNext?.Invoke());
            var nextRect = next.GetComponent<RectTransform>();
            nextRect.offsetMin = new Vector2(18f, 18f);
            nextRect.offsetMax = new Vector2(-18f, 70f);
        }

        Text SideStat(Transform parent, string label, float y)
        {
            var name = MakeText(parent, label, 16, Muted, TextAnchor.MiddleLeft);
            Pin(name.rectTransform, 22f, y, 230f, 30f);
            var value = MakeText(parent, "", 16, Paper, TextAnchor.MiddleRight);
            var rect = value.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-22f, -y);
            rect.sizeDelta = new Vector2(150f, 30f);
            return value;
        }

        void FillFrames(IReadOnlyList<CapturedMoment> moments)
        {
            int shown = 0;
            if (moments != null)
            {
                for (int i = 0; i < moments.Count && shown < 3; i++)
                {
                    if (moments[i].photo == null)
                        continue;
                    SetFrame(shown, moments[i].photo);
                    shown++;
                }
            }

            for (int i = shown; i < 3; i++)
            {
                SetFrame(i, null);
                _frames[i].gameObject.SetActive(false);
            }

            const float pad = 8f;
            const float gap = 6f;
            const float inner = 1400f - pad * 2f;
            const float frameH = 392f - pad * 2f;
            float frameW = shown <= 1 ? inner : (inner - gap * (shown - 1)) / shown;
            for (int i = 0; i < shown; i++)
            {
                _frames[i].gameObject.SetActive(true);
                Pin(_frames[i].rectTransform, pad + i * (frameW + gap), pad, frameW, frameH);
            }

            bool has = shown > 0;
            if (_player != null)
                _player.SetActive(has);
            float lift = has ? 0f : 406f;
            Pin(_tubeTitle.rectTransform, 28f, 478f - lift, 1380f, 40f);
            Pin(_metaLine.rectTransform, 28f, 518f - lift, 1100f, 26f);
            Pin(_likeTrack, 28f, 550f - lift, 260f, 6f);
            Pin(_commentHead.rectTransform, 28f, 572f - lift, 400f, 28f);
            for (int i = 0; i < _reviewRows.Length; i++)
                Pin(_reviewRows[i].GetComponent<RectTransform>(), 28f, 608f + i * 96f - lift, 1400f, 88f);
        }

        void SetFrame(int index, Texture2D photo)
        {
            if (_frameSprites[index] != null)
            {
                Destroy(_frameSprites[index]);
                _frameSprites[index] = null;
            }

            var image = _frames[index];
            if (photo == null)
            {
                image.sprite = null;
                image.color = new Color(0.05f, 0.04f, 0.06f, 1f);
                return;
            }

            var sprite = Sprite.Create(photo, new Rect(0f, 0f, photo.width, photo.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            _frameSprites[index] = sprite;
            image.sprite = sprite;
            image.color = Color.white;
        }

        static float FrameQuality(IReadOnlyList<CapturedMoment> moments)
        {
            if (moments == null || moments.Count == 0)
                return 0f;
            float sum = 0f;
            for (int i = 0; i < moments.Count; i++)
            {
                var grade = moments[i].grade;
                sum += grade == CaptureGrade.Cast ? 8f : grade == CaptureGrade.Prop ? 5f : 2f;
            }

            return sum / moments.Count;
        }

        static int LinkPercent(IReadOnlyList<CapturedMoment> moments)
        {
            if (moments == null || moments.Count == 0)
                return 0;
            int framed = 0;
            for (int i = 0; i < moments.Count; i++)
            {
                if (moments[i].Framed)
                    framed++;
            }

            return Mathf.RoundToInt(100f * framed / moments.Count);
        }

        static int BrightCount(IReadOnlyList<CapturedMoment> moments)
        {
            if (moments == null)
                return 0;
            int n = 0;
            for (int i = 0; i < moments.Count; i++)
            {
                if (moments[i].grade == CaptureGrade.Cast)
                    n++;
            }

            return n;
        }

        static string CutList(IReadOnlyList<CapturedMoment> moments)
        {
            if (moments == null || moments.Count == 0)
                return "Кадров не было.";
            var seen = new Dictionary<string, int>();
            var body = "";
            int n = Mathf.Min(moments.Count, 5);
            for (int i = 0; i < n; i++)
            {
                var moment = moments[i];
                string title = moment.Title;
                seen.TryGetValue(title, out int times);
                times++;
                seen[title] = times;
                float mark = moment.grade == CaptureGrade.Cast ? 7f : moment.grade == CaptureGrade.Prop ? 4.5f : 2f;
                if (times > 1)
                    mark = Mathf.Max(1f, mark * 0.5f);
                string who = moment.actorNames != null && moment.actorNames.Count > 0
                    ? string.Join(", ", moment.actorNames)
                    : moment.Framed ? "в кадре" : "пусто";
                if (body.Length > 0)
                    body += "\n";
                string length = moment.duration > 0.05f ? "  " + Comma(moment.duration) + " с" : "";
                body += (i + 1) + ". " + title + " — " + Comma(mark) + length + (times > 1 ? " (повтор)" : "") + "\n" + who;
            }

            if (moments.Count > n)
                body += "\n… ещё " + (moments.Count - n);
            return body;
        }

        static string Grouped(int value)
        {
            string digits = Mathf.Abs(value).ToString();
            var buf = "";
            for (int i = 0; i < digits.Length; i++)
            {
                if (i > 0 && (digits.Length - i) % 3 == 0)
                    buf += " ";
                buf += digits[i];
            }

            return value < 0 ? "-" + buf : buf;
        }

        static string Comma(float value)
        {
            return value.ToString("0.0").Replace('.', ',');
        }

        static void Pin(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }

        static Sprite _disc;

        static Sprite Disc()
        {
            if (_disc != null)
                return _disc;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Bilinear;
            float r = n * 0.5f - 0.5f;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - n * 0.5f;
                    float dy = y + 0.5f - n * 0.5f;
                    px[y * n + x] = dx * dx + dy * dy <= r * r
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            _disc = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);
            _disc.hideFlags = HideFlags.HideAndDontSave;
            return _disc;
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
            float picAspect = photo.width / (float)Mathf.Max(1, photo.height);
            const float innerW = 240f;
            float innerH = innerW / picAspect;
            rt.sizeDelta = new Vector2(innerW + 24f, innerH + 24f);
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
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            var well = _slots[slot].Well.rect;
            float maxW = well.width > 8f ? well.width : 148f;
            float maxH = well.height > 8f ? well.height : 200f;
            float cardW = rt.sizeDelta.x;
            float cardH = rt.sizeDelta.y;
            float fit = Mathf.Min(maxW / cardW, maxH / cardH);
            rt.sizeDelta = new Vector2(cardW * fit, cardH * fit);
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
