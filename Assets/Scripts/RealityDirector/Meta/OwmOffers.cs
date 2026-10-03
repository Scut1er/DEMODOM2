using System.Collections.Generic;

namespace RealityDirector.Meta
{
    public static class OwmOffers
    {
        public static List<MarketingOffer> All(int reputation)
        {
            var all = new[]
            {
                new MarketingOffer { id = "MKT_001", cardId = "CARD_SP_002", title = "Холодильник Hell Cola", blurb = "Hell Cola. Поставьте брендированный холодильник в Situation и покажите продукт в эфире.", kind = OfferKind.Contract, price = 0, minReputation = 0, payout = 120, scoreHit = 2, flag = "" },
                new MarketingOffer { id = "MKT_002", cardId = "CARD_SP_001", title = "Leviathan в кадре", blurb = "Inferno Shipyards. Премиальный постер нового адского лайнера. Чем драматичнее сцена рядом — тем лучше.", kind = OfferKind.Contract, price = 0, minReputation = 20, payout = 150, scoreHit = 2, flag = "" },
                new MarketingOffer { id = "MKT_003", cardId = "CARD_SP_003", title = "Кредит на эмоции", blurb = "Brimstone Bank. Банк хочет услышать название бренда в момент конфликта.", kind = OfferKind.Contract, price = 0, minReputation = 10, payout = 110, scoreHit = 1, flag = "" },
                new MarketingOffer { id = "MKT_004", cardId = "TEMP_SP_PIZZA", title = "Пицца примирения", blurb = "Purgatory Pizza. Доставьте пиццу в комнату и добейтесь, чтобы минимум два актёра оказались в кадре рядом с ней.", kind = OfferKind.Contract, price = 0, minReputation = 0, payout = 90, scoreHit = 1, flag = "" },
                new MarketingOffer { id = "MKT_005", cardId = "TEMP_SP_SOULCLOUD", title = "Резервное копирование души", blurb = "SoulCloud. Облачный сервис хочет ассоциироваться с reveal: бренд должен быть в кадре во время раскрытия секрета.", kind = OfferKind.Contract, price = 0, minReputation = 35, payout = 180, scoreHit = 3, flag = "" },
                new MarketingOffer { id = "MKT_006", cardId = "TEMP_SP_PITFIT", title = "Фитнес после скандала", blurb = "PitFit. Снимите участника в движении рядом с брендированным спортивным реквизитом.", kind = OfferKind.Contract, price = 0, minReputation = 0, payout = 100, scoreHit = 1, flag = "" },
                new MarketingOffer { id = "MKT_007", cardId = "TEMP_SP_BOX", title = "Доставка к кульминации", blurb = "Damnazon. Посылка должна попасть в кадр и стать частью значимого события.", kind = OfferKind.Contract, price = 0, minReputation = 15, payout = 130, scoreHit = 2, flag = "" },
                new MarketingOffer { id = "MKT_008", cardId = "TEMP_SP_INSURANCE", title = "Страховка от всего", blurb = "Eternal Insurance. Страховая хочет быть в кадре во время ProductionDamage или паники.", kind = OfferKind.Contract, price = 0, minReputation = 25, payout = 160, scoreHit = 2, flag = "" },
                new MarketingOffer { id = "MKT_009", cardId = "TEMP_SP_NETHERNET", title = "Связь даже после предательства", blurb = "NetherNet. Бренд связи требует footage с двумя актёрами и Relationship event.", kind = OfferKind.Contract, price = 0, minReputation = 5, payout = 115, scoreHit = 1, flag = "" },
                new MarketingOffer { id = "MKT_010", cardId = "TEMP_SP_SCREEN", title = "Премьера внутри премьеры", blurb = "SinCinema. Покажите брендированный экран/постер в любом technically good footage.", kind = OfferKind.Contract, price = 0, minReputation = 0, payout = 85, scoreHit = 1, flag = "" },
                new MarketingOffer { id = "MKT_011", cardId = "", title = "Дополнительный HellToken пакет", blurb = "Разово увеличивает бюджет HellToken следующей Situation.", kind = OfferKind.Purchase, price = 35, minReputation = 0, payout = 0, scoreHit = 0, flag = "HellTokenPack" },
                new MarketingOffer { id = "MKT_012", cardId = "", title = "Срочный монтажный совет", blurb = "Показывает qualitative preview лучшей связи между двумя footage в Montage.", kind = OfferKind.Purchase, price = 25, minReputation = 0, payout = 0, scoreHit = 0, flag = "MontageHint" },
                new MarketingOffer { id = "MKT_013", cardId = "", title = "Запасной микрофон", blurb = "Следующий footage не получает penalty за плохой звук.", kind = OfferKind.Purchase, price = 30, minReputation = 0, payout = 0, scoreHit = 0, flag = "TechFloor" },
                new MarketingOffer { id = "MKT_014", cardId = "", title = "Дополнительный слот футажа", blurb = "На следующей Situation +1 capture slot.", kind = OfferKind.Purchase, price = 45, minReputation = 0, payout = 0, scoreHit = 0, flag = "ExtraCaptureSlot" },
                new MarketingOffer { id = "MKT_015", cardId = "", title = "Подкупить ассистента", blurb = "Перед следующей Situation посмотреть верхние 3 карты Library и выбрать порядок.", kind = OfferKind.Purchase, price = 40, minReputation = 0, payout = 0, scoreHit = 0, flag = "PeekLibrary" },
                new MarketingOffer { id = "MKT_016", cardId = "", title = "Реквизит со скидкой", blurb = "Следующая Environment Card стоит на $0.75 HellToken дешевле.", kind = OfferKind.Purchase, price = 30, minReputation = 0, payout = 0, scoreHit = 0, flag = "EnvDiscount" },
                new MarketingOffer { id = "MKT_017", cardId = "", title = "Второй шанс", blurb = "После следующего failed Event check один раз разрешить reroll.", kind = OfferKind.Purchase, price = 50, minReputation = 0, payout = 0, scoreHit = 0, flag = "EventReroll" },
                new MarketingOffer { id = "MKT_018", cardId = "", title = "Мониторинг HellTube", blurb = "Перед эфиром показывает, какой tone сейчас доминирует в Final Cut.", kind = OfferKind.Purchase, price = 20, minReputation = 0, payout = 0, scoreHit = 0, flag = "ToneForecast" },
                new MarketingOffer { id = "MKT_019", cardId = "CARD_REVEAL_001", title = "Гримёрный компромат", blurb = "Получить одну случайную temp Reveal/Secret карту на текущий Episode.", kind = OfferKind.Purchase, price = 40, minReputation = 0, payout = 0, scoreHit = 0, flag = "" },
                new MarketingOffer { id = "MKT_020", cardId = "", title = "Контракт без мелкого шрифта", blurb = "Заплатить сейчас, чтобы убрать штраф Sponsor Reputation за один проваленный контракт в этом Episode.", kind = OfferKind.Purchase, price = 60, minReputation = 0, payout = 0, scoreHit = 0, flag = "SponsorShield" },
            };
            var list = new List<MarketingOffer>();
            for (int i = 0; i < all.Length; i++)
            {
                if (reputation >= all[i].minReputation)
                    list.Add(all[i]);
            }
            return list;
        }
    }
}
