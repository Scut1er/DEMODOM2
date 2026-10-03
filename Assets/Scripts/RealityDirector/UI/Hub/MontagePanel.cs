using System.Collections.Generic;
using System.Text;
using RealityDirector.Capture;
using RealityDirector.Core;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Монтаж как игра: карточка кадра объясняет его ценность, между слотами — сила связи,
    // под склейкой — почему связность такая, справа — как это увидит аудитория и итог перед эфиром.
    public class MontagePanel
    {
        RectTransform _audience;
        Text _audienceText;
        RectTransform _breakdown;
        Text _breakdownText;
        RectTransform _tip;
        Text _tipText;
        Font _font;

        public RectTransform AudienceFocus => _audience;
        public RectTransform BreakdownFocus => _breakdown;

        public static MontagePanel Build(RectTransform page, Font font)
        {
            var p = new MontagePanel { _font = font };
            p._audience = Plate(page, "audience", 1440f, 474f, 444f, 450f);
            p._audienceText = Body(p._audience, font, 17);
            p._breakdown = Plate(page, "breakdown", 36f, 792f, 1380f, 128f);
            p._breakdownText = Body(p._breakdown, font, 16);

            var tipImg = UiKit.Img("clipTip", page, null, Color.white);
            UiKit.Dress(tipImg, UiKit.Frame.Dark);
            tipImg.color = new Color(1f, 1f, 1f, 0.96f);
            p._tip = tipImg.rectTransform;
            p._tip.anchorMin = p._tip.anchorMax = new Vector2(0f, 1f);
            p._tip.pivot = new Vector2(0f, 1f);
            p._tip.sizeDelta = new Vector2(330f, 120f);
            p._tipText = Body(p._tip, font, 16);
            p._tip.gameObject.SetActive(false);
            return p;
        }

        static RectTransform Plate(RectTransform page, string name, float x, float y, float w, float h)
        {
            var img = UiKit.Img(name, page, null, Color.white);
            UiKit.DressSolid(img, UiKit.Frame.Dialog);
            var rect = img.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        static Text Body(RectTransform parent, Font font, int size)
        {
            var t = UiKit.Txt("body", parent, "", size, UiKit.Paper, TextAnchor.UpperLeft, font != null ? font : UiKit.Body);
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.05f;
            UiKit.Stretch(t.rectTransform, 0f);
            t.rectTransform.offsetMin = new Vector2(18f, 12f);
            t.rectTransform.offsetMax = new Vector2(-16f, -14f);
            return t;
        }

        // ---------- Итог и аудитория ----------

        public void Show(CutReport r, int slots, IList<string> extra)
        {
            var sb = new StringBuilder();
            sb.Append("<b><color=#F2C35C>КАК ЭТО УВИДИТ АУДИТОРИЯ</color></b>");
            sb.Append("\n").Append(Tone("ДРАМА", r.drama, "#7FB8FF")).Append("   ").Append(Tone("ТРЭШ", r.trash, "#FF7A5C")).Append("   ").Append(Tone("СЕМЬЯ", r.family, "#7FE08A"));
            if (r.clips.Count == 0)
            {
                sb.Append("\n\n<color=#A89F96>Положите кадры в слоты — здесь появится прогноз.</color>");
                _audienceText.text = sb.ToString();
                Breakdown(r);
                return;
            }

            sb.Append("\n\n<color=#A89F96>История:</color> ").Append(string.IsNullOrEmpty(r.story) ? "<color=#FF7A5C>пока не складывается</color>" : r.story);
            if (r.combos.Count > 0)
            {
                sb.Append("\n<color=#A89F96>Комбо:</color>");
                foreach (var c in r.combos)
                    sb.Append("\n  <b><color=#F2C35C>").Append(c.title).Append("</color></b>  <color=#C9BFB8>").Append(c.effect).Append("</color>");
            }

            sb.Append("\n\n<b>ИТОГ МОНТАЖА</b>   ").Append(r.clips.Count).Append("/").Append(slots).Append(" кадров");
            sb.Append("\nПотенциал рейтинга: ").Append(Paint(r.potential));
            sb.Append("\nСвязность: <b>").Append(r.coherence).Append("%</b>");
            sb.Append("\nЭмоциональность: ").Append(Paint(r.emotion));
            sb.Append("\nПовторяемость: ").Append(PaintBad(r.repetition));
            if (extra != null)
            {
                foreach (var line in extra)
                {
                    if (!string.IsNullOrEmpty(line))
                        sb.Append("\n").Append(line);
                }
            }

            sb.Append("\n<color=#A89F96><size=14>Прогноз, а не гарантия: точная оценка — в эфире.</size></color>");
            _audienceText.text = sb.ToString();
            Breakdown(r);
        }

        void Breakdown(CutReport r)
        {
            var sb = new StringBuilder();
            sb.Append("<b>СВЯЗНОСТЬ ").Append(r.coherence).Append("%</b>   <color=#A89F96>почему так:</color>");
            int shown = 0;
            foreach (var p in r.plus)
            {
                if (shown >= 4)
                    break;
                sb.Append(shown % 2 == 0 ? "\n" : "      ").Append("<color=#7FE08A>+ ").Append(p).Append("</color>");
                shown++;
            }

            foreach (var m in r.minus)
            {
                if (shown >= 6)
                    break;
                sb.Append(shown % 2 == 0 ? "\n" : "      ").Append("<color=#FF7A5C>− ").Append(m).Append("</color>");
                shown++;
            }

            if (shown == 0)
                sb.Append(r.clips.Count < 2 ? "\n<color=#A89F96>Связность считается между соседними кадрами: нужно минимум два.</color>" : "\n<color=#A89F96>Кадры не связаны и не мешают друг другу.</color>");
            _breakdownText.text = sb.ToString();
        }

        static string Tone(string name, int value, string color)
        {
            return "<color=" + color + "><b>" + name + " " + CutAnalysis.Arrows(value) + "</b></color>";
        }

        static string Paint(int level)
        {
            string color = level >= 2 ? "#7FE08A" : level == 1 ? "#F2C35C" : "#FF7A5C";
            return "<color=" + color + "><b>" + CutAnalysis.Level(level) + "</b></color>";
        }

        static string PaintBad(int level)
        {
            string color = level >= 2 ? "#FF7A5C" : level == 1 ? "#F2C35C" : "#7FE08A";
            return "<color=" + color + "><b>" + CutAnalysis.Level(level) + "</b></color>";
        }

        // ---------- Карточка кадра ----------

        // Под фото: что случилось и роль в истории, кто, тон, сила и качество. Наведение — почему кадр интересен.
        public void Decorate(GameObject card, FootageClip clip)
        {
            var f = CutAnalysis.Facts(clip);
            var sb = new StringBuilder();
            string name = f.sponsor ? "РЕКЛАМА · " + f.what : f.what;
            sb.Append("<b>").Append(name).Append("</b>  <color=#F2C35C><size=13>").Append(CutAnalysis.RoleTitle(f.main)).Append("</size></color>");
            sb.Append("\n<color=#C9BFB8>").Append(f.actors.Count > 0 ? string.Join(" + ", f.actors) : f.blank ? "пусто" : "реквизит").Append("</color>");
            var tone = new List<string>();
            if (f.drama > 0)
                tone.Add("<color=#7FB8FF>драма +" + f.drama + "</color>");
            if (f.trash > 0)
                tone.Add("<color=#FF7A5C>трэш +" + f.trash + "</color>");
            if (f.family > 0)
                tone.Add("<color=#7FE08A>семья +" + f.family + "</color>");
            if (tone.Count > 0)
                sb.Append("\n").Append(string.Join("  ", tone));
            sb.Append("\n").Append(Pips(f.intensity)).Append("  <color=").Append(f.quality >= 2 ? "#7FE08A" : f.quality == 1 ? "#C9BFB8" : "#FF7A5C").Append(">")
                .Append(f.quality >= 2 ? "отличный" : f.quality == 1 ? "хороший" : "брак").Append("</color>");

            var label = UiKit.Txt("facts", card.transform, sb.ToString(), 15, Color.white, TextAnchor.UpperLeft, _font != null ? _font : UiKit.Body);
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(10f, -120f);
            rect.sizeDelta = new Vector2(192f, 150f);

            var hover = card.AddComponent<Hover>();
            var why = new StringBuilder();
            why.Append("<b>Почему этот кадр интересен</b>");
            foreach (var w in f.why)
                why.Append("\n• ").Append(w);
            why.Append("\n<color=#A89F96>Роли: ");
            var roles = new List<string>();
            foreach (var r in f.roles)
                roles.Add(CutAnalysis.RoleWord(r));
            why.Append(string.Join(", ", roles)).Append("</color>");
            string text = why.ToString();
            hover.enter = () => ShowTip((RectTransform)card.transform, text);
            hover.exit = HideTip;
        }

        static string Pips(int n)
        {
            var sb = new StringBuilder("<color=#FF8A4C>");
            for (int i = 0; i < 5; i++)
                sb.Append(i < n ? "●" : "<color=#5A4E52>●</color>");
            sb.Append("</color>");
            return sb.ToString();
        }

        void ShowTip(RectTransform card, string text)
        {
            _tipText.text = text;
            float h = _tipText.preferredHeight + 30f;
            _tip.sizeDelta = new Vector2(330f, h);
            var parent = (RectTransform)_tip.parent;
            var corners = new Vector3[4];
            card.GetWorldCorners(corners);
            Vector2 topRight = parent.InverseTransformPoint(corners[2]);
            Vector2 local = new Vector2(topRight.x + 8f + parent.rect.width * parent.pivot.x, topRight.y - parent.rect.height * (1f - parent.pivot.y));
            if (local.x + 330f > parent.rect.width - 10f)
            {
                Vector2 topLeft = parent.InverseTransformPoint(corners[1]);
                local.x = topLeft.x - 338f + parent.rect.width * parent.pivot.x;
            }

            _tip.anchoredPosition = local;
            _tip.gameObject.SetActive(true);
            _tip.SetAsLastSibling();
        }

        void HideTip()
        {
            if (_tip != null)
                _tip.gameObject.SetActive(false);
        }

        // ---------- Связь между слотами ----------

        public GameObject LinkMark(RectTransform row, CutLink link)
        {
            var go = new GameObject("link", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(row, false);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = 104f;
            element.preferredHeight = 120f;
            var img = go.GetComponent<Image>();
            Color color = link.level >= 2 ? UiKit.Good : link.level == 1 ? UiKit.Gold : UiKit.Ember;
            img.color = new Color(color.r, color.g, color.b, 0.16f);
            var arrow = UiKit.Txt("arrow", go.transform, "→", 34, color, TextAnchor.UpperCenter, _font != null ? _font : UiKit.Body);
            UiKit.Stretch(arrow.rectTransform, 0f);
            arrow.rectTransform.offsetMax = new Vector2(0f, -8f);
            var word = UiKit.Txt("word", go.transform, link.level >= 2 ? "ХОРОШАЯ\nСВЯЗЬ" : link.level == 1 ? "СЛАБАЯ\nСВЯЗЬ" : "РЕЗКИЙ\nПЕРЕХОД", 13, color, TextAnchor.LowerCenter, _font != null ? _font : UiKit.Body);
            word.fontStyle = FontStyle.Bold;
            UiKit.Stretch(word.rectTransform, 0f);
            word.rectTransform.offsetMin = new Vector2(0f, 10f);

            var hover = go.AddComponent<Hover>();
            var sb = new StringBuilder();
            sb.Append("<b>").Append(link.level >= 2 ? "Хорошая связь" : link.level == 1 ? "Слабая связь" : "Резкий переход").Append("</b>  ")
                .Append(link.score >= 0 ? "+" : "").Append(link.score).Append(" к связности");
            foreach (var r in link.reasons)
                sb.Append("\n").Append(r.StartsWith("−") ? "<color=#FF7A5C>" : "<color=#7FE08A>").Append(r).Append("</color>");
            if (link.reasons.Count == 0)
                sb.Append("\n<color=#A89F96>ничего общего, но и не мешают</color>");
            string text = sb.ToString();
            hover.enter = () => ShowTip((RectTransform)go.transform, text);
            hover.exit = HideTip;
            return go;
        }

        class Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public System.Action enter;
            public System.Action exit;

            public void OnPointerEnter(PointerEventData eventData)
            {
                enter?.Invoke();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                exit?.Invoke();
            }

            void OnDisable()
            {
                exit?.Invoke();
            }
        }
    }
}
