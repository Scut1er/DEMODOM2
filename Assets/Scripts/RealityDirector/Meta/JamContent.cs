using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Минимум контента, чтобы пулы можно было проверить без ручных ассетов.
    public static class JamContent
    {
        public static void Fill(List<RoomDefinition> rooms, List<RoomDefinition> created)
        {
            var events = OwmEvents.All();
            for (int i = 0; i < events.Length; i++)
                Keep(rooms, created, events[i]);

            int situations = Count(rooms, RoomType.Situation);

            if (situations < 4)
            {
                var pack = Situations();
                for (int i = 0; i < pack.Length; i++)
                    Keep(rooms, created, pack[i]);
            }
        }

        public static List<MarketingOffer> Offers(int reputation)
        {
            return OwmOffers.All(reputation);
        }

        static void Keep(List<RoomDefinition> rooms, List<RoomDefinition> created, RoomDefinition room)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i] != null && rooms[i].Id == room.Id)
                {
                    if (Application.isPlaying)
                        Object.Destroy(room);
                    return;
                }
            }

            room.hideFlags = HideFlags.DontSave;
            rooms.Add(room);
            created.Add(room);
        }

        static int Count(List<RoomDefinition> rooms, RoomType type)
        {
            int n = 0;
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i] != null && rooms[i].Type == type && !rooms[i].Id.StartsWith("placeholder"))
                    n++;
            }

            return n;
        }

        static EventRoomDefinition[] Events()
        {
            return new[]
            {
                Ev("event_rumor", "Слух из гримёрки", "Кто-то уже знает. Вопрос, узнают ли остальные до съёмки.",
                    Ch("Слить в общий чат", "Слух пошёл. На площадке будет давление.", Flag("RumorSpread"), Tag("Rumor"), Mod("stress", 12)),
                    Ch("Разобраться тихо", "Секрет остался секретом. Пока.", Flag("SecretKept")),
                    Ch("Исповедь в камеру", "В руку падает разовая исповедь.", Card("confession_cam"), Mod("stress", 6))),
                Ev("event_bar", "Мини-бар", "Площадка открыла бар раньше вызова.",
                    Ch("Оставить открытым", "Самоконтроль просядет к следующей съёмке.", Mod("stress", 8), Mod("anger", 6), Tag("Alcohol")),
                    Ch("Закрыть", "Трезво и скучно. Босс не в восторге.", Flag("BarClosed")),
                    Ch("Тост за шоу", "Все выпили. Конфликт станет ближе.", Mod("anger", 10), Tag("Alcohol"))),
                Ev("event_call", "Звонок из дома", "Одному из участников звонят родные.",
                    Ch("Дать трубку", "Разговор тёплый. Грусть останется на площадке.", Mod("sadness", 14), Tag("Family")),
                    Ch("Сбросить", "Человек закрылся. Злость копится.", Mod("anger", 10), Flag("CallDropped")),
                    Ch("Поставить на громкую", "Все слышали. Это уже материал, если снимете.", Mod("stress", 10), Tag("Reveal"))),
                Ev("event_vote", "Ночной совет", "Участники хотят выгнать кого-то ещё до эфира.",
                    Ch("Разрешить голосование", "Лагеря оформились.", Flag("VoteHeld"), Mod("anger", 12)),
                    Ch("Запретить", "Копили злость молча.", Mod("stress", 8)),
                    Ch("Подсказать жертву", "Ты ткнул пальцем. Это запомнят.", Flag("ProducerPicked"), Mod("hostility", 15))),
                Ev("event_gift", "Посылка", "На проходной коробка без обратного адреса.",
                    Ch("Открыть на камеру", "Внутри чужие вещи. К следующей съёмке — повод.", Tag("Secret"), Mod("stress", 6)),
                    Ch("Выбросить", "Момент умер в мусорке.", Flag("GiftTrashed")),
                    Ch("Отдать тихо", "Кто-то должен. Доверие или подстава — на площадке.", Card("confession_cam"))),
                Ev("event_leak", "Слив в чат спонсора", "Бренд увидел черновой кадр.",
                    Ch("Извиниться", "Репутация не просела. Пока.", Flag("SponsorCalmed")),
                    Ch("Сделать вид", "Они заметили. Следующий контракт будет злее.", Mod("stress", 4), Tag("SponsorRisk")),
                    Ch("Пообещать интеграцию", "В руку падает рекламная карта.", Card("sponsor_energy"))),
                Ev("event_fight_talk", "Разбор полётов", "После прошлой стычки люди не разговаривают.",
                    Ch("Свести", "Им придётся стоять рядом.", Mod("anger", 8), Flag("ForcedTalk")),
                    Ch("Развести", "Тишина. Связки для монтажа меньше.", Mod("sadness", 6)),
                    Ch("Спросить, кто начал", "Кто-то станет виноватым.", Mod("hostility", 10), Tag("Blame"))),
                Ev("event_guest", "Гость на пороге", "Продюсерский гость уже в лифте.",
                    Ch("Впустить", "На площадке лишний человек и лишний конфликт.", Flag("GuestIn"), Mod("stress", 10)),
                    Ch("Не пускать", "Скучнее, зато свои.", Flag("GuestOut")),
                    Ch("Пустить только в кухню", "Короткий визит. Снимите, если успеете.", Flag("GuestIn"), Mod("anger", 4)))
            };
        }

        static SituationRoomDefinition[] Situations()
        {
            return new[]
            {
                Sit("situation_cold", "Холодное открытие", "Первый заход. Никто ещё не обязан взрываться.", "инициатор — вспыльчивый, свидетель — кто угодно"),
                Sit("situation_kitchen", "Ночная кухня", "Холодильник, еда, чужие нервы.", "цель — тот, кто паникует от огня"),
                Sit("situation_after", "Разбор после конфликта", "Если до этого копилась злость — она здесь."),
                Sit("situation_confession", "Исповедальная", "Камера близко, оправданий мало.", "цель — сентиментальный или паникёр")
            };
        }

        static SituationRoomDefinition Sit(string id, string title, string description, string roles = "")
        {
            var room = ScriptableObject.CreateInstance<SituationRoomDefinition>();
            room.name = id;
            room.SetId(id);
            room.title = title;
            room.subtitle = "СЪЁМКА";
            room.description = description;
            room.roleBrief = roles;
            room.scene = SceneFlow.Episode;
            room.weight = 1f;
            room.icon = MapNodeKind.Scene;
            room.color = new Color(0.45f, 0.28f, 0.32f, 1f);
            return room;
        }

        static EventRoomDefinition Ev(string id, string title, string body, params EventChoice[] choices)
        {
            var room = ScriptableObject.CreateInstance<EventRoomDefinition>();
            room.name = id;
            room.SetId(id);
            room.title = title;
            room.subtitle = "СОБЫТИЕ";
            room.description = body;
            room.body = body;
            room.weight = 1f;
            room.icon = MapNodeKind.Mystery;
            room.color = new Color(0.28f, 0.32f, 0.48f, 1f);
            room.choices = new List<EventChoice>(choices);
            return room;
        }

        static EventChoice Ch(string label, string result, params Effect[] effects)
        {
            return new EventChoice { label = label, resultText = result, effects = new List<Effect>(effects) };
        }

        static Effect Flag(string key)
        {
            return new Effect { type = EffectType.SetEpisodeFlag, key = key };
        }

        static Effect Tag(string key)
        {
            return new Effect { type = EffectType.AddNarrativeTag, key = key };
        }

        static Effect Card(string id)
        {
            return new Effect { type = EffectType.AddTempCard, key = id };
        }

        static Effect Mod(string key, int value)
        {
            return new Effect { type = EffectType.NextRoomModifier, key = key, value = value };
        }

        static MarketingOffer Buy(string id, string title, string blurb, int price, int rep)
        {
            return new MarketingOffer { cardId = id, title = title, blurb = blurb, kind = OfferKind.Purchase, price = price, minReputation = rep };
        }

        static MarketingOffer Deal(string id, string title, string blurb, int rep, int pay, int hit)
        {
            return new MarketingOffer
            {
                cardId = id,
                title = title,
                blurb = blurb,
                kind = OfferKind.Contract,
                minReputation = rep,
                payout = pay,
                scoreHit = hit
            };
        }
    }
}
