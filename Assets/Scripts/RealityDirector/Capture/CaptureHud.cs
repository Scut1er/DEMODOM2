using System;
using System.Collections.Generic;
using System.Text;
using RealityDirector.Cards;
using RealityDirector.Core;
using RealityDirector.NPC;
using RealityDirector.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.Capture
{
    // Что видит камера — маленькой полупрозрачной плашкой сбоку от рамки, а не окном посреди кадра.
    // Рамка (мировой прицел CaptureSystem) остаётся главным инструментом: плашка и REC живут у её края
    // и не заслоняют людей. Данные — те же, по которым кадр попадёт в футаж (CaptureSystem.Scan).
    public class CaptureHud : MonoBehaviour
    {
        CaptureSystem _capture;
        CardStage _stage;
        Func<bool> _sponsorDeal;
        RectTransform _root;
        RectTransform _panel;
        Text _body;
        RectTransform _rec;
        Text _recText;
        Image _recDot;
        readonly CaptureSystem.FrameScan _scan = new CaptureSystem.FrameScan();
        float _nextScan;
        string _text = "";

        public static CaptureHud Create(CaptureSystem capture, CardStage stage, Func<bool> sponsorDeal)
        {
            var go = new GameObject("CaptureHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var hud = go.AddComponent<CaptureHud>();
            hud._capture = capture;
            hud._stage = stage;
            hud._sponsorDeal = sponsorDeal;
            hud.Build();
            return hud;
        }

        void Build()
        {
            _root = (RectTransform)transform;
            var plate = UiKit.Img("frameInfo", _root, null, Color.white);
            UiKit.Dress(plate, UiKit.Frame.Dark);
            plate.color = new Color(1f, 1f, 1f, 0.78f);
            _panel = plate.rectTransform;
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0f, 1f);
            _panel.sizeDelta = new Vector2(Width, 100f);
            _body = UiKit.Txt("body", _panel, "", 18, UiKit.Paper, TextAnchor.UpperLeft, UiKit.Body);
            _body.supportRichText = true;
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.lineSpacing = 1.05f;
            UiKit.Stretch(_body.rectTransform, 0f);
            _body.rectTransform.offsetMin = new Vector2(14f, 10f);
            _body.rectTransform.offsetMax = new Vector2(-12f, -10f);

            _rec = UiKit.Rect("rec", _root);
            _rec.anchorMin = _rec.anchorMax = new Vector2(0.5f, 0.5f);
            _rec.pivot = new Vector2(0f, 0f);
            _rec.sizeDelta = new Vector2(230f, 26f);
            _recDot = UiKit.Img("dot", _rec, UiKit.Circle(), UiKit.Blood);
            UiKit.Place(_recDot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(14f, 14f));
            _recText = UiKit.Txt("t", _rec, "", 17, UiKit.Paper, TextAnchor.MiddleLeft, UiKit.Body);
            _recText.fontStyle = FontStyle.Bold;
            UiKit.Place(_recText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(210f, 26f));
            UiKit.Shadow(_recText);
            Show(false);
        }

        void Show(bool on)
        {
            if (_panel.gameObject.activeSelf != on)
                _panel.gameObject.SetActive(on);
            if (_rec.gameObject.activeSelf != on)
                _rec.gameObject.SetActive(on);
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (_capture == null || cam == null || !_capture.Mode || !_capture.FrameVisible)
            {
                Show(false);
                return;
            }

            Show(true);
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 0.15f;
                _capture.Scan(_scan);
                string text = Describe();
                if (text != _text)
                {
                    _text = text;
                    _body.text = text;
                    _panel.sizeDelta = new Vector2(Width, Mathf.Max(54f, _body.preferredHeight + 24f));
                }
            }

            Place(cam);
            Rec();
        }

        const float Width = 290f;

        // Плашка справа от рамки (у правого края экрана — слева), REC — сразу над плашкой: у края рамки,
        // но не над головами (там реплики участников).
        void Place(Camera cam)
        {
            Vector2 c = _capture.FrameCenter;
            Vector2 min = Local(cam, c + new Vector2(-CaptureSystem.HalfX, -CaptureSystem.HalfY));
            Vector2 max = Local(cam, c + new Vector2(CaptureSystem.HalfX, CaptureSystem.HalfY));
            float halfW = _root.rect.width * 0.5f;
            float halfH = _root.rect.height * 0.5f;
            float w = _panel.sizeDelta.x;
            bool right = max.x + 14f + w <= halfW - 8f;
            _panel.pivot = new Vector2(right ? 0f : 1f, 1f);
            float x = right ? max.x + 14f : min.x - 14f;
            float y = Mathf.Clamp(max.y, -halfH + _panel.sizeDelta.y + 8f, halfH - 8f);
            _panel.anchoredPosition = new Vector2(x, y);
            _rec.pivot = new Vector2(right ? 0f : 1f, 0f);
            _rec.anchoredPosition = new Vector2(right ? x + 4f : x - 4f, y + 6f);
        }

        Vector2 Local(Camera cam, Vector2 world)
        {
            Vector3 screen = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var local);
            return local;
        }

        void Rec()
        {
            bool live = _capture.Recording;
            _recDot.color = live ? (Mathf.Repeat(Time.unscaledTime, 1f) < 0.6f ? UiKit.Blood : new Color(0.4f, 0.05f, 0.08f)) : new Color(0.55f, 0.5f, 0.5f, 0.8f);
            _recText.text = live
                ? "REC " + Clock(_capture.Recorded) + " / " + Clock(CaptureSystem.MaxClip)
                : _capture.IsFull ? "слоты футажа заняты" : "зажми ЛКМ — запись";
            _recText.color = live ? UiKit.Paper : UiKit.Muted;
        }

        static string Clock(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return "00:" + s.ToString("00");
        }

        // ---------- Текст ----------

        string Describe()
        {
            var sb = new StringBuilder();
            sb.Append("<color=#F2C35C><b>В КАДРЕ</b></color>");
            if (_scan.actors.Count == 0 && !_scan.fire)
            {
                sb.Append("\n<color=#A89F96>никого — наведи на людей</color>");
                return sb.ToString();
            }

            foreach (var npc in _scan.actors)
            {
                if (npc == null)
                    continue;
                sb.Append("\n<b>").Append(npc.DisplayName).Append("</b> — <color=").Append(npc.MoodHex()).Append(">").Append(Doing(npc)).Append("</color>");
            }

            var events = new List<string>();
            foreach (var tag in _scan.tags)
            {
                string name = EventName(tag);
                if (name != null && !events.Contains(name))
                    events.Add(name);
                if (events.Count >= 3)
                    break;
            }

            if (_scan.fire && !events.Contains("пожар"))
                events.Insert(0, "пожар");
            if (events.Count > 0)
                sb.Append("\n<color=#FF8A4C>").Append(string.Join(" · ", events)).Append("</color>");

            bool sponsor = false;
            foreach (var line in Props(ref sponsor))
                sb.Append("\n").Append(line);

            int score = Score(sponsor, events.Count);
            string level = score >= 5 ? "ВЫСОКАЯ" : score >= 2 ? "СРЕДНЯЯ" : "НИЗКАЯ";
            string color = score >= 5 ? CardBrief.Red : score >= 2 ? CardBrief.Gold : CardBrief.Muted;
            sb.Append("\nОценка момента: <color=").Append(color).Append("><b>").Append(level).Append("</b></color>");
            if (CaptureSystem.BonusActive)
                sb.Append("\n<color=#8FE3FF>★ нужный момент: качество выше</color>");
            return sb.ToString();
        }

        // Что человек делает прямо сейчас — действие важнее фоновой эмоции.
        static string Doing(NPCController npc)
        {
            if (npc.IsFighting)
                return "дерётся";
            if (npc.IsCrying)
                return "плачет";
            if (npc.Action == NpcActionId.Panic)
                return "в панике";
            if (npc.Action == NpcActionId.SeekFight)
                return "лезет в драку";
            return npc.Mood();
        }

        static string EventName(string tag)
        {
            switch (tag)
            {
                case MomentTags.Warmth:
                case MomentTags.Misery:
                case "Public":
                case "Private":
                case "Pressure":
                    return null; // фон, не событие
                case MomentTags.Sponsor:
                    return "реклама в кадре";
            }

            string name = CardBrief.TagName(tag);
            return name == tag && tag.Length > 0 && char.IsUpper(tag[0]) && !IsKnown(tag) ? null : name;
        }

        static bool IsKnown(string tag)
        {
            return tag == MomentTags.Fight || tag == MomentTags.Fire || tag == MomentTags.Crying || tag == MomentTags.Hug || tag == MomentTags.Conflict;
        }

        IEnumerable<string> Props(ref bool sponsor)
        {
            var lines = new List<string>();
            if (_stage == null)
                return lines;
            foreach (var prop in _stage.Props)
            {
                if (prop == null || prop.Def == null)
                    continue;
                Vector2 p = prop.transform.position;
                if (Mathf.Abs(p.x - _scan.center.x) > CaptureSystem.HalfX + 0.2f || Mathf.Abs(p.y - _scan.center.y) > CaptureSystem.HalfY + 0.4f)
                    continue;
                string name = StageProp.LabelOf(prop.Kind);
                if (string.IsNullOrEmpty(name))
                    continue;
                if (prop.Sponsor)
                {
                    sponsor = true;
                    lines.Add("<color=#7FE08A>" + name + " — спонсор" + (_sponsorDeal != null && _sponsorDeal() ? " ✓" : "") + "</color>");
                }
                else
                {
                    lines.Add("<color=#C9BFB8>" + name + "</color>");
                }

                if (lines.Count >= 2)
                    break;
            }

            return lines;
        }

        // Без формул: люди в кадре, сильные события, высокие эмоции, спонсор, бонус качества.
        int Score(bool sponsor, int events)
        {
            int score = Mathf.Min(2, _scan.actors.Count);
            score += Mathf.Min(2, events) * 2;
            foreach (var npc in _scan.actors)
            {
                if (npc != null && Mathf.Max(npc.Anger, Mathf.Max(npc.Stress, Mathf.Max(npc.Sadness, npc.Attraction))) >= 60)
                {
                    score++;
                    break;
                }
            }

            if (sponsor && _sponsorDeal != null && _sponsorDeal())
                score++;
            if (CaptureSystem.BonusActive)
                score++;
            if (_scan.fire)
                score += 2;
            return score;
        }
    }
}
