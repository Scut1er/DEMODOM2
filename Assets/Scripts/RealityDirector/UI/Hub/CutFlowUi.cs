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
        MontagePanel _panel;
        // Доп. строки итога монтажа от хаба: спонсор, задача зрителей, купленные подсказки.
        public Func<List<FootageClip>, List<string>> Extra;
        public CutReport Report { get; private set; }
        public RectTransform AudienceFocus => _panel != null ? _panel.AudienceFocus : _coherence != null ? _coherence.rectTransform : null;
        public RectTransform BreakdownFocus => _panel != null ? _panel.BreakdownFocus : null;

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
            // Хлопушка (арт Icons_1) у заголовка — монтажная узнаётся с одного взгляда.
            var slate = UiKit.Img("slate", page, UiKit.Load("Art/UI/Illustrations/clapperboard"), Color.white);
            slate.preserveAspect = true;
            float titleX = 36f;
            if (slate.sprite != null)
            {
                Pin(slate.rectTransform, 30f, 10f, 64f, 64f);
                titleX = 104f;
            }
            else
                Destroy(slate.gameObject);
            var head = TextOn(page, "МОНТАЖ", 28, new Color(0.96f, 0.78f, 0.22f, 1f), TextAnchor.UpperLeft);
            Pin(head.rectTransform, titleX, 18f, 600f, 60f);
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

            var shot = TextOn(page, "ОТСНЯТО ЗА ВЫПУСК  ·  клик — в слот, наведи — почему кадр интересен, колёсико листает ряд, ▶ смотрит ролик", 16, new Color(0.7f, 0.64f, 0.6f, 1f), TextAnchor.UpperLeft);
            Pin(shot.rectTransform, 36f, 150f, 1600f, 28f);
            _libraryView = ScrollRow(page, 36f, 178f, 1848f, 288f, out _libraryRow);

            var airLabel = TextOn(page, "В ЭФИР  ·  порядок имеет значение: стрелка между кадрами — сила связи (наведи — почему)", 16, new Color(0.7f, 0.64f, 0.6f, 1f), TextAnchor.UpperLeft);
            Pin(airLabel.rectTransform, 36f, 474f, 1380f, 28f);
            _cutRow = Row(page, 36f, 502f, 1380f, 284f);
            _panel = MontagePanel.Build(page, _font);

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
                var empty = TextOn(_libraryRow, "Пусто. Сдать можно, но пустой эфир не оплачивается.", 18, new Color(0.7f, 0.64f, 0.6f, 1f), TextAnchor.MiddleLeft);
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
            for (int i = 0; i < _order.Count && i < _slots; i++)
            {
                var c = Find(_order[i]);
                if (c != null)
                    chosen.Add(c);
            }

            // Склейка оценивается целиком: связи соседей, повторы, комбо, как это увидит аудитория.
            var report = CutAnalysis.Analyze(chosen);
            Report = report;
            int linkIndex = 0;
            for (int i = 0; i < _slots; i++)
            {
                FootageClip clip = null;
                if (i < _order.Count)
                    clip = Find(_order[i]);
                int index = i;
                var card = clip != null ? Card(_cutRow, clip, index == _picked) : EmptySlot(_cutRow, i + 1);
                if (clip == null)
                    continue;
                card.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _picked = index;
                    RefreshCut();
                });
                if (i < _order.Count - 1 && linkIndex < report.links.Count && _panel != null)
                    _panel.LinkMark(_cutRow, report.links[linkIndex++]);
            }

            int coherence = report.coherence;
            if (_panel != null)
                _panel.Show(report, _slots, Extra != null ? Extra(chosen) : null);
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

        // HellTube (пак HellTube): слева — плеер с тремя кадрами эфира, название выпуска и комментарии,
        // справа — итоги эфира и финальный монтаж по порядку. Всё — из результата эфира и ката, ничего сверх эфира.
        const string Tube = "Art/UI/HellTube/";
        static readonly Color TubeRed = new Color(1f, 0.19f, 0.3f, 1f);
        static readonly Color TubeMuted = new Color(0.71f, 0.66f, 0.68f, 1f);

        void BuildAir(RectTransform page, FeedbackResult result, List<FootageClip> cut, string title, int pay, int coherence, string payLine)
        {
            UiKit.Backdrop(page, "Art/Intro/bg/scene_5", new Rect(0f, 0.3f, 1f, 0.7f), 0.9f);
            var report = cut != null && cut.Count > 0 ? CutAnalysis.Analyze(cut) : null;

            // ---------- верхняя полоса ----------
            var bar = Panel(page, new Color(0.06f, 0.03f, 0.05f, 0.96f));
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, 64f);
            bar.anchoredPosition = Vector2.zero;
            var flame = Icon(bar, "icon_flame_tint", TubeRed);
            Pin(flame.rectTransform, 26f, 14f, 34f, 36f);
            var logo = TextOn(bar, "HELLTUBE", 22, TubeRed, TextAnchor.MiddleLeft);
            Pin(logo.rectTransform, 66f, 8f, 230f, 48f);
            if (UiKit.Display != null)
            {
                logo.font = UiKit.Display;
                logo.fontSize = 34;
            }

            UiKit.Shadow(logo);
            var tag = TextOn(bar, "смотри, пока горишь", 14, TubeMuted, TextAnchor.MiddleLeft);
            Pin(tag.rectTransform, 300f, 16f, 260f, 32f);
            // Поиск — декорация канала (как у настоящего видеохостинга), не поле ввода.
            var search = Tile(bar, "Panels/panel_search_9slice", false);
            Pin(search, 640f, 9f, 640f, 46f);
            Thin(search, 1.6f);
            var lens = Icon(search, "icon_search", TubeMuted);
            Pin(lens.rectTransform, 22f, 12f, 22f, 22f);
            var hint = TextOn(search, "Поиск по грехам", 15, new Color(0.55f, 0.5f, 0.55f, 1f), TextAnchor.MiddleLeft);
            Pin(hint.rectTransform, 56f, 7f, 400f, 32f);
            var channel = TextOn(bar, "канал  <b><color=#FF304D>ONLY WHAT MATTERS</color></b>", 18, TubeMuted, TextAnchor.MiddleRight);
            channel.supportRichText = true;
            channel.rectTransform.anchorMin = channel.rectTransform.anchorMax = new Vector2(1f, 1f);
            channel.rectTransform.pivot = new Vector2(1f, 1f);
            channel.rectTransform.anchoredPosition = new Vector2(-30f, -12f);
            channel.rectTransform.sizeDelta = new Vector2(520f, 40f);
            var line = Img(page, "Decor/divider_neon_red", Color.white);
            Pin(line.rectTransform, 0f, 62f, 1920f, 6f);

            // ---------- плеер: кадры эфира по порядку ----------
            var video = Tile(page, "Panels/panel_video_frame_9slice", true);
            Pin(video, 36f, 82f, 1210f, 340f);
            _watch = video;
            int shown = 0;
            float total = 0f;
            if (cut != null)
            {
                for (int i = 0; i < cut.Count && shown < 3; i++)
                    total += cut[i].duration;
                for (int i = 0; i < cut.Count && shown < 3; i++)
                {
                    var clip = cut[i];
                    var thumb = Thumb(video, clip, report != null && i < report.clips.Count ? report.clips[i] : null, shown + 1);
                    Pin(thumb, 22f + shown * 393f, 18f, 381f, 246f);
                    shown++;
                }
            }

            if (shown == 0)
            {
                var empty = TextOn(video, "В эфир ничего не вошло — зритель смотрел заставку.", 22, TubeMuted, TextAnchor.MiddleCenter);
                Pin(empty.rectTransform, 22f, 18f, 1166f, 246f);
            }

            // Кнопка «смотреть» открывает просмотр первого кадра (VHS); полоса — длина эфира.
            var play = new GameObject("play", typeof(RectTransform), typeof(Image), typeof(Button));
            play.transform.SetParent(video, false);
            var playImage = play.GetComponent<Image>();
            playImage.sprite = UiKit.Load(Tube + "Icons/icon_play");
            playImage.preserveAspect = true;
            Pin(play.GetComponent<RectTransform>(), 26f, 280f, 34f, 34f);
            var first = cut != null && cut.Count > 0 ? cut[0] : null;
            play.GetComponent<Button>().onClick.AddListener(() => OpenWatch(first));
            var track = Img(video, "Progress/progress_thin_bg_9slice", Color.white);
            track.type = Image.Type.Sliced;
            Pin(track.rectTransform, 76f, 290f, 900f, 14f);
            var played = Img(track.rectTransform, "Progress/progress_thin_fill_white_9slice", TubeRed);
            played.type = Image.Type.Sliced;
            played.rectTransform.anchorMin = Vector2.zero;
            played.rectTransform.anchorMax = new Vector2(0.06f, 1f);
            played.rectTransform.offsetMin = played.rectTransform.offsetMax = Vector2.zero;
            var time = TextOn(video, "0:00 / " + Clock(total), 16, TubeMuted, TextAnchor.MiddleLeft);
            Pin(time.rectTransform, 992f, 280f, 160f, 34f);

            // ---------- название и цифры ----------
            int views = Mathf.RoundToInt(8000f + result.score * 8000f);
            int likes = Mathf.RoundToInt(result.score * 10f);
            var heading = TextOn(page, string.IsNullOrEmpty(title) ? "Серия" : title, 30, Color.white, TextAnchor.UpperLeft);
            heading.fontStyle = FontStyle.Bold;
            Pin(heading.rectTransform, 40f, 436f, 1200f, 44f);
            string linked = cut != null && cut.Count >= 2 ? "связность " + coherence + "%   ·   " : "";
            var meta = TextOn(page, Group(views) + " просмотров   ·   нравится " + likes + "%   ·   " + linked + "рейтинг " + Comma(result.score) + " / 10",
                17, TubeMuted, TextAnchor.UpperLeft);
            Pin(meta.rectTransform, 40f, 482f, 1200f, 28f);
            _numbers = meta.rectTransform;

            // ---------- комментарии ----------
            int comments = result.reviews != null ? result.reviews.Count : 0;
            var bubble = Icon(page, "icon_comment", TubeRed);
            Pin(bubble.rectTransform, 40f, 520f, 26f, 26f);
            var head = TextOn(page, "КОММЕНТАРИИ   ·   " + comments, 18, Color.white, TextAnchor.MiddleLeft);
            head.fontStyle = FontStyle.Bold;
            Pin(head.rectTransform, 76f, 518f, 600f, 30f);
            var box = Tile(page, "Panels/panel_comments_9slice", true);
            Pin(box, 36f, 556f, 1210f, 494f);
            _comments = box;
            var list = Scroll(box);
            for (int i = 0; i < comments; i++)
                Comment(list, result.reviews[i], result.wish);

            // ---------- итоги эфира ----------
            var side = Tile(page, "Panels/panel_results_9slice", true);
            Pin(side, 1290f, 82f, 594f, 470f);
            _pay = side;
            var eye = Icon(side, "icon_eye_tint", TubeRed);
            Pin(eye.rectTransform, 28f, 26f, 34f, 34f);
            var sideTitle = TextOn(side, "ИТОГИ ЭФИРА", 24, TubeRed, TextAnchor.MiddleLeft);
            sideTitle.fontStyle = FontStyle.Bold;
            Pin(sideTitle.rectTransform, 72f, 22f, 480f, 42f);
            _viewsStat = Stat(side, 84f, "icon_eye_tint", "Просмотры", Group(views), -1f);
            _ratingStat = Stat(side, 146f, "icon_star_tint", "Рейтинг", Comma(result.score) + " / 10", result.score / 10f);
            // Связность — между соседними кадрами: с одним кадром её нет.
            bool paired = cut != null && cut.Count >= 2;
            _linkStat = Stat(side, 208f, "icon_link_tint", "Связность монтажа", paired ? coherence + "%" : "нужны 2 кадра", paired ? coherence / 100f : -1f);
            _incomeStat = Stat(side, 270f, "icon_coins_tint", "Доход", "+" + pay + " кр", -1f);
            var note = TextOn(side, payLine ?? "", 16, UiKit.Gold, TextAnchor.UpperLeft);
            note.supportRichText = true;
            Pin(note.rectTransform, 30f, 340f, 534f, 110f);

            // ---------- финальный монтаж ----------
            var final = Tile(page, "Panels/panel_final_cut_9slice", true);
            Pin(final, 1290f, 566f, 594f, 384f);
            var clapper = Icon(final, "icon_clapper_tint", TubeRed);
            Pin(clapper.rectTransform, 28f, 22f, 32f, 32f);
            var finalTitle = TextOn(final, "ФИНАЛЬНЫЙ МОНТАЖ", 22, TubeRed, TextAnchor.MiddleLeft);
            finalTitle.fontStyle = FontStyle.Bold;
            Pin(finalTitle.rectTransform, 72f, 18f, 480f, 40f);
            if (cut == null || cut.Count == 0)
            {
                var none = TextOn(final, "В эфир ничего не вошло.", 18, TubeMuted, TextAnchor.UpperLeft);
                Pin(none.rectTransform, 30f, 80f, 534f, 40f);
            }

            for (int i = 0; cut != null && i < cut.Count && i < 3; i++)
            {
                var facts = report != null && i < report.clips.Count ? report.clips[i] : null;
                var link = report != null && i < report.links.Count ? report.links[i] : null;
                CutRow(final, 72f + i * 100f, i + 1, cut[i], facts, link);
            }

            var next = new GameObject("next", typeof(RectTransform), typeof(Image), typeof(Button));
            next.transform.SetParent(page, false);
            var nextRect = next.GetComponent<RectTransform>();
            Pin(nextRect, 1290f, 962f, 594f, 80f);
            var nextImage = next.GetComponent<Image>();
            nextImage.sprite = UiKit.Load(Tube + "Buttons/button_primary_9slice");
            nextImage.type = nextImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            if (nextImage.sprite == null)
                nextImage.color = UiKit.Blood;
            var nextLabel = TextOn(next.transform, "ДАЛЬШЕ  <size=18>(Space)</size>", 26, Color.white, TextAnchor.MiddleCenter);
            nextLabel.supportRichText = true;
            nextLabel.fontStyle = FontStyle.Bold;
            Stretch(nextLabel.rectTransform);
            next.GetComponent<Button>().onClick.AddListener(() => Next?.Invoke());
            UiKit.Pulse(next.GetComponent<Button>());
        }

        // Кадр эфира в плеере: снимок под рамкой тона, внизу — номер и роль кадра в истории.
        RectTransform Thumb(RectTransform parent, FootageClip clip, ClipFacts facts, int number)
        {
            var root = new GameObject("clip" + number, typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            var window = new GameObject("photo", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            window.SetParent(root, false);
            Stretch(window);
            window.offsetMin = new Vector2(6f, 6f);
            window.offsetMax = new Vector2(-6f, -6f);
            var dark = Panel(window, new Color(0.05f, 0.03f, 0.05f, 1f));
            Stretch(dark);
            if (clip.photo != null)
            {
                var raw = new GameObject("img", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
                raw.transform.SetParent(window, false);
                var rawImage = raw.GetComponent<RawImage>();
                rawImage.texture = clip.photo;
                rawImage.raycastTarget = false;
                var rect = raw.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                var fit = raw.GetComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = clip.photo.width / Mathf.Max(1f, clip.photo.height);
            }

            var frame = Img(root, "Footage/footage_frame_" + FrameTone(clip.mood), Color.white);
            Stretch(frame.rectTransform);
            string role = facts != null ? CutAnalysis.RoleWord(facts.main) : (clip.title ?? "кадр");
            var label = TextOn(root, number + ". " + role.ToUpperInvariant(), 18, MoodStyle.ColorOf(clip.mood), TextAnchor.MiddleLeft);
            label.fontStyle = FontStyle.Bold;
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 0f);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
            label.rectTransform.offsetMin = new Vector2(38f, 10f);
            label.rectTransform.offsetMax = new Vector2(-30f, 46f);
            UiKit.Shadow(label);
            return root;
        }

        // Тон кадра → рамка пака: драма — золото исповеди, трэш — красный конфликт, семья — розовая романтика.
        static string FrameTone(ShowMood mood)
        {
            switch (mood)
            {
                case ShowMood.Drama: return "confession";
                case ShowMood.Trash: return "conflict";
                default: return "romance";
            }
        }

        void Comment(RectTransform list, ViewerReview review, string wish)
        {
            float height = review.offer ? 86f : 68f;
            var row = new GameObject("comment", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(list, false);
            row.GetComponent<LayoutElement>().preferredHeight = height;
            var plate = row.GetComponent<Image>();
            plate.sprite = UiKit.Load(Tube + (review.offer ? "Comments/comment_row_featured_9slice" : "Comments/comment_row_default_9slice"));
            plate.type = Image.Type.Sliced;
            plate.raycastTarget = true;
            var rowRect = row.GetComponent<RectTransform>();
            string author = review.author ?? "";
            // Аватар демона — по автору (один и тот же автор — одна и та же мордочка).
            var avatar = Img(rowRect, "Icons/avatar_demon_0" + (1 + Hash(author) % 7), Color.white);
            avatar.preserveAspect = true;
            avatar.rectTransform.anchorMin = avatar.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            avatar.rectTransform.pivot = new Vector2(0f, 0.5f);
            avatar.rectTransform.anchoredPosition = new Vector2(14f, 0f);
            avatar.rectTransform.sizeDelta = new Vector2(44f, 44f);
            Color mood = review.score >= 7 ? UiKit.Good : review.score >= 4 ? UiKit.Gold : UiKit.Ember;
            var who = TextOn(rowRect, (review.offer ? "★  " : "") + author, 16, AuthorColor(author), TextAnchor.UpperLeft);
            who.fontStyle = FontStyle.Bold;
            Pin(who.rectTransform, 72f, 8f, 700f, 24f);
            var body = TextOn(rowRect, review.body ?? "", 16, new Color(0.92f, 0.88f, 0.84f, 1f), TextAnchor.UpperLeft);
            body.supportRichText = true;
            Pin(body.rectTransform, 72f, 32f, review.offer ? 900f : 1000f, height - 36f);
            if (review.offer)
            {
                _taskRow = rowRect;
                var take = ButtonAt(rowRect, "☆ взять", Vector2.zero, ToggleTask);
                var takeRect = take.GetComponent<RectTransform>();
                takeRect.anchorMin = takeRect.anchorMax = new Vector2(1f, 0.5f);
                takeRect.pivot = new Vector2(1f, 0.5f);
                takeRect.anchoredPosition = new Vector2(-16f, 0f);
                takeRect.sizeDelta = new Vector2(168f, 46f);
                var gold = take.GetComponent<Image>();
                gold.sprite = UiKit.Load(Tube + "Buttons/button_gold_9slice");
                gold.type = Image.Type.Sliced;
                gold.color = Color.white;
                _takePlate = gold;
                _takeLabel = take.GetComponentInChildren<Text>();
                _wish = review.wish;
                _wishLabel = wish;
                return;
            }

            // Оценка зрителя: сердце цвета настроения и «9/10».
            var heart = Icon(rowRect, "icon_heart", mood);
            heart.rectTransform.anchorMin = heart.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            heart.rectTransform.pivot = new Vector2(1f, 0.5f);
            heart.rectTransform.anchoredPosition = new Vector2(-74f, 0f);
            heart.rectTransform.sizeDelta = new Vector2(22f, 22f);
            var score = TextOn(rowRect, review.score + "/10", 16, mood, TextAnchor.MiddleRight);
            score.fontStyle = FontStyle.Bold;
            score.rectTransform.anchorMin = score.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            score.rectTransform.pivot = new Vector2(1f, 0.5f);
            score.rectTransform.anchoredPosition = new Vector2(-18f, 0f);
            score.rectTransform.sizeDelta = new Vector2(52f, 24f);
        }

        // Строка финального монтажа: номер, снимок, роль и кто в кадре, длина; справа — связь со следующим кадром.
        void CutRow(RectTransform parent, float y, int number, FootageClip clip, ClipFacts facts, CutLink link)
        {
            var row = Tile(parent, "Panels/panel_generic_dark_9slice", false);
            Pin(row, 20f, y, 554f, 92f);
            var num = TextOn(row, number.ToString(), 30, UiKit.Gold, TextAnchor.MiddleCenter);
            num.fontStyle = FontStyle.Bold;
            Pin(num.rectTransform, 8f, 16f, 40f, 60f);
            var shot = new GameObject("shot", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            shot.SetParent(row, false);
            Pin(shot, 52f, 10f, 128f, 72f);
            var dark = Panel(shot, new Color(0.05f, 0.03f, 0.05f, 1f));
            Stretch(dark);
            if (clip.photo != null)
            {
                var raw = new GameObject("img", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
                raw.transform.SetParent(shot, false);
                raw.GetComponent<RawImage>().texture = clip.photo;
                raw.GetComponent<RawImage>().raycastTarget = false;
                var rect = raw.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                var fit = raw.GetComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = clip.photo.width / Mathf.Max(1f, clip.photo.height);
            }

            string role = facts != null ? CutAnalysis.RoleWord(facts.main) : (clip.title ?? "кадр");
            var name = TextOn(row, role.ToUpperInvariant(), 18, MoodStyle.ColorOf(clip.mood), TextAnchor.UpperLeft);
            name.fontStyle = FontStyle.Bold;
            Pin(name.rectTransform, 194f, 10f, 250f, 26f);
            string who = clip.actorNames != null && clip.actorNames.Count > 0 ? string.Join(", ", clip.actorNames) : clip.Framed ? "в кадре" : "пусто";
            var sub = TextOn(row, (clip.title ?? "") + "  ·  " + who + "\n" + Comma(clip.duration) + " с", 14, TubeMuted, TextAnchor.UpperLeft);
            Pin(sub.rectTransform, 194f, 36f, 240f, 52f);
            if (link != null)
            {
                var linkText = TextOn(row, "связка\n" + CutAnalysis.Level(link.level).ToLowerInvariant(), 14,
                    link.level >= 2 ? UiKit.Good : link.level == 1 ? UiKit.Gold : UiKit.Ember, TextAnchor.MiddleRight);
                linkText.rectTransform.anchorMin = linkText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                linkText.rectTransform.pivot = new Vector2(1f, 0.5f);
                linkText.rectTransform.anchoredPosition = new Vector2(-24f, 0f);
                linkText.rectTransform.sizeDelta = new Vector2(104f, 48f);
            }
        }

        // Строка итогов: значок, подпись, значение; доля 0..1 — полоска под строкой (−1 — без полоски).
        RectTransform Stat(RectTransform parent, float y, string icon, string label, string value, float share)
        {
            var mark = Marker(parent, 16f, y, 562f, 56f);
            var glyph = Icon(parent, icon, Color.white);
            Pin(glyph.rectTransform, 30f, y + 8f, 28f, 28f);
            var left = TextOn(parent, label, 18, TubeMuted, TextAnchor.MiddleLeft);
            Pin(left.rectTransform, 72f, y + 4f, 300f, 36f);
            var right = TextOn(parent, value, 22, Color.white, TextAnchor.MiddleRight);
            right.fontStyle = FontStyle.Bold;
            var rect = right.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-30f, -(y + 4f));
            rect.sizeDelta = new Vector2(220f, 36f);
            if (share >= 0f)
            {
                var back = Img(parent, "Progress/progress_thin_bg_9slice", Color.white);
                back.type = Image.Type.Sliced;
                Pin(back.rectTransform, 72f, y + 42f, 492f, 10f);
                var fill = Img(back.rectTransform, "Progress/progress_thin_fill_white_9slice",
                    share >= 0.7f ? UiKit.Good : share >= 0.4f ? UiKit.Gold : UiKit.Ember);
                fill.type = Image.Type.Sliced;
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(share), 1f);
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            }

            return mark;
        }

        // Вертикальная прокрутка комментариев внутри рамки.
        static RectTransform Scroll(RectTransform box)
        {
            var view = new GameObject("view", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            view.transform.SetParent(box, false);
            var viewRect = view.GetComponent<RectTransform>();
            Stretch(viewRect);
            viewRect.offsetMin = new Vector2(16f, 14f);
            viewRect.offsetMax = new Vector2(-16f, -14f);
            view.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            var content = new GameObject("list", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            content.SetParent(viewRect, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.GetComponent<ScrollRect>();
            scroll.viewport = viewRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return content;
        }

        // Плашка пака (9-slice), под ней — сплошная тёмная заливка, если окно должно закрывать фон.
        static RectTransform Tile(RectTransform parent, string sprite, bool solid)
        {
            var holder = new GameObject("tile", typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(parent, false);
            if (solid)
            {
                var fill = Panel(holder, new Color(0.07f, 0.035f, 0.055f, 1f));
                Stretch(fill);
                fill.offsetMin = new Vector2(6f, 6f);
                fill.offsetMax = new Vector2(-6f, -6f);
            }

            var frame = Img(holder, sprite, Color.white);
            if (frame.sprite != null)
                frame.type = Image.Type.Sliced;
            else
                frame.color = new Color(0.1f, 0.06f, 0.09f, 0.94f);
            Stretch(frame.rectTransform);
            return holder;
        }

        // Тоньше рамка 9-slice (для невысоких плашек: края пака рассчитаны на крупный размер).
        static void Thin(RectTransform tile, float k)
        {
            foreach (var image in tile.GetComponentsInChildren<Image>())
            {
                if (image.type == Image.Type.Sliced)
                    image.pixelsPerUnitMultiplier = k;
            }
        }

        static Image Img(RectTransform parent, string sprite, Color color)
        {
            var img = UiKit.Img("img", parent, UiKit.Load(Tube + sprite), color);
            return img;
        }

        static Image Icon(RectTransform parent, string icon, Color color)
        {
            var img = UiKit.Img("icon", parent, UiKit.Load(Tube + "Icons/" + icon), color);
            img.preserveAspect = true;
            return img;
        }

        static int Hash(string s)
        {
            int h = 7;
            foreach (char c in s ?? "")
                h = h * 31 + c;
            return Mathf.Abs(h);
        }

        // Цвет ника — как в чатах: у каждого автора свой, из палитры HellTube.
        static Color AuthorColor(string author)
        {
            Color[] palette =
            {
                new Color(1f, 0.82f, 0.36f), new Color(0.45f, 0.9f, 0.64f), new Color(1f, 0.42f, 0.5f),
                new Color(1f, 0.58f, 0.3f), new Color(0.72f, 0.56f, 1f), new Color(0.4f, 0.86f, 1f), new Color(1f, 0.45f, 0.75f)
            };
            return palette[Hash(author) % palette.Length];
        }

        static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
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

        GameObject Card(RectTransform parent, FootageClip clip, bool picked)
        {
            var go = new GameObject("card", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = 212f;
            element.preferredHeight = 276f;
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
                rect.sizeDelta = new Vector2(196f, 104f);
                raw.GetComponent<RawImage>().texture = clip.photo;
                raw.GetComponent<RawImage>().raycastTarget = false;
            }

            if (_panel != null)
            {
                // Что в кадре, роль в истории, тон, сила и качество — и подсказка «почему интересен».
                _panel.Decorate(go, clip);
            }
            else
            {
                string name = clip.title ?? "КАДР";
                if (clip.tags != null && clip.tags.Contains(MomentTags.Sponsor))
                    name = "РЕКЛАМА · " + name;
                var label = TextOn(go.transform, "<b>" + name + "</b>\n<color=#B9A4A8>" + Comma(clip.duration) + " с  ·  " + MoodStyle.Short(clip.mood) + "</color>", 16, Color.white, TextAnchor.UpperLeft);
                Pin(label.rectTransform, 12f, 156f, 176f, 64f);
            }
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
            element.preferredWidth = 212f;
            element.preferredHeight = 276f;
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
