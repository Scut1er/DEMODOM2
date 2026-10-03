using System;
using System.Collections.Generic;
using RealityDirector.Events;

namespace RealityDirector.Meta
{
    // Что показать на витрине маркетинга и как объяснить каждое предложение игроку:
    // что получишь, сколько стоит, сколько действует, почему нельзя; у контракта — бренд, задача, карта,
    // выплата, последствия успеха и провала, требование репутации. Без UI — его рисует MarketingView.
    public class OfferView
    {
        public MarketingOffer offer;
        public bool contract;
        public string title;
        public string brand;
        public string gets;
        public string task;
        public string card;
        public string price;
        public string lifetime;
        public string success;
        public string fail;
        public string requirement;
        public bool available;
        public bool done;
        public string reason;
    }

    public static class MarketingDesk
    {
        // Флаги покупок, которые игра исполняет (иначе предложение не продаётся — нельзя продать пустышку).
        static readonly Dictionary<string, (string gets, string lifetime)> Flags = new Dictionary<string, (string, string)>
        {
            { "HellTokenPack", ("+$2 HellToken на следующую съёмку", "одна съёмка") },
            { "ExtraCaptureSlot", ("+1 слот футажа на следующей съёмке", "одна съёмка") },
            { "EnvDiscount", ("следующая карта окружения дешевле на $0.75", "до первой такой карты") },
            { "EventReroll", ("после провала в событии — второй бросок", "до первого провала") },
            { "SponsorShield", ("проваленный контракт не снизит репутацию", "до эфира") },
            { "PeekLibrary", ("на следующей съёмке три сильнейшие карты колоды придут в руку первыми", "одна съёмка") },
            { "TechFloor", ("первый кадр следующей съёмки — повышенного качества", "одна съёмка") },
            { "MontageHint", ("в монтаже — совет: лучшая пара кадров", "до эфира") },
            { "ToneForecast", ("перед эфиром — точный прогноз оценки", "до эфира") }
        };

        public static bool Supports(string flag)
        {
            return string.IsNullOrEmpty(flag) || Flags.ContainsKey(flag);
        }

        // Комната показывает часть своих предложений: выбор детерминирован (узел + сид карты), поэтому
        // одна и та же комната на карте не меняет витрину при возврате и после загрузки.
        public static List<MarketingOffer> Pick(MarketingRoomDefinition room, List<MarketingOffer> fallback, int seed)
        {
            var source = room != null && room.offers != null && room.offers.Count > 0 ? room.offers : fallback;
            var buys = new List<MarketingOffer>();
            var deals = new List<MarketingOffer>();
            foreach (var o in source ?? new List<MarketingOffer>())
            {
                if (o == null)
                    continue;
                (o.kind == OfferKind.Contract ? deals : buys).Add(o);
            }

            var rng = new Random(seed);
            Trim(buys, room != null ? room.purchasesShown : 0, rng);
            Trim(deals, room != null ? room.contractsShown : 0, rng);
            buys.AddRange(deals);
            return buys;
        }

        static void Trim(List<MarketingOffer> list, int keep, Random rng)
        {
            if (keep <= 0 || list.Count <= keep)
                return;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            list.RemoveRange(keep, list.Count - keep);
        }

        public static OfferView Describe(MarketingOffer o, SeasonState season, EpisodeState ep, int contractSlots, Func<string, EventDefinition> find)
        {
            var v = new OfferView { offer = o, contract = o.kind == OfferKind.Contract, title = o.title };
            var card = string.IsNullOrEmpty(o.cardId) ? null : find?.Invoke(o.cardId);
            v.card = card != null ? card.displayName : o.cardId;
            int reputation = season != null ? season.sponsorReputation : 0;

            if (v.contract)
            {
                Split(o.blurb, out v.brand, out v.task);
                if (!string.IsNullOrEmpty(o.brand))
                    v.brand = o.brand;
                v.gets = "карта «" + v.card + "» до эфира";
                v.price = "+" + o.payout + " ЕБ";
                v.success = "+" + o.payout + " ЕБ, репутация +12";
                v.fail = "репутация −15";
                v.lifetime = "кадр с брендом должен попасть в эфир" + (o.scoreHit > 0 ? " · отзывы −" + o.scoreHit : "");
                v.requirement = o.minReputation > 0 ? "репутация " + o.minReputation + "+ (у вас " + reputation + ")" : "";
                v.done = ep != null && ep.contracts != null && ep.contracts.Exists(c => c != null && c.grantedCardId == o.cardId);
                int active = ep != null && ep.contracts != null ? ep.contracts.FindAll(c => c != null && c.status == ContractStatus.Active).Count : 0;
                if (v.done)
                    v.reason = "принят";
                else if (reputation < o.minReputation)
                    v.reason = "нужна репутация " + o.minReputation;
                else if (active >= contractSlots)
                    v.reason = "слоты контрактов заняты (" + contractSlots + ")";
                else if (card == null)
                    v.reason = "карты бренда нет";
            }
            else
            {
                if (card != null)
                {
                    v.gets = "карта «" + card.displayName + "» в колоду выпуска";
                    v.lifetime = "до эфира этого выпуска";
                }
                else if (!string.IsNullOrEmpty(o.flag) && Flags.TryGetValue(o.flag, out var f))
                {
                    v.gets = f.gets;
                    v.lifetime = f.lifetime;
                }
                else
                {
                    v.gets = o.blurb;
                    v.lifetime = "";
                }

                v.task = o.blurb;
                v.price = o.price + " УЕ";
                string bought = "bought_" + (string.IsNullOrEmpty(o.id) ? o.title : o.id);
                v.done = ep != null && ep.HasFlag(bought);
                int cash = ep != null ? ep.cash : 0;
                if (v.done)
                    v.reason = "куплено";
                else if (!Supports(o.flag))
                    v.reason = "пока недоступно";
                else if (!string.IsNullOrEmpty(o.cardId) && card == null)
                    v.reason = "карты нет";
                else if (!string.IsNullOrEmpty(o.cardId) && ep != null && ep.tempCards.Contains(o.cardId))
                    v.reason = "карта уже в выпуске";
                else if (o.price > cash)
                    v.reason = "не хватает " + (o.price - cash) + " УЕ";
            }

            v.available = string.IsNullOrEmpty(v.reason);
            return v;
        }

        // «Hell Cola. Поставьте холодильник…» → бренд «Hell Cola», задача — остальное.
        static void Split(string blurb, out string brand, out string task)
        {
            brand = "";
            task = blurb ?? "";
            if (string.IsNullOrEmpty(blurb))
                return;
            int dot = blurb.IndexOf(". ", StringComparison.Ordinal);
            if (dot > 0 && dot <= 28)
            {
                brand = blurb.Substring(0, dot);
                task = blurb.Substring(dot + 2);
            }
        }
    }
}
