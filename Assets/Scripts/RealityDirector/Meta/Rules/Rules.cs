using System;
using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Всё, что видят условия и меняют эффекты.
    public class RuleContext
    {
        public SeasonState season;
        public EpisodeState episode;
        public SeasonTone tone;

        public RuleContext(SeasonState season, EpisodeState episode, SeasonTone tone)
        {
            this.season = season;
            this.episode = episode;
            this.tone = tone;
        }
    }

    // Проверка условий и применение эффектов. Общие для комнат, событий, офферов и монтажа.
    public static class Rules
    {
        public static bool Check(IList<Condition> list, RuleContext ctx)
        {
            return FirstFailed(list, ctx) == null;
        }

        public static Condition FirstFailed(IList<Condition> list, RuleContext ctx)
        {
            if (list == null)
                return null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && !Check(list[i], ctx))
                    return list[i];
            }

            return null;
        }

        public static bool Check(Condition c, RuleContext ctx)
        {
            bool ok = Raw(c, ctx);
            return c.not ? !ok : ok;
        }

        static bool Raw(Condition c, RuleContext ctx)
        {
            var s = ctx.season;
            var e = ctx.episode;
            int episodeNumber = e != null ? e.Number : (s != null ? s.episodeIndex + 1 : 1);
            switch (c.type)
            {
                case ConditionType.EpisodeAtLeast: return episodeNumber >= c.value;
                case ConditionType.EpisodeAtMost: return episodeNumber <= c.value;
                case ConditionType.BudgetAtLeast: return s != null && s.money >= c.value;
                case ConditionType.CastAtLeast: return e != null && e.cast.Count >= c.value;
                case ConditionType.CastAtMost: return e == null || e.cast.Count <= c.value;
                case ConditionType.CastHasActor: return e != null && e.cast.Contains(c.key);
                case ConditionType.EpisodeFlag: return e != null && e.HasFlag(c.key);
                case ConditionType.SeasonFlag: return s != null && s.HasFlag(c.key);
                case ConditionType.NarrativeTag: return e != null && e.HasTag(c.key);
                case ConditionType.RoomVisited: return e != null && e.Visited(c.key);
                case ConditionType.CrewLevelAtLeast: return s != null && s.Level(Track(c.key)) >= c.value;
                case ConditionType.ContractActive: return e != null && e.ContractActive(c.key);
                case ConditionType.ToneAtLeast: return ctx.tone != null && ctx.tone.Get(c.mood) >= c.value;
                case ConditionType.CashAtLeast: return e != null && e.cash >= c.value;
                default: return true;
            }
        }

        public static string FailText(Condition c)
        {
            if (c == null)
                return null;
            if (!string.IsNullOrEmpty(c.failText))
                return c.failText;
            switch (c.type)
            {
                case ConditionType.EpisodeAtLeast: return c.not ? "Только до выпуска " + c.value : "С выпуска " + c.value;
                case ConditionType.EpisodeAtMost: return c.not ? "Только после выпуска " + c.value : "До выпуска " + c.value;
                case ConditionType.BudgetAtLeast: return "Нужен бюджет " + c.value + " кр";
                case ConditionType.CastAtLeast: return "Нужно участников: " + c.value;
                case ConditionType.CastAtMost: return "Участников не больше " + c.value;
                case ConditionType.CastHasActor: return (c.not ? "Без участника " : "Нужен участник ") + c.key;
                case ConditionType.CrewLevelAtLeast: return "Нужно: " + TrackName(Track(c.key)) + " ур. " + c.value;
                case ConditionType.ToneAtLeast: return "Нужно: " + MoodStyle.Short(c.mood) + " " + c.value;
                case ConditionType.CashAtLeast: return "Нужно " + c.value + " нал";
                default: return "Условие не выполнено";
            }
        }

        public static void Apply(IList<Effect> list, RuleContext ctx)
        {
            if (list == null)
                return;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                    Apply(list[i], ctx);
            }
        }

        public static void Apply(Effect fx, RuleContext ctx)
        {
            var s = ctx.season;
            var e = ctx.episode;
            switch (fx.type)
            {
                case EffectType.Budget:
                    if (s != null)
                        s.money = Mathf.Max(0, s.money + fx.value);
                    break;
                case EffectType.Tone:
                    if (ctx.tone != null && fx.value > 0)
                        ctx.tone.Add(fx.mood, fx.value);
                    break;
                case EffectType.SetEpisodeFlag: e?.SetFlag(fx.key); break;
                case EffectType.ClearEpisodeFlag: e?.flags.Remove(fx.key); break;
                case EffectType.SetSeasonFlag: s?.SetFlag(fx.key); break;
                case EffectType.ClearSeasonFlag: s?.flags.Remove(fx.key); break;
                case EffectType.AddNarrativeTag: e?.AddTag(fx.key); break;
                case EffectType.AddTempCard:
                    if (e != null && !string.IsNullOrEmpty(fx.key))
                        e.tempCards.Add(fx.key);
                    break;
                case EffectType.RemoveTempCard: e?.tempCards.Remove(fx.key); break;
                case EffectType.NextRoomModifier: e?.AddModifier(e.nextRoomModifiers, fx.key, fx.value); break;
                case EffectType.BroadcastModifier: e?.AddModifier(e.broadcastModifiers, fx.key, fx.value); break;
                case EffectType.Cash:
                    if (e != null)
                        e.cash = Mathf.Max(0, e.cash + fx.value);
                    break;
                case EffectType.AddDeckCard:
                    if (s != null && !string.IsNullOrEmpty(fx.key) && !s.owned.Contains(fx.key))
                        s.owned.Add(fx.key);
                    if (s != null)
                        s.played.Remove(fx.key);
                    break;
                case EffectType.RemoveDeckCard:
                    if (s != null)
                    {
                        s.owned.Remove(fx.key);
                        s.picked.Remove(fx.key);
                    }

                    break;
            }
        }

        // Видимая игроку часть эффектов (бюджет, тон). Флаги и теги — скрытые последствия.
        public static string Describe(IList<Effect> list)
        {
            return Describe(list, null);
        }

        // Куда сдвинет выбор — до выбора, коротко: «+80 кр · Трэш ↑ · стресс каста ↑». Без скрытых формул,
        // но направление видно у каждого эффекта. Флаги и теги — одной строкой «последствия позже».
        public static string Preview(IList<Effect> list, Func<string, string> cardName)
        {
            if (list == null)
                return "";
            var parts = new List<string>();
            bool later = false;
            foreach (var fx in list)
            {
                if (fx == null)
                    continue;
                string card = cardName != null ? cardName(fx.key) : fx.key;
                switch (fx.type)
                {
                    case EffectType.Budget:
                        if (fx.value != 0)
                            parts.Add((fx.value > 0 ? "+" : "") + fx.value + " кр");
                        break;
                    case EffectType.Cash:
                        if (fx.value != 0)
                            parts.Add((fx.value > 0 ? "+" : "") + fx.value + " нал");
                        break;
                    case EffectType.Tone:
                        if (fx.value != 0)
                            parts.Add(MoodStyle.Paint(MoodStyle.Short(fx.mood) + (fx.value > 0 ? (fx.value >= 5 ? " ↑↑" : " ↑") : " ↓"), fx.mood));
                        break;
                    case EffectType.AddTempCard:
                        parts.Add("карта «" + card + "»");
                        break;
                    case EffectType.AddDeckCard:
                        parts.Add("в колоду «" + card + "»");
                        break;
                    case EffectType.RemoveTempCard:
                    case EffectType.RemoveDeckCard:
                        parts.Add("минус карта «" + card + "»");
                        break;
                    case EffectType.NextRoomModifier:
                        if (fx.value != 0)
                            parts.Add(ModifierName(fx.key) + (fx.value > 0 ? " ↑" : " ↓") + " в след. съёмке");
                        break;
                    case EffectType.SetEpisodeFlag:
                    case EffectType.SetSeasonFlag:
                    case EffectType.BroadcastModifier:
                        later = true;
                        break;
                }
            }

            if (later)
                parts.Add("последствия позже");
            return string.Join("  ·  ", parts);
        }

        static string ModifierName(string key)
        {
            switch ((key ?? "").ToLowerInvariant())
            {
                case "stress": return "стресс каста";
                case "anger": return "злость каста";
                case "sadness": return "грусть каста";
                case "hostility": return "вражда в касте";
                default: return key;
            }
        }

        // Видимая игроку часть эффектов. cardName — имя карты по id (null — показать id).
        public static string Describe(IList<Effect> list, Func<string, string> cardName)
        {
            if (list == null)
                return "";
            var parts = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                var fx = list[i];
                if (fx == null)
                    continue;
                string card = cardName != null ? cardName(fx.key) : fx.key;
                switch (fx.type)
                {
                    case EffectType.Budget:
                        if (fx.value != 0)
                            parts.Add("бюджет " + (fx.value > 0 ? "+" : "") + fx.value + " кр");
                        break;
                    case EffectType.Cash:
                        if (fx.value != 0)
                            parts.Add((fx.value > 0 ? "+" : "") + fx.value + " нал");
                        break;
                    case EffectType.Tone:
                        if (fx.value != 0)
                            parts.Add(MoodStyle.Paint(MoodStyle.Short(fx.mood) + (fx.value > 0 ? " +" : " ") + fx.value, fx.mood));
                        break;
                    case EffectType.NextRoomModifier:
                        if (fx.value != 0)
                            parts.Add(ModifierName(fx.key) + (fx.value > 0 ? " +" : " ") + fx.value + " в следующей съёмке");
                        break;
                    case EffectType.AddTempCard:
                        parts.Add("карта «" + card + "» до эфира");
                        break;
                    case EffectType.RemoveTempCard:
                        parts.Add("пропала карта «" + card + "»");
                        break;
                    case EffectType.AddDeckCard:
                        parts.Add("в колоду: «" + card + "»");
                        break;
                    case EffectType.RemoveDeckCard:
                        parts.Add("из колоды: «" + card + "»");
                        break;
                }
            }

            return string.Join("   ·   ", parts);
        }

        public static CrewTrack Track(string key)
        {
            return Enum.TryParse(key, true, out CrewTrack track) ? track : CrewTrack.Cast;
        }

        public static string TrackName(CrewTrack track)
        {
            switch (track)
            {
                case CrewTrack.Cast: return "Кастинг";
                case CrewTrack.Operators: return "Съёмочная";
                default: return "Сценарная";
            }
        }
    }
}
