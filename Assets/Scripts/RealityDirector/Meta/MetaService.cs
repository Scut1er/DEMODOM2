using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Events;
using RealityDirector.NPC;
using UnityEngine;

namespace RealityDirector.Meta
{
    public enum CrewTrack
    {
        Cast,
        Operators,
        Writers
    }

    // Логика хаба без UI: колода, магазин, прокачка команды.
    public class MetaService
    {
        readonly SeasonState _state;
        readonly SeasonTone _tone;
        readonly IReadOnlyList<EventDefinition> _catalog;

        public string Reject { get; private set; }

        public MetaService(SeasonState state, SeasonTone tone, IReadOnlyList<EventDefinition> catalog)
        {
            _state = state;
            _tone = tone;
            _catalog = catalog;
        }

        // Ник «dev» (без учёта регистра): в колоде все карты магазина хаба, категории открыты без Сценаристов.
        public bool Dev => _state.producerName != null && _state.producerName.Trim().ToLowerInvariant() == "dev";

        public int GrantDevDeck()
        {
            if (!Dev || _catalog == null)
                return 0;
            int added = 0;
            for (int i = 0; i < _catalog.Count; i++)
            {
                var def = _catalog[i];
                if (def == null || def.sponsor || def.price <= 0 || _state.Owns(def.id))
                    continue;
                _state.owned.Add(def.id);
                added++;
            }

            return added;
        }

        // Карт в руке на съёмке (SeasonConfig → выпуск). GDD §15: placeholder 5.
        public int HandSize()
        {
            var ep = _state.episode;
            return ep != null && ep.handSize > 0 ? ep.handSize : EpisodeState.DefaultHandSize;
        }

        // Рабочая колода съёмки: вся коллекция сезона + разовые карты выпуска.
        public int DeckCount()
        {
            int n = 0;
            for (int i = 0; i < _state.owned.Count; i++)
            {
                if (Find(_state.owned[i]) != null)
                    n++;
            }

            var ep = _state.episode;
            if (ep != null)
            {
                for (int i = 0; i < ep.tempCards.Count; i++)
                {
                    if (!_state.Owns(ep.tempCards[i]) && Find(ep.tempCards[i]) != null)
                        n++;
                }
            }

            return n;
        }

        public int Level(CrewTrack track)
        {
            switch (track)
            {
                case CrewTrack.Cast: return _state.castLevel;
                case CrewTrack.Operators: return _state.operatorLevel;
                default: return _state.writerLevel;
            }
        }

        public bool TryUpgrade(CrewTrack track)
        {
            int level = Level(track);
            int max = Progression.MaxFor(track);
            int cost = Progression.UpgradeCost(track == CrewTrack.Cast, level, max);
            if (level >= max || cost <= 0 || _state.money < cost)
            {
                Reject = level >= max ? "Уже максимум." : "Не хватает кр.";
                return false;
            }

            _state.money -= cost;
            if (track == CrewTrack.Cast)
                _state.castLevel++;
            else if (track == CrewTrack.Operators)
                _state.operatorLevel++;
            else
                _state.writerLevel++;
            Reject = null;
            return true;
        }

        public bool TryBuy(string id)
        {
            if (_state.Owns(id))
                return false;
            var def = Find(id);
            if (def == null || def.price <= 0 || def.sponsor || _state.money < def.price)
            {
                Reject = "Не хватает кр.";
                return false;
            }

            _state.money -= def.price;
            _state.owned.Add(id);
            Reject = null;
            return true;
        }

        public bool TryBuyOffer(MarketingOffer offer)
        {
            if (offer == null)
                return false;
            if (offer.kind == OfferKind.Contract)
                return TryTakeContract(offer.cardId, offer.payout, offer.scoreHit);

            var ep = _state.episode;
            if (ep == null)
                return false;
            ep.EnsureLists();
            string bought = "bought_" + (string.IsNullOrEmpty(offer.id) ? offer.title : offer.id);
            if (ep.HasFlag(bought))
            {
                Reject = "Уже куплено в этом выпуске.";
                return false;
            }

            if (offer.price > 0 && ep.cash < offer.price)
            {
                Reject = "Не хватает кассы выпуска.";
                return false;
            }

            if (!string.IsNullOrEmpty(offer.cardId))
            {
                if (ep.tempCards.Contains(offer.cardId))
                {
                    Reject = "Эта карта уже в выпуске.";
                    return false;
                }

                if (Find(offer.cardId) == null)
                {
                    Reject = "Карты нет в колоде контента.";
                    return false;
                }
            }

            if (offer.price > 0)
                ep.cash -= offer.price;
            if (!string.IsNullOrEmpty(offer.cardId))
                ep.tempCards.Add(offer.cardId);
            if (!string.IsNullOrEmpty(offer.flag))
                ep.SetFlag(offer.flag);
            ep.SetFlag(bought);
            Reject = null;
            return true;
        }

        // Магазин выпуска: карта в руку до эфира, потом сгорает. Платит нал, не кр.
        public bool TryBuyRun(string id)
        {
            var ep = _state.episode;
            if (ep == null || ep.tempCards.Contains(id))
                return false;
            var def = Find(id);
            int cost = def != null ? def.runPrice : 0;
            if (def == null || cost <= 0 || ep.cash < cost)
            {
                Reject = "Не хватает кассы выпуска.";
                return false;
            }

            if (!def.sponsor && _state.Owns(id))
            {
                Reject = "Уже в колоде сезона.";
                return false;
            }

            ep.cash -= cost;
            ep.tempCards.Add(id);
            Reject = null;
            return true;
        }

        // Контракт не списывает нал. Выплата — только если карта сыграна и кадр в монтаже.
        public bool TryTakeContract(string cardId, int payout, int scoreHit)
        {
            var ep = _state.episode;
            if (ep == null || string.IsNullOrEmpty(cardId))
                return false;
            ep.EnsureLists();
            int active = 0;
            for (int i = 0; i < ep.contracts.Count; i++)
            {
                if (ep.contracts[i].grantedCardId == cardId)
                {
                    Reject = "Этот контракт уже взят.";
                    return false;
                }

                if (ep.contracts[i].status == ContractStatus.Active)
                    active++;
            }

            int slots = Progression.ContractSlots(_state.writerLevel);
            if (active >= slots)
            {
                Reject = "Слоты контрактов заняты (" + slots + ").";
                return false;
            }

            ep.contracts.Add(new SponsorContractState
            {
                offerId = "deal_" + cardId,
                brandId = cardId,
                grantedCardId = cardId,
                payout = payout,
                scoreHit = scoreHit,
                status = ContractStatus.Active,
                cardWasPlayed = false
            });
            if (!ep.tempCards.Contains(cardId))
                ep.tempCards.Add(cardId);
            Reject = null;
            return true;
        }

        public string ReputationLine()
        {
            int r = _state.sponsorReputation;
            string tier = r <= 20 ? "токсичный" : r <= 40 ? "сомнительный" : r <= 60 ? "надёжный" : r <= 80 ? "востребованный" : "любимчик";
            var ep = _state.episode;
            int active = 0;
            if (ep != null)
            {
                ep.EnsureLists();
                for (int i = 0; i < ep.contracts.Count; i++)
                {
                    if (ep.contracts[i].status == ContractStatus.Active)
                        active++;
                }
            }

            return "Репутация спонсоров " + r + " · " + tier
                   + "  ·  контракты " + active + "/" + Progression.ContractSlots(_state.writerLevel)
                   + (Reject != null ? "\n" + Reject : "");
        }

        // Начало съёмки (GDD 0.3): Deck → shuffle → Library → draw Hand. Разовые карты выпуска (магазин, спонсор)
        // сдаются первыми — за них уже заплачено. «Использовано» прошлой съёмки обнуляется: карты не сгорают.
        public bool TryEmbark(List<string> hand)
        {
            _state.EndSituation();
            _state.picked.Clear();
            hand.Clear();

            var deck = new List<string>();
            for (int i = 0; i < _state.owned.Count; i++)
            {
                string id = _state.owned[i];
                if (Find(id) != null && !deck.Contains(id))
                    deck.Add(id);
            }

            Shuffle(deck);
            var order = new List<string>();
            var ep = _state.episode;
            if (ep != null)
            {
                for (int i = 0; i < ep.tempCards.Count; i++)
                {
                    string id = ep.tempCards[i];
                    if (Find(id) == null || order.Contains(id))
                        continue;
                    deck.Remove(id);
                    order.Add(id);
                }
            }

            order.AddRange(deck);
#if UNITY_EDITOR
            // Мастерская карт → «Проверить в квартире»: эта карта первой в руке.
            string test = CardLibrary.TakeTestCard();
            if (!string.IsNullOrEmpty(test) && Find(test) != null)
            {
                if (!_state.owned.Contains(test))
                    _state.owned.Add(test);
                order.Remove(test);
                order.Insert(0, test);
            }
#endif
            // Урок съёмки: «Поджог» обязан прийти в первой руке.
            if (_state.wantsTutorial && _state.tutorialBeat < 4 && order.Remove(TutorialCard))
                order.Insert(0, TutorialCard);

            // Карта на участника без такой черты в касте не сдаётся: иначе «клик по Злому» лежит в руке впустую.
            var castIds = ep != null ? ep.cast : null;
            for (int i = order.Count - 1; i >= 0; i--)
            {
                if (!FitsCast(Find(order[i]), castIds))
                    order.RemoveAt(i);
            }

            int size = HandSize();
            for (int i = 0; i < order.Count; i++)
            {
                if (i < size)
                    hand.Add(order[i]);
                else
                    _state.library.Add(order[i]);
            }

            Reject = null;
            return true;
        }

        public const string TutorialCard = "fridge_fire";

        // Цель-участник с ограничением по черте играется, только если такая черта есть в касте.
        public static bool FitsCast(EventDefinition def, IList<string> castIds)
        {
            if (def == null)
                return false;
            if (def.PlayTarget != TargetType.Actor || !def.limitTrait)
                return true;
            if (castIds == null || castIds.Count == 0)
                return true;
            for (int i = 0; i < castIds.Count; i++)
            {
                var trait = MainTrait(castIds[i]);
                if (trait == def.targetTrait)
                    return true;
                if (def.targetTrait == TraitId.Panicker && trait == TraitId.Sentimental)
                    return true;
                if (def.targetTrait == TraitId.Sentimental && trait == TraitId.Panicker)
                    return true;
            }

            return false;
        }

        static TraitId MainTrait(string id)
        {
            var all = ContentLibrary.All<ActorDefinition>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].Id == id)
                    return all[i].mainTrait;
            }

            if (id == "npc_zloi")
                return TraitId.Aggressive;
            if (id == "npc_dobryak")
                return TraitId.Panicker;
            return TraitId.Sentimental;
        }

        static void Shuffle(List<string> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public void ClearReject()
        {
            Reject = null;
        }

        // Подсказка внизу окна колоды (например, «собери колоду перед съёмкой»).
        public void Note(string text)
        {
            Reject = text;
        }

        public EventDefinition Find(string id)
        {
            if (_catalog == null)
                return null;
            for (int i = 0; i < _catalog.Count; i++)
            {
                if (_catalog[i] != null && _catalog[i].id == id)
                    return _catalog[i];
            }

            return null;
        }

        public PrepModel BuildPrep()
        {
            int slots = HandSize();
            int available = DeckCount();
            string writers = "ур. " + _state.writerLevel + "\nконтрактов: " + Progression.ContractSlots(_state.writerLevel);
            int hype = Mathf.RoundToInt(Progression.HypeBonus(_state.castLevel) * 100f);
            int draw = Mathf.Min(slots, available);
            string slotsLabel = "В колоде " + available + ". На съёмке сдаётся " + draw
                                + (available > draw ? ". Сыграл — на место приходит следующая." : ".");
            if (_state.episode != null && _state.episode.tempCards.Count > 0)
                slotsLabel += "  ·  разовых карт выпуска: " + _state.episode.tempCards.Count + " (сдаются первыми)";

            return new PrepModel
            {
                episodeNumber = _state.episodeIndex + 1,
                money = _state.money,
                slots = slots,
                picked = 0,
                available = available,
                canStart = true,
                reject = Reject,
                moneyText = MoneyLine(),
                shopFooter = "Покупка открывает карту навсегда — она остаётся в колоде сезона.",
                slotsLabel = slotsLabel,
                crew = new[]
                {
                    CrewButtonOf("УЧАСТНИКИ", true, _state.castLevel, "ур. " + _state.castLevel + "\nмест: " + CastRoster.Seats(_state.castLevel) + "\nчек +" + hype + "%"),
                    CrewButtonOf("ОПЕРАТОРЫ", false, _state.operatorLevel, "ур. " + Mathf.Min(_state.operatorLevel, Progression.OperatorMaxLevel) + "\nза сцену: " + Progression.CaptureSlots(_state.operatorLevel) + (_state.operatorLevel >= 2 ? "\nтег в монтаже" : ""), Progression.OperatorMaxLevel),
                    CrewButtonOf("СЦЕНАРИСТЫ", false, _state.writerLevel, writers + "\n" + CategoryLine(_state.writerLevel))
                },
                deck = CollectCards(false),
                shop = CollectCards(true)
            };
        }

        public CrewInfo Crew(CrewTrack track)
        {
            int max = Progression.MaxFor(track);
            int level = Mathf.Min(Level(track), max);
            bool maxed = Level(track) >= max;
            int cost = Progression.UpgradeCost(track == CrewTrack.Cast, Level(track), max);
            var info = new CrewInfo
            {
                track = track,
                level = level,
                maxLevel = max,
                maxed = maxed,
                affordable = !maxed && _state.money >= cost,
                cost = maxed ? "—" : cost + " кр",
                upgradeLabel = maxed ? "МАКСИМУМ" : "УЛУЧШИТЬ ДО УРОВНЯ " + (level + 1)
            };

            int up = Mathf.Min(level + 1, max);
            switch (track)
            {
                case CrewTrack.Cast:
                    info.title = "КАСТИНГ";
                    info.description = "Места в касте и, с 3 уровня, подсказка скрытой черты. Чек растёт следом.";
                    info.now = CastLines(level);
                    info.next = CastLines(up);
                    break;
                case CrewTrack.Operators:
                    info.title = "СЪЁМОЧНАЯ";
                    info.description = "Слоты футажа на одну сцену. Старт — 5. Два апгрейда, каждый +1. В монтаже берёшь 3 кадра из всего, что снял за выпуск. Со 2 уровня виден общий тег соседних кадров.";
                    info.now = OpLines(level);
                    info.next = OpLines(up);
                    break;
                default:
                    info.title = "СЦЕНАРНАЯ";
                    info.description = "Новые категории карт и второй слот контракта на 4 уровне.";
                    info.now = WriterLines(level);
                    info.next = WriterLines(up);
                    break;
            }

            if (maxed)
                info.next = "Ветка прокачана полностью.";
            return info;
        }

        static string CastLines(int level)
        {
            int hype = Mathf.RoundToInt(Progression.HypeBonus(level) * 100f);
            return "• мест в касте: " + CastRoster.Seats(level)
                   + "\n• скрытая черта: " + (level >= 3 ? "видна в кастинге" : "закрыта")
                   + "\n• бонус к чеку: +" + hype + "%";
        }

        static string OpLines(int level)
        {
            return "• слотов за сцену: " + Progression.CaptureSlots(level)
                   + "\n• в эфир: " + Progression.AirSlots + " из всей библиотеки выпуска"
                   + "\n• монтаж: " + (level >= 2 ? "имя общего тега" : "только связка");
        }

        static string WriterLines(int level)
        {
            return "• " + CategoryLine(level)
                   + "\n• контрактов: " + Progression.ContractSlots(level);
        }

        static string CategoryLine(int level)
        {
            if (level >= 4)
                return "категории: все";
            if (level >= 3)
                return "категории: +разоблачение";
            if (level >= 2)
                return "категории: +социальные";
            return "категории: провокация, среда";
        }

        public StatsModel Stats()
        {
            return new StatsModel
            {
                rating = _state.rated > 0 ? (_state.ratingSum / (float)_state.rated).ToString("0.0") + " / 10" : "—",
                budget = MoneyLine(),
                episode = (_state.episodeIndex + 1).ToString(),
                drama = _tone != null ? _tone.Drama / (float)SeasonTone.Cap : 0f,
                trash = _tone != null ? _tone.Trash / (float)SeasonTone.Cap : 0f,
                family = _tone != null ? _tone.Family / (float)SeasonTone.Cap : 0f,
                dramaValue = _tone != null ? _tone.Drama : 0,
                trashValue = _tone != null ? _tone.Trash : 0,
                familyValue = _tone != null ? _tone.Family : 0
            };
        }

        public int CastLevel => _state.castLevel;

        public string ToneText()
        {
            if (_tone == null || _tone.Total <= 0)
                return "Тон сезона: пока не задан";
            return "Тон сезона:  "
                   + MoodStyle.Paint(MoodStyle.Short(ShowMood.Drama) + " " + _tone.Drama, ShowMood.Drama) + "   "
                   + MoodStyle.Paint(MoodStyle.Short(ShowMood.Trash) + " " + _tone.Trash, ShowMood.Trash) + "   "
                   + MoodStyle.Paint(MoodStyle.Short(ShowMood.Family) + " " + _tone.Family, ShowMood.Family);
        }

        public string SeasonEndText()
        {
            string tone = _tone != null && _tone.TryLead(out ShowMood lead)
                ? MoodStyle.Paint(MoodStyle.Full(lead), lead)
                : "ничья — концовку не выбрать";
            return "Тон сезона: " + tone + "\nБюджет: " + _state.money + " кр\nСами концовки напишем следом.";
        }

        public List<string> TaskLines()
        {
            var lines = new List<string>(_state.tasks.Count);
            for (int i = 0; i < _state.tasks.Count; i++)
                lines.Add(_state.tasks[i].label);
            return lines;
        }

        CrewButton CrewButtonOf(string title, bool cast, int level, string detail, int maxLevel = Progression.MaxLevel)
        {
            bool maxed = level >= maxLevel;
            int cost = Progression.UpgradeCost(cast, level, maxLevel);
            return new CrewButton
            {
                title = title,
                level = level,
                detail = detail,
                maxed = maxed,
                affordable = !maxed && _state.money >= cost,
                costLabel = maxed ? "МАКС" : "апгрейд " + cost + " кр"
            };
        }

        PrepCard[] CollectCards(bool shop)
        {
            var list = new List<PrepCard>();
            if (_catalog == null)
                return list.ToArray();
            for (int i = 0; i < _catalog.Count; i++)
            {
                var def = _catalog[i];
                if (def == null)
                    continue;
                bool owned = _state.Owns(def.id);
                if (!Dev && !Progression.CategoryOpen(def.category, _state.writerLevel))
                    continue;
                if (shop)
                {
                    if (owned || def.sponsor || def.price <= 0)
                        continue;
                }
                else if (!owned)
                {
                    continue;
                }

                var moods = new ShowMood[def.moods != null ? def.moods.Count : 0];
                for (int m = 0; m < moods.Length; m++)
                    moods[m] = def.moods[m];
                list.Add(new PrepCard
                {
                    id = def.id,
                    title = def.displayName,
                    hint = def.hint,
                    price = def.price,
                    picked = false,
                    affordable = _state.money >= def.price,
                    color = def.cardColor,
                    art = def.cardArt,
                    category = def.category,
                    moods = moods
                });
            }

            if (!shop && _state.episode != null)
            {
                var temps = _state.episode.tempCards;
                for (int i = 0; i < temps.Count; i++)
                {
                    if (Listed(list, temps[i]))
                        continue;
                    var def = Find(temps[i]);
                    if (def != null)
                        list.Add(CardOf(def, true, "нал"));
                }
            }

            return list.ToArray();
        }

        public PrepModel BuildRunShop()
        {
            var prep = BuildPrep();
            prep.moneyText = MoneyLine();
            prep.shopFooter = "Касса выпуска — деньги только этого выпуска, не кр сезона. Карты сгорят после эфира. Спонсор в кадре: больше кр, отзывы хуже.";
            prep.shop = CollectRun();
            return prep;
        }

        PrepCard[] CollectRun()
        {
            var list = new List<PrepCard>();
            var ep = _state.episode;
            if (_catalog == null || ep == null)
                return list.ToArray();
            for (int i = 0; i < _catalog.Count; i++)
            {
                var def = _catalog[i];
                if (def == null || def.sponsor || def.runPrice <= 0 || ep.tempCards.Contains(def.id))
                    continue;
                if (!def.sponsor && _state.Owns(def.id))
                    continue;
                list.Add(CardOf(def, false, "нал"));
            }

            return list.ToArray();
        }

        bool IsTemp(string id)
        {
            return _state.episode != null && _state.episode.tempCards.Contains(id);
        }

        string MoneyLine()
        {
            if (_state.episode == null)
                return _state.money + " кр";
            return _state.money + " кр   ·   касса выпуска " + _state.episode.cash;
        }

        PrepCard CardOf(EventDefinition def, bool inHand, string unit)
        {
            bool temp = IsTemp(def.id);
            int price = unit == "нал" ? def.runPrice : def.price;
            var moods = new ShowMood[def.moods != null ? def.moods.Count : 0];
            for (int m = 0; m < moods.Length; m++)
                moods[m] = def.moods[m];
            return new PrepCard
            {
                id = def.id,
                title = def.displayName,
                hint = def.sponsor ? "спонсор · " + def.hint : def.hint,
                price = price,
                unit = unit,
                temporary = temp,
                picked = inHand,
                affordable = unit == "нал"
                    ? _state.episode != null && _state.episode.cash >= price
                    : _state.money >= price,
                color = def.cardColor,
                art = def.cardArt,
                category = def.category,
                moods = moods
            };
        }

        static bool Listed(List<PrepCard> list, string id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].id == id)
                    return true;
            }

            return false;
        }
    }
}
