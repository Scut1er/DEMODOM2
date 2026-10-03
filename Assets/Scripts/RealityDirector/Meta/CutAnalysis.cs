using System.Collections.Generic;
using RealityDirector.Capture;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Роль кадра в истории выпуска — выводится из тегов и реплик футажа, а не задаётся руками.
    public enum NarrativeRole
    {
        Setup,
        Escalation,
        Reveal,
        Reaction,
        Conflict,
        Romance,
        Comedy,
        Payoff,
        Climax,
        Aftermath
    }

    // Что зритель увидит в одном кадре: кто, что случилось, роль в истории, сила, качество, тон.
    public class ClipFacts
    {
        public FootageClip clip;
        public readonly List<string> actors = new List<string>();
        public readonly HashSet<string> tags = new HashSet<string>();
        public readonly List<NarrativeRole> roles = new List<NarrativeRole>();
        public NarrativeRole main;
        public int intensity;
        public int quality;
        public int drama;
        public int trash;
        public int family;
        public string what;
        public readonly List<string> why = new List<string>();
        public bool sponsor;
        public bool blank;
    }

    // Связь соседних кадров A → B: очки, уровень (2 — хорошая, 1 — слабая, 0 — резкий переход) и причины.
    public class CutLink
    {
        public int score;
        public int level;
        public readonly List<string> reasons = new List<string>();
    }

    public class CutCombo
    {
        public string id;
        public string title;
        public string effect;
    }

    public class CutReport
    {
        public readonly List<ClipFacts> clips = new List<ClipFacts>();
        public readonly List<CutLink> links = new List<CutLink>();
        public readonly List<string> plus = new List<string>();
        public readonly List<string> minus = new List<string>();
        public readonly List<CutCombo> combos = new List<CutCombo>();
        public int coherence;
        public int drama;
        public int trash;
        public int family;
        public string story;
        public int potential;
        public int emotion;
        public int repetition;
        public float ratingBonus;
        public bool sponsor;
        public readonly List<string> actors = new List<string>();
        public string topActor;
        public int topActorCount;
        public bool backwards;

        public bool Has(string combo)
        {
            return combos.Exists(c => c.id == combo);
        }
    }

    // Монтаж как игра в комбинаторику: отдельный хороший кадр ≠ хороший выпуск.
    // Оценивает каждый кадр, связь соседей, повторы, комбо и то, как это увидит аудитория.
    // Всё — по тегам, ролям и людям в кадре, без привязки к конкретным картам и сценам.
    public static class CutAnalysis
    {
        static readonly HashSet<string> Strong = new HashSet<string> { MomentTags.Fight, MomentTags.Slap, MomentTags.Fire };
        static readonly HashSet<string> Medium = new HashSet<string>
        {
            MomentTags.Crying, "Reveal", "Secret", "SecretOut", "Confession", "Betrayal", "Humiliation", "Panic", "Argument",
            MomentTags.Conflict, "Flirt", MomentTags.Hug, "Jealousy", "Theft", "Reunion", "Triangle"
        };

        // Фон, а не тема: по ним связь «общая тема» не считается.
        static readonly HashSet<string> Background = new HashSet<string>
        {
            "Public", "Private", "Pressure", MomentTags.Warmth, MomentTags.Misery, MomentTags.Sponsor, "Music", "Party", "Witness", "Attention"
        };

        public static ClipFacts Facts(FootageClip clip)
        {
            var f = new ClipFacts { clip = clip };
            if (clip == null)
                return f;
            if (clip.actorNames != null)
            {
                foreach (var a in clip.actorNames)
                {
                    if (!string.IsNullOrEmpty(a) && !f.actors.Contains(a))
                        f.actors.Add(a);
                }
            }

            if (clip.tags != null)
            {
                foreach (var t in clip.tags)
                {
                    if (!string.IsNullOrEmpty(t))
                        f.tags.Add(t);
                }
            }

            bool qualityBonus = false;
            bool qualityPenalty = false;
            if (clip.cues != null)
            {
                foreach (var cue in clip.cues)
                {
                    int bar = cue != null ? cue.IndexOf('|') : -1;
                    if (bar <= 0)
                        continue;
                    string tag = cue.Substring(0, bar);
                    if (tag == "Quality")
                    {
                        // «Quality|+1» — «нужный момент», «Quality|-1» — плохой звук (события выпуска).
                        if (cue.Substring(bar + 1).StartsWith("-"))
                            qualityPenalty = true;
                        else
                            qualityBonus = true;
                    }
                    else
                        f.tags.Add(tag);
                }
            }

            if (clip.exposed != NPC.HiddenTrait.None)
                f.tags.Add("SecretOut");
            f.blank = clip.grade == CaptureGrade.Blank;
            f.sponsor = f.tags.Contains(MomentTags.Sponsor);

            // Сила 0–5: пустой кадр — 0; люди в кадре — 1; сильные события +2, средние +1; двое и больше +1.
            if (!f.blank)
            {
                int k = clip.grade == CaptureGrade.Cast ? 1 : 0;
                bool strong = false;
                int medium = 0;
                foreach (var t in f.tags)
                {
                    if (Strong.Contains(t))
                        strong = true;
                    else if (Medium.Contains(t))
                        medium++;
                }

                k += strong ? 2 : 0;
                k += Mathf.Min(2, medium);
                if (f.actors.Count >= 2)
                    k++;
                f.intensity = Mathf.Clamp(k, 1, 5);
            }

            f.quality = f.blank ? 0 : (f.intensity >= 4 || qualityBonus) ? 2 : 1;
            if (qualityPenalty && !f.blank)
                f.quality = Mathf.Max(0, f.quality - 1);
            Roles(f);
            Tone(f);
            f.what = !string.IsNullOrEmpty(clip.title) && clip.title != "КАДР" ? Cap(clip.title) : Cap(RoleWord(f.main));
            Why(f, qualityBonus);
            if (qualityPenalty && !f.blank)
                f.why.Add("техника: плохой звук — качество ниже");
            return f;
        }

        static bool Any(ClipFacts f, params string[] tags)
        {
            foreach (var t in tags)
            {
                if (f.tags.Contains(t))
                    return true;
            }

            return false;
        }

        static void Roles(ClipFacts f)
        {
            var r = f.roles;
            if (f.blank)
            {
                f.main = NarrativeRole.Setup;
                return;
            }

            bool conflict = Any(f, MomentTags.Conflict, "Argument", MomentTags.Fight, MomentTags.Slap, "Theft", "Blame");
            if (Any(f, MomentTags.Fight, MomentTags.Slap, MomentTags.Fire) || f.intensity >= 4)
                r.Add(NarrativeRole.Climax);
            if (Any(f, "Reveal", "Secret", "SecretOut", "Confession", "Betrayal", "Authentic"))
                r.Add(NarrativeRole.Reveal);
            if (conflict)
                r.Add(NarrativeRole.Conflict);
            if (Any(f, "Flirt", "Romance", MomentTags.Hug, "Reunion", "Triangle"))
                r.Add(NarrativeRole.Romance);
            if (Any(f, "Provocation", "Humiliation", "PettyHumiliation", "Pressure", "Rumour", "Alarm", "Popularity", "AudienceQuestion", "Jealousy", "Suspicious", "Disinhibition", "Alcohol") && !Any(f, MomentTags.Fight))
                r.Add(NarrativeRole.Escalation);
            if (Any(f, MomentTags.Crying, "Panic", "Disgust", "Cold", "Laugh", "Applause", "Shock"))
                r.Add(NarrativeRole.Reaction);
            if (Any(f, "Reconcile", MomentTags.Hug, "Confession", "Applause") && !conflict)
                r.Add(NarrativeRole.Payoff);
            if (Any(f, "Comedy", "Slip", "Slippery", "Singing", "Gift") || (f.tags.Contains(MomentTags.Chaos) && !f.tags.Contains(MomentTags.Fire)))
                r.Add(NarrativeRole.Comedy);
            if (Any(f, MomentTags.Crying, MomentTags.Misery, "Cold", "Reconcile", "Calm") && f.intensity <= 3)
                r.Add(NarrativeRole.Aftermath);
            // Завязка — спокойный кадр, где люди только сходятся: тепло, вечеринка, подарок, публика. Слёзы и ссоры — не завязка.
            bool calm = Any(f, MomentTags.Warmth, "Calm", "Gift", "Party", "Music", "Attention", "Surprise", "Public", "Flirt");
            bool heavy = conflict || r.Contains(NarrativeRole.Reveal) || r.Contains(NarrativeRole.Reaction) || r.Contains(NarrativeRole.Climax)
                         || r.Contains(NarrativeRole.Payoff) || r.Contains(NarrativeRole.Aftermath);
            if (r.Count == 0 || (calm && f.intensity <= 2 && !heavy))
                r.Add(NarrativeRole.Setup);

            NarrativeRole[] order =
            {
                NarrativeRole.Climax, NarrativeRole.Reveal, NarrativeRole.Conflict, NarrativeRole.Romance, NarrativeRole.Escalation,
                NarrativeRole.Reaction, NarrativeRole.Payoff, NarrativeRole.Comedy, NarrativeRole.Aftermath, NarrativeRole.Setup
            };
            foreach (var o in order)
            {
                if (r.Contains(o))
                {
                    f.main = o;
                    break;
                }
            }
        }

        static void Tone(ClipFacts f)
        {
            if (f.blank)
                return;
            int k = Mathf.Max(1, f.intensity);
            switch (f.clip.mood)
            {
                case ShowMood.Drama: f.drama += k; break;
                case ShowMood.Trash: f.trash += k; break;
                default: f.family += k; break;
            }

            if (Any(f, MomentTags.Fight, MomentTags.Fire, MomentTags.Chaos, "Slip", "Humiliation"))
                f.trash++;
            if (Any(f, MomentTags.Crying, "Reveal", "Confession", "Betrayal", "SecretOut"))
                f.drama++;
            if (Any(f, MomentTags.Hug, "Reconcile", MomentTags.Warmth))
                f.family++;
        }

        static void Why(ClipFacts f, bool qualityBonus)
        {
            if (f.blank)
            {
                f.why.Add("в кадре никого — пустой угол");
                return;
            }

            f.why.Add(f.actors.Count >= 2 ? "в кадре двое: " + string.Join(" и ", f.actors) : f.actors.Count == 1 ? "в кадре " + f.actors[0] : "в кадре только реквизит");
            foreach (var t in f.tags)
            {
                if (Strong.Contains(t) || Medium.Contains(t))
                    f.why.Add(Cards.CardBrief.TagName(t));
                if (f.why.Count >= 4)
                    break;
            }

            if (f.intensity >= 4)
                f.why.Add("эмоции на пределе");
            int events = 0;
            foreach (var t in f.tags)
            {
                if (!Background.Contains(t))
                    events++;
            }

            if (events >= 3)
                f.why.Add("цепочка событий ×" + events);
            if (f.tags.Contains("SecretOut"))
                f.why.Add("редкое: раскрылась скрытая черта");
            if (f.sponsor)
                f.why.Add("бренд спонсора в кадре");
            if (qualityBonus)
                f.why.Add("техника: «нужный момент» — качество выше");
        }

        // ---------- Склейка ----------

        public static CutReport Analyze(IList<FootageClip> cut)
        {
            var report = new CutReport();
            if (cut == null)
                return report;
            foreach (var clip in cut)
            {
                if (clip != null)
                    report.clips.Add(Facts(clip));
            }

            int n = report.clips.Count;
            if (n == 0)
            {
                report.story = null;
                return report;
            }

            foreach (var f in report.clips)
            {
                report.drama += f.drama;
                report.trash += f.trash;
                report.family += f.family;
                report.sponsor |= f.sponsor;
                foreach (var a in f.actors)
                {
                    if (!report.actors.Contains(a))
                        report.actors.Add(a);
                }
            }

            int sum = 0;
            for (int i = 0; i + 1 < n; i++)
            {
                var link = Link(report.clips[i], report.clips[i + 1]);
                report.links.Add(link);
                sum += link.score;
                foreach (var reason in link.reasons)
                {
                    var list = reason.StartsWith("−") ? report.minus : report.plus;
                    string text = reason.Substring(2);
                    if (!list.Contains(text))
                        list.Add(text);
                }
            }

            float coherence = n == 1 ? (report.clips[0].blank ? 0f : 45f) : 50f + (float)sum / (n - 1);

            // Повторы: одно лицо во всех кадрах, три одинаковые роли, одна и та же съёмка.
            Repetition(report, ref coherence);
            Opening(report, ref coherence);
            Combos(report, ref coherence);

            report.coherence = Mathf.Clamp(Mathf.RoundToInt(coherence), 0, 100);
            Preview(report);
            return report;
        }

        static CutLink Link(ClipFacts a, ClipFacts b)
        {
            var link = new CutLink();
            if (a.blank || b.blank)
            {
                link.score -= 25;
                link.reasons.Add("− пустой кадр рвёт историю");
            }

            string shared = null;
            foreach (var x in a.actors)
            {
                if (b.actors.Contains(x))
                {
                    shared = x;
                    break;
                }
            }

            if (shared != null)
            {
                link.score += 18;
                link.reasons.Add("+ персонаж сохраняется между кадрами (" + shared + ")");
            }

            bool rule = false;
            if (Pair(a, b, new[] { NarrativeRole.Setup }, new[] { NarrativeRole.Escalation, NarrativeRole.Conflict, NarrativeRole.Romance, NarrativeRole.Reveal }))
            {
                link.score += 14;
                link.reasons.Add("+ завязка получила развитие");
                rule = true;
            }

            if (Pair(a, b, new[] { NarrativeRole.Escalation }, new[] { NarrativeRole.Conflict, NarrativeRole.Climax }))
            {
                link.score += 16;
                link.reasons.Add("+ напряжение взорвалось");
                rule = true;
            }

            if (Pair(a, b, new[] { NarrativeRole.Reveal }, new[] { NarrativeRole.Reaction, NarrativeRole.Conflict, NarrativeRole.Climax }))
            {
                link.score += 20;
                link.reasons.Add("+ раскрытие получило реакцию");
                rule = true;
            }

            if (Pair(a, b, new[] { NarrativeRole.Romance }, new[] { NarrativeRole.Conflict, NarrativeRole.Escalation, NarrativeRole.Climax }))
            {
                link.score += 16;
                link.reasons.Add("+ романтика обернулась ревностью и ссорой");
                rule = true;
            }

            if (Pair(a, b, new[] { NarrativeRole.Conflict, NarrativeRole.Climax }, new[] { NarrativeRole.Reaction, NarrativeRole.Aftermath, NarrativeRole.Payoff }))
            {
                link.score += 16;
                link.reasons.Add("+ конфликт получил последствия");
                rule = true;
            }

            if (Pair(a, b, new[] { NarrativeRole.Reaction }, new[] { NarrativeRole.Payoff, NarrativeRole.Aftermath }))
            {
                link.score += 10;
                link.reasons.Add("+ реакция пришла к развязке");
                rule = true;
            }

            string topic = null;
            foreach (var t in a.tags)
            {
                if (!Background.Contains(t) && b.tags.Contains(t))
                {
                    topic = t;
                    break;
                }
            }

            if (topic != null)
            {
                link.score += 8;
                link.reasons.Add("+ общая тема: " + Cards.CardBrief.TagName(topic));
            }

            if (a.main == b.main && (a.main == NarrativeRole.Conflict || a.main == NarrativeRole.Climax || a.main == NarrativeRole.Comedy))
            {
                link.score -= 12;
                link.reasons.Add("− снова то же самое: " + RoleWord(a.main));
            }

            if (shared == null && !rule && topic == null && a.clip.mood != b.clip.mood)
            {
                link.score -= 16;
                link.reasons.Add("− резкий переход: другие люди и другой тон");
            }

            link.level = link.score >= 18 ? 2 : link.score >= 0 ? 1 : 0;
            return link;
        }

        static bool Pair(ClipFacts a, ClipFacts b, NarrativeRole[] from, NarrativeRole[] to)
        {
            bool x = false;
            foreach (var r in from)
                x |= a.roles.Contains(r);
            if (!x)
                return false;
            foreach (var r in to)
            {
                if (b.roles.Contains(r))
                    return true;
            }

            return false;
        }

        static void Repetition(CutReport report, ref float coherence)
        {
            int n = report.clips.Count;
            // Кто чаще всех в кадре.
            var counts = new Dictionary<string, int>();
            foreach (var f in report.clips)
            {
                foreach (var a in f.actors)
                {
                    counts.TryGetValue(a, out int c);
                    counts[a] = c + 1;
                }
            }

            foreach (var kv in counts)
            {
                if (kv.Value > report.topActorCount)
                {
                    report.topActorCount = kv.Value;
                    report.topActor = kv.Key;
                }
            }

            if (n < 3)
                return;
            if (report.topActorCount >= n && report.actors.Count == 1)
            {
                coherence -= 8;
                report.repetition++;
                report.minus.Add("одно лицо во всех кадрах — шоу одного " + report.topActor);
            }

            bool same = true;
            for (int i = 1; i < n; i++)
                same &= report.clips[i].main == report.clips[0].main;
            if (same)
            {
                coherence -= 10;
                report.repetition += 2;
                report.minus.Add("три похожих кадра подряд: " + RoleWord(report.clips[0].main));
            }

            bool sameMood = true;
            for (int i = 1; i < n; i++)
                sameMood &= report.clips[i].clip.mood == report.clips[0].clip.mood;
            if (sameMood)
                report.repetition++;
            bool sameScene = true;
            for (int i = 1; i < n; i++)
                sameScene &= report.clips[i].clip.nodeId == report.clips[0].clip.nodeId;
            if (sameScene)
                report.repetition++;
        }

        // Начинать со взрыва без завязки — зритель не понимает, кто все эти люди.
        // Последствия раньше причины («сначала помирились, потом подрались») — история задом наперёд.
        static void Opening(CutReport report, ref float coherence)
        {
            if (report.clips.Count < 2)
                return;
            for (int i = 0; i < report.clips.Count && !report.backwards; i++)
            {
                var a = report.clips[i];
                if (a.main != NarrativeRole.Payoff && a.main != NarrativeRole.Aftermath)
                    continue;
                for (int j = i + 1; j < report.clips.Count; j++)
                {
                    var b = report.clips[j];
                    if (b.main == NarrativeRole.Conflict || b.main == NarrativeRole.Climax || b.main == NarrativeRole.Escalation)
                    {
                        report.backwards = true;
                        coherence -= 6;
                        report.minus.Add("последствия показаны раньше причины");
                        break;
                    }
                }
            }

            var first = report.clips[0];
            if (first.main == NarrativeRole.Climax && !first.roles.Contains(NarrativeRole.Setup))
            {
                coherence -= 4;
                report.minus.Add("начинается сразу со взрыва, без завязки");
            }
        }

        static void Combos(CutReport report, ref float coherence)
        {
            var c = report.clips;
            int n = c.Count;
            // ЗАВЯЗКА → РАЗВЯЗКА: тема заявлена и закрыта, общий участник.
            for (int i = 0; i < n && !report.Has("setup_payoff"); i++)
            {
                if (!c[i].roles.Contains(NarrativeRole.Setup))
                    continue;
                for (int j = i + 1; j < n; j++)
                {
                    if ((c[j].roles.Contains(NarrativeRole.Payoff) || c[j].roles.Contains(NarrativeRole.Climax)) && Shares(c[i], c[j]))
                    {
                        Add(report, "setup_payoff", "ЗАВЯЗКА → РАЗВЯЗКА", "связность ↑ · драма ↑");
                        coherence += 8;
                        report.drama += 2;
                        break;
                    }
                }
            }

            // РАСКРЫТИЕ → РЕАКЦИЯ: секрет вышел, и следующий кадр показывает, как это приняли.
            for (int i = 0; i + 1 < n; i++)
            {
                if (c[i].roles.Contains(NarrativeRole.Reveal) && (c[i + 1].roles.Contains(NarrativeRole.Reaction) || c[i + 1].roles.Contains(NarrativeRole.Conflict)))
                {
                    Add(report, "reveal_reaction", "РАСКРЫТИЕ → РЕАКЦИЯ", "драма ↑↑ · интерес зрителя ↑");
                    report.drama += 3;
                    coherence += 4;
                    break;
                }
            }

            // ЛЮБОВНЫЙ ТРЕУГОЛЬНИК: трое разных людей, романтика и ревность/ссора.
            bool romance = c.Exists(f => f.roles.Contains(NarrativeRole.Romance));
            bool clash = c.Exists(f => f.roles.Contains(NarrativeRole.Conflict) || f.tags.Contains("Jealousy"));
            if (report.actors.Count >= 3 && romance && clash)
            {
                Add(report, "triangle", "ЛЮБОВНЫЙ ТРЕУГОЛЬНИК", "драма ↑ · трэш ↑");
                report.drama += 2;
                report.trash += 2;
            }

            // ПОЛНАЯ АРКА: начало → взрыв → последствия, с общим участником.
            for (int i = 0; i < n && !report.Has("arc"); i++)
            {
                if (!(c[i].roles.Contains(NarrativeRole.Setup) || c[i].roles.Contains(NarrativeRole.Escalation) || c[i].roles.Contains(NarrativeRole.Romance)))
                    continue;
                for (int j = i + 1; j < n && !report.Has("arc"); j++)
                {
                    if (!(c[j].roles.Contains(NarrativeRole.Conflict) || c[j].roles.Contains(NarrativeRole.Climax) || c[j].roles.Contains(NarrativeRole.Reveal)))
                        continue;
                    for (int k = j + 1; k < n; k++)
                    {
                        bool end = c[k].roles.Contains(NarrativeRole.Aftermath) || c[k].roles.Contains(NarrativeRole.Payoff) || c[k].roles.Contains(NarrativeRole.Reaction);
                        if (end && Shares(c[i], c[j]) && (Shares(c[j], c[k]) || Shares(c[i], c[k])))
                        {
                            Add(report, "arc", "ПОЛНАЯ АРКА", "связность ↑↑ · потенциал рейтинга ↑");
                            coherence += 12;
                            break;
                        }
                    }
                }
            }

            // ХАОС-НАРЕЗКА: три сильных кадра без общих людей — трэш, но не история.
            if (n >= 3 && c.TrueForAll(f => f.intensity >= 3))
            {
                bool loose = true;
                for (int i = 0; i + 1 < n; i++)
                    loose &= !Shares(c[i], c[i + 1]);
                if (loose)
                {
                    Add(report, "chaos", "ХАОС-НАРЕЗКА", "трэш ↑↑ · связность ↓↓");
                    report.trash += 4;
                    coherence -= 15;
                }
            }
        }

        static bool Shares(ClipFacts a, ClipFacts b)
        {
            foreach (var x in a.actors)
            {
                if (b.actors.Contains(x))
                    return true;
            }

            return false;
        }

        static void Add(CutReport report, string id, string title, string effect)
        {
            if (!report.Has(id))
                report.combos.Add(new CutCombo { id = id, title = title, effect = effect });
        }

        static void Preview(CutReport report)
        {
            int n = report.clips.Count;
            float intensity = 0f;
            float quality = 0f;
            foreach (var f in report.clips)
            {
                intensity += f.intensity;
                quality += f.quality;
            }

            intensity /= Mathf.Max(1, n);
            quality /= Mathf.Max(1, n);
            report.emotion = intensity >= 3.5f ? 2 : intensity >= 2f ? 1 : 0;
            report.repetition = Mathf.Clamp(report.repetition, 0, 2);

            if (report.coherence >= 45 && n >= 2)
            {
                var words = new List<string>();
                foreach (var f in report.clips)
                {
                    string w = RoleWord(f.main);
                    if (words.Count == 0 || words[words.Count - 1] != w)
                        words.Add(w);
                }

                report.story = string.Join(" → ", words);
            }
            else
            {
                report.story = n >= 2 ? null : (n == 1 ? RoleWord(report.clips[0].main) : null);
            }

            float potential = report.coherence / 20f + intensity + quality + report.combos.Count * 1.5f - report.repetition * 0.7f + (n < 3 ? -2f : 0f);
            report.potential = potential >= 9f ? 2 : potential >= 5.5f ? 1 : 0;

            // Сколько склейка добавит к оценке эфира (−1.5…+1.5): связность, комбо, повторы.
            float bonus = (report.coherence - 50) / 50f + report.combos.Count * 0.3f - report.repetition * 0.25f;
            if (report.Has("chaos"))
                bonus += 0.4f; // трэш-аудитория любит хаос
            report.ratingBonus = n == 0 ? 0f : Mathf.Clamp(bonus, -1.5f, 1.5f);
        }

        public static string RoleWord(NarrativeRole role)
        {
            switch (role)
            {
                case NarrativeRole.Setup: return "знакомство";
                case NarrativeRole.Escalation: return "напряжение";
                case NarrativeRole.Reveal: return "раскрытие";
                case NarrativeRole.Reaction: return "реакция";
                case NarrativeRole.Conflict: return "конфликт";
                case NarrativeRole.Romance: return "романтика";
                case NarrativeRole.Comedy: return "комедия";
                case NarrativeRole.Payoff: return "развязка";
                case NarrativeRole.Climax: return "кульминация";
                default: return "последствия";
            }
        }

        public static string RoleTitle(NarrativeRole role)
        {
            return RoleWord(role).ToUpperInvariant();
        }

        public static string Level(int level)
        {
            return level >= 2 ? "ВЫСОКИЙ" : level == 1 ? "СРЕДНИЙ" : "НИЗКИЙ";
        }

        public static string Arrows(int value)
        {
            if (value >= 8)
                return "↑↑";
            if (value >= 3)
                return "↑";
            if (value >= 1)
                return "·";
            return "↓";
        }

        static string Cap(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            string low = s.ToLowerInvariant();
            return char.ToUpperInvariant(low[0]) + low.Substring(1);
        }
    }
}
