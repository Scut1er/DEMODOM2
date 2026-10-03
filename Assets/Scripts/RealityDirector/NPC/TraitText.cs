using System.Collections.Generic;

namespace RealityDirector.NPC
{
    // Как черта выглядит для игрока: короткое описание и 2–3 склонности.
    // Берётся из TraitDefinition (shortDescription, gameplayHints); если дизайнер не заполнил —
    // собирается из самих данных черты: чувствительность, склонности и правила реакций (ReactionRuleSet).
    public static class TraitText
    {
        public static string Description(TraitDefinition trait)
        {
            if (trait == null)
                return "";
            if (!string.IsNullOrEmpty(trait.shortDescription))
                return trait.shortDescription;
            return Fallback(trait.traitId);
        }

        public static List<string> Hints(TraitDefinition trait, ReactionRuleSet rules, int max = 3)
        {
            var list = new List<string>();
            if (trait != null && trait.gameplayHints != null)
            {
                foreach (var h in trait.gameplayHints)
                {
                    if (!string.IsNullOrEmpty(h) && list.Count < max)
                        list.Add(h);
                }
            }

            if (list.Count > 0)
                return list;

            // Из данных черты.
            if (trait != null)
            {
                Gain(list, trait.angerGain, "злость");
                Gain(list, trait.stressGain, "стресс");
                Gain(list, trait.sadnessGain, "грусть");
                Gain(list, trait.attractionGain, "влечение");
            }

            if (rules != null && rules.rules != null)
            {
                var sorted = new List<ReactionRule>(rules.rules);
                sorted.Sort((a, b) => b.priority.CompareTo(a.priority));
                foreach (var r in sorted)
                {
                    if (list.Count >= max)
                        break;
                    if (r == null || string.IsNullOrEmpty(r.eventTag))
                        continue;
                    string line = (r.othersOnly ? "чужое «" : r.requireTargetSelf ? "«" : "«") + Cards.CardBrief.TagName(r.eventTag)
                                  + (r.requireTargetSelf ? "» на него → " : "» → ") + Outcome(r);
                    if (!list.Contains(line))
                        list.Add(line);
                }
            }

            if (trait != null && list.Count < max)
            {
                if (trait.fightProne)
                    list.Add("легко лезет в драку");
                else if (trait.panicProne)
                    list.Add("легко впадает в панику");
            }

            if (list.Count > max)
                list.RemoveRange(max, list.Count - max);
            return list;
        }

        static void Gain(List<string> list, float gain, string stat)
        {
            if (gain >= 1.15f)
                list.Add(stat + " растёт быстрее");
            else if (gain <= 0.85f)
                list.Add(stat + " растёт медленнее");
        }

        static string Outcome(ReactionRule r)
        {
            switch (r.action)
            {
                case NpcActionId.SeekFight: return "драка";
                case NpcActionId.Panic: return "паника";
            }

            int best = System.Math.Max(System.Math.Max(r.anger, r.stress), System.Math.Max(r.sadness, r.attraction));
            if (best <= 0)
                return "реагирует";
            if (best == r.anger)
                return "злость";
            if (best == r.stress)
                return "стресс";
            if (best == r.sadness)
                return "грусть";
            return "влечение";
        }

        static string Fallback(TraitId id)
        {
            switch (id)
            {
                case TraitId.Aggressive: return "Вспыхивает быстрее всех и первым лезет в драку.";
                case TraitId.Sentimental: return "Всё принимает близко к сердцу — легко плачет.";
                case TraitId.Timid: return "Боится шума и конфликтов, теряется на людях.";
                case TraitId.Panicker: return "Чуть что — паника: бегает, кричит, мешает всем.";
                case TraitId.Jealous: return "Ревнует: чужая романтика бесит.";
                case TraitId.Cowardly: return "Избегает опасности любой ценой.";
                case TraitId.Vain: return "Любит камеру и внимание к себе.";
                case TraitId.Opportunist: return "Ищет выгоду в любой ситуации.";
                case TraitId.Honest: return "Не умеет врать — правда вылезает сама.";
                case TraitId.Shy: return "Стесняется камер и чужих людей.";
                default: return "Непредсказуем — делает то, чего никто не ждёт.";
            }
        }
    }
}
