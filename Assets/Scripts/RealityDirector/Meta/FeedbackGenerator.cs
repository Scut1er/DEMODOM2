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
        // Зритель знает только эфир (GDD §28): имена — только тех, кто в показанных роликах сделал то, о чём речь.
        // Никаких «Злой»/«Добряк» по умолчанию: участника могло не быть в выпуске.
        public static FeedbackResult Build(EpisodeContext context, IReadOnlyList<CapturedMoment> moments, SeasonTone tone)
        {
            var fighters = Who(moments, MomentTags.Fight);
            var criers = Who(moments, MomentTags.Crying);
            var aired = Aired(moments);

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
                    body = fighters.Count >= 2
                        ? "Понравилось, наконец-то " + fighters[0] + " и " + fighters[1] + " " + MoodStyle.Paint("сцепились", ShowMood.Trash) + "!"
                        : fighters.Count == 1
                            ? "Понравилось: " + fighters[0] + " — " + MoodStyle.Paint("в драке", ShowMood.Trash) + ". Наконец-то!"
                            : "Понравилось, наконец-то " + MoodStyle.Paint("драка", ShowMood.Trash) + " в кадре!"
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
                    ? (criers.Count > 0 ? criers[0] + " — " : "") + MoodStyle.Paint(criers.Count > 0 ? "в слезах" : "Слёзы", ShowMood.Drama) + " прямо в кадре. Беру салфетки и ещё серию."
                    : WantTears(aired)
            });

            ViewerWishId id = PickWish(crying, familyShot, trashShot, tone);
            int index = id == ViewerWishId.Fight ? 0 : id == ViewerWishId.Hug ? 1 : 2;
            reviews[index].offer = true;
            reviews[index].wish = id;
            reviews[index].body = OfferBody(id, aired, tone);

            PunishBlanks(reviews, moments, index);
            RevealSecrets(reviews, moments, index);
            EnsureUnique(reviews);

            int sum = 0;
            for (int i = 0; i < reviews.Count; i++)
                sum += reviews[i].score;
            float avg = reviews.Count > 0 ? sum / (float)reviews.Count : 0f;

            return new FeedbackResult
            {
                reviews = reviews,
                score = UnityEngine.Mathf.Round(avg * 10f) / 10f,
                wish = OfferLabel(id, tone),
                nextWish = id
            };
        }

        static string WantTears(List<string> aired)
        {
            return aired.Count > 0
                ? "Хотелось бы увидеть " + MoodStyle.Paint("слёзы", ShowMood.Drama) + ". " + aired[0] + " держится — зря."
                : "Хотелось бы увидеть " + MoodStyle.Paint("слёзы", ShowMood.Drama) + ". Хоть чьи-нибудь.";
        }

        // Имена из подсказок показанных роликов (пустые кадры не в счёт).
        static List<string> Who(IReadOnlyList<CapturedMoment> moments, string tag)
        {
            var names = new List<string>();
            if (moments == null)
                return names;
            for (int i = 0; i < moments.Count; i++)
            {
                if (moments[i].grade == CaptureGrade.Blank)
                    continue;
                var found = CapturedMoment.Who(moments[i].cues, tag);
                for (int n = 0; n < found.Count; n++)
                {
                    if (!names.Contains(found[n]))
                        names.Add(found[n]);
                }
            }

            return names;
        }

        // Все, кто попал в эфир.
        static List<string> Aired(IReadOnlyList<CapturedMoment> moments)
        {
            var names = new List<string>();
            if (moments == null)
                return names;
            for (int i = 0; i < moments.Count; i++)
            {
                if (moments[i].grade != CaptureGrade.Cast || moments[i].actorNames == null)
                    continue;
                for (int n = 0; n < moments[i].actorNames.Count; n++)
                {
                    string name = moments[i].actorNames[n];
                    if (!string.IsNullOrEmpty(name) && !names.Contains(name))
                        names.Add(name);
                }
            }

            return names;
        }

        // Спонсор уже в кадре: каждый отзыв ниже, оценка серии пересчитывается.
        public static FeedbackResult ApplySponsor(FeedbackResult result, int hit)
        {
            if (hit <= 0 || result.reviews == null || result.reviews.Count == 0)
                return result;
            int sum = 0;
            for (int i = 0; i < result.reviews.Count; i++)
            {
                var review = result.reviews[i];
                review.score = UnityEngine.Mathf.Max(1, review.score - hit);
                if (i == 0)
                    review.body += " Реклама в кадре — дёшево.";
                sum += review.score;
            }

            result.score = UnityEngine.Mathf.Round(sum * 10f / result.reviews.Count) / 10f;
            return result;
        }

        // Зритель видит только финальный кат. context сюда не передаём — вырезанное он не знает.
        public static FeedbackResult BuildCut(IReadOnlyList<CapturedMoment> cut, SeasonTone tone, int coherence, bool sponsorAired,
            CutReport report = null, string brand = null)
        {
            // Пустой эфир: смотреть нечего. Раньше общие отзывы давали ~4/10 и 50–60 кр за пустоту.
            if (cut == null || cut.Count == 0)
                return EmptyAir(tone);
            var result = Build(null, cut, tone);
            if (result.reviews == null)
                result.reviews = new List<ViewerReview>();
            var extra = HellTubeComments.Pick(cut, tone, coherence, sponsorAired, 4, report, brand);
            var seen = new HashSet<string>();
            var people = new HashSet<string>();
            for (int i = 0; i < result.reviews.Count; i++)
            {
                seen.Add(result.reviews[i].body ?? "");
                if (!string.IsNullOrEmpty(result.reviews[i].author))
                    people.Add(result.reviews[i].author);
            }
            for (int i = 0; i < extra.Count && result.reviews.Count < 6; i++)
            {
                if (!seen.Add(extra[i].body ?? ""))
                    continue;
                if (!string.IsNullOrEmpty(extra[i].author) && !people.Add(extra[i].author))
                    continue;
                result.reviews.Add(extra[i]);
            }

            int sum = 0;
            for (int i = 0; i < result.reviews.Count; i++)
                sum += result.reviews[i].score;
            result.score = result.reviews.Count > 0
                ? UnityEngine.Mathf.Round(sum * 10f / result.reviews.Count) / 10f
                : 0f;
            // Монтаж двигает оценку: связность, комбо и повторы (CutAnalysis) — до ±1.5.
            if (report != null && report.clips.Count > 0 && result.score > 0f)
                result.score = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.Round((result.score + report.ratingBonus) * 10f) / 10f, 1f, 10f);
            return result;
        }

        static FeedbackResult EmptyAir(SeasonTone tone)
        {
            var reviews = new List<ViewerReview>
            {
                new ViewerReview { author = "Аня", score = 1, body = "Серия вышла, а смотреть нечего. Это шутка такая?" },
                new ViewerReview { author = "Кирилл", score = 1, body = "Пустой эфир. Даже обоев не показали." },
                new ViewerReview
                {
                    author = "Марина",
                    score = 1,
                    offer = true,
                    wish = ViewerWishId.Cry,
                    body = OfferBody(ViewerWishId.Cry, new List<string>(), tone)
                },
                new ViewerReview { author = "суккуб_с_попкорном", score = 1, body = "серия вышла, а смотреть нечего" }
            };

            return new FeedbackResult
            {
                reviews = reviews,
                score = 1f,
                wish = OfferLabel(ViewerWishId.Cry, tone),
                nextWish = ViewerWishId.Cry
            };
        }

        static List<ViewerReview> Extras(IReadOnlyList<CapturedMoment> cut, int coherence, bool sponsorAired)
        {
            bool fight = HasTag(cut, MomentTags.Fight);
            bool fire = HasTag(cut, MomentTags.Fire);
            bool crying = HasTag(cut, MomentTags.Crying);
            bool hug = HasTag(cut, MomentTags.Hug);
            bool blank = false;
            bool repeat = false;
            var titles = new HashSet<string>();
            int n = cut != null ? cut.Count : 0;
            for (int i = 0; i < n; i++)
            {
                if (cut[i].grade == CaptureGrade.Blank)
                    blank = true;
                string title = cut[i].Title;
                if (!titles.Add(title))
                    repeat = true;
            }

            var list = new List<ViewerReview>(8);
            if (coherence >= 70 && n >= 2)
                list.Add(Line("девятый_круг_FM", 8, "Это уже история, а не нарезка криков."));
            if (coherence <= 30 && n >= 2)
                list.Add(Line("скучающий_демон", 4, "я ничего не понял, но мужик где-то упал"));
            if (sponsorAired)
                list.Add(Line("котёл_номер_7", 3, "опять банку в лицо. это шоу или ларёк?"));
            if (repeat)
                list.Add(Line("мама_антихриста", 4, "один и тот же момент крутите дважды"));
            if (fire)
                list.Add(Line("суккуб_с_попкорном", 9, "НАКОНЕЦ-ТО НОРМАЛЬНОЕ ТЕЛЕВИДЕНИЕ"));
            if (fight)
                list.Add(Line("котёл_номер_7", 9, "перемотал разговоры, драка 10/10"));
            if (crying)
                list.Add(Line("девятый_круг_FM", 7, "почему плачет? зато это показали"));
            if (hug)
                list.Add(Line("мама_антихриста", 7, "Она заслуживает лучшего"));
            if (blank)
                list.Add(Line("скучающий_демон", 2, "в эфире обои. я за людей плачу"));
            if (n == 0)
                list.Add(Line("суккуб_с_попкорном", 1, "серия вышла, а смотреть нечего"));
            list.Add(Line("скучающий_демон", 5, "я досмотрел. для этого канала это уже много"));
            list.Add(Line("мама_антихриста", 5, "поставьте на повтор, я не доела"));
            list.Add(Line("девятый_круг_FM", 6, "шум, лица, кто-то орёт. беру"));
            int start = (coherence + n * 3) % Pool.Length;
            for (int k = 0; k < Pool.Length; k++)
            {
                int score = 3 + ((start + k) % 6);
                list.Add(Line(Authors[(start + k) % Authors.Length], score, Pool[(start + k) % Pool.Length]));
            }

            return list;
        }

        static readonly string[] Authors =
        {
            "девятый_круг_FM", "скучающий_демон", "котёл_номер_7", "мама_антихриста", "суккуб_с_попкорном"
        };

        static readonly string[] Pool =
        {
            "я включил на фон и в итоге орёл в экран",
            "это не шоу, это чужая кухня",
            "где драка? я за неё заплатил вниманием",
            "лицо крупно. наконец-то",
            "монтаж как после аварии",
            "они смотрят в камеру. мне стыдно за них",
            "тишина в кадре страшнее крика",
            "опять холодильник. убейте уже технику",
            "я поставил лайк из жалости",
            "кто продюсер? пусть выйдет и извинится",
            "серия короче рекламы. или это и была реклама",
            "плакал не я. почти",
            "обнимашки на фоне кринжа. беру",
            "оператор дышит в микрофон. это персонаж?",
            "вырезали лучшее, я чувствую",
            "пустой угол — тоже высказывание. плохое",
            "тон скачет. определитесь, вы цирк или семья",
            "я узнал свою тётю. выключите",
            "чат орёт, и он прав",
            "это уже третья серия про еду. я голоден и зол",
            "скрытая черта так и осталась скрытой. трусы",
            "если это финал, я требую продолжение",
            "звука нет, лица есть. как немое кино, только хуже",
            "я перемотал. потом вернул. потом пожалел",
            "связка кадров есть. смысла нет. мне норм",
            "кто-то явно играет, кто-то нет. интересно кто",
            "поставьте субтитры, я ору вместе с ними",
            "рейтинг завышен. я один это вижу?",
            "моя мама сказала выключить. я не выключил",
            "кадр с дверью дольше, чем с людьми",
            "они устали. это видно. это и есть шоу",
            "я ждал признания и получил чай",
            "спор из ничего. мой любимый жанр",
            "камера трясётся. оператор тоже человек. к сожалению",
            "вынесите мусор из кадра. и из сценария",
            "это нельзя показывать детям. поэтому я смотрю",
            "один смотрит в пол, другой в камеру. любовный треугольник с полом",
            "я поставил дизлайк и всё равно досмотрел",
            "серия пахнет дешёвым контрактом",
            "если следующий выпуск тише, я отпишусь. нет, не отпишусь",
            "герой серии — тот, кто молчал",
            "я сохранил момент. стыдно, но сохранил"
        };

        static ViewerReview Line(string author, int score, string body)
        {
            return new ViewerReview { author = author, score = score, body = body };
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

        static string OfferBody(ViewerWishId id, List<string> aired, SeasonTone tone)
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
                    return WantTears(aired);
            }
        }

        public static string OfferLabel(ViewerWishId id, SeasonTone tone)
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
                    return MoodStyle.Paint("слёзы в кадре", ShowMood.Drama);
            }
        }

        static void RevealSecrets(List<ViewerReview> reviews, IReadOnlyList<CapturedMoment> moments, int offerIndex)
        {
            int cursor = 0;
            if (Saw(moments, HiddenTrait.Kleptomaniac))
            {
                var thief = Who(moments, HiddenTrait.Kleptomaniac.ToString());
                WriteSecret(reviews, offerIndex, ref cursor, (thief.Count > 0 ? thief[0] : "Кто-то") + " в кадре шарит по чужому. Клептомания.");
            }

            if (Saw(moments, HiddenTrait.Prankster))
            {
                var joker = Who(moments, HiddenTrait.Prankster.ToString());
                WriteSecret(reviews, offerIndex, ref cursor, (joker.Count > 0 ? joker[0] + " — автор пранка" : "Это пранк") + ". В кадре не случайность.");
            }
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
