using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Вариант события, готовый к показу.
    public class EventChoiceModel
    {
        public int index;
        public string label;
        public string description;
        public string costLabel;
        public string chanceLabel;
        public bool available;
        public string reason;
        // Что будет: при успехе и при провале (направление каждого эффекта), шанс.
        public string preview;
        public string failPreview;
        public int chance = 100;
    }

    public class EventOutcome
    {
        public bool success;
        public string text;
        public string summary;
    }

    // Логика события без UI: роли → имена, доступность вариантов, розыгрыш выбора.
    public static class EventResolver
    {
#if UNITY_EDITOR
        // «Проверить» из мастерской событий: хаб откроет это событие сразу при запуске.
        public const string TestEventPref = "RealityDirector.TestEventId";

        public static string TakeTestEvent()
        {
            string id = UnityEditor.EditorPrefs.GetString(TestEventPref, "");
            if (!string.IsNullOrEmpty(id))
                UnityEditor.EditorPrefs.DeleteKey(TestEventPref);
            return id;
        }
#endif

        // Роль → id участника. Один и тот же узел карты всегда даёт тех же людей (seed выпуска + узел).
        public static Dictionary<string, string> CastRoles(EventRoomDefinition def, EpisodeState episode, string nodeId)
        {
            var roles = new Dictionary<string, string>();
            if (def == null || def.roles == null)
                return roles;
            var cast = episode != null ? new List<string>(episode.cast) : new List<string>();
            int seed = (episode != null ? episode.mapSeed : 0) ^ StableHash(nodeId ?? def.Id);
            var rng = new System.Random(seed);
            var free = new List<string>(cast);
            foreach (var role in def.roles)
            {
                if (role == null || string.IsNullOrEmpty(role.key) || roles.ContainsKey(role.key))
                    continue;
                string actor = null;
                if (!string.IsNullOrEmpty(role.actorId))
                    actor = role.actorId;
                else if (free.Count > 0)
                    actor = free[rng.Next(free.Count)];
                else if (cast.Count > 0)
                    actor = cast[rng.Next(cast.Count)];
                if (actor != null)
                {
                    free.Remove(actor);
                    roles[role.key] = actor;
                }
            }

            return roles;
        }

        // {роль} → имя. Неизвестная роль остаётся как есть — её видно и в игре, и в проверке редактора.
        public static string Fill(string text, Dictionary<string, string> roles, Func<string, string> nameOf)
        {
            if (string.IsNullOrEmpty(text) || roles == null || roles.Count == 0)
                return text ?? "";
            var sb = new StringBuilder(text);
            foreach (var kv in roles)
                sb.Replace("{" + kv.Key + "}", nameOf != null ? nameOf(kv.Value) : kv.Value);
            return sb.ToString();
        }

        public static List<EventChoiceModel> Choices(EventRoomDefinition def, RuleContext ctx, Dictionary<string, string> roles, Func<string, string> nameOf,
            Func<string, string> cardName = null)
        {
            var list = new List<EventChoiceModel>();
            if (def == null || def.choices == null)
                return list;
            for (int i = 0; i < def.choices.Count; i++)
            {
                var c = def.choices[i];
                if (c == null)
                    continue;
                string reason = Blocker(c, ctx);
                if (reason != null && c.hideIfUnavailable)
                    continue;
                list.Add(new EventChoiceModel
                {
                    index = i,
                    label = Fill(c.label, roles, nameOf),
                    description = Fill(c.description, roles, nameOf),
                    costLabel = CostLabel(c),
                    chanceLabel = c.chance < 100 ? "шанс " + Mathf.Clamp(c.chance, 1, 100) + "%" : "",
                    available = reason == null,
                    reason = reason,
                    preview = Rules.Preview(c.effects, cardName),
                    failPreview = c.chance < 100 ? Rules.Preview(c.failEffects, cardName) : "",
                    chance = Mathf.Clamp(c.chance, 1, 100)
                });
            }

            return list;
        }

        // Причина, по которой вариант закрыт. null — доступен.
        public static string Blocker(EventChoice c, RuleContext ctx)
        {
            var failed = Rules.FirstFailed(c.conditions, ctx);
            if (failed != null)
                return Rules.FailText(failed);
            if (c.costMoney > 0 && (ctx.season == null || ctx.season.money < c.costMoney))
                return "Нужно " + c.costMoney + " кр";
            if (c.costCash > 0 && (ctx.episode == null || ctx.episode.cash < c.costCash))
                return "Нужно " + c.costCash + " нал";
            return null;
        }

        public static string CostLabel(EventChoice c)
        {
            var parts = new List<string>();
            if (c.costMoney > 0)
                parts.Add("−" + c.costMoney + " кр");
            if (c.costCash > 0)
                parts.Add("−" + c.costCash + " нал");
            return string.Join("  ", parts);
        }

        // Платим цену, бросаем шанс, применяем эффекты и теги. rng — для теста можно подать свой.
        public static EventOutcome Resolve(EventChoice c, RuleContext ctx, Dictionary<string, string> roles, Func<string, string> nameOf,
            Func<string, string> cardName, System.Random rng = null)
        {
            if (ctx.season != null && c.costMoney > 0)
                ctx.season.money = Mathf.Max(0, ctx.season.money - c.costMoney);
            if (ctx.episode != null && c.costCash > 0)
                ctx.episode.cash = Mathf.Max(0, ctx.episode.cash - c.costCash);

            int chance = Mathf.Clamp(c.chance, 1, 100);
            int roll = rng != null ? rng.Next(100) : UnityEngine.Random.Range(0, 100);
            bool success = chance >= 100 || roll < chance;
            if (!success && ctx.episode != null && ctx.episode.HasFlag("EventReroll"))
            {
                ctx.episode.flags.Remove("EventReroll");
                roll = rng != null ? rng.Next(100) : UnityEngine.Random.Range(0, 100);
                success = roll < chance;
            }

            var effects = success ? c.effects : c.failEffects;
            var tags = success ? c.resultTags : c.failTags;
            Rules.Apply(effects, ctx);
            if (ctx.episode != null && tags != null)
            {
                foreach (var tag in tags)
                    ctx.episode.AddTag(tag);
            }

            string text = success ? c.resultText : (string.IsNullOrEmpty(c.failText) ? c.resultText : c.failText);
            if (string.IsNullOrEmpty(text))
                text = success ? "Сработало так, как вы хотели." : "Не вышло — и это заметили.";
            var summary = new List<string>();
            string cost = CostLabel(c);
            if (cost.Length > 0)
                summary.Add(cost);
            string fx = Rules.Describe(effects, cardName);
            if (fx.Length > 0)
                summary.Add(fx);
            if (summary.Count == 0)
                summary.Add(HasHidden(effects) ? "видимых изменений нет — последствия скажутся позже" : "ничего не изменилось");
            return new EventOutcome
            {
                success = success,
                text = Fill(text, roles, nameOf),
                summary = string.Join("   ·   ", summary)
            };
        }

        static bool HasHidden(IList<Effect> effects)
        {
            if (effects == null)
                return false;
            foreach (var e in effects)
            {
                if (e != null && (e.type == EffectType.SetEpisodeFlag || e.type == EffectType.SetSeasonFlag || e.type == EffectType.BroadcastModifier || e.type == EffectType.AddNarrativeTag))
                    return true;
            }

            return false;
        }

        // Вход в событие: сюжетные теги события.
        public static void Enter(EventRoomDefinition def, RuleContext ctx)
        {
            if (def == null || ctx.episode == null || def.eventTags == null)
                return;
            foreach (var tag in def.eventTags)
                ctx.episode.AddTag(tag);
        }

        static int StableHash(string s)
        {
            unchecked
            {
                int h = 23;
                if (s != null)
                {
                    foreach (char ch in s)
                        h = h * 31 + ch;
                }

                return h;
            }
        }
    }
}
