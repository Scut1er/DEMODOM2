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
        public int score;
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
                    : familyShot
                        ? "Сняли, как они " + MoodStyle.Paint("обнялись", ShowMood.Family) + ". Тепло, но где искра?"
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

            int sum = 0;
            for (int i = 0; i < reviews.Count; i++)
                sum += reviews[i].score;

            return new FeedbackResult
            {
                reviews = reviews,
                score = UnityEngine.Mathf.RoundToInt(sum / (float)reviews.Count),
                wish = OfferLabel(id, soft, tone),
                nextWish = id
            };
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

        static bool HasMood(IReadOnlyList<CapturedMoment> moments, ShowMood mood)
        {
            if (moments == null)
                return false;
            for (int i = 0; i < moments.Count; i++)
            {
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
                if (moments[i].tags != null && moments[i].tags.Contains(tag))
                    return true;
            }

            return false;
        }
    }
}
