using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Events;
using RealityDirector.Meta;
using RealityDirector.NPC;
using RealityDirector.UI;
using RealityDirector.Util;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Карта человеческим языком: что произойдёт при розыгрыше, где её взять, кто как отреагирует.
    public static class CardInsight
    {
        // По порядку TargetType.
        public static readonly string[] TargetNames = { "Объект в квартире", "Участник", "Весь дом сразу", "Пара участников", "Зона / комната", "Карта в руке", "Использованная карта" };

        static List<ReactionRule> _rules;
        static List<TraitId> _ruleTraits;

        static CardInsight()
        {
            EditorApplication.projectChanged += () => _rules = null;
        }

        // Тон даёт только первые две метки — так считает квартира.
        public const int MoodLimit = 2;

        public static bool Obtainable(EventDefinition c)
        {
            return c.starter || (c.price > 0 && !c.sponsor) || c.runPrice > 0;
        }

        public static string Sources(EventDefinition c)
        {
            var parts = new List<string>();
            if (c.starter)
                parts.Add("стартовая колода");
            if (c.price > 0 && !c.sponsor)
                parts.Add("магазин хаба " + c.price + " кр");
            if (c.runPrice > 0)
                parts.Add("магазин выпуска " + c.runPrice + " нал");
            return parts.Count > 0 ? string.Join("  ·  ", parts) : "нигде не выдаётся";
        }

        public static string Badge(EventDefinition c)
        {
            if (c.status == CardStatus.Disabled)
                return "ВЫКЛ";
            if (c.sponsor)
                return "СПОНСОР";
            if (c.starter)
                return "СТАРТ";
            if (c.runPrice > 0 && c.price <= 0)
                return "ВЫПУСК";
            if (c.price > 0)
                return "ХАБ";
            return "—";
        }

        // «Злость +1d6 (+2) · d6 → d8: рядом тег Alcohol».
        public static List<string> DiceLines(EventDefinition c)
        {
            var lines = new List<string>();
            if (c.diceEffects == null)
                return lines;
            foreach (var d in c.diceEffects)
            {
                if (d == null)
                    continue;
                string what = d.subject == DiceSubject.Relationship ? Enum(d.axis)
                    : d.subject == DiceSubject.Check ? (string.IsNullOrEmpty(d.check) ? "Проверка" : d.check)
                    : Enum(d.stat);
                string line = what + (d.lower ? " −" : " +") + Mathf.Max(1, d.count) + "d" + Dice.Faces(d.die) + (d.bonus != 0 ? (d.bonus > 0 ? " +" : " ") + d.bonus : "");
                if (!string.IsNullOrEmpty(d.onlyIf))
                    line += (d.onlyIf.StartsWith("при ") ? ", " : ", только если ") + d.onlyIf;
                if (d.roll == DiceRoll.Advantage)
                    line += ", преимущество";
                else if (d.roll == DiceRoll.Disadvantage)
                    line += ", помеха";
                foreach (var up in d.stepUps)
                {
                    if (up == null)
                        continue;
                    string when = up.condition == StepUpCondition.TargetStatAtLeast
                        ? Enum(up.stat).ToLowerInvariant() + " ≥ " + up.value
                        : Enum(up.condition).ToLowerInvariant() + " «" + up.key + "»";
                    line += "  ·  d" + Dice.Faces(Dice.Step(d.die, up.steps)) + ", если " + when;
                }

                lines.Add(line);
            }

            return lines;
        }

        public static string Passport(EventDefinition c)
        {
            return HellToken.Format(c.cost) + " HellToken  ·  " + CategoryName(c.category) + "  ·  Tier " + c.tier + "  ·  " + Enum(c.rarity) + "  ·  " + Enum(c.status);
        }

        // Категории карт — те, что понимает игра (Progression.CategoryOpen): по ним карты открываются уровнем Сценаристов.
        public static readonly string[] Categories = { "", "Provocation", "Environment", "Comedy", "Control", "Social", "Confession", "DeckManagement", "Reveal", "Sponsor" };
        public static readonly string[] CategoryNames =
        {
            "Без категории — открыта сразу",
            "Провокация — открыта сразу",
            "Окружение — открыта сразу",
            "Хаос / комедия — открыта сразу",
            "Контроль — открыта сразу",
            "Социальная — Сценаристы ур. 2",
            "Исповедь — Сценаристы ур. 2",
            "Управление колодой — Сценаристы ур. 2",
            "Раскрытие / секрет — Сценаристы ур. 3",
            "Спонсор — открыта сразу"
        };

        public static string CategoryName(string category)
        {
            int i = System.Array.IndexOf(Categories, category ?? "");
            if (i < 0)
                return category + " (неизвестная — откроется только на Сценаристах ур. 4)";
            string name = CategoryNames[i];
            int dash = name.IndexOf(" —");
            return dash > 0 ? name.Substring(0, dash) : name;
        }

        public static int CategoryIndex(string category)
        {
            return System.Array.IndexOf(Categories, category ?? "");
        }

        // Русское имя значения enum из [InspectorName].
        public static string Enum(System.Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = field != null ? (InspectorNameAttribute)System.Attribute.GetCustomAttribute(field, typeof(InspectorNameAttribute)) : null;
            return attr != null ? attr.displayName : value.ToString();
        }

        // Что произойдёт при розыгрыше — по шагам.
        public static List<string> Play(EventDefinition c)
        {
            var lines = new List<string>();
            if (c.effects != null && c.effects.Count > 0)
            {
                lines.AddRange(RealityDirector.Cards.CardBrief.What(c));
                foreach (var why in RealityDirector.Cards.CardRuntime.Problems(c))
                    lines.Add("NOT RUNTIME SUPPORTED: " + why);
                return lines;
            }

            if (c.status == CardStatus.Disabled)
                lines.Add("Карта выключена — её нет в игре.");
            if (c.PlayTarget != c.targetType)
                lines.Add("Цель «" + TargetNames[(int)c.targetType] + "» квартира пока не умеет — сейчас карта играется как «" + TargetNames[(int)c.PlayTarget] + "».");
            switch (c.PlayTarget)
            {
                case TargetType.Global:
                    lines.Add("Играется сразу на весь дом — без клика по цели.");
                    break;
                case TargetType.Actor:
                    lines.Add(c.limitTrait
                        ? "Игрок кликает по участнику с чертой «" + TraitName(c.targetTrait) + "» (" + ActorsWith(c.targetTrait) + ")."
                        : "Игрок кликает по любому участнику.");
                    break;
                case TargetType.Object:
                    lines.Add(string.IsNullOrEmpty(c.requiredObjectId)
                        ? "Игрок кликает по любому объекту квартиры."
                        : "Игрок кликает по объекту «" + ObjectName(c.requiredObjectId) + "».");
                    break;
            }

            if (c.ignite)
                lines.Add(c.PlayTarget == TargetType.Object ? "Объект загорается." : "Поджог работает только при цели «объект» — здесь не сработает.");
            if (c.rageSeconds > 0f)
                lines.Add(c.PlayTarget == TargetType.Actor ? "Цель злится " + c.rageSeconds.ToString("0.#") + " сек." : "Злость работает только при цели «участник» — здесь не сработает.");
            if (c.tags != null && c.tags.Count > 0)
                lines.Add("В доме происходит событие с тегами: " + string.Join(", ", c.tags) + ".");
            if (c.moods != null && c.moods.Count > 0)
            {
                var gained = new List<string>();
                for (int i = 0; i < c.moods.Count && i < MoodLimit; i++)
                    gained.Add(MoodStyle.Full(c.moods[i]) + " +" + SeasonTone.CardGain);
                lines.Add("Тон сезона: " + string.Join(", ", gained) + ".");
            }

            if (c.sponsor)
                lines.Add("Спонсор: после сцены +" + c.sponsorPay + " кр, но каждый отзыв зрителей −" + c.sponsorScoreHit + ".");
            if (System.Array.IndexOf(DesignerData.SpecialCardIds, c.id) >= 0)
                lines.Add("Плюс особая механика из кода квартиры (по id «" + c.id + "»).");
            return lines;
        }

        // Кто и как отреагирует на теги карты (правила реакций участников из кода квартиры).
        public static List<string> Forecast(EventDefinition c)
        {
            var lines = new List<string>();
            if (c.tags == null || c.tags.Count == 0)
                return lines;
            LoadRules();
            for (int i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                if (!c.tags.Contains(rule.eventTag))
                    continue;
                string who = ActorsWith(_ruleTraits[i]);
                if (who == "никто из каста")
                    continue;
                who += " (" + TraitName(_ruleTraits[i]) + ")";
                string when = "";
                if (rule.requireTargetSelf)
                    when += " если карту сыграли на него";
                if (rule.requireRage)
                    when += (when.Length > 0 ? " и" : " если") + " он уже злится";
                if (rule.requireTargetSelf && c.PlayTarget != TargetType.Actor)
                    when += " (у этой карты не участник — не сработает)";
                lines.Add(who + " — " + ActionName(rule.action) + (string.IsNullOrEmpty(rule.emote) ? "" : " «" + rule.emote + "»") + when + ".");
            }

            return lines;
        }

        static void LoadRules()
        {
            if (_rules != null)
                return;
            _rules = new List<ReactionRule>();
            _ruleTraits = new List<TraitId>();
            _traitNames = new Dictionary<TraitId, string>();
            var content = PitchContent.Create();
            foreach (var set in content.AllRuleSets())
            {
                if (set == null)
                    continue;
                foreach (var rule in set.rules)
                {
                    // Копия: набор ниже удаляется, а встроенные правила живут только в нём.
                    _rules.Add(JsonUtility.FromJson<ReactionRule>(JsonUtility.ToJson(rule)));
                    _ruleTraits.Add(set.trait);
                }

                var trait = content.TraitOf(set.trait);
                if (trait != null)
                    _traitNames[set.trait] = trait.displayName;
            }

            content.DestroyAssets(true);
        }

        static Dictionary<TraitId, string> _traitNames;

        // Кто из участников (ассеты Characters) с этой главной чертой.
        public static string ActorsWith(TraitId trait)
        {
            var names = new List<string>();
            foreach (var actor in DesignerData.LoadAll<ActorDefinition>())
            {
                if (actor.available && actor.mainTrait == trait)
                    names.Add(actor.displayName);
            }

            return names.Count > 0 ? string.Join(", ", names) : "никто из каста";
        }

        public static string TraitName(TraitId trait)
        {
            LoadRules();
            return _traitNames != null && _traitNames.TryGetValue(trait, out var name) ? name : trait.ToString();
        }

        static string ActionName(NpcActionId action)
        {
            switch (action)
            {
                case NpcActionId.Panic: return "паникует";
                case NpcActionId.SeekFight: return "лезет в драку";
                case NpcActionId.Fight: return "дерётся";
                case NpcActionId.Emote: return "реагирует";
                case NpcActionId.Roam: return "бродит";
                default: return "ничего не делает";
            }
        }

        public static string ObjectName(string id)
        {
            int i = System.Array.IndexOf(DesignerData.ObjectIds, id);
            return i >= 0 ? DesignerData.ObjectNames[i] : id;
        }

        // Карта как в игре: та же раскладка и те же источники, что у CardFace (рамка по категории, арт по карте).
        public static void DrawCard(Rect r, EventDefinition c)
        {
            var style = CardVisuals.StyleOf(c.category);
            Color accent = style != null ? style.accent : Color.white;
            EditorGUI.DrawRect(CardFace.Within(r, CardFace.BackBox), new Color(0.075f, 0.04f, 0.06f, 1f));

            var artRect = CardFace.Within(r, CardFace.ArtBox);
            var art = CardVisuals.Art(c);
            if (art != null)
                DrawSpriteCover(artRect, art);
            else
            {
                EditorGUI.DrawRect(artRect, new Color(accent.r * 0.45f, accent.g * 0.45f, accent.b * 0.45f, 1f));
                if (style != null && style.icon != null)
                    DrawSprite(new Rect(artRect.x + artRect.width * 0.3f, artRect.y + artRect.height * 0.14f, artRect.width * 0.4f, artRect.height * 0.72f), style.icon);
            }

            var plate = CardVisuals.Element("DescriptionBackplate");
            if (plate != null)
                DrawSprite(CardFace.Within(r, CardFace.TextPlateBox), plate, false);
            if (style != null && style.frame != null)
                DrawSprite(r, style.frame, false);

            float k = r.width / 176f;
            var title = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = Mathf.RoundToInt(13 * k) };
            title.normal.textColor = Color.white;
            var body = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = Mathf.RoundToInt(10 * k) };
            body.normal.textColor = new Color(0.95f, 0.9f, 0.84f);
            var small = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = Mathf.RoundToInt(9 * k) };
            small.normal.textColor = new Color(0.75f, 0.66f, 0.66f);
            var badge = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(12 * k) };
            badge.normal.textColor = new Color(0.95f, 0.76f, 0.36f);

            var category = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(10 * k) };
            category.normal.textColor = Color.Lerp(accent, Color.white, 0.25f);
            GUI.Label(CardFace.Within(r, CardFace.CategoryBox), RealityDirector.Cards.CardBrief.CategoryName(c.category), category);
            GUI.Label(CardFace.Within(r, CardFace.TitleBox), string.IsNullOrEmpty(c.displayName) ? "(без названия)" : c.displayName.ToUpperInvariant(), title);
            var cost = CardFace.Within(r, CardFace.CostBox);
            var costBadge = CardVisuals.Element("CostBadge");
            if (costBadge != null)
                DrawSprite(cost, costBadge, false);
            GUI.Label(cost, HellToken.Format(c.cost), badge);
            if (c.diceEffects != null && c.diceEffects.Count > 0 && c.diceEffects[0] != null)
            {
                var dice = CardFace.Within(r, CardFace.DiceBox);
                var diceBadge = CardVisuals.Element("DiceBadge");
                if (diceBadge != null)
                    DrawSprite(dice, diceBadge, false);
                var dieStyle = new GUIStyle(badge);
                dieStyle.normal.textColor = new Color(0.78f, 0.86f, 1f);
                GUI.Label(dice, Dice.Notation(c.diceEffects[0]), dieStyle);
            }

            GUI.Label(CardFace.Within(r, CardFace.TextBox), CardVisuals.Text(c), body);
            GUI.Label(CardFace.Within(r, CardFace.HintBox), c.hint, small);

            if (art == null)
            {
                var warn = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.LowerCenter, wordWrap = true };
                warn.normal.textColor = new Color(1f, 0.6f, 0.4f);
                GUI.Label(artRect, "нет арта: поле «Арт» или " + CardVisuals.ArtFolder + c.id + ".png", warn);
            }
        }

        // Арт заполняет окно без растяжения: лишнее по краям срезается (как в игре).
        public static void DrawSpriteCover(Rect r, Sprite sprite)
        {
            var tex = sprite.texture;
            var t = sprite.textureRect;
            float aspect = t.width / t.height;
            float target = r.width / r.height;
            var uv = new Rect(t.x / tex.width, t.y / tex.height, t.width / tex.width, t.height / tex.height);
            if (aspect > target)
            {
                float w = uv.width * target / aspect;
                uv.x += (uv.width - w) * 0.5f;
                uv.width = w;
            }
            else
            {
                float h = uv.height * aspect / target;
                uv.y += (uv.height - h) * 0.5f;
                uv.height = h;
            }

            GUI.DrawTextureWithTexCoords(r, tex, uv, true);
        }

        public static void DrawSprite(Rect r, Sprite sprite, bool keepAspect)
        {
            if (keepAspect)
            {
                DrawSprite(r, sprite);
                return;
            }

            var tex = sprite.texture;
            var t = sprite.textureRect;
            GUI.DrawTextureWithTexCoords(r, tex, new Rect(t.x / tex.width, t.y / tex.height, t.width / tex.width, t.height / tex.height), true);
        }

        public static void DrawSprite(Rect r, Sprite sprite)
        {
            var tex = sprite.texture;
            var t = sprite.textureRect;
            var uv = new Rect(t.x / tex.width, t.y / tex.height, t.width / tex.width, t.height / tex.height);
            float aspect = t.width / t.height;
            var fit = r;
            if (r.width / r.height > aspect)
                fit = new Rect(r.center.x - r.height * aspect * 0.5f, r.y, r.height * aspect, r.height);
            else
                fit = new Rect(r.x, r.center.y - r.width / aspect * 0.5f, r.width, r.width / aspect);
            GUI.DrawTextureWithTexCoords(fit, tex, uv, true);
        }
    }
}
