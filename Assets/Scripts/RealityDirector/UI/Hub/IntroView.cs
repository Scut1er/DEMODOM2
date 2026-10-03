using System;
using System.Collections;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Интро из OWM_intro_vn_kit: кадры, титры, реплики босса, подпись ника.
    public class IntroView : MonoBehaviour
    {
        const string Art = "Art/Intro/";

        [SerializeField] Button next;

        public event Action Next;

        Image _bg;
        GameObject _mat;
        GameObject _caption;
        CanvasGroup _captionGroup;
        Text _captionText;
        GameObject _dialog;
        GameObject _nameplate;
        Text _name;
        Text _stage;
        Text _body;
        RectTransform _cue;
        GameObject _input;
        InputField _field;
        Button _sign;
        Text _inputTitle;
        Text _inputFine;
        Text _inputButton;
        Button _catcher;
        Font _font;
        Beat[] _beats;
        int _i;
        int _epoch;
        bool _typing;
        bool _skip;
        bool _ready;
        string _nick;
        float _cueY;

        [Serializable]
        class Script
        {
            public Beat[] beats;
        }

        [Serializable]
        class Beat
        {
            public int scene;
            public string bg;
            public string type;
            public string text;
            public bool small;
            public string speaker;
            public bool offscreen;
            public string stage;
            public string title;
            public string placeholder;
            public string button;
            public string fineprint;
        }

        void Awake()
        {
            if (next != null)
                next.gameObject.SetActive(false);
        }

        public void Play()
        {
            Ensure();
            _nick = "";
            _i = 0;
            if (_beats == null || _beats.Length == 0)
            {
                Finish();
                return;
            }

            ShowBeat();
        }

        void Ensure()
        {
            if (_ready)
                return;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            for (int i = transform.childCount - 1; i >= 0; i--)
                transform.GetChild(i).gameObject.SetActive(false);

            _bg = GetComponent<Image>();
            if (_bg != null)
            {
                _bg.color = Color.white;
                _bg.raycastTarget = false;
            }

            var asset = Resources.Load<TextAsset>(Art + "intro_script");
            if (asset != null)
            {
                var script = JsonUtility.FromJson<Script>("{\"beats\":" + asset.text + "}");
                _beats = script != null ? script.beats : null;
            }

            BuildCaption();
            BuildDialog();
            BuildInput();
            _catcher = Clicker("advance", OnAdvance);
            _ready = true;
        }

        void ShowBeat()
        {
            _epoch++;
            var beat = _beats[_i];
            _typing = false;
            _skip = false;
            if (_bg != null)
            {
                string plate = Plate(beat.bg);
                var sprite = Resources.Load<Sprite>(Art + "bg/" + plate);
                if (sprite != null)
                    _bg.sprite = sprite;
            }

            bool caption = beat.type == "caption";
            bool input = beat.type == "input";
            bool baked = caption && beat.bg == "intro_01_hell_corp";
            _mat.SetActive(!caption && !input);
            _caption.SetActive(caption && !baked);
            _dialog.SetActive(!caption && !input);
            _input.SetActive(input);
            _catcher.gameObject.SetActive(!input);
            _cue.gameObject.SetActive(false);

            if (caption)
            {
                if (baked)
                {
                    StartCoroutine(HoldCaption(_epoch));
                    return;
                }

                _captionText.text = beat.text ?? "";
                _captionText.fontSize = beat.small ? 30 : 72;
                _captionText.fontStyle = beat.small ? FontStyle.Italic : FontStyle.BoldAndItalic;
                _captionText.color = Hex(beat.small ? "#e9c9a0" : "#fff3df");
                _captionGroup.alpha = 0f;
                StartCoroutine(FadeCaption(_epoch));
                StartCoroutine(HoldCaption(_epoch));
                return;
            }

            if (input)
            {
                _input.transform.SetAsLastSibling();
                _inputTitle.text = string.IsNullOrEmpty(beat.title) ? "ПОДПИСЬ ПРОДЮСЕРА" : beat.title.ToUpperInvariant();
                _inputFine.text = beat.fineprint ?? "";
                _inputButton.text = string.IsNullOrEmpty(beat.button) ? "ПОДПИСАТЬ" : beat.button.ToUpperInvariant();
                _field.text = "";
                if (_field.placeholder is Text hint)
                    hint.text = string.IsNullOrEmpty(beat.placeholder) ? "Введите имя" : beat.placeholder;
                _sign.interactable = false;
                _field.ActivateInputField();
                return;
            }

            bool named = !string.IsNullOrEmpty(beat.speaker);
            _nameplate.SetActive(named);
            if (named)
            {
                string who = beat.speaker.ToUpperInvariant();
                if (beat.offscreen)
                    who += "  <size=18><color=#e9b9cf><i>(за кадром)</i></color></size>";
                _name.text = who;
            }

            bool staged = !string.IsNullOrEmpty(beat.stage) || beat.type == "stage_only";
            _stage.gameObject.SetActive(staged);
            _stage.text = beat.type == "stage_only" ? "" : "*" + beat.stage + "*";
            string line = beat.type == "stage_only" ? "*" + beat.text + "*" : beat.text ?? "";
            _body.color = beat.type == "stage_only" ? Hex("#d9a0b8") : Color.white;
            _body.fontStyle = beat.type == "stage_only" ? FontStyle.Italic : FontStyle.Normal;
            var bodyRect = _body.rectTransform;
            bodyRect.anchoredPosition = new Vector2(236f, staged && beat.type != "stage_only" ? -912f : -872f);
            bodyRect.sizeDelta = new Vector2(1450f, staged && beat.type != "stage_only" ? 130f : 170f);
            StartCoroutine(TypeLine(line, _epoch));
        }

        void OnAdvance()
        {
            if (_beats == null || _i < 0 || _i >= _beats.Length)
                return;
            if (_beats[_i].type == "input")
                return;
            if (_typing)
            {
                _skip = true;
                return;
            }

            Advance();
        }

        void Advance()
        {
            if (_i + 1 >= _beats.Length)
                Finish();
            else
            {
                _i++;
                ShowBeat();
            }
        }

        void Finish()
        {
            _epoch++;
            string name = string.IsNullOrWhiteSpace(_nick) ? "Продюсер" : _nick.Trim();
            if (GameSession.State != null)
            {
                GameSession.State.producerName = name;
                GameSession.Commit();
            }

            Next?.Invoke();
        }

        IEnumerator FadeCaption(int epoch)
        {
            float t = 0f;
            while (t < 0.8f && epoch == _epoch)
            {
                t += Time.unscaledDeltaTime;
                _captionGroup.alpha = Mathf.Clamp01(t / 0.8f);
                yield return null;
            }

            if (epoch == _epoch)
                _captionGroup.alpha = 1f;
        }

        IEnumerator HoldCaption(int epoch)
        {
            yield return new WaitForSecondsRealtime(3.3f);
            if (epoch == _epoch)
                Advance();
        }

        IEnumerator TypeLine(string line, int epoch)
        {
            _typing = true;
            _body.text = "";
            float step = 1f / 45f;
            float wait = 0f;
            int n = 0;
            while (n < line.Length && epoch == _epoch && !_skip)
            {
                wait += Time.unscaledDeltaTime;
                while (wait >= step && n < line.Length)
                {
                    wait -= step;
                    n++;
                }

                _body.text = line.Substring(0, n);
                yield return null;
            }

            if (epoch != _epoch)
                yield break;
            _body.text = line;
            _typing = false;
            _cue.gameObject.SetActive(true);
        }

        void Update()
        {
            if (_cue == null || !_cue.gameObject.activeSelf)
                return;
            float y = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 0.8f) * 4f;
            var pos = _cue.anchoredPosition;
            pos.y = _cueY + y;
            _cue.anchoredPosition = pos;
        }

        void BuildCaption()
        {
            _caption = new GameObject("caption", typeof(RectTransform), typeof(CanvasGroup));
            _caption.transform.SetParent(transform, false);
            Stretch(_caption.GetComponent<RectTransform>());
            _captionGroup = _caption.GetComponent<CanvasGroup>();
            var back = ArtImage(_caption.transform, "capBack", Art + "vn/caption_backdrop", false);
            Pin(back.rectTransform, 0f, 700f, 1920f, 300f);
            var scrim = ArtImage(_caption.transform, "scrim", null, false);
            scrim.color = new Color(0.03f, 0.008f, 0.015f, 0.94f);
            Pin(scrim.rectTransform, 180f, 770f, 1560f, 210f);
            Divider(_caption.transform, 708f, false);
            Divider(_caption.transform, 932f, true);
            _captionText = Words(_caption.transform, 72, Hex("#fff3df"), FontStyle.BoldAndItalic, TextAnchor.MiddleCenter);
            Pin(_captionText.rectTransform, 160f, 760f, 1600f, 160f);
            Glow(_captionText, Hex("#ff3355"));
            _caption.SetActive(false);
        }

        void BuildDialog()
        {
            _dialog = new GameObject("dialog", typeof(RectTransform));
            _dialog.transform.SetParent(transform, false);
            Stretch(_dialog.GetComponent<RectTransform>());
            var mat = ArtImage(transform, "mat", null, false);
            mat.color = new Color(0.04f, 0.015f, 0.02f, 1f);
            Pin(mat.rectTransform, 140f, 755f, 1640f, 310f);
            _mat = mat.gameObject;
            _mat.transform.SetSiblingIndex(_dialog.transform.GetSiblingIndex());
            _mat.SetActive(false);
            var box = ArtImage(_dialog.transform, "box", Art + "vn/dialog_box", true);
            Pin(box.rectTransform, 160f, 820f, 1600f, 240f);
            var eye = ArtImage(_dialog.transform, "eye", Art + "vn/eye_emblem", false);
            Pin(eye.rectTransform, 900f, 788f, 120f, 70f);
            var plate = ArtImage(_dialog.transform, "plate", Art + "vn/nameplate_boss", true);
            _nameplate = plate.gameObject;
            Pin(plate.rectTransform, 210f, 786f, 340f, 64f);
            _name = Words(plate.transform, 28, Hex("#ffd7ea"), FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(_name.rectTransform);
            Glow(_name, Hex("#ff4fa3"));
            _stage = Words(_dialog.transform, 26, Hex("#d9a0b8"), FontStyle.Italic, TextAnchor.UpperLeft);
            Pin(_stage.rectTransform, 236f, 872f, 1450f, 36f);
            _body = Words(_dialog.transform, 32, Color.white, FontStyle.Normal, TextAnchor.UpperLeft);
            _body.lineSpacing = 1.45f;
            Pin(_body.rectTransform, 236f, 872f, 1450f, 170f);
            var cue = ArtImage(_dialog.transform, "more", Art + "vn/continue_indicator", false);
            _cue = cue.rectTransform;
            Pin(_cue, 1690f, 1000f, 40f, 40f);
            _cueY = _cue.anchoredPosition.y;
            _dialog.SetActive(false);
        }

        void BuildInput()
        {
            _input = new GameObject("sign", typeof(RectTransform));
            _input.transform.SetParent(transform, false);
            Stretch(_input.GetComponent<RectTransform>());
            var dim = ArtImage(_input.transform, "dim", null, false);
            dim.color = new Color(0f, 0f, 0f, 0.4f);
            Stretch(dim.rectTransform);
            var panel = ArtImage(_input.transform, "panel", Art + "vn/input_panel", true);
            Pin(panel.rectTransform, 560f, 300f, 800f, 440f);
            _inputTitle = Words(panel.transform, 34, Hex("#f3e2c0"), FontStyle.Bold, TextAnchor.MiddleCenter);
            _inputTitle.text = "ПОДПИСЬ ПРОДЮСЕРА";
            Pin(_inputTitle.rectTransform, 40f, 48f, 720f, 48f);

            var fieldGo = new GameObject("field", typeof(RectTransform), typeof(Image), typeof(InputField));
            fieldGo.transform.SetParent(panel.transform, false);
            var fieldImage = fieldGo.GetComponent<Image>();
            fieldImage.sprite = Resources.Load<Sprite>(Art + "vn/input_field");
            fieldImage.type = Image.Type.Sliced;
            Pin(fieldGo.GetComponent<RectTransform>(), 90f, 120f, 620f, 72f);
            var typed = Words(fieldGo.transform, 32, Color.white, FontStyle.Italic, TextAnchor.MiddleLeft);
            Stretch(typed.rectTransform);
            typed.rectTransform.offsetMin = new Vector2(18f, 0f);
            typed.rectTransform.offsetMax = new Vector2(-18f, 0f);
            var hint = Words(fieldGo.transform, 28, new Color(1f, 1f, 1f, 0.35f), FontStyle.Italic, TextAnchor.MiddleLeft);
            hint.text = "Введите имя";
            Stretch(hint.rectTransform);
            hint.rectTransform.offsetMin = new Vector2(18f, 0f);
            hint.rectTransform.offsetMax = new Vector2(-18f, 0f);
            _field = fieldGo.GetComponent<InputField>();
            _field.textComponent = typed;
            _field.placeholder = hint;
            _field.characterLimit = 20;
            _field.lineType = InputField.LineType.SingleLine;
            _field.caretColor = Color.white;
            _field.onValueChanged.AddListener(value => _sign.interactable = !string.IsNullOrWhiteSpace(value));
            var ink = new GameObject("ink", typeof(RectTransform), typeof(Image));
            ink.transform.SetParent(fieldGo.transform, false);
            var inkImage = ink.GetComponent<Image>();
            inkImage.color = new Color(0.08f, 0.04f, 0.06f, 1f);
            inkImage.raycastTarget = false;
            var inkRect = ink.GetComponent<RectTransform>();
            Stretch(inkRect);
            inkRect.offsetMin = new Vector2(10f, 22f);
            inkRect.offsetMax = new Vector2(-10f, -22f);
            ink.transform.SetAsFirstSibling();
            var skin = fieldGo.AddComponent<FieldSkin>();
            skin.image = fieldImage;
            skin.normal = fieldImage.sprite;
            skin.focus = Resources.Load<Sprite>(Art + "vn/input_field_focus");

            var buttonGo = new GameObject("signBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(panel.transform, false);
            var buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.sprite = Resources.Load<Sprite>(Art + "vn/button_normal");
            buttonImage.type = Image.Type.Sliced;
            Pin(buttonGo.GetComponent<RectTransform>(), 260f, 212f, 280f, 80f);
            _sign = buttonGo.GetComponent<Button>();
            _sign.targetGraphic = buttonImage;
            _sign.transition = Selectable.Transition.SpriteSwap;
            var swap = _sign.spriteState;
            swap.highlightedSprite = Resources.Load<Sprite>(Art + "vn/button_hover");
            swap.pressedSprite = Resources.Load<Sprite>(Art + "vn/button_pressed");
            swap.disabledSprite = Resources.Load<Sprite>(Art + "vn/button_disabled");
            _sign.spriteState = swap;
            _sign.interactable = false;
            _sign.onClick.AddListener(() =>
            {
                _nick = _field.text.Trim();
                Advance();
            });
            _inputButton = Words(buttonGo.transform, 26, Hex("#fff3df"), FontStyle.Bold, TextAnchor.MiddleCenter);
            _inputButton.text = "ПОДПИСАТЬ";
            Stretch(_inputButton.rectTransform);

            _inputFine = Words(panel.transform, 15, Hex("#a8898f"), FontStyle.Italic, TextAnchor.UpperCenter);
            _inputFine.verticalOverflow = VerticalWrapMode.Truncate;
            Pin(_inputFine.rectTransform, 80f, 318f, 640f, 72f);
            _input.SetActive(false);
        }

        void Divider(Transform parent, float y, bool flip)
        {
            var image = ArtImage(parent, "rule", Art + "vn/caption_divider", false);
            Pin(image.rectTransform, 510f, y, 900f, 40f);
            if (flip)
                image.rectTransform.localScale = new Vector3(1f, -1f, 1f);
        }

        Button Clicker(string name, Action click)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            Stretch(go.GetComponent<RectTransform>());
            var image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => click());
            return button;
        }

        Image ArtImage(Transform parent, string name, string path, bool sliced)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            if (!string.IsNullOrEmpty(path))
            {
                image.sprite = Resources.Load<Sprite>(path);
                image.type = sliced && image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                image.color = Color.white;
                image.preserveAspect = false;
            }

            return image;
        }

        Text Words(Transform parent, int size, Color color, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject("t", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        static void Glow(Text text, Color color)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1.6f, -1.6f);
        }

        static string Plate(string bg)
        {
            switch (bg)
            {
                case "intro_02_contract": return "scene_2";
                case "intro_04_boss_chair": return "scene_4";
                case "intro_05_monitors": return "scene_5";
                case "intro_06_glasses": return "scene_6";
                case "intro_07_staff": return "scene_7";
                default: return "scene_1";
            }
        }

        static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out var color);
            return color;
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

        class FieldSkin : MonoBehaviour, ISelectHandler, IDeselectHandler
        {
            public Image image;
            public Sprite normal;
            public Sprite focus;

            public void OnSelect(BaseEventData eventData)
            {
                if (image != null && focus != null)
                    image.sprite = focus;
            }

            public void OnDeselect(BaseEventData eventData)
            {
                if (image != null && normal != null)
                    image.sprite = normal;
            }
        }
    }
}
