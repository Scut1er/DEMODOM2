using System.Collections.Generic;
using RealityDirector.Capture;
using RealityDirector.Core;
using RealityDirector.NPC;

namespace RealityDirector.Meta
{
    public class ViewerReview
    {
        public string author;
        public int score;
        public string body;
        public bool offer;
        public ViewerWishId wish;
    }

    public struct FeedbackResult
    {
        public List<ViewerReview> reviews;
        public float score;
        public string wish;
        public string payLine;
        public ViewerWishId nextWish;
    }

    public static class FeedbackGenerator
    {
        public static FeedbackResult Build(EpisodeContext context, IReadOnlyList<CapturedMoment> moments, IReadOnlyList<NPCController> cast, SeasonTone tone)
        {
            NPCController aggressive = null;
            NPCController sentimental = null;
            if (cast != null)
            {
                for (int i = 0; i < cast.Count; i++)
                {
                    if (cast[i].Trait == null)
                        continue;
                    if (cast[i].Trait.traitId == TraitId.Aggressive)
                        aggressive = cast[i];
                    else if (cast[i].Trait.traitId == TraitId.Sentimental)
                        sentimental = cast[i];
                }
            }

            string zloi = aggressive != null ? aggressive.DisplayName : "Злой";
            string victim = sentimental != null ? sentimental.AccusativeName : "Добряку";
            string soft = sentimental != null ? sentimental.DisplayName : "Добряк";

            bool fightShot = HasTag(moments, MomentTags.Fight);
            bool fightHappened = context != null && context.Had(MomentTags.Fight);
            bool fire = context != null && context.Had(MomentTags.Fire);
            bool crying = HasTag(moments, MomentTags.Crying);
            bool familyShot = HasMood(moments, ShowMood.Family);
            bool hugShot = HasTag(moments, MomentTags.Hug);
            bool trashShot = fightShot || HasMood(moments, ShowMood.Trash);

            var reviews = new List<ViewerReview>(3);
            if (fightShot)
            {
                reviews.Add(new ViewerReview
                {
                    author = "Аня",
                    score = 8,
                    body = "Понравилось, наконец-то " + zloi + " дал " + MoodStyle.Paint("по роже", ShowMood.Trash) + " " + victim + "!"
                });
            }
            else if (fightHappened)
            {
                reviews.Add(new ViewerReview
                {
                    author = "Аня",
                    score = 5,
                    body = MoodStyle.Paint("Драка", ShowMood.Trash) + " была, но в кадр не попала. Оператор, ты где?"
                });
            }
            else
            {
                reviews.Add(new ViewerReview
                {
                    author = "Аня",
                    score = 3,
                    body = "Скучно. Где " + MoodStyle.Paint("драка", ShowMood.Trash) + "? Мы ради этого включили."
                });
            }

            reviews.Add(new ViewerReview
            {
                author = "Кирилл",
                score = fire ? 7 : familyShot ? 7 : 5,
                body = fire
                    ? "Холодильник полыхнул — и понеслось. Вот это " + MoodStyle.Paint("хаос", ShowMood.Trash) + "."
                    : hugShot
                        ? "Сняли, как они " + MoodStyle.Paint("обнялись", ShowMood.Family) + ". Тепло, но где искра?"
                        : familyShot
                            ? "Тихий кадр с людьми. Тепло есть, объятий нет."
                            : "Кухня цела. А где " + MoodStyle.Paint("хаос", ShowMood.Trash) + "?"
            });

            reviews.Add(new ViewerReview
            {
                author = "Марина",
                score = crying ? 8 : 6,
                body = crying
                    ? soft + " " + MoodStyle.Paint("рыдал", ShowMood.Drama) + " в кадре. Беру салфетки и ещё серию."
                    : "Хотелось бы увидеть, как " + soft + " " + MoodStyle.Paint("расплакался", ShowMood.Drama) + "."
            });

            ViewerWishId id = PickWish(crying, familyShot, trashShot, tone);
            int index = id == ViewerWishId.Fight ? 0 : id == ViewerWishId.Hug ? 1 : 2;
            reviews[index].offer = true;
            reviews[index].wish = id;
            reviews[index].body = OfferBody(id, soft, tone);

            PunishBlanks(reviews, moments, index);
            RevealSecrets(reviews, moments, index, zloi, soft);
            EnsureUnique(reviews);

            int sum = 0;
            for (int i = 0; i < reviews.Count; i++)
                sum += reviews[i].score;
            float avg = reviews.Count > 0 ? sum / (float)reviews.Count : 0f;

            return new FeedbackResult
            {
                reviews = reviews,
                score = UnityEngine.Mathf.Round(avg * 10f) / 10f,
                wish = OfferLabel(id, soft, tone),
                nextWish = id
            };
        }

        static void PunishBlanks(List<ViewerReview> reviews, IReadOnlyList<CapturedMoment> moments, int offerIndex)
        {
            int blanks = 0;
            int total = moments != null ? moments.Count : 0;
            if (moments != null)
            {
                for (int i = 0; i < moments.Count; i++)
                {
                    if (moments[i].grade == CaptureGrade.Blank)
                        blanks++;
                }
            }

            if (blanks == 0)
                return;

            bool onlyBlanks = total > 0 && blanks == total;
            for (int i = 0; i < reviews.Count; i++)
            {
                if (i == offerIndex)
                {
                    reviews[i].score = UnityEngine.Mathf.Max(1, reviews[i].score - blanks);
                    continue;
                }

                if (onlyBlanks)
                {
                    reviews[i].score = 2;
                    reviews[i].body = i == 0
                        ? "В кадре никого. Я за людей плачу, не за обои."
                        : "Вся серия — пустые слоты. Я это выключила.";
                    continue;
                }

                reviews[i].score = UnityEngine.Mathf.Max(1, reviews[i].score - blanks);
                if (i == (offerIndex == 1 ? 2 : 1))
                    reviews[i].body = blanks == 1
                        ? "Один кадр — голая стена. Слот зря сожгли, такое мы не смотрим."
                        : "Часть кадров — пустой угол. Нам это не понравилось.";
            }
        }

        static void EnsureUnique(List<ViewerReview> reviews)
        {
            string[] spare =
            {
                "Монтаж рваный, я потеряла нить.",
                "Оператор, половина кадров мимо.",
                "Шум ради шума. Лиц не хватило."
            };
            var seen = new HashSet<string>();
            for (int i = 0; i < reviews.Count; i++)
            {
                string key = reviews[i].body ?? "";
                if (seen.Add(key))
                    continue;
                for (int s = 0; s < spare.Length; s++)
                {
                    if (!seen.Add(spare[s]))
                        continue;
                    reviews[i].body = spare[s];
                    break;
                }
            }
        }

        static ViewerWishId PickWish(bool crying, bool familyShot, bool trashShot, SeasonTone tone)
        {
            if (!crying)
                return ViewerWishId.Cry;
            if (!familyShot)
                return ViewerWishId.Hug;
            if (!trashShot)
                return ViewerWishId.Fight;
            return ViewerWishId.HoldTone;
        }

        static string OfferBody(ViewerWishId id, string soft, SeasonTone tone)
        {
            switch (id)
            {
                case ViewerWishId.Hug:
                    return "Хочу, чтобы они " + MoodStyle.Paint("обнялись", ShowMood.Family) + ". Тепла не хватило.";
                case ViewerWishId.Fight:
                    return "Дайте нам " + MoodStyle.Paint("драку", ShowMood.Trash) + ". Мы ради этого включили.";
                case ViewerWishId.HoldTone:
                    if (tone == null || !tone.TryLead(out ShowMood lead))
                        return "Выберите один тон и держите его. Не расползайтесь.";
                    return "Держите этот тон: " + MoodStyle.Paint(MoodStyle.Full(lead), lead) + ". Не сливайте.";
                default:
                    return "Хотелось бы увидеть, как " + soft + " " + MoodStyle.Paint("расплакался", ShowMood.Drama) + ".";
            }
        }

        public static string OfferLabel(ViewerWishId id, string soft, SeasonTone tone)
        {
            switch (id)
            {
                case ViewerWishId.Hug:
                    return MoodStyle.Paint("объятия", ShowMood.Family);
                case ViewerWishId.Fight:
                    return MoodStyle.Paint("драка", ShowMood.Trash);
                case ViewerWishId.HoldTone:
                    if (tone == null || !tone.TryLead(out ShowMood lead))
                        return "один тон";
                    return MoodStyle.Paint(MoodStyle.Full(lead), lead);
                default:
                    return MoodStyle.Paint("слёзы " + soft, ShowMood.Drama);
            }
        }

        static void RevealSecrets(List<ViewerReview> reviews, IReadOnlyList<CapturedMoment> moments, int offerIndex, string zloi, string soft)
        {
            int cursor = 0;
            if (Saw(moments, HiddenTrait.Kleptomaniac))
                WriteSecret(reviews, offerIndex, ref cursor, soft + " в кадре шарит по чужому. Клептоман.");
            if (Saw(moments, HiddenTrait.Prankster))
                WriteSecret(reviews, offerIndex, ref cursor, zloi + " это подстроил. В кадре пранк, не случайность.");
        }

        static void WriteSecret(List<ViewerReview> reviews, int offerIndex, ref int cursor, string body)
        {
            for (int n = 0; n < reviews.Count; n++)
            {
                int i = (cursor + n) % reviews.Count;
                if (i == offerIndex)
                    continue;
                reviews[i].body = body;
                reviews[i].score = UnityEngine.Mathf.Max(reviews[i].score, 8);
                cursor = i + 1;
                return;
            }
        }

        static bool Saw(IReadOnlyList<CapturedMoment> moments, HiddenTrait trait)
        {
            if (moments == null)
                return false;
            for (int i = 0; i < moments.Count; i++)
            {
                if (moments[i].grade != CaptureGrade.Cast)
                    continue;
                if (moments[i].exposed == trait)
                    return true;
            }

            return false;
        }

        static bool HasMood(IReadOnlyList<CapturedMoment> moments, ShowMood mood)
        {
            if (moments == null)
                return false;
            for (int i = 0; i < moments.Count; i++)
            {
                if (moments[i].grade == CaptureGrade.Blank)
                    continue;
                if (mood == ShowMood.Family && moments[i].grade != CaptureGrade.Cast)
                    continue;
                if (moments[i].mood == mood)
                    return true;
            }

            return false;
        }

        static bool HasTag(IReadOnlyList<CapturedMoment> moments, string tag)
        {
            if (moments == null)
                return false;
            for (int i = 0; i < moments.Count; i++)
            {
                if (moments[i].grade == CaptureGrade.Blank)
                    continue;
                if (moments[i].tags != null && moments[i].tags.Contains(tag))
                    return true;
            }

            return false;
        }
    }
}
