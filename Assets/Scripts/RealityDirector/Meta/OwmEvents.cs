using System.Collections.Generic;
using RealityDirector.Core;
using UnityEngine;

namespace RealityDirector.Meta
{
    public static class OwmEvents
    {
        public static EventRoomDefinition[] All()
        {
            return new[]
            {
                Ev("evt_001", "Звонок из ада", "Продакшен / риск", "Телефон в аппаратной раскалён докрасна. Голос в трубке не моргает: «Нам нужен скандал. Сегодня. Иначе контракт сгорит — вместе с вами».", 7f, true,
                    new[] { new Condition { type = ConditionType.EpisodeAtLeast, value = 1, key = "" } },
                    new[] { "Production", "Board", "Risk" },
                    Ch("Пообещать скандал", 60, "+80 кр; Trash +6; EpisodeFlag: BoardHappy", "Stress каста +2; SponsorRep -3", new Effect[] { new Effect { type = EffectType.Budget, value = 80, mood = (ShowMood)0, key = "" }, new Effect { type = EffectType.Tone, value = 6, mood = (ShowMood)1, key = "" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "BoardHappy" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "BoardHappy" } }, new Effect[] { new Effect { type = EffectType.NextRoomModifier, value = 2, mood = (ShowMood)0, key = "stress" } }),
                    Ch("Продать эфир спонсору", 100, "+55 кр; получить временную Sponsor-карту; Family -2", "", new Effect[] { new Effect { type = EffectType.Budget, value = 55, mood = (ShowMood)0, key = "" }, new Effect { type = EffectType.Tone, value = -2, mood = (ShowMood)2, key = "" }, new Effect { type = EffectType.AddTempCard, value = 0, mood = (ShowMood)0, key = "CARD_SP_001" } }, new Effect[] {  }),
                    Ch("Бросить трубку", 100, "Confidence продюсера +1; BoardAngry flag; след. Event weight конфликтов +20%", "", new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "BoardAngry" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "BoardAngry" } }, new Effect[] {  })
                ),
                Ev("evt_002", "Утечка переписки", "Секрет / reveal", "В общий чат команды случайно улетает скриншот личной переписки одного из участников. Пока никто из каста не видел.", 10f, true,
                    new[] { new Condition { type = ConditionType.CastAtLeast, value = 2, key = "" } },
                    new[] { "Secret", "Reveal", "Rumour" },
                    Ch("Слить участникам", 75, "RumorSpread; выбранный актёр Stress +3; Reveal setup", "Палево: Trust к продюсеру -2", new Effect[] { new Effect { type = EffectType.NextRoomModifier, value = 3, mood = (ShowMood)0, key = "stress" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "RumorSpread" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "RumorSpread" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Reveal" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Reveal" } }, new Effect[] {  }),
                    Ch("Удалить и замять", 100, "Budget -15; Secret flag; Trust +1", "", new Effect[] { new Effect { type = EffectType.Budget, value = -15, mood = (ShowMood)0, key = "" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Secret" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Secret" } }, new Effect[] {  }),
                    Ch("Сохранить на потом", 100, "Получить temp-карту «Показать переписку»; ProducerRisk +1", "", new Effect[] { new Effect { type = EffectType.AddTempCard, value = 0, mood = (ShowMood)0, key = "CARD_REVEAL_001" } }, new Effect[] {  })
                ),
                Ev("evt_003", "Бывший у ворот", "Романтика / конфликт", "Охрана сообщает: бывший партнёр одного из участников стоит у студии и требует «пять минут поговорить».", 6f, true,
                    null,
                    new[] { "Romance", "Ex", "Conflict" },
                    Ch("Впустить в эфирный день", 70, "Добавить ExActor guest на следующую Situation; Drama +4", "Гость срывается: Stress всем +2", new Effect[] { new Effect { type = EffectType.Tone, value = 4, mood = (ShowMood)0, key = "" } }, new Effect[] { new Effect { type = EffectType.NextRoomModifier, value = 2, mood = (ShowMood)0, key = "stress" } }),
                    Ch("Записать исповедь отдельно", 100, "Temp Confession card; Reveal tag", "", new Effect[] { new Effect { type = EffectType.AddTempCard, value = 0, mood = (ShowMood)0, key = "confession_cam" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Reveal" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Reveal" } }, new Effect[] {  }),
                    Ch("Не пускать", 100, "Family +2; выбранный актёр Trust +1", "", new Effect[] { new Effect { type = EffectType.Tone, value = 2, mood = (ShowMood)2, key = "" } }, new Effect[] {  })
                ),
                Ev("evt_004", "Пропавший микрофон", "Продакшен / комедия", "Перед съёмкой исчез беспроводной микрофон. В последний раз его видели рядом с самым любопытным участником.", 8f, false,
                    null,
                    new[] { "Production", "Comedy", "Theft" },
                    Ch("Обвинить участника", 55, "Stress цели +2; Suspicious; при успехе найти микрофон", "Неверное обвинение: Trust -2", new Effect[] { new Effect { type = EffectType.NextRoomModifier, value = 2, mood = (ShowMood)0, key = "stress" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Suspicious" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Suspicious" } }, new Effect[] {  }),
                    Ch("Купить новый", 100, "Budget -35; TechnicalQuality next Situation +1", "", new Effect[] { new Effect { type = EffectType.Budget, value = -35, mood = (ShowMood)0, key = "" } }, new Effect[] {  }),
                    Ch("Снимать на запасной", 100, "Budget без изменений; next footage technicalQuality -1", "", new Effect[] {  }, new Effect[] {  })
                ),
                Ev("evt_005", "Скандал в гримёрке", "Конфликт", "Два участника спорят из-за того, кто занял зеркало и кто «украл образ». До камеры дело ещё не дошло.", 10f, false,
                    new[] { new Condition { type = ConditionType.CastAtLeast, value = 2, key = "" } },
                    new[] { "Conflict", "Backstage" },
                    Ch("Подлить масла", 100, "Hostility между парой +2; Conflict setup", "", new Effect[] { new Effect { type = EffectType.NextRoomModifier, value = 2, mood = (ShowMood)0, key = "hostility" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Conflict" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Conflict" } }, new Effect[] {  }),
                    Ch("Развести по разным комнатам", 100, "Stress -1 обоим; следующий private setup", "", new Effect[] { new Effect { type = EffectType.NextRoomModifier, value = -1, mood = (ShowMood)0, key = "stress" } }, new Effect[] {  }),
                    Ch("Позвать камеру позже", 100, "AddEpisodeFlag: GroomingRoomConflict; next Situation starts with conflict event", "", new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "GroomingRoomConflict" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "GroomingRoomConflict" } }, new Effect[] {  })
                ),
                Ev("evt_006", "Комитет благопристойности ада", "Сатира / цензура", "Приходит письмо: «Ваш выпуск недостаточно нарушает возрастные ограничения. Просим добавить хоть что-нибудь возмутительное».", 5f, true,
                    new[] { new Condition { type = ConditionType.EpisodeAtLeast, value = 1, key = "" } },
                    new[] { "Satire", "Censorship", "Board" },
                    Ch("Пообещать больше трэша", 100, "Trash +4; ViewerTask: Outrage", "", new Effect[] { new Effect { type = EffectType.Tone, value = 4, mood = (ShowMood)1, key = "" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "ViewerTask_Outrage" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Outrage" } }, new Effect[] {  }),
                    Ch("Отправить формальный ответ", 100, "Budget -10; Family +2", "", new Effect[] { new Effect { type = EffectType.Budget, value = -10, mood = (ShowMood)0, key = "" }, new Effect { type = EffectType.Tone, value = 2, mood = (ShowMood)2, key = "" } }, new Effect[] {  }),
                    Ch("Игнорировать", 50, "Ничего", "Fine: Budget -40", new Effect[] {  }, new Effect[] { new Effect { type = EffectType.Budget, value = -40, mood = (ShowMood)0, key = "" } })
                ),
                Ev("evt_007", "Клип утёк до эфира", "Audience / риск", "Короткий backstage-клип уже гуляет по HellTube до монтажа выпуска.", 7f, true,
                    null,
                    new[] { "Audience", "Leak", "Viral" },
                    Ch("Оседлать хайп", 80, "RatingPotential +1; Trash +2", "Spoiler: CoherencePenalty next Broadcast", new Effect[] { new Effect { type = EffectType.Tone, value = 2, mood = (ShowMood)1, key = "" } }, new Effect[] {  }),
                    Ch("Пожаловаться и удалить", 100, "Budget -20; Family +1", "", new Effect[] { new Effect { type = EffectType.Budget, value = -20, mood = (ShowMood)0, key = "" }, new Effect { type = EffectType.Tone, value = 1, mood = (ShowMood)2, key = "" } }, new Effect[] {  }),
                    Ch("Подкинуть второй тизер", 60, "ViewsPotential +2; Reveal tag", "AudienceFatigue +1", new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Reveal" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Reveal" } }, new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "AudienceFatigue" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "AudienceFatigue" } })
                ),
                Ev("evt_008", "Спонсор просит «чуть органичнее»", "Sponsor", "Бренд прислал правки: «Логотип должен быть естественной частью эмоционального кризиса».", 8f, false,
                    new[] { new Condition { type = ConditionType.ContractActive, value = 0, key = "" } },
                    new[] { "Sponsor", "Contract" },
                    Ch("Согласиться", 100, "Получить временную Sponsor integration card; payout +20", "", new Effect[] { new Effect { type = EffectType.AddTempCard, value = 0, mood = (ShowMood)0, key = "CARD_SP_001" } }, new Effect[] {  }),
                    Ch("Потребовать доплату", 60, "Payout +50", "SponsorRep -5", new Effect[] {  }, new Effect[] {  }),
                    Ch("Отказаться", 100, "SponsorRep -2; Family +1", "", new Effect[] { new Effect { type = EffectType.Tone, value = 1, mood = (ShowMood)2, key = "" } }, new Effect[] {  })
                ),
                Ev("evt_009", "Актёр хочет уйти", "Каст / драма", "Один участник запирается в комнате и пишет продюсеру: «Я больше не могу. Снимайте без меня».", 6f, false,
                    null,
                    new[] { "Cast", "Stress", "Drama" },
                    Ch("Уговорить остаться", 65, "Actor stays; Stress +2; Trust +1", "Actor temporary unavailable next Situation", new Effect[] { new Effect { type = EffectType.NextRoomModifier, value = 2, mood = (ShowMood)0, key = "stress" } }, new Effect[] {  }),
                    Ch("Дать день отдыха", 100, "Actor unavailable next Situation; Trust +2; Stress -2", "", new Effect[] { new Effect { type = EffectType.NextRoomModifier, value = -2, mood = (ShowMood)0, key = "stress" } }, new Effect[] {  }),
                    Ch("Напомнить про контракт", 100, "Actor stays; Hostility to Producer +2; Trash +1", "", new Effect[] { new Effect { type = EffectType.Tone, value = 1, mood = (ShowMood)1, key = "" }, new Effect { type = EffectType.NextRoomModifier, value = 2, mood = (ShowMood)0, key = "hostility" } }, new Effect[] {  })
                ),
                Ev("evt_010", "Подарок от фаната", "Фанаты / риск", "Курьер приносит огромную коробку без обратного адреса. На ней написано: «Для любимчика сезона».", 9f, false,
                    null,
                    new[] { "Fan", "Gift", "Jealousy" },
                    Ch("Передать участнику", 70, "Gift prop next Situation; Confidence +2", "Jealousy event у других", new Effect[] {  }, new Effect[] {  }),
                    Ch("Проверить содержимое", 100, "Budget -5; Secret discovered flag", "", new Effect[] { new Effect { type = EffectType.Budget, value = -5, mood = (ShowMood)0, key = "" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "discovered" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Secret" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Secret" } }, new Effect[] {  }),
                    Ch("Оставить в реквизите", 100, "Получить temp Environment card «Коробка с подарком»", "", new Effect[] { new Effect { type = EffectType.AddTempCard, value = 0, mood = (ShowMood)0, key = "CARD_ENV_001" } }, new Effect[] {  })
                ),
                Ev("evt_011", "Сломалась камера", "Продакшен", "Одна из камер умирает с запахом озона прямо перед съёмкой.", 7f, false,
                    null,
                    new[] { "Production", "Camera" },
                    Ch("Срочно чинить", 70, "Budget -25; FootageSlots unchanged", "Budget -25; next Situation footageSlots -1", new Effect[] { new Effect { type = EffectType.Budget, value = -25, mood = (ShowMood)0, key = "" } }, new Effect[] { new Effect { type = EffectType.Budget, value = -25, mood = (ShowMood)0, key = "" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "CaptureSlotMinus" } }),
                    Ch("Снимать оставшимися", 100, "next Situation footageSlots -1; TechnicalQuality +0", "", new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "CaptureSlotMinus" } }, new Effect[] {  }),
                    Ch("Арендовать премиум-камеру", 100, "Budget -60; TechnicalQuality next footage +2", "", new Effect[] { new Effect { type = EffectType.Budget, value = -60, mood = (ShowMood)0, key = "" } }, new Effect[] {  })
                ),
                Ev("evt_012", "Письмо из прошлой жизни", "Secret / character", "Участнику приходит конверт без марки. Внутри — фотография и фраза: «Ты же обещал никогда сюда не возвращаться».", 6f, true,
                    new[] { new Condition { type = ConditionType.CastAtLeast, value = 2, key = "" } },
                    new[] { "Secret", "Past", "Reveal" },
                    Ch("Отдать письмо", 100, "Stress +2; Secret flag; Reveal opportunity", "", new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Secret" }, new Effect { type = EffectType.NextRoomModifier, value = 2, mood = (ShowMood)0, key = "stress" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Secret" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Reveal" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Reveal" } }, new Effect[] {  }),
                    Ch("Спрятать", 100, "ProducerSecret flag; будущая Reveal карта получает bonus", "", new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "ProducerSecret" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Reveal" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Reveal" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "ProducerSecret" } }, new Effect[] {  }),
                    Ch("Показать другому участнику", 80, "Rumour + Suspicious у пары", "Trust к продюсеру -1", new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Suspicious" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Suspicious" } }, new Effect[] {  })
                ),
                Ev("evt_013", "Опрос зрителей", "Audience", "HellTube запускает голосование: «Кто здесь самый фальшивый?»", 7f, false,
                    null,
                    new[] { "Audience", "Popularity" },
                    Ch("Показать результаты касту", 100, "Top actor Confidence -1; Bottom actor Anger +2; Drama +2", "", new Effect[] { new Effect { type = EffectType.Tone, value = 2, mood = (ShowMood)0, key = "" }, new Effect { type = EffectType.NextRoomModifier, value = 2, mood = (ShowMood)0, key = "anger" } }, new Effect[] {  }),
                    Ch("Скрыть до эфира", 100, "Family +1; ProducerKnowledge flag", "", new Effect[] { new Effect { type = EffectType.Tone, value = 1, mood = (ShowMood)2, key = "" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "ProducerKnowledge" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "ProducerKnowledge" } }, new Effect[] {  }),
                    Ch("Подкрутить результаты", 50, "Выбранный actor Popularity +2", "Scandal flag; Trash +3", new Effect[] {  }, new Effect[] { new Effect { type = EffectType.Tone, value = 3, mood = (ShowMood)1, key = "" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Scandal" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Scandal" } })
                ),
                Ev("evt_014", "Соседи устраивают вечеринку", "Chaos / environment", "За стеной начинается вечеринка громче вашего шоу. Бас двигает стаканы по столу.", 8f, false,
                    null,
                    new[] { "Chaos", "Party", "Noise" },
                    Ch("Пригласить соседей", 100, "Party setup; Chaos +2; 1 guest role", "", new Effect[] {  }, new Effect[] {  }),
                    Ch("Вызвать адскую полицию", 70, "Noise removed; Family +1", "Соседи мстят: next Situation Stress +1", new Effect[] { new Effect { type = EffectType.Tone, value = 1, mood = (ShowMood)2, key = "" } }, new Effect[] { new Effect { type = EffectType.NextRoomModifier, value = 1, mood = (ShowMood)0, key = "stress" } }),
                    Ch("Снимать как есть", 100, "TechnicalQuality -1; Comedy tag", "", new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Comedy" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Comedy" } }, new Effect[] {  })
                ),
                Ev("evt_015", "Слух о подставном романе", "Romance / rumor", "В команде уверены, что одна из пар «романтически интересна» только по просьбе продюсеров.", 8f, false,
                    null,
                    new[] { "Romance", "Rumour", "Meta" },
                    Ch("Поддержать слух", 100, "RumourSpread; Trash +2; Suspicious +1", "", new Effect[] { new Effect { type = EffectType.Tone, value = 2, mood = (ShowMood)1, key = "" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Suspicious" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Suspicious" } }, new Effect[] {  }),
                    Ch("Опровергнуть", 100, "Family +2; Trust пары +1", "", new Effect[] { new Effect { type = EffectType.Tone, value = 2, mood = (ShowMood)2, key = "" } }, new Effect[] {  }),
                    Ch("Предложить им доказать обратное", 65, "Romance setup; Attraction +2", "Awkward; Stress +1", new Effect[] {  }, new Effect[] { new Effect { type = EffectType.NextRoomModifier, value = 1, mood = (ShowMood)0, key = "stress" } })
                ),
                Ev("evt_016", "Перепутанные комнаты", "Comedy / production", "Ассистент перепутал таблички на дверях. Половина каста просыпается не там, где планировалось.", 9f, false,
                    null,
                    new[] { "Comedy", "Rooms", "Production" },
                    Ch("Оставить как есть", 100, "Randomize starting room positions next Situation; Comedy +2", "", new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Comedy" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Comedy" } }, new Effect[] {  }),
                    Ch("Вернуть всех по плану", 100, "No chaos; ProductionConfidence +1", "", new Effect[] {  }, new Effect[] {  }),
                    Ch("Сделать вид, что так задумано", 80, "Random positions; RatingPotential +1", "StaffStress +1", new Effect[] {  }, new Effect[] { new Effect { type = EffectType.NextRoomModifier, value = 1, mood = (ShowMood)0, key = "stress" } })
                ),
                Ev("evt_017", "Продюсер-конкурент", "Meta / rivalry", "Соседний канал предлагает купить ваш самый скандальный материал до эфира.", 5f, true,
                    null,
                    new[] { "Meta", "Competition", "Footage" },
                    Ch("Продать", 100, "Budget +100; удалить 1 high-value footage из library", "", new Effect[] { new Effect { type = EffectType.Budget, value = 100, mood = (ShowMood)0, key = "" } }, new Effect[] {  }),
                    Ch("Отказать", 100, "RatingPotential +1; RivalProducer flag", "", new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "RivalProducer" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "RivalProducer" } }, new Effect[] {  }),
                    Ch("Продать фальшивку", 55, "Budget +70; Trash +1", "Fine -50; RivalProducerAngry", new Effect[] { new Effect { type = EffectType.Budget, value = 70, mood = (ShowMood)0, key = "" }, new Effect { type = EffectType.Tone, value = 1, mood = (ShowMood)1, key = "" } }, new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "RivalProducerAngry" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "RivalProducerAngry" } })
                ),
                Ev("evt_018", "Психолог требует паузу", "Cast / ethics satire", "Штатный психолог пишет: «Участники на пределе». Через минуту приходит второе сообщение: «Но цифры отличные».", 6f, false,
                    null,
                    new[] { "Cast", "Stress", "Satire" },
                    Ch("Дать паузу", 100, "Stress всем -2; Family +3; next Situation intensity -1", "", new Effect[] { new Effect { type = EffectType.Tone, value = 3, mood = (ShowMood)2, key = "" }, new Effect { type = EffectType.NextRoomModifier, value = -2, mood = (ShowMood)0, key = "stress" } }, new Effect[] {  }),
                    Ch("Снимать дальше", 100, "Stress всем +1; Drama +2", "", new Effect[] { new Effect { type = EffectType.Tone, value = 2, mood = (ShowMood)0, key = "" }, new Effect { type = EffectType.NextRoomModifier, value = 1, mood = (ShowMood)0, key = "stress" } }, new Effect[] {  }),
                    Ch("Попросить «профессиональное мнение для камеры»", 70, "Temp Confession card; Comedy +1", "PsychologistUnavailable flag", new Effect[] { new Effect { type = EffectType.AddTempCard, value = 0, mood = (ShowMood)0, key = "confession_cam" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Comedy" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Comedy" } }, new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "PsychologistUnavailable" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "PsychologistUnavailable" } })
                ),
                Ev("evt_019", "Анонимный компромат", "Reveal / secret", "На продюсерскую почту приходит архив: фото, чеки и голосовое. Отправитель: «друг шоу».", 5f, true,
                    new[] { new Condition { type = ConditionType.EpisodeAtLeast, value = 2, key = "" } },
                    new[] { "Secret", "Reveal", "Risk" },
                    Ch("Проверить подлинность", 75, "VerifiedSecret flag; Reveal potency +2", "Budget -15; false lead", new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "VerifiedSecret" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Reveal" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Reveal" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "VerifiedSecret" } }, new Effect[] { new Effect { type = EffectType.Budget, value = -15, mood = (ShowMood)0, key = "" } }),
                    Ch("Сразу использовать", 55, "Drama +4; Temp Reveal card", "ScandalBackfire; Trust -2", new Effect[] { new Effect { type = EffectType.Tone, value = 4, mood = (ShowMood)0, key = "" }, new Effect { type = EffectType.AddTempCard, value = 0, mood = (ShowMood)0, key = "CARD_REVEAL_001" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "Reveal" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "Reveal" } }, new Effect[] { new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "ScandalBackfire" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "ScandalBackfire" } }),
                    Ch("Удалить", 100, "Family +2; no flag", "", new Effect[] { new Effect { type = EffectType.Tone, value = 2, mood = (ShowMood)2, key = "" } }, new Effect[] {  })
                ),
                Ev("evt_020", "Совет директоров хочет больше огня", "Board / chaos", "На планёрке один из директоров молча ставит на стол огнетушитель. Второй спрашивает: «А где огонь?»", 7f, false,
                    new[] { new Condition { type = ConditionType.EpisodeAtLeast, value = 1, key = "" } },
                    new[] { "Board", "Chaos", "Production" },
                    Ch("Добавить опасный реквизит", 100, "Получить temp Environment card; Chaos +3", "", new Effect[] { new Effect { type = EffectType.AddTempCard, value = 0, mood = (ShowMood)0, key = "CARD_ENV_001" } }, new Effect[] {  }),
                    Ch("Предложить эмоциональный огонь", 100, "Provocation cards cost -0.25 next Situation", "", new Effect[] {  }, new Effect[] {  }),
                    Ch("Сослаться на бюджет", 100, "Budget +20; BoardDisappointed flag", "", new Effect[] { new Effect { type = EffectType.Budget, value = 20, mood = (ShowMood)0, key = "" }, new Effect { type = EffectType.SetEpisodeFlag, value = 0, mood = (ShowMood)0, key = "BoardDisappointed" }, new Effect { type = EffectType.AddNarrativeTag, value = 0, mood = (ShowMood)0, key = "BoardDisappointed" } }, new Effect[] {  })
                ),
            };
        }

        static EventRoomDefinition Ev(string id, string title, string subtitle, string body, float weight, bool unique, Condition[] conditions, string[] tags, params EventChoice[] choices)
        {
            var room = ScriptableObject.CreateInstance<EventRoomDefinition>();
            room.name = id;
            room.SetId(id);
            room.title = title;
            room.subtitle = subtitle;
            room.description = body;
            room.body = body;
            room.weight = weight;
            room.uniquePerEpisode = unique;
            room.icon = MapNodeKind.Mystery;
            room.color = new Color(0.28f, 0.32f, 0.48f, 1f);
            room.roles = new List<EventRole> { new EventRole { key = "актёр" }, new EventRole { key = "второй" } };
            room.choices = new List<EventChoice>(choices);
            if (tags != null) room.eventTags = new List<string>(tags);
            if (conditions != null) room.conditions = new List<Condition>(conditions);
            return room;
        }

        static EventChoice Ch(string label, int chance, string ok, string fail, Effect[] effects, Effect[] failEffects)
        {
            return new EventChoice
            {
                label = label,
                chance = chance,
                resultText = ok,
                failText = fail,
                effects = new List<Effect>(effects ?? new Effect[0]),
                failEffects = new List<Effect>(failEffects ?? new Effect[0])
            };
        }
    }
}
