using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Events;
using RealityDirector.Meta;
using RealityDirector.NPC;
using RealityDirector.Util;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Карта человеческим языком: что произойдёт при розыгрыше, где её взять, кто как отреагирует.
    public static class CardInsight
    {
        public static readonly string[] TargetNames = { "Объект в квартире", "Участник", "Весь дом сразу" };

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
                string line = Enum(d.stat) + " +" + Mathf.Max(1, d.count) + "d" + Dice.Faces(d.die) + (d.bonus != 0 ? (d.bonus > 0 ? " +" : " ") + d.bonus : "");
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
            return "$" + c.hellTokenCost.ToString("0.00") + "  ·  " + CategoryName(c.category) + "  ·  Tier " + c.tier + "  ·  " + Enum(c.rarity) + "  ·  " + Enum(c.status);
        }

        // Категории карт — те, что понимает игра (Progression.CategoryOpen): по ним карты открываются уровнем Сценаристов.
        public static readonly string[] Categories = { "", "Provocation", "Environment", "Comedy", "Social", "Confession", "Reveal", "Sponsor" };
        public static readonly string[] CategoryNames =
        {
            "Без категории — открыта сразу",
            "Провокация — открыта сразу",
            "Окружение — открыта сразу",
            "Комедия — открыта сразу",
            "Социальная — Сценаристы ур. 2",
            "Исповедь — Сценаристы ур. 2",
            "Раскрытие — Сценаристы ур. 3",
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
            if (c.status == CardStatus.Disabled)
                lines.Add("Карта выключена — её нет в игре.");
            switch (c.targetType)
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
                lines.Add(c.targetType == TargetType.Object ? "Объект загорается." : "Поджог работает только при цели «объект» — здесь не сработает.");
            if (c.rageSeconds > 0f)
                lines.Add(c.targetType == TargetType.Actor ? "Цель злится " + c.rageSeconds.ToString("0.#") + " сек." : "Злость работает только при цели «участник» — здесь не сработает.");
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
                string when = "";
                if (rule.requireTargetSelf)
                    when += " если карту сыграли на него";
                if (rule.requireRage)
                    when += (when.Length > 0 ? " и" : " если") + " он уже злится";
                if (rule.requireTargetSelf && c.targetType != TargetType.Actor)
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
            var content = PitchContent.Create();
            foreach (var set in new[] { content.AggressiveRules, content.SentimentalRules, content.PanickerRules })
            {
                if (set == null)
                    continue;
                foreach (var rule in set.rules)
                {
                    _rules.Add(rule);
                    _ruleTraits.Add(set.trait);
                }
            }

            foreach (var card in content.All)
                Object.DestroyImmediate(card);
            Object.DestroyImmediate(content.Aggressive);
            Object.DestroyImmediate(content.Sentimental);
            Object.DestroyImmediate(content.Panicker);
            Object.DestroyImmediate(content.AggressiveRules);
            Object.DestroyImmediate(content.SentimentalRules);
            Object.DestroyImmediate(content.PanickerRules);
        }

        public static string ActorsWith(TraitId trait)
        {
            // Черты участникам квартиры задаёт код квартиры: агрессивный — npc_zloi, сентиментальный — npc_dobryak.
            string id = trait == TraitId.Aggressive ? "npc_zloi" : trait == TraitId.Sentimental ? "npc_dobryak" : null;
            if (id == null)
                return "никто из каста";
            foreach (var actor in DesignerData.LoadAll<ActorDefinition>())
            {
                if (actor.Id == id)
                    return actor.displayName;
            }

            return trait == TraitId.Aggressive ? "Злой" : "Добряк";
        }

        public static string TraitName(TraitId trait)
        {
            switch (trait)
            {
                case TraitId.Aggressive: return "агрессивный";
                case TraitId.Sentimental: return "сентиментальный";
                default: return "робкий";
            }
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

        // Карта как в игре: рамка по первому тону, арт, название, подсказка, значки тона и цены.
        public static void DrawCard(Rect r, EventDefinition c)
        {
            var mood = c.moods != null && c.moods.Count > 0 ? c.moods[0] : ShowMood.Trash;
            var frame = GameArt.CardFrame(mood);
            if (frame != null)
            {
                DrawSprite(r, frame);
                var inner = new Rect(r.x + r.width * 0.12f, r.y + r.height * 0.1f, r.width * 0.76f, r.height * 0.8f);
                EditorGUI.DrawRect(inner, new Color(0.12f, 0.1f, 0.13f, 0.92f));
            }
            else
            {
                EditorGUI.DrawRect(r, c.cardColor);
            }

            var title = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = 13 };
            title.normal.textColor = Color.white;
            var small = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            small.normal.textColor = new Color(0.9f, 0.86f, 0.8f);

            GUI.Label(new Rect(r.x + 10, r.y + r.height * 0.12f, r.width - 20, 34), string.IsNullOrEmpty(c.displayName) ? "(без названия)" : c.displayName, title);
            var artRect = new Rect(r.center.x - r.width * 0.25f, r.y + r.height * 0.3f, r.width * 0.5f, r.width * 0.5f);
            EditorGUI.DrawRect(artRect, c.cardColor);
            if (c.cardArt != null)
                DrawSprite(artRect, c.cardArt);
            else
                GUI.Label(artRect, "встроенная\nиконка", small);
            GUI.Label(new Rect(r.x + 12, artRect.yMax + 4, r.width - 24, 34), c.hint, small);

            float y = r.yMax - r.height * 0.17f;
            float x = r.x + r.width * 0.2f;
            for (int i = 0; c.moods != null && i < c.moods.Count && i < MoodLimit; i++)
            {
                var dot = new Rect(x + i * 22f, y, 16f, 16f);
                EditorGUI.DrawRect(dot, MoodStyle.ColorOf(c.moods[i]));
                GUI.Label(dot, MoodStyle.Short(c.moods[i]).Substring(0, 1), small);
            }

            var badge = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
            badge.normal.textColor = c.sponsor ? new Color(1f, 0.75f, 0.3f) : Color.white;
            string price = c.sponsor ? "+" + c.sponsorPay + " кр" : c.runPrice > 0 ? c.runPrice + " нал" : c.price > 0 ? c.price + " кр" : c.starter ? "старт" : "";
            GUI.Label(new Rect(r.x, y - 1, r.width * 0.8f, 18f), price, badge);
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
