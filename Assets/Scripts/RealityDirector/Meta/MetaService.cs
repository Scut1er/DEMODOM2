using System.Collections.Generic;
using RealityDirector.Core;
using RealityDirector.Events;
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

        public int SlotsNow()
        {
            return Progression.EventSlots(_state.writerLevel, _state.episodeIndex);
        }

        public void TrimPicked()
        {
            int slots = SlotsNow();
            for (int i = _state.picked.Count - 1; i >= 0; i--)
            {
                if (!_state.Owns(_state.picked[i]) || _state.IsPlayed(_state.picked[i]))
                    _state.picked.RemoveAt(i);
            }

            while (_state.picked.Count > slots)
                _state.picked.RemoveAt(_state.picked.Count - 1);
        }

        // Добирает колоду до лимита — для запуска квартиры напрямую из редактора.
        public void AutoPick()
        {
            TrimPicked();
            int slots = SlotsNow();
            for (int i = 0; i < _state.owned.Count && _state.picked.Count < slots; i++)
            {
                string id = _state.owned[i];
                if (!_state.IsPlayed(id) && !_state.picked.Contains(id))
                    _state.picked.Add(id);
            }
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
            int cost = Progression.UpgradeCost(track == CrewTrack.Cast, level);
            if (level >= Progression.MaxLevel || cost <= 0 || _state.money < cost)
            {
                Reject = level >= Progression.MaxLevel ? "Уже максимум." : "Не хватает кр.";
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

        public bool TogglePick(string id)
        {
            if (IsTemp(id))
                return true;
            if (_state.IsPlayed(id) || !_state.Owns(id))
                return false;
            if (_state.picked.Contains(id))
            {
                _state.picked.Remove(id);
                Reject = null;
                return true;
            }

            if (_state.picked.Count >= SlotsNow())
            {
                Reject = "Слоты заняты — сними одну карту.";
                return false;
            }

            _state.picked.Add(id);
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
                Reject = "Не хватает нала.";
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

        // Снимать можно с любым числом выбранных карт — от нуля до лимита слотов.
        public bool CanStart()
        {
            return _state.picked.Count <= SlotsNow();
        }

        // Переносит выбранные карты в руку сессии. Неиспользованные вернутся в колоду.
        public bool TryEmbark(List<string> hand)
        {
            if (!CanStart())
                return false;
            hand.Clear();
            hand.AddRange(_state.picked);
            _state.picked.Clear();
            var ep = _state.episode;
            if (ep != null)
            {
                for (int i = 0; i < ep.tempCards.Count; i++)
                {
                    if (!hand.Contains(ep.tempCards[i]))
                        hand.Add(ep.tempCards[i]);
                }
            }

            Reject = null;
            return true;
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
            int slots = SlotsNow();
            int available = _state.UnplayedCount();
            int later = Progression.EventSlots(_state.writerLevel, 1);
            string writers = _state.episodeIndex <= 0
                ? "ур. " + _state.writerLevel + "\nсейчас 1 карта\nдальше " + later
                : "ур. " + _state.writerLevel + "\nкарт в серию: " + later;
            int hype = Mathf.RoundToInt(Progression.HypeBonus(_state.castLevel) * 100f);
            string slotsLabel = _state.episodeIndex <= 0
                ? "Обучение: в серию берётся 1 карта. Со следующей серии слоты от сценаристов, минимум 2."
                : "Карт в серию: " + slots + "  ·  несыгранных в колоде: " + available;
            if (_state.episode != null && _state.episode.tempCards.Count > 0)
                slotsLabel += "  ·  из магазина выпуска уже в руке: " + _state.episode.tempCards.Count;

            return new PrepModel
            {
                episodeNumber = _state.episodeIndex + 1,
                money = _state.money,
                slots = slots,
                picked = _state.picked.Count,
                available = available,
                canStart = CanStart(),
                reject = Reject,
                moneyText = MoneyLine(),
                shopFooter = "Покупка открывает карту навсегда — она остаётся в колоде сезона.",
                slotsLabel = slotsLabel,
                crew = new[]
                {
                    CrewButtonOf("УЧАСТНИКИ", true, _state.castLevel, "ур. " + _state.castLevel + "\nмест: " + CastRoster.Seats(_state.castLevel) + "\nчек +" + hype + "%"),
                    CrewButtonOf("ОПЕРАТОРЫ", false, _state.operatorLevel, "ур. " + _state.operatorLevel + "\nкадров: " + Progression.CaptureSlots(_state.operatorLevel) + (_state.operatorLevel >= 2 ? "\nтег в монтаже" : "")),
                    CrewButtonOf("СЦЕНАРИСТЫ", false, _state.writerLevel, writers + "\n" + CategoryLine(_state.writerLevel))
                },
                deck = CollectCards(false),
                shop = CollectCards(true)
            };
        }

        public CrewInfo Crew(CrewTrack track)
        {
            int level = Level(track);
            bool maxed = level >= Progression.MaxLevel;
            int cost = Progression.UpgradeCost(track == CrewTrack.Cast, level);
            var info = new CrewInfo
            {
                track = track,
                level = level,
                maxed = maxed,
                affordable = !maxed && _state.money >= cost,
                cost = maxed ? "—" : cost + " кр",
                upgradeLabel = maxed ? "МАКСИМУМ" : "УЛУЧШИТЬ ДО УРОВНЯ " + (level + 1)
            };

            int up = Mathf.Min(level + 1, Progression.MaxLevel);
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
                    info.description = "Сколько клипов влезает в выпуск. Со 2 уровня монтаж показывает общий тег соседних кадров.";
                    info.now = OpLines(level);
                    info.next = OpLines(up);
                    break;
                default:
                    info.title = "СЦЕНАРНАЯ";
                    info.description = "Новые категории карт и второй слот контракта на 4 уровне. Число карт в серию — следом.";
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
            return "• клипов за выпуск: " + Progression.CaptureSlots(level)
                   + "\n• монтаж: " + (level >= 2 ? "имя общего тега" : "только связка");
        }

        static string WriterLines(int level)
        {
            return "• карт в серию: " + Progression.EventSlots(level, 1)
                   + "\n• " + CategoryLine(level)
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

        CrewButton CrewButtonOf(string title, bool cast, int level, string detail)
        {
            bool maxed = level >= Progression.MaxLevel;
            int cost = Progression.UpgradeCost(cast, level);
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
                if (!Progression.CategoryOpen(def.category, _state.writerLevel))
                    continue;
                if (shop)
                {
                    if (owned || def.sponsor || def.price <= 0)
                        continue;
                }
                else if (!owned || _state.IsPlayed(def.id))
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
                    picked = _state.picked.Contains(def.id),
                    affordable = _state.money >= def.price,
                    color = def.cardColor,
                    art = def.cardArt,
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
            prep.shopFooter = "Только до эфира этого выпуска, потом сгорят. Спонсор в кадре: больше кр, отзывы хуже.";
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
            return _state.money + " кр   ·   " + _state.episode.cash + " нал";
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
                picked = inHand || _state.picked.Contains(def.id),
                affordable = unit == "нал"
                    ? _state.episode != null && _state.episode.cash >= price
                    : _state.money >= price,
                color = def.cardColor,
                art = def.cardArt,
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
