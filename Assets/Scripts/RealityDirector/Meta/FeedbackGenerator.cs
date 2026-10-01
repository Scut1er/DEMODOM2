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
    }

    public struct FeedbackResult
    {
        public List<ViewerReview> reviews;
        public int score;
        public string wish;
    }

    public static class FeedbackGenerator
    {
        public static FeedbackResult Build(EpisodeContext context, IReadOnlyList<CapturedMoment> moments, IReadOnlyList<NPCController> cast)
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
            bool crying = (context != null && context.Had(MomentTags.Crying)) || HasTag(moments, MomentTags.Crying);

            var reviews = new List<ViewerReview>(3);
            if (fightShot)
            {
                reviews.Add(new ViewerReview
                {
                    author = "Аня",
                    score = 8,
                    body = "Понравилось, наконец-то " + zloi + " дал по роже " + victim + "!"
                });
            }
            else if (fightHappened)
            {
                reviews.Add(new ViewerReview
                {
                    author = "Аня",
                    score = 5,
                    body = "Драка была, но в кадр не попала. Оператор, ты где?"
                });
            }
            else
            {
                reviews.Add(new ViewerReview
                {
                    author = "Аня",
                    score = 3,
                    body = "Скучно. Где драка? Мы ради этого включили."
                });
            }

            reviews.Add(new ViewerReview
            {
                author = "Кирилл",
                score = fire ? 7 : 5,
                body = fire
                    ? "Холодильник полыхнул — и понеслось. Вот это режиссура."
                    : "Кухня цела. А где хаос?"
            });

            reviews.Add(new ViewerReview
            {
                author = "Марина",
                score = crying ? 8 : 6,
                body = crying
                    ? soft + " рыдал в кадре. Беру салфетки и ещё серию."
                    : "Хотелось бы увидеть, как " + soft + " расплакался."
            });

            int sum = 0;
            for (int i = 0; i < reviews.Count; i++)
                sum += reviews[i].score;

            return new FeedbackResult
            {
                reviews = reviews,
                score = UnityEngine.Mathf.RoundToInt(sum / (float)reviews.Count),
                wish = "Пожелание на следующую серию: пусть " + soft + " расплачется."
            };
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
