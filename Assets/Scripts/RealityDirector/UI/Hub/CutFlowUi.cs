using System;
using System.Collections.Generic;
using RealityDirector.Capture;
using RealityDirector.Core;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Монтаж (библиотека сверху, кат снизу) и HellTube по финальному кату.
    public class CutFlowUi : MonoBehaviour
    {
        public event Action<List<string>> Confirm;
        public event Action Next;

        Font _font;
        GameObject _root;
        GameObject _montage;
        GameObject _air;
        Text _boss;
        Text _coherence;
        RectTransform _libraryRow;
        RectTransform _libraryView;
        RectTransform _airButton;
        public RectTransform LibraryFocus => _libraryView != null ? _libraryView : _libraryRow;
        public RectTransform CutFocus => _cutRow;
        public RectTransform AirFocus => _airButton;
        public RectTransform BossFocus => _boss != null ? _boss.transform as RectTransform : null;
        public RectTransform CoherenceFocus => _coherence != null ? _coherence.transform as RectTransform : null;
        public int CutCount => _order.Count;
        public int LibraryCount => _library.Count;
        public event Action Edited;
        public RectTransform WatchFocus => _watch;
        public RectTransform NumbersFocus => _numbers;
        public RectTransform CommentsFocus => _comments;
        public RectTransform PayFocus => _pay;
        public RectTransform TaskFocus => _taskRow != null ? _taskRow : _comments;
        public RectTransform ViewsFocus => _viewsStat != null ? _viewsStat : _numbers;
        public RectTransform LikesFocus => _numbers;
        public RectTransform RatingFocus => _ratingStat != null ? _ratingStat : _numbers;
        public RectTransform AirCoherenceFocus => _linkStat != null ? _linkStat : _numbers;
        public RectTransform IncomeFocus => _incomeStat != null ? _incomeStat : _pay;
        RectTransform _watch;
        RectTransform _numbers;
        RectTransform _comments;
        RectTransform _pay;
        RectTransform _taskRow;
        RectTransform _viewsStat;
        RectTransform _ratingStat;
        RectTransform _linkStat;
        RectTransform _incomeStat;
        RectTransform _cutRow;
        readonly List<FootageClip> _library = new List<FootageClip>();
        readonly List<string> _order = new List<string>();
        int _slots = 3;
        int _picked = -1;
        bool _taken;
        Text _takeLabel;
        Image _takePlate;
        ViewerWishId _wish;
        string _wishLabel;
        GameObject _viewer;
        RawImage _viewerPicture;
        Material _vhs;
        Text _viewerMeta;
        FootageClip _playing;
        float _playT;

        public bool AirVisible => _air != null && _air.activeSelf;
        public bool TaskTaken => _taken;

        Image _meter;

        public static CutFlowUi Create()
        {
            var go = new GameObject("CutFlow");
            return go.AddComponent<CutFlowUi>();
        }

        void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            _root = gameObject;
            BuildMontage();
            _air = new GameObject("Air", typeof(RectTransform));
            _air.transform.SetParent(transform, false);
            Stretch(_air.GetComponent<RectTransform>());
            _montage.SetActive(false);
            _air.SetActive(false);
        }

        bool _holdAir;

        public void HoldAir(bool hold)
        {
            _holdAir = hold;
            var button = _airButton != null ? _airButton.GetComponent<Button>() : null;
            if (button != null)
                button.interactable = !hold;
        }

        public void SetLocked(bool locked)
        {
            if (_montage != null)
                LockButtons(_montage, locked);
            if (_air != null)
                LockButtons(_air, locked);
            if (_holdAir && _airButton != null)
            {
                var button = _airButton.GetComponent<Button>();
                if (button != null)
                    button.interactable = false;
            }
        }

        static void LockButtons(GameObject root, bool locked)
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].interactable = !locked;
        }

        public void Hide()
        {
            CloseWatch();
            if (_montage != null)
                _montage.SetActive(false);
            if (_air != null)
                _air.SetActive(false);
        }

        public void ShowMontage(List<FootageClip> library, int slots)
        {
            _library.Clear();
            if (library != null)
                _library.AddRange(library);
            _order.Clear();
            _picked = -1;
            _slots = Mathf.Max(1, slots);
            CloseWatch();
            _air.SetActive(false);
            _montage.SetActive(true);
            _montage.transform.SetAsLastSibling();
            RebuildLibrary();
            RefreshCut();
        }

        public void ShowAir(FeedbackResult result, List<FootageClip> cut, string title, int pay, int coherence, string payLine)
        {
            _taken = false;
            _taskRow = null;
            _takeLabel = null;
            _takePlate = null;
            _wish = ViewerWishId.None;
            _wishLabel = null;
            _montage.SetActive(false);
            Clear(_air.transform);
            _air.SetActive(true);
            _air.transform.SetAsLastSibling();
            var page = Panel(_air.transform, new Color(0.07f, 0.055f, 0.08f, 1f));
            Stretch(page);
            BuildAir(page, result, cut, title, pay, coherence, payLine);
        }

        void BuildMontage()
        {
            _montage = new GameObject("Montage", typeof(RectTransform));
            _montage.transform.SetParent(transform, false);
            Stretch(_montage.GetComponent<RectTransform>());
            var page = Panel(_montage.transform, new Color(0.07f, 0.055f, 0.08f, 1f));
            Stretch(page);
            // Монтажная — та же диспетчерская канала, что и хаб, только темнее: смотрим на кадры.
            UiKit.Backdrop(page, "Art/Intro/bg/scene_5", new Rect(0f, 0.3f, 1f, 0.7f), 0.86f);
            var head = TextOn(page, "МОНТАЖ", 28, new Color(0.96f, 0.78f, 0.22f, 1f), TextAnchor.UpperLeft);
            Pin(head.rectTransform, 36f, 18f, 600f, 60f);
            if (UiKit.Display != null)
                head.font = UiKit.Display;
            head.fontSize = 48;
            UiKit.Shadow(head, 3f);
            _boss = TextOn(page, "", 20, new Color(0.9f, 0.86f, 0.8f, 1f), TextAnchor.UpperLeft);
            Pin(_boss.rectTransform, 36f, 78f, 1400f, 64f);
            _coherence = TextOn(page, "", 20, new Color(0.42f, 0.82f, 0.48f, 1f), TextAnchor.UpperRight);
            var coh = _coherence.rectTransform;
            coh.anchorMin = coh.anchorMax = new Vector2(1f, 1f);
            coh.pivot = new Vector2(1f, 1f);
            coh.anchoredPosition = new Vector2(-36f, -28f);
            coh.sizeDelta = new Vector2(420f, 40f);
            _coherence.fontStyle = FontStyle.Bold;
            // Связность — полоской: видно, насколько склейка держится, ещё до цифры.
            var meterBack = Panel(page, new Color(1f, 1f, 1f, 0.12f));
            meterBack.anchorMin = meterBack.anchorMax = new Vector2(1f, 1f);
            meterBack.pivot = new Vector2(1f, 1f);
            meterBack.anchoredPosition = new Vector2(-36f, -74f);
            meterBack.sizeDelta = new Vector2(420f, 12f);
            _meter = Panel(meterBack, Color.white).GetComponent<Image>();
            var meterRect = _meter.rectTransform;
            meterRect.anchorMin = Vector2.zero;
            meterRect.anchorMax = new Vector2(0f, 1f);
            meterRect.pivot = new Vector2(0f, 0.5f);
            meterRect.offsetMin = Vector2.zero;
            meterRect.offsetMax = Vector2.zero;

            var shot = TextOn(page, "ОТСНЯТО ЗА ВЫПУСК  ·  клик — в один из 3 слотов, колёсико листает ряд, ▶ смотрит ролик", 16, new Color(0.7f, 0.64f, 0.6f, 1f), TextAnchor.UpperLeft);
            Pin(shot.rectTransform, 36f, 160f, 1400f, 28f);
            _libraryView = ScrollRow(page, 36f, 196f, 1848f, 250f, out _libraryRow);

            var airLabel = TextOn(page, "В ЭФИР  ·  порядок имеет значение, ↔ — соседние кадры про одно", 16, new Color(0.7f, 0.64f, 0.6f, 1f), TextAnchor.UpperLeft);
            Pin(airLabel.rectTransform, 36f, 470f, 1200f, 28f);
            _cutRow = Row(page, 36f, 508f, 1400f, 230f);

            ButtonAt(page, "РАНЬШЕ", new Vector2(36f, 78f), () => Move(-1));
            ButtonAt(page, "ПОЗЖЕ", new Vector2(220f, 78f), () => Move(1));
            ButtonAt(page, "УБРАТЬ", new Vector2(404f, 78f), RemovePicked);
            var go = ButtonAt(page, "В ЭФИР", new Vector2(1560f, 78f), () => Confirm?.Invoke(new List<string>(_order)));
            _airButton = go.GetComponent<RectTransform>();
            var goRect = go.GetComponent<RectTransform>();
            goRect.sizeDelta = new Vector2(320f, 72f);
            UiKit.Primary(go.GetComponent<Button>(), 24);
            UiKit.Pulse(go.GetComponent<Button>());
        }

        void RebuildLibrary()
        {
            Clear(_libraryRow);
            for (int i = 0; i < _library.Count; i++)
            {
                int index = i;
                var clip = _library[i];
                var card = Card(_libraryRow, clip, false);
                card.GetComponent<Button>().onClick.AddListener(() => ToggleLibrary(index));
            }

            if (_library.Count == 0)
            {
                var empty = TextOn(_libraryRow, "Пусто. В эфир можно сдать и так.", 18, new Color(0.7f, 0.64f, 0.6f, 1f), TextAnchor.MiddleLeft);
                empty.rectTransform.sizeDelta = new Vector2(600f, 40f);
            }
        }

        void ToggleLibrary(int index)
        {
            var clip = _library[index];
            int at = _order.IndexOf(clip.id);
            if (at >= 0)
            {
                _order.RemoveAt(at);
                if (_picked == at)
                    _picked = -1;
                else if (_picked > at)
                    _picked--;
            }
            else if (_order.Count < _slots)
            {
                _order.Add(clip.id);
                _picked = _order.Count - 1;
            }

            RefreshCut();
            Edited?.Invoke();
        }

        void Move(int dir)
        {
            if (_picked < 0 || _picked >= _order.Count)
                return;
            int next = _picked + dir;
            if (next < 0 || next >= _order.Count)
                return;
            string id = _order[_picked];
            _order.RemoveAt(_picked);
            _order.Insert(next, id);
            _picked = next;
            RefreshCut();
            Edited?.Invoke();
        }

        void RemovePicked()
        {
            if (_picked < 0 || _picked >= _order.Count)
                return;
            _order.RemoveAt(_picked);
            _picked = -1;
            RefreshCut();
            Edited?.Invoke();
        }

        void RefreshCut()
        {
            Clear(_cutRow);
            var chosen = new List<FootageClip>();
            for (int i = 0; i < _slots; i++)
            {
                FootageClip clip = null;
                if (i < _order.Count)
                    clip = Find(_order[i]);
                if (clip != null)
                    chosen.Add(clip);
                int index = i;
                var card = clip != null ? Card(_cutRow, clip, index == _picked) : EmptySlot(_cutRow, i + 1);
                if (clip == null)
                    continue;
                card.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _picked = index;
                    RefreshCut();
                });
                if (i < _order.Count - 1)
                {
                    var next = Find(_order[i + 1]);
                    bool link = next != null && MontageCut.Shares(clip, next);
                    bool named = link && GameSession.State != null && GameSession.State.operatorLevel >= 2;
                    string markText = named ? MontageCut.SharedTag(clip, next) : link ? "↔" : "·";
                    var mark = TextOn(_cutRow, markText, named ? 14 : 28, link ? new Color(0.96f, 0.78f, 0.22f, 1f) : new Color(0.45f, 0.4f, 0.42f, 1f), TextAnchor.MiddleCenter);
                    var element = mark.gameObject.AddComponent<LayoutElement>();
                    element.preferredWidth = named ? 88f : 36f;
                    element.preferredHeight = 200f;
                }
            }

            int coherence = MontageCut.Coherence(chosen);
            _boss.text = MontageCut.Boss(coherence, chosen.Count, _library.Count);
            bool paired = chosen.Count >= 2;
            _coherence.text = paired ? "связность " + coherence + "%" : "связность — нужны 2 кадра";
            if (_meter != null)
            {
                var r = _meter.rectTransform;
                r.anchorMax = new Vector2(paired ? Mathf.Clamp01(coherence / 100f) : 0f, 1f);
                _meter.color = !paired ? UiKit.Muted : coherence >= 70 ? UiKit.Good : coherence >= 40 ? UiKit.Gold : UiKit.Ember;
                _coherence.color = _meter.color;
            }
            PaintLibrary();
        }

        void PaintLibrary()
        {
            int card = 0;
            for (int i = 0; i < _libraryRow.childCount; i++)
            {
                var image = _libraryRow.GetChild(i).GetComponent<Image>();
                if (image == null || card >= _library.Count)
                    continue;
                bool on = _order.Contains(_library[card].id);
                image.color = on ? new Color(1f, 0.82f, 0.4f, 1f) : Color.white;
                card++;
            }
        }

        FootageClip Find(string id)
        {
            for (int i = 0; i < _library.Count; i++)
            {
                if (_library[i].id == id)
                    return _library[i];
            }

            return null;
        }

        void BuildAir(RectTransform page, FeedbackResult result, List<FootageClip> cut, string title, int pay, int coherence, string payLine)
        {
            var bar = Panel(page, new Color(0.09f, 0.07f, 0.1f, 1f));
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, 52f);
            bar.anchoredPosition = Vector2.zero;
            var logo = TextOn(bar, "HELLTUBE", 22, new Color(0.96f, 0.78f, 0.22f, 1f), TextAnchor.MiddleLeft);
            Pin(logo.rectTransform, 28f, 4f, 220f, 44f);
            if (UiKit.Display != null)
            {
                logo.font = UiKit.Display;
                logo.fontSize = 32;
            }

            UiKit.Shadow(logo);
            var tag = TextOn(bar, "смотри, пока горишь", 14, new Color(0.7f, 0.64f, 0.6f, 1f), TextAnchor.MiddleLeft);
            Pin(tag.rectTransform, 250f, 10f, 320f, 32f);

            float top = 70f;
            int shown = 0;
            if (cut != null)
            {
                for (int i = 0; i < cut.Count && shown < 3; i++)
                {
                    if (cut[i].photo == null)
                        continue;
                    var frame = Panel(page, Color.black);
                    Pin(frame, 36f + shown * 430f, top, 410f, 220f);
                    var raw = new GameObject("photo", typeof(RectTransform), typeof(RawImage));
                    raw.transform.SetParent(frame, false);
                    var rawRect = raw.GetComponent<RectTransform>();
                    Stretch(rawRect);
                    rawRect.offsetMin = new Vector2(6f, 6f);
                    rawRect.offsetMax = new Vector2(-6f, -6f);
                    raw.GetComponent<RawImage>().texture = cut[i].photo;
                    raw.GetComponent<RawImage>().raycastTarget = false;
                    shown++;
                }
            }

            if (shown > 0)
                top += 236f;
            var heading = TextOn(page, string.IsNullOrEmpty(title) ? "Серия" : title, 26, Color.white, TextAnchor.UpperLeft);
            Pin(heading.rectTransform, 36f, top, 1100f, 40f);
            top += 40f;
            int views = Mathf.RoundToInt(8000f + result.score * 8000f);
            int likes = Mathf.RoundToInt(result.score * 10f);
            var meta = TextOn(page, Group(views) + " просмотров   ·   нравится " + likes + "%   ·   связность " + coherence + "%", 16, new Color(0.7f, 0.64f, 0.6f, 1f), TextAnchor.UpperLeft);
            Pin(meta.rectTransform, 36f, top, 1100f, 28f);
            _numbers = meta.rectTransform;
            _watch = shown > 0 ? Marker(page, 36f, 70f, Mathf.Max(410f, shown * 430f - 20f), 220f) : heading.rectTransform;
            top += 36f;

            int comments = result.reviews != null ? result.reviews.Count : 0;
            float commentTop = top;
            var head = TextOn(page, "КОММЕНТАРИИ   ·   " + comments, 16, new Color(0.7f, 0.64f, 0.6f, 1f), TextAnchor.UpperLeft);
            Pin(head.rectTransform, 36f, top, 600f, 24f);
            top += 28f;
            for (int i = 0; i < comments; i++)
            {
                var review = result.reviews[i];
                var row = Panel(page, new Color(0.12f, 0.1f, 0.14f, 1f));
                UiKit.Dress(row.GetComponent<Image>(), review.offer ? UiKit.Frame.GoldTile : UiKit.Frame.Dark);
                Pin(row, 36f, top, 1180f, review.offer ? 78f : 64f);
                string author = review.author ?? "";
                // Аватар с буквой и оценка зрителя сердцем: доволен — зелёное, так себе — золото, зол — красное.
                Color mood = review.score >= 7 ? UiKit.Good : review.score >= 4 ? UiKit.Gold : UiKit.Ember;
                var avatar = Panel(row, mood);
                var avatarImage = avatar.GetComponent<Image>();
                avatarImage.sprite = UiKit.Circle();
                Pin(avatar, 16f, 12f, 40f, 40f);
                var letter = TextOn(avatar, author.Length > 0 ? author.Substring(0, 1).ToUpperInvariant() : "?", 20, UiKit.Ink, TextAnchor.MiddleCenter);
                letter.fontStyle = FontStyle.Bold;
                Stretch(letter.rectTransform);
                var who = TextOn(row, (review.offer ? "★  " : "") + author, 16, Color.white, TextAnchor.UpperLeft);
                who.fontStyle = FontStyle.Bold;
                Pin(who.rectTransform, 70f, 8f, 700f, 24f);
                var body = TextOn(row, review.body ?? "", 16, new Color(0.9f, 0.86f, 0.8f, 1f), TextAnchor.UpperLeft);
                Pin(body.rectTransform, 70f, 32f, review.offer ? 840f : 1000f, 40f);
                if (!review.offer)
                {
                    var heart = Panel(row, mood);
                    var heartImage = heart.GetComponent<Image>();
                    heartImage.sprite = UiKit.Icon("icon_heart");
                    heartImage.preserveAspect = true;
                    heart.anchorMin = heart.anchorMax = new Vector2(1f, 0.5f);
                    heart.pivot = new Vector2(1f, 0.5f);
                    heart.anchoredPosition = new Vector2(-64f, 0f);
                    heart.sizeDelta = new Vector2(24f, 24f);
                    var score = TextOn(row, review.score + "/10", 16, mood, TextAnchor.MiddleRight);
                    score.fontStyle = FontStyle.Bold;
                    score.rectTransform.anchorMin = score.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                    score.rectTransform.pivot = new Vector2(1f, 0.5f);
                    score.rectTransform.anchoredPosition = new Vector2(-14f, 0f);
                    score.rectTransform.sizeDelta = new Vector2(48f, 24f);
                }
                if (review.offer)
                {
                    _taskRow = row;
                    var take = ButtonAt(row, "☆ взять", Vector2.zero, ToggleTask);
                    var takeRect = take.GetComponent<RectTransform>();
                    takeRect.anchorMin = takeRect.anchorMax = new Vector2(1f, 0.5f);
                    takeRect.pivot = new Vector2(1f, 0.5f);
                    takeRect.anchoredPosition = new Vector2(-12f, 0f);
                    takeRect.sizeDelta = new Vector2(168f, 44f);
                    _takePlate = take.GetComponent<Image>();
                    _takeLabel = take.GetComponentInChildren<Text>();
                    _wish = review.wish;
                    _wishLabel = result.wish;
                }

                top += review.offer ? 88f : 72f;
            }

            _comments = comments > 0 ? Marker(page, 36f, commentTop, 1180f, top - commentTop) : null;
            var side = Panel(page, new Color(0.1f, 0.08f, 0.12f, 1f));
            UiKit.DressSolid(side.GetComponent<Image>(), UiKit.Frame.Gold, 10f);
            _pay = side;
            side.anchorMin = new Vector2(1f, 1f);
            side.anchorMax = new Vector2(1f, 1f);
            side.pivot = new Vector2(1f, 1f);
            side.anchoredPosition = new Vector2(-28f, -70f);
            side.sizeDelta = new Vector2(420f, 760f);
            var sideTitle = TextOn(side, "ИТОГИ ЭФИРА", 20, new Color(0.95f, 0.45f, 0.38f, 1f), TextAnchor.UpperLeft);
            Pin(sideTitle.rectTransform, 20f, 16f, 380f, 32f);
            _viewsStat = Stat(side, 64f, "Просмотры", Group(views));
            _ratingStat = Stat(side, 112f, "Рейтинг", Comma(result.score) + " / 10");
            // Рейтинг полоской под цифрой, цвет — насколько эфир удался.
            var ratingBack = Panel(side, new Color(1f, 1f, 1f, 0.1f));
            Pin(ratingBack, 20f, 146f, 380f, 8f);
            var ratingFill = Panel(ratingBack, result.score >= 7f ? UiKit.Good : result.score >= 4f ? UiKit.Gold : UiKit.Ember);
            ratingFill.anchorMin = Vector2.zero;
            ratingFill.anchorMax = new Vector2(Mathf.Clamp01(result.score / 10f), 1f);
            ratingFill.offsetMin = Vector2.zero;
            ratingFill.offsetMax = Vector2.zero;
            _linkStat = Stat(side, 160f, "Связность монтажа", cut != null && cut.Count >= 2 ? coherence + "%" : "нужны 2 кадра");
            _incomeStat = Stat(side, 208f, "Доход", "+" + pay + " кр");
            var note = TextOn(side, payLine ?? "", 16, new Color(0.96f, 0.78f, 0.22f, 1f), TextAnchor.UpperLeft);
            Pin(note.rectTransform, 20f, 260f, 380f, 80f);
            var list = TextOn(side, CutLines(cut), 16, new Color(0.9f, 0.86f, 0.8f, 1f), TextAnchor.UpperLeft);
            Pin(list.rectTransform, 20f, 350f, 380f, 250f);
            var next = ButtonAt(side, "ДАЛЬШЕ (Space)", new Vector2(20f, 680f), () => Next?.Invoke());
            var nextRect = next.GetComponent<RectTransform>();
            nextRect.anchorMin = nextRect.anchorMax = new Vector2(0f, 1f);
            nextRect.pivot = new Vector2(0f, 1f);
            nextRect.anchoredPosition = new Vector2(20f, -680f);
            nextRect.sizeDelta = new Vector2(380f, 64f);
            UiKit.Primary(next.GetComponent<Button>(), 20);
            UiKit.Pulse(next.GetComponent<Button>());
        }

        static RectTransform Marker(RectTransform parent, float x, float y, float w, float h)
        {
            var go = new GameObject("focus", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        void ToggleTask()
        {
            if (_taken)
                return;
            var state = GameSession.State;
            if (state == null || _wish == ViewerWishId.None)
            {
                PaintTake("пусто", new Color(0.2f, 0.18f, 0.22f, 1f));
                return;
            }

            if (state.HasTask(_wish))
            {
                PaintTake("★ уже есть", new Color(0.45f, 0.32f, 0.12f, 1f));
                return;
            }

            if (state.tasks.Count >= 4)
            {
                PaintTake("мест нет", new Color(0.45f, 0.16f, 0.14f, 1f));
                return;
            }

            if (!state.Accept(_wish, _wishLabel))
            {
                PaintTake("не взялось", new Color(0.45f, 0.16f, 0.14f, 1f));
                return;
            }

            _taken = true;
            PaintTake("★ в задачах", new Color(0.45f, 0.32f, 0.12f, 1f));
            GameSession.Save();
        }

        void PaintTake(string value, Color plate)
        {
            if (_takeLabel != null)
                _takeLabel.text = value;
            if (_takePlate != null)
                _takePlate.color = plate;
        }

        static string CutLines(List<FootageClip> cut)
        {
            if (cut == null || cut.Count == 0)
                return "В эфир ничего не вошло.";
            var body = "МОНТАЖ";
            for (int i = 0; i < cut.Count; i++)
            {
                var clip = cut[i];
                string who = clip.actorNames != null && clip.actorNames.Count > 0 ? string.Join(", ", clip.actorNames) : clip.Framed ? "в кадре" : "пусто";
                body += "\n" + (i + 1) + ". " + clip.title + "  " + Comma(clip.duration) + " с\n" + who;
            }

            return body;
        }

        RectTransform Stat(RectTransform parent, float y, string label, string value)
        {
            var mark = Marker(parent, 12f, y, 396f, 36f);
            var left = TextOn(parent, label, 16, new Color(0.7f, 0.64f, 0.6f, 1f), TextAnchor.MiddleLeft);
            Pin(left.rectTransform, 20f, y, 220f, 32f);
            var right = TextOn(parent, value, 18, Color.white, TextAnchor.MiddleRight);
            var rect = right.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -y);
            rect.sizeDelta = new Vector2(180f, 32f);
            return mark;
        }

        GameObject Card(RectTransform parent, FootageClip clip, bool picked)
        {
            var go = new GameObject("card", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = 200f;
            element.preferredHeight = 230f;
            var plate = go.GetComponent<Image>();
            UiKit.Dress(plate, picked ? UiKit.Frame.GoldTile : UiKit.Frame.Dialog, picked ? 1f : 1.6f);
            // Полоска тона кадра сверху — по ней склейку читают с одного взгляда.
            var stripe = Panel(go.transform, MoodStyle.ColorOf(clip.mood));
            stripe.anchorMin = new Vector2(0f, 1f);
            stripe.anchorMax = new Vector2(1f, 1f);
            stripe.pivot = new Vector2(0.5f, 1f);
            stripe.offsetMin = new Vector2(10f, -6f);
            stripe.offsetMax = new Vector2(-10f, -2f);
            stripe.GetComponent<Image>().raycastTarget = false;
            if (clip.photo != null)
            {
                var raw = new GameObject("photo", typeof(RectTransform), typeof(RawImage));
                raw.transform.SetParent(go.transform, false);
                var rect = raw.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(8f, -8f);
                rect.sizeDelta = new Vector2(184f, 140f);
                raw.GetComponent<RawImage>().texture = clip.photo;
                raw.GetComponent<RawImage>().raycastTarget = false;
            }

            string name = clip.title ?? "КАДР";
            if (clip.tags != null && clip.tags.Contains(MomentTags.Sponsor))
                name = "РЕКЛАМА · " + name;
            var label = TextOn(go.transform, "<b>" + name + "</b>\n<color=#B9A4A8>" + Comma(clip.duration) + " с  ·  " + MoodStyle.Short(clip.mood) + "</color>", 16, Color.white, TextAnchor.UpperLeft);
            Pin(label.rectTransform, 12f, 156f, 176f, 64f);
            var watch = new GameObject("watch", typeof(RectTransform), typeof(Image), typeof(Button));
            watch.transform.SetParent(go.transform, false);
            var watchRect = watch.GetComponent<RectTransform>();
            watchRect.anchorMin = watchRect.anchorMax = new Vector2(1f, 1f);
            watchRect.pivot = new Vector2(1f, 1f);
            watchRect.anchoredPosition = new Vector2(-8f, -8f);
            watchRect.sizeDelta = new Vector2(40f, 28f);
            watch.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.05f, 0.92f);
            var mark = TextOn(watch.transform, "▶", 16, Color.white, TextAnchor.MiddleCenter);
            Stretch(mark.rectTransform);
            var shown = clip;
            watch.GetComponent<Button>().onClick.AddListener(() => OpenWatch(shown));
            return go;
        }

        void OpenWatch(FootageClip clip)
        {
            if (clip == null)
                return;
            EnsureViewer();
            _playing = clip;
            _playT = 0f;
            string who = clip.actorNames != null && clip.actorNames.Count > 0 ? string.Join(", ", clip.actorNames) : "никого в кадре";
            string tags = clip.tags != null && clip.tags.Count > 0 ? string.Join(" · ", clip.tags) : "";
            _viewerMeta.text = (clip.title ?? "КАДР") + "   " + Comma(clip.duration) + " с\n" + who + (tags.Length > 0 ? "\n" + tags : "");
            _viewer.SetActive(true);
            _viewer.transform.SetAsLastSibling();
            PaintWatch();
        }

        void CloseWatch()
        {
            _playing = null;
            if (_viewer != null)
                _viewer.SetActive(false);
        }

        void EnsureViewer()
        {
            if (_viewer != null)
                return;
            _viewer = new GameObject("viewer", typeof(RectTransform), typeof(Image), typeof(Button));
            _viewer.transform.SetParent(transform, false);
            Stretch(_viewer.GetComponent<RectTransform>());
            _viewer.GetComponent<Image>().color = new Color(0.02f, 0.015f, 0.03f, 0.92f);
            var dim = _viewer.GetComponent<Button>();
            dim.onClick.AddListener(CloseWatch);
            var frame = Panel(_viewer.transform, Color.black);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = new Vector2(0f, 40f);
            frame.sizeDelta = new Vector2(960f, 540f);
            var raw = new GameObject("picture", typeof(RectTransform), typeof(RawImage));
            raw.transform.SetParent(frame, false);
            var rawRect = raw.GetComponent<RectTransform>();
            Stretch(rawRect);
            rawRect.offsetMin = new Vector2(8f, 8f);
            rawRect.offsetMax = new Vector2(-8f, -8f);
            _viewerPicture = raw.GetComponent<RawImage>();
            _viewerPicture.raycastTarget = false;
            var shader = Resources.Load<Shader>("Shaders/Vhs");
            if (shader != null)
            {
                _vhs = new Material(shader);
                _vhs.hideFlags = HideFlags.HideAndDontSave;
                _viewerPicture.material = _vhs;
            }
            _viewerMeta = TextOn(_viewer.transform, "", 22, Color.white, TextAnchor.UpperCenter);
            var meta = _viewerMeta.rectTransform;
            meta.anchorMin = meta.anchorMax = new Vector2(0.5f, 0f);
            meta.pivot = new Vector2(0.5f, 0f);
            meta.anchoredPosition = new Vector2(0f, 36f);
            meta.sizeDelta = new Vector2(1100f, 110f);
            var close = ButtonAt(_viewer.transform, "ЗАКРЫТЬ", new Vector2(1680f, 980f), CloseWatch);
            var closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-36f, -28f);
            _viewer.SetActive(false);
        }

        void Update()
        {
            if (_playing == null || _viewer == null || !_viewer.activeSelf)
                return;
            int n = _playing.frames != null ? _playing.frames.Count : 0;
            if (n > 1)
            {
                float dur = Mathf.Max(_playing.duration, n * 0.28f);
                _playT += Time.unscaledDeltaTime;
                if (_playT >= dur)
                    _playT -= dur;
            }

            if (_vhs != null)
                _vhs.SetFloat("_VhsTime", Time.unscaledTime);
            PaintWatch();
        }

        void OnDestroy()
        {
            if (_vhs != null)
                Destroy(_vhs);
        }

        void PaintWatch()
        {
            if (_playing == null || _viewerPicture == null)
                return;
            var frames = _playing.frames;
            int n = frames != null ? frames.Count : 0;
            Texture2D tex = _playing.photo;
            int index = 0;
            if (n > 0)
            {
                float dur = Mathf.Max(_playing.duration, n * 0.28f);
                index = n == 1 ? 0 : Mathf.Clamp(Mathf.FloorToInt(_playT / dur * n), 0, n - 1);
                if (frames[index] != null)
                    tex = frames[index];
            }

            _viewerPicture.texture = tex;
            _viewerPicture.color = tex != null ? Color.white : new Color(0.2f, 0.16f, 0.18f, 1f);
        }

        GameObject EmptySlot(RectTransform parent, int number)
        {
            var go = new GameObject("empty", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = 200f;
            element.preferredHeight = 230f;
            var slot = go.GetComponent<Image>();
            slot.sprite = UiKit.Load("Art/UI/CoreGameplay/UI/HUD/capture_frame_corners");
            slot.type = Image.Type.Simple;
            slot.color = new Color(0.95f, 0.76f, 0.36f, 0.3f);
            var label = TextOn(go.transform, "КАДР " + number, 16, new Color(0.45f, 0.4f, 0.42f, 1f), TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            return go;
        }

        RectTransform ScrollRow(RectTransform parent, float x, float y, float w, float h, out RectTransform content)
        {
            var viewGo = new GameObject("scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewGo.transform.SetParent(parent, false);
            var view = viewGo.GetComponent<RectTransform>();
            Pin(view, x, y, w, h);
            var plate = viewGo.GetComponent<Image>();
            plate.color = new Color(0f, 0f, 0f, 0.01f);
            plate.raycastTarget = true;

            var contentGo = new GameObject("row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(view, false);
            content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var layout = contentGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(0, 12, 8, 8);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var fit = contentGo.GetComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scroll = viewGo.GetComponent<ScrollRect>();
            scroll.viewport = view;
            scroll.content = content;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 60f;
            return view;
        }

        RectTransform Row(RectTransform parent, float x, float y, float w, float h)
        {
            var go = new GameObject("row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Pin(rect, x, y, w, h);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            return rect;
        }

        GameObject ButtonAt(Transform parent, string label, Vector2 pos, Action click)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(170f, 48f);
            go.GetComponent<Image>().color = new Color(0.2f, 0.18f, 0.22f, 1f);
            var text = TextOn(go.transform, label, 16, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            UiKit.Secondary(go.GetComponent<Button>());
            go.GetComponent<Button>().onClick.AddListener(() => click());
            return go;
        }

        Text TextOn(Transform parent, string value, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject("text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            UiTypography.Apply(text, UiTypography.ForSize(size));
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static RectTransform Panel(Transform parent, Color color)
        {
            var go = new GameObject("panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<RectTransform>();
        }

        static void Pin(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        static string Group(int value)
        {
            return value.ToString("N0").Replace('\u00a0', ' ').Replace(',', ' ');
        }

        static string Comma(float value)
        {
            return value.ToString("0.0").Replace('.', ',');
        }
    }
}
