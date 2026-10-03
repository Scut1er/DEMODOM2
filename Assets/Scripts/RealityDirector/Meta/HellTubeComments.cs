using System.Collections.Generic;
using RealityDirector.Capture;
using RealityDirector.Core;
using RealityDirector.NPC;
using UnityEngine;

namespace RealityDirector.Meta
{
    [System.Serializable]
    public class HellTubeComment
    {
        public string id;
        [Tooltip("Автор (ник зрителя). {actor} — имя участника из эфира.")]
        public string persona;
        public string category;
        [Tooltip("Текст. {actor} {actorA} {actorB} {event} {brand} {tone} {situation} {episode} — только из того, что было в эфире.")]
        [TextArea(1, 3)] public string template;
        [Tooltip("Drama / Trash / Family / Neutral — от него оценка зрителя.")]
        public string tone;
        [Tooltip("Чем выше — тем раньше выбирается (конкретные реакции выше общих).")]
        public int priority;
        public int weight;
        [Tooltip("Когда уместен: Drama, Trash, Family, Romance, Conflict, Reveal, Sponsor, Technical, EditingGood, EditingBad, "
                 + "Repetition, OneActor, Combo:reveal_reaction / arc / chaos / triangle / setup_payoff, Seq:romance_conflict… Пусто — всегда.")]
        public string required;
        [Tooltip("Смысловая группа: из одной группы в ленту попадает один комментарий.")]
        public string group;
    }


    public static class HellTubeComments
    {
        public static readonly HellTubeComment[] All =
        {
            new HellTubeComment { id = "HTC_001", persona = "скучающий_демон", category = "Generic", template = "ну хотя бы не скучно", tone = "Neutral", priority = 20, weight = 10, required = "", group = "generic" },
            new HellTubeComment { id = "HTC_002", persona = "мама_антихриста", category = "Generic", template = "я включил на пять минут и почему-то досмотрел", tone = "Neutral", priority = 25, weight = 9, required = "", group = "generic" },
            new HellTubeComment { id = "HTC_003", persona = "диванный_критик", category = "Generic", template = "это вообще законно показывать до ужина?", tone = "Neutral", priority = 30, weight = 8, required = "", group = "generic" },
            new HellTubeComment { id = "HTC_004", persona = "шиппер_666", category = "Generic", template = "кто-нибудь объяснит, почему я опять здесь", tone = "Neutral", priority = 20, weight = 7, required = "", group = "generic" },
            new HellTubeComment { id = "HTC_005", persona = "монтажёр_с_опытом", category = "Generic", template = "серия ощущается как плохое решение, которое я повторю", tone = "Neutral", priority = 25, weight = 10, required = "", group = "generic" },
            new HellTubeComment { id = "HTC_006", persona = "фанат_драмы", category = "Generic", template = "не понял ничего, но палец лайкнул сам", tone = "Neutral", priority = 30, weight = 9, required = "", group = "generic" },
            new HellTubeComment { id = "HTC_007", persona = "я_тут_ради_мемов", category = "Generic", template = "у этого шоу есть сюжет или только последствия", tone = "Neutral", priority = 20, weight = 8, required = "", group = "generic" },
            new HellTubeComment { id = "HTC_008", persona = "хейтер_шоу", category = "Generic", template = "я пришёл за фоном, а получил личную драму", tone = "Neutral", priority = 25, weight = 7, required = "", group = "generic" },
            new HellTubeComment { id = "HTC_009", persona = "продажный_инсайдер", category = "Generic", template = "мне стыдно, но следующую серию тоже включу", tone = "Neutral", priority = 30, weight = 10, required = "", group = "generic" },
            new HellTubeComment { id = "HTC_010", persona = "фанат_{actor}", category = "Generic", template = "сегодня ад особенно телевизионный", tone = "Neutral", priority = 20, weight = 9, required = "", group = "generic" },
            new HellTubeComment { id = "HTC_011", persona = "скучающий_демон", category = "Drama", template = "{actor} смотрит так, будто сейчас кто-то потеряет доверие и мебель", tone = "Drama", priority = 65, weight = 10, required = "Drama", group = "drama_specific" },
            new HellTubeComment { id = "HTC_012", persona = "мама_антихриста", category = "Drama", template = "вот это уже не ссора, это полноценный сюжет", tone = "Drama", priority = 70, weight = 9, required = "Drama", group = "drama_specific" },
            new HellTubeComment { id = "HTC_013", persona = "диванный_критик", category = "Drama", template = "когда {actorA} сказал это {actorB}, я реально замолчал", tone = "Drama", priority = 75, weight = 8, required = "Drama", group = "drama_specific" },
            new HellTubeComment { id = "HTC_014", persona = "шиппер_666", category = "Drama", template = "наконец-то драма с последствиями, а не просто крик", tone = "Drama", priority = 65, weight = 7, required = "Drama", group = "drama_specific" },
            new HellTubeComment { id = "HTC_015", persona = "монтажёр_с_опытом", category = "Drama", template = "это был тот момент, когда всё окончательно поехало", tone = "Drama", priority = 70, weight = 10, required = "Drama", group = "drama_specific" },
            new HellTubeComment { id = "HTC_016", persona = "фанат_драмы", category = "Drama", template = "кто дал им столько эмоций и ни капли самоконтроля", tone = "Drama", priority = 75, weight = 9, required = "Drama", group = "drama_specific" },
            new HellTubeComment { id = "HTC_017", persona = "я_тут_ради_мемов", category = "Drama", template = "я чувствую напряжение через экран, неприятно и прекрасно", tone = "Drama", priority = 65, weight = 8, required = "Drama", group = "drama_specific" },
            new HellTubeComment { id = "HTC_018", persona = "хейтер_шоу", category = "Drama", template = "если после этого они помирятся, я перестану верить в причинность", tone = "Drama", priority = 70, weight = 7, required = "Drama", group = "drama_specific" },
            new HellTubeComment { id = "HTC_019", persona = "продажный_инсайдер", category = "Drama", template = "эта серия сделала один разговор важнее всего сезона", tone = "Drama", priority = 75, weight = 10, required = "Drama", group = "drama_specific" },
            new HellTubeComment { id = "HTC_020", persona = "фанат_{actor}", category = "Drama", template = "вот ради таких развалов отношений мы и платим электричеством", tone = "Drama", priority = 65, weight = 9, required = "Drama", group = "drama_specific" },
            new HellTubeComment { id = "HTC_021", persona = "скучающий_демон", category = "Trash", template = "это телевидение уровня «пожар, но красиво»", tone = "Trash", priority = 60, weight = 10, required = "Trash", group = "trash_specific" },
            new HellTubeComment { id = "HTC_022", persona = "мама_антихриста", category = "Trash", template = "трэш такой плотный, что можно резать ножом", tone = "Trash", priority = 65, weight = 9, required = "Trash", group = "trash_specific" },
            new HellTubeComment { id = "HTC_023", persona = "диванный_критик", category = "Trash", template = "я не горжусь тем, что мне это понравилось", tone = "Trash", priority = 70, weight = 8, required = "Trash", group = "trash_specific" },
            new HellTubeComment { id = "HTC_024", persona = "шиппер_666", category = "Trash", template = "кто-нибудь уберите продюсера от красной кнопки", tone = "Trash", priority = 60, weight = 7, required = "Trash", group = "trash_specific" },
            new HellTubeComment { id = "HTC_025", persona = "монтажёр_с_опытом", category = "Trash", template = "выпуск буквально кричит «не повторять дома», отлично", tone = "Trash", priority = 65, weight = 10, required = "Trash", group = "trash_specific" },
            new HellTubeComment { id = "HTC_026", persona = "фанат_драмы", category = "Trash", template = "ещё немного и монтаж начнёт дымиться", tone = "Trash", priority = 70, weight = 9, required = "Trash", group = "trash_specific" },
            new HellTubeComment { id = "HTC_027", persona = "я_тут_ради_мемов", category = "Trash", template = "я думал ниже уже некуда. спасибо за эксперимент", tone = "Trash", priority = 60, weight = 8, required = "Trash", group = "trash_specific" },
            new HellTubeComment { id = "HTC_028", persona = "хейтер_шоу", category = "Trash", template = "отвратительно. когда повтор?", tone = "Trash", priority = 65, weight = 7, required = "Trash", group = "trash_specific" },
            new HellTubeComment { id = "HTC_029", persona = "продажный_инсайдер", category = "Trash", template = "эта серия пахнет дешёвым реквизитом и дорогими последствиями", tone = "Trash", priority = 70, weight = 10, required = "Trash", group = "trash_specific" },
            new HellTubeComment { id = "HTC_030", persona = "фанат_{actor}", category = "Trash", template = "если это стратегия — она пугающе работает", tone = "Trash", priority = 60, weight = 9, required = "Trash", group = "trash_specific" },
            new HellTubeComment { id = "HTC_031", persona = "скучающий_демон", category = "Family", template = "неожиданно мило, я пришёл вообще-то страдать", tone = "Family", priority = 55, weight = 10, required = "Family", group = "family_specific" },
            new HellTubeComment { id = "HTC_032", persona = "мама_антихриста", category = "Family", template = "редкий выпуск, после которого никого не хочется заблокировать", tone = "Family", priority = 60, weight = 9, required = "Family", group = "family_specific" },
            new HellTubeComment { id = "HTC_033", persona = "диванный_критик", category = "Family", template = "можно иногда просто дать людям поговорить, оказывается", tone = "Family", priority = 65, weight = 8, required = "Family", group = "family_specific" },
            new HellTubeComment { id = "HTC_034", persona = "шиппер_666", category = "Family", template = "{actorA} и {actorB} сегодня почти как нормальные", tone = "Family", priority = 55, weight = 7, required = "Family", group = "family_specific" },
            new HellTubeComment { id = "HTC_035", persona = "монтажёр_с_опытом", category = "Family", template = "мне вернули веру в адскую человечность", tone = "Family", priority = 60, weight = 10, required = "Family", group = "family_specific" },
            new HellTubeComment { id = "HTC_036", persona = "фанат_драмы", category = "Family", template = "это было слишком тепло для этого канала", tone = "Family", priority = 65, weight = 9, required = "Family", group = "family_specific" },
            new HellTubeComment { id = "HTC_037", persona = "я_тут_ради_мемов", category = "Family", template = "где скандал? почему я улыбаюсь?", tone = "Family", priority = 55, weight = 8, required = "Family", group = "family_specific" },
            new HellTubeComment { id = "HTC_038", persona = "хейтер_шоу", category = "Family", template = "примирение сработало лучше, чем я ожидал", tone = "Family", priority = 60, weight = 7, required = "Family", group = "family_specific" },
            new HellTubeComment { id = "HTC_039", persona = "продажный_инсайдер", category = "Family", template = "вот такой выпуск я бы показал маме. маме-антихриста", tone = "Family", priority = 65, weight = 10, required = "Family", group = "family_specific" },
            new HellTubeComment { id = "HTC_040", persona = "фанат_{actor}", category = "Family", template = "семейный тон в аду — всё ещё звучит как угроза, но мне нравится", tone = "Family", priority = 55, weight = 9, required = "Family", group = "family_specific" },
            new HellTubeComment { id = "HTC_041", persona = "скучающий_демон", category = "Romance", template = "я официально шипперю {actorA} и {actorB}", tone = "Drama", priority = 70, weight = 10, required = "Romance", group = "romance" },
            new HellTubeComment { id = "HTC_042", persona = "мама_антихриста", category = "Romance", template = "они смотрят друг на друга слишком долго, это улика", tone = "Drama", priority = 75, weight = 9, required = "Romance", group = "romance" },
            new HellTubeComment { id = "HTC_043", persona = "диванный_критик", category = "Romance", template = "если {actorA} опять скажет «мы просто друзья», я вызываю монтажёра", tone = "Drama", priority = 80, weight = 8, required = "Romance", group = "romance" },
            new HellTubeComment { id = "HTC_044", persona = "шиппер_666", category = "Romance", template = "романтика началась мило и закончилась как положено — ревностью", tone = "Drama", priority = 70, weight = 7, required = "Romance", group = "romance" },
            new HellTubeComment { id = "HTC_045", persona = "монтажёр_с_опытом", category = "Romance", template = "{actorA}+{actorB} это либо любовь, либо очень дорогой конфликт", tone = "Drama", priority = 75, weight = 10, required = "Romance", group = "romance" },
            new HellTubeComment { id = "HTC_046", persona = "фанат_драмы", category = "Romance", template = "пожалуйста, не ставьте между ними третьего человека. хотя ставьте", tone = "Drama", priority = 80, weight = 9, required = "Romance", group = "romance" },
            new HellTubeComment { id = "HTC_047", persona = "я_тут_ради_мемов", category = "Romance", template = "этот флирт был слышен громче микрофона", tone = "Drama", priority = 70, weight = 8, required = "Romance", group = "romance" },
            new HellTubeComment { id = "HTC_048", persona = "хейтер_шоу", category = "Romance", template = "у них химия, у продюсера калькулятор", tone = "Drama", priority = 75, weight = 7, required = "Romance", group = "romance" },
            new HellTubeComment { id = "HTC_049", persona = "продажный_инсайдер", category = "Romance", template = "я не верю в любовь, но верю в эту сюжетную линию", tone = "Drama", priority = 80, weight = 10, required = "Romance", group = "romance" },
            new HellTubeComment { id = "HTC_050", persona = "фанат_{actor}", category = "Romance", template = "кто-нибудь сохраните этот кадр до того, как всё разрушат", tone = "Drama", priority = 70, weight = 9, required = "Romance", group = "romance" },
            new HellTubeComment { id = "HTC_051", persona = "скучающий_демон", category = "Conflict", template = "{actorA} и {actorB} выбрали насилие как язык любви к рейтингу", tone = "Trash", priority = 75, weight = 10, required = "Conflict", group = "conflict" },
            new HellTubeComment { id = "HTC_052", persona = "мама_антихриста", category = "Conflict", template = "этот спор начал жить своей жизнью", tone = "Trash", priority = 80, weight = 9, required = "Conflict", group = "conflict" },
            new HellTubeComment { id = "HTC_053", persona = "диванный_критик", category = "Conflict", template = "я ставлю на мебель, она пока единственная держится", tone = "Trash", priority = 85, weight = 8, required = "Conflict", group = "conflict" },
            new HellTubeComment { id = "HTC_054", persona = "шиппер_666", category = "Conflict", template = "{actor} держался ровно семь секунд, новый рекорд", tone = "Trash", priority = 75, weight = 7, required = "Conflict", group = "conflict" },
            new HellTubeComment { id = "HTC_055", persona = "монтажёр_с_опытом", category = "Conflict", template = "конфликт отличный, аргументы закончились ещё минуту назад", tone = "Trash", priority = 80, weight = 10, required = "Conflict", group = "conflict" },
            new HellTubeComment { id = "HTC_056", persona = "фанат_драмы", category = "Conflict", template = "они уже не спорят, они производят контент", tone = "Trash", priority = 85, weight = 9, required = "Conflict", group = "conflict" },
            new HellTubeComment { id = "HTC_057", persona = "я_тут_ради_мемов", category = "Conflict", template = "кто сказал «давайте спокойно» и зачем его сразу проигнорировали", tone = "Trash", priority = 75, weight = 8, required = "Conflict", group = "conflict" },
            new HellTubeComment { id = "HTC_058", persona = "хейтер_шоу", category = "Conflict", template = "у этого конфликта есть начало, середина и адвокат", tone = "Trash", priority = 80, weight = 7, required = "Conflict", group = "conflict" },
            new HellTubeComment { id = "HTC_059", persona = "продажный_инсайдер", category = "Conflict", template = "если это был план продюсера, мне страшно за следующий", tone = "Trash", priority = 85, weight = 10, required = "Conflict", group = "conflict" },
            new HellTubeComment { id = "HTC_060", persona = "фанат_{actor}", category = "Conflict", template = "я пересмотрел момент с криком трижды. исследовательские цели", tone = "Trash", priority = 75, weight = 9, required = "Conflict", group = "conflict" },
            new HellTubeComment { id = "HTC_061", persona = "скучающий_демон", category = "Reveal", template = "Я ЗНАЛА ЧТО ЭТО ВСПЛЫВЁТ", tone = "Drama", priority = 85, weight = 10, required = "Reveal", group = "reveal" },
            new HellTubeComment { id = "HTC_062", persona = "мама_антихриста", category = "Reveal", template = "так вот зачем они показывали {event} раньше", tone = "Drama", priority = 90, weight = 9, required = "Reveal", group = "reveal" },
            new HellTubeComment { id = "HTC_063", persona = "диванный_критик", category = "Reveal", template = "секрет продержался меньше, чем рекламная интеграция", tone = "Drama", priority = 95, weight = 8, required = "Reveal", group = "reveal" },
            new HellTubeComment { id = "HTC_064", persona = "шиппер_666", category = "Reveal", template = "{actor} сказал это вслух. ВСЛУХ.", tone = "Drama", priority = 85, weight = 7, required = "Reveal", group = "reveal" },
            new HellTubeComment { id = "HTC_065", persona = "монтажёр_с_опытом", category = "Reveal", template = "после такого reveal назад уже не смонтируешь", tone = "Drama", priority = 90, weight = 10, required = "Reveal", group = "reveal" },
            new HellTubeComment { id = "HTC_066", persona = "фанат_драмы", category = "Reveal", template = "я теперь понимаю половину предыдущих взглядов", tone = "Drama", priority = 95, weight = 9, required = "Reveal", group = "reveal" },
            new HellTubeComment { id = "HTC_067", persona = "я_тут_ради_мемов", category = "Reveal", template = "вот это payoff, спасибо монтажу", tone = "Drama", priority = 85, weight = 8, required = "Reveal", group = "reveal" },
            new HellTubeComment { id = "HTC_068", persona = "хейтер_шоу", category = "Reveal", template = "кто слил это продюсеру и почему я хочу пожать ему руку", tone = "Drama", priority = 90, weight = 7, required = "Reveal", group = "reveal" },
            new HellTubeComment { id = "HTC_069", persona = "продажный_инсайдер", category = "Reveal", template = "секрет раскрыт, доверие тоже", tone = "Drama", priority = 95, weight = 10, required = "Reveal", group = "reveal" },
            new HellTubeComment { id = "HTC_070", persona = "фанат_{actor}", category = "Reveal", template = "этот reveal реально изменил весь выпуск задним числом", tone = "Drama", priority = 85, weight = 9, required = "Reveal", group = "reveal" },
            new HellTubeComment { id = "HTC_071", persona = "скучающий_демон", category = "Editing", template = "редкий случай когда монтажёр реально пришёл на работу", tone = "Neutral", priority = 80, weight = 10, required = "EditingGood", group = "editing" },
            new HellTubeComment { id = "HTC_072", persona = "мама_антихриста", category = "Editing", template = "первый кадр задал вопрос, последний ответил — чудеса", tone = "Neutral", priority = 85, weight = 9, required = "EditingGood", group = "editing" },
            new HellTubeComment { id = "HTC_073", persona = "диванный_критик", category = "Editing", template = "а почему они сначала расстались, а потом познакомились", tone = "Neutral", priority = 90, weight = 8, required = "EditingBad", group = "editing" },
            new HellTubeComment { id = "HTC_074", persona = "шиппер_666", category = "Editing", template = "монтаж прыгает так, будто между сценами был пожар", tone = "Neutral", priority = 80, weight = 7, required = "EditingBad", group = "editing" },
            new HellTubeComment { id = "HTC_075", persona = "монтажёр_с_опытом", category = "Editing", template = "три одинаковых конфликта подряд — смелое признание в отсутствии выбора", tone = "Neutral", priority = 85, weight = 10, required = "Repetition", group = "editing" },
            new HellTubeComment { id = "HTC_076", persona = "фанат_драмы", category = "Editing", template = "это сериал про {actor} или остальные просто массовка", tone = "Neutral", priority = 90, weight = 9, required = "OneActor", group = "editing" },
            new HellTubeComment { id = "HTC_077", persona = "я_тут_ради_мемов", category = "Editing", template = "переход {actorA} → {actorB} наконец-то имеет смысл", tone = "Neutral", priority = 80, weight = 8, required = "EditingGood", group = "editing" },
            new HellTubeComment { id = "HTC_078", persona = "хейтер_шоу", category = "Editing", template = "сюжет собран из осколков, но хотя бы картинка красивая", tone = "Neutral", priority = 85, weight = 7, required = "EditingBad", group = "editing" },
            new HellTubeComment { id = "HTC_079", persona = "продажный_инсайдер", category = "Editing", template = "эта последовательность реально усилила финальный момент", tone = "Neutral", priority = 90, weight = 10, required = "EditingGood", group = "editing" },
            new HellTubeComment { id = "HTC_080", persona = "фанат_{actor}", category = "Editing", template = "кто поставил karaoke между reveal и дракой, я хочу поговорить", tone = "Neutral", priority = 80, weight = 9, required = "EditingBad", group = "editing" },
            new HellTubeComment { id = "HTC_081", persona = "скучающий_демон", category = "Sponsor", template = "они что, реально поставили {brand} посреди расставания 💀", tone = "Neutral", priority = 70, weight = 10, required = "Sponsor", group = "sponsor" },
            new HellTubeComment { id = "HTC_082", persona = "мама_антихриста", category = "Sponsor", template = "бренд в кадре пережил отношения, уважение", tone = "Neutral", priority = 75, weight = 9, required = "Sponsor", group = "sponsor" },
            new HellTubeComment { id = "HTC_083", persona = "диванный_критик", category = "Sponsor", template = "{brand}: когда твой продукт эмоционально стабильнее участников", tone = "Neutral", priority = 80, weight = 8, required = "Sponsor", group = "sponsor" },
            new HellTubeComment { id = "HTC_084", persona = "шиппер_666", category = "Sponsor", template = "интеграция настолько нативная, что я почувствовал бухгалтерию", tone = "Neutral", priority = 70, weight = 7, required = "Sponsor", group = "sponsor" },
            new HellTubeComment { id = "HTC_085", persona = "монтажёр_с_опытом", category = "Sponsor", template = "я пришёл за драмой, ушёл с желанием купить {brand}. ненавижу вас", tone = "Neutral", priority = 75, weight = 10, required = "Sponsor", group = "sponsor" },
            new HellTubeComment { id = "HTC_086", persona = "фанат_драмы", category = "Sponsor", template = "спонсорский кадр был смешнее половины серии", tone = "Neutral", priority = 80, weight = 9, required = "Sponsor", group = "sponsor" },
            new HellTubeComment { id = "HTC_087", persona = "я_тут_ради_мемов", category = "Sponsor", template = "если {brand} заплатили за ЭТО, дайте им бонус", tone = "Neutral", priority = 70, weight = 8, required = "Sponsor", group = "sponsor" },
            new HellTubeComment { id = "HTC_088", persona = "хейтер_шоу", category = "Sponsor", template = "продукт попал в кадр, достоинство — нет", tone = "Neutral", priority = 75, weight = 7, required = "Sponsor", group = "sponsor" },
            new HellTubeComment { id = "HTC_089", persona = "продажный_инсайдер", category = "Sponsor", template = "никогда ещё реклама не выглядела так виновато", tone = "Neutral", priority = 80, weight = 10, required = "Sponsor", group = "sponsor" },
            new HellTubeComment { id = "HTC_090", persona = "фанат_{actor}", category = "Sponsor", template = "контракт выполнен, моральный долг остался", tone = "Neutral", priority = 70, weight = 9, required = "Sponsor", group = "sponsor" },
            new HellTubeComment { id = "HTC_091", persona = "скучающий_демон", category = "Technical", template = "картинка красивая, происходящее ужасное — баланс", tone = "Neutral", priority = 55, weight = 10, required = "Technical", group = "technical" },
            new HellTubeComment { id = "HTC_092", persona = "мама_антихриста", category = "Technical", template = "оператор поймал ровно тот момент, когда всё сломалось", tone = "Neutral", priority = 60, weight = 9, required = "Technical", group = "technical" },
            new HellTubeComment { id = "HTC_093", persona = "диванный_критик", category = "Technical", template = "звук будто записывали из другого круга ада", tone = "Neutral", priority = 65, weight = 8, required = "Technical", group = "technical" },
            new HellTubeComment { id = "HTC_094", persona = "шиппер_666", category = "Technical", template = "кадр отличный, содержание требует адвоката", tone = "Neutral", priority = 55, weight = 7, required = "Technical", group = "technical" },
            new HellTubeComment { id = "HTC_095", persona = "монтажёр_с_опытом", category = "Technical", template = "впервые камера успела за хаосом", tone = "Neutral", priority = 60, weight = 10, required = "Technical", group = "technical" },
            new HellTubeComment { id = "HTC_096", persona = "фанат_драмы", category = "Technical", template = "этот ракурс сделал половину сцены", tone = "Neutral", priority = 65, weight = 9, required = "Technical", group = "technical" },
            new HellTubeComment { id = "HTC_097", persona = "я_тут_ради_мемов", category = "Technical", template = "кто бы ни держал камеру — премию ему, терапию тоже", tone = "Neutral", priority = 55, weight = 8, required = "Technical", group = "technical" },
            new HellTubeComment { id = "HTC_098", persona = "хейтер_шоу", category = "Technical", template = "технически плохо, эмоционально идеально", tone = "Neutral", priority = 60, weight = 7, required = "Technical", group = "technical" },
            new HellTubeComment { id = "HTC_099", persona = "продажный_инсайдер", category = "Technical", template = "идеальный момент, снятый буквально за секунду до катастрофы", tone = "Neutral", priority = 65, weight = 10, required = "Technical", group = "technical" },
            new HellTubeComment { id = "HTC_100", persona = "фанат_{actor}", category = "Technical", template = "камера видела больше, чем участники хотели бы", tone = "Neutral", priority = 55, weight = 9, required = "Technical", group = "technical" },

            // Монтаж V2: комбо и последовательности (теги из CutAnalysis). Самые конкретные — выше приоритет.
            new HellTubeComment { id = "HTC_101", persona = "драма_наркоман", category = "Combo", template = "Я ЗНАЛА ЧТО {actorB} ТАК ОТРЕАГИРУЕТ", tone = "Drama", priority = 97, weight = 10, required = "Combo:reveal_reaction", group = "combo_reveal" },
            new HellTubeComment { id = "HTC_102", persona = "монтажёр_с_опытом", category = "Combo", template = "раскрыли — и сразу лицо {actorB}. вот это монтаж", tone = "Drama", priority = 96, weight = 9, required = "Combo:reveal_reaction", group = "combo_reveal" },
            new HellTubeComment { id = "HTC_103", persona = "шиппер_666", category = "Combo", template = "реакция на секрет лучше самого секрета", tone = "Drama", priority = 95, weight = 8, required = "Combo:reveal_reaction", group = "combo_reveal" },
            new HellTubeComment { id = "HTC_104", persona = "диванный_критик", category = "Combo", template = "начало, взрыв, последствия — у этой серии есть сюжет, я в шоке", tone = "Drama", priority = 97, weight = 10, required = "Combo:arc", group = "combo_arc" },
            new HellTubeComment { id = "HTC_105", persona = "мама_антихриста", category = "Combo", template = "у {actorA} полный круг ада за три кадра", tone = "Drama", priority = 96, weight = 9, required = "Combo:arc", group = "combo_arc" },
            new HellTubeComment { id = "HTC_106", persona = "продажный_инсайдер", category = "Combo", template = "вот это называется история, а не нарезка", tone = "Neutral", priority = 95, weight = 8, required = "Combo:arc", group = "combo_arc" },
            new HellTubeComment { id = "HTC_107", persona = "я_тут_ради_мемов", category = "Combo", template = "я не понял ни одного перехода, но мне понравилось всё", tone = "Trash", priority = 96, weight = 10, required = "Combo:chaos", group = "combo_chaos" },
            new HellTubeComment { id = "HTC_108", persona = "скучающий_демон", category = "Combo", template = "это не монтаж, это пожарная тревога в формате серии", tone = "Trash", priority = 95, weight = 9, required = "Combo:chaos", group = "combo_chaos" },
            new HellTubeComment { id = "HTC_109", persona = "хейтер_шоу", category = "Combo", template = "три катастрофы подряд и ни одной причины. идеально", tone = "Trash", priority = 94, weight = 8, required = "Combo:chaos", group = "combo_chaos" },
            new HellTubeComment { id = "HTC_110", persona = "шиппер_666", category = "Combo", template = "{actorA}, {actorB} и третий лишний — треугольник с острыми углами", tone = "Drama", priority = 96, weight = 10, required = "Combo:triangle", group = "combo_triangle" },
            new HellTubeComment { id = "HTC_111", persona = "фанат_драмы", category = "Combo", template = "три человека, две эмоции, один диван", tone = "Trash", priority = 95, weight = 9, required = "Combo:triangle", group = "combo_triangle" },
            new HellTubeComment { id = "HTC_112", persona = "монтажёр_с_опытом", category = "Combo", template = "первый кадр был ружьём, последний — выстрелом. уважаю", tone = "Neutral", priority = 95, weight = 10, required = "Combo:setup_payoff", group = "combo_setup" },
            new HellTubeComment { id = "HTC_113", persona = "мама_антихриста", category = "Combo", template = "показали завязку — и выстрелило в финале", tone = "Drama", priority = 94, weight = 9, required = "Combo:setup_payoff", group = "combo_setup" },
            new HellTubeComment { id = "HTC_114", persona = "фанат_{actor}", category = "Sequence", template = "после флирта терпения у {actor} хватило ровно на семь секунд", tone = "Drama", priority = 92, weight = 10, required = "Seq:romance_conflict", group = "seq_romance" },
            new HellTubeComment { id = "HTC_115", persona = "шиппер_666", category = "Sequence", template = "романтика началась мило и закончилась как положено — скандалом", tone = "Drama", priority = 91, weight = 9, required = "Seq:romance_conflict", group = "seq_romance" },
            new HellTubeComment { id = "HTC_116", persona = "диванный_критик", category = "Sequence", template = "сначала уколы, потом взрыв — {actorA} доводили профессионально", tone = "Trash", priority = 90, weight = 9, required = "Seq:escalation_conflict", group = "seq_escalation" },
            new HellTubeComment { id = "HTC_117", persona = "мама_антихриста", category = "Sequence", template = "после драки показали, как им плохо. неожиданно по-человечески", tone = "Family", priority = 90, weight = 9, required = "Seq:conflict_aftermath", group = "seq_aftermath" },
            new HellTubeComment { id = "HTC_118", persona = "хейтер_шоу", category = "Editing", template = "снова драка. и снова. я понял, спасибо", tone = "Neutral", priority = 89, weight = 9, required = "Repetition", group = "repetition" },
            new HellTubeComment { id = "HTC_119", persona = "диванный_критик", category = "Editing", template = "{actorA} ссорится, потом поёт, потом опять ссорится. кто монтировал?", tone = "Neutral", priority = 88, weight = 9, required = "EditingBad", group = "bad_editing" },
            new HellTubeComment { id = "HTC_120", persona = "фанат_драмы", category = "Editing", template = "это сериал про {actor} или остальные просто массовка", tone = "Neutral", priority = 88, weight = 8, required = "OneActor", group = "one_actor" },
        };

        public static List<ViewerReview> Pick(IReadOnlyList<CapturedMoment> cut, SeasonTone season, int coherence, bool sponsorAired, int want,
            CutReport report = null, string brand = null)
        {
            var facts = Facts(cut, season, coherence, sponsorAired);
            if (report != null)
                Montage(facts, report);
            if (!string.IsNullOrEmpty(brand))
                facts.brand = brand;
            var eligible = new List<HellTubeComment>();
            var pool = Pool();
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] != null && Matches(pool[i], facts))
                    eligible.Add(pool[i]);
            }
            eligible.Sort(Compare);
            var picked = new List<HellTubeComment>();
            var groups = new HashSet<string>();
            var authors = new HashSet<string>();
            int generic = 0;
            for (int i = 0; i < eligible.Count && picked.Count < want; i++)
            {
                var c = eligible[i];
                string g = string.IsNullOrEmpty(c.group) ? c.id : c.group;
                // Один автор — один комментарий в ленте.
                if (authors.Contains(c.persona ?? ""))
                    continue;
                if (!groups.Add(g))
                    continue;
                authors.Add(c.persona ?? "");
                bool filler = c.category == "Generic";
                if (filler && generic >= 2)
                    continue;
                if (filler)
                    generic++;
                picked.Add(c);
            }
            var list = new List<ViewerReview>(picked.Count);
            for (int i = 0; i < picked.Count; i++)
                list.Add(ToReview(picked[i], facts));
            return list;
        }

        static HellTubeComment[] _pool;

        static HellTubeComment[] Pool()
        {
            if (_pool != null)
                return _pool;
            var asset = Resources.Load<HellTubeCommentPool>("Content/HellTubeComments");
            _pool = asset != null && asset.comments != null && asset.comments.Count > 0 ? asset.comments.ToArray() : All;
            return _pool;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPool()
        {
            _pool = null;
        }

        static int Compare(HellTubeComment a, HellTubeComment b)
        {
            int p = b.priority.CompareTo(a.priority);
            return p != 0 ? p : b.weight.CompareTo(a.weight);
        }

        class CutFacts
        {
            public readonly HashSet<string> tags = new HashSet<string>();
            public readonly List<string> actors = new List<string>();
            public string eventName = "эфир";
            public string brand = "бренд";
            public string tone = "шум";
            public string situation = "квартира";
            public int coherence;
            public bool sponsor;
            public bool blank;
            public bool reveal;
        }

        static CutFacts Facts(IReadOnlyList<CapturedMoment> cut, SeasonTone season, int coherence, bool sponsorAired)
        {
            var f = new CutFacts { coherence = coherence, sponsor = sponsorAired };
            if (season != null && season.TryLead(out ShowMood lead))
                f.tone = MoodStyle.Full(lead);
            int n = cut != null ? cut.Count : 0;
            for (int i = 0; i < n; i++)
            {
                var m = cut[i];
                if (m == null) continue;
                if (m.grade == CaptureGrade.Blank) f.blank = true;
                if (m.exposed != HiddenTrait.None) f.reveal = true;
                if (m.mood == ShowMood.Drama) f.tags.Add("Drama");
                if (m.mood == ShowMood.Trash) f.tags.Add("Trash");
                if (m.mood == ShowMood.Family) f.tags.Add("Family");
                if (m.tags != null)
                {
                    for (int t = 0; t < m.tags.Count; t++)
                    {
                        string tag = m.tags[t];
                        if (tag == MomentTags.Fight || tag == MomentTags.Conflict) { f.tags.Add("Conflict"); f.tags.Add("Trash"); }
                        else if (tag == MomentTags.Crying || tag == MomentTags.Misery) f.tags.Add("Drama");
                        else if (tag == MomentTags.Hug || tag == MomentTags.Warmth) { f.tags.Add("Family"); f.tags.Add("Romance"); }
                        else if (tag == MomentTags.Sponsor) { f.tags.Add("Sponsor"); f.sponsor = true; }
                        else if (tag == MomentTags.Fire || tag == MomentTags.Chaos) f.tags.Add("Trash");
                    }
                }
                if (m.actorNames != null)
                {
                    for (int a = 0; a < m.actorNames.Count; a++)
                    {
                        if (!string.IsNullOrEmpty(m.actorNames[a]) && !f.actors.Contains(m.actorNames[a]))
                            f.actors.Add(m.actorNames[a]);
                    }
                }
                if (!string.IsNullOrEmpty(m.Title) && f.eventName == "эфир")
                    f.eventName = m.Title;
            }
            if (f.coherence >= 70) f.tags.Add("Editing");
            if (f.coherence <= 30 && n >= 2) f.tags.Add("Editing");
            if (f.blank) f.tags.Add("Technical");
            if (f.reveal) f.tags.Add("Reveal");
            if (f.sponsor) f.tags.Add("Sponsor");
            return f;
        }

        // Факты монтажа (CutAnalysis): комбо, последовательности, качество склейки, повторы, «шоу одного».
        // Комментарий может сослаться только на то, что было в эфире: всё берётся из кадров ката.
        static void Montage(CutFacts f, CutReport r)
        {
            int n = r.clips.Count;
            f.tags.Remove("Editing");
            if (n >= 2 && r.coherence >= 70)
                f.tags.Add("EditingGood");
            if (n >= 2 && r.coherence <= 35)
                f.tags.Add("EditingBad");
            if (r.repetition >= 2)
                f.tags.Add("Repetition");
            if (n >= 3 && r.topActorCount >= n)
            {
                f.tags.Add("OneActor");
                f.actors.Remove(r.topActor);
                f.actors.Insert(0, r.topActor);
            }

            foreach (var c in r.combos)
                f.tags.Add("Combo:" + c.id);
            foreach (var clip in r.clips)
            {
                if (clip.roles.Contains(NarrativeRole.Romance))
                    f.tags.Add("Romance");
                if (clip.roles.Contains(NarrativeRole.Reveal))
                    f.tags.Add("Reveal");
                if (clip.roles.Contains(NarrativeRole.Conflict) || clip.roles.Contains(NarrativeRole.Climax))
                    f.tags.Add("Conflict");
            }

            for (int i = 0; i + 1 < n; i++)
            {
                var a = r.clips[i];
                var b = r.clips[i + 1];
                if (a.roles.Contains(NarrativeRole.Romance) && (b.roles.Contains(NarrativeRole.Conflict) || b.roles.Contains(NarrativeRole.Climax)))
                    f.tags.Add("Seq:romance_conflict");
                if (a.roles.Contains(NarrativeRole.Escalation) && (b.roles.Contains(NarrativeRole.Conflict) || b.roles.Contains(NarrativeRole.Climax)))
                    f.tags.Add("Seq:escalation_conflict");
                if ((a.roles.Contains(NarrativeRole.Conflict) || a.roles.Contains(NarrativeRole.Climax)) && (b.roles.Contains(NarrativeRole.Aftermath) || b.roles.Contains(NarrativeRole.Reaction)))
                    f.tags.Add("Seq:conflict_aftermath");
            }

            // В РАСКРЫТИЕ → РЕАКЦИЯ {actorB} — тот, кто реагирует.
            for (int i = 0; i + 1 < n; i++)
            {
                if (r.clips[i].roles.Contains(NarrativeRole.Reveal) && r.clips[i + 1].actors.Count > 0)
                {
                    string reacting = r.clips[i + 1].actors[0];
                    f.actors.Remove(reacting);
                    f.actors.Insert(Mathf.Min(1, f.actors.Count), reacting);
                    break;
                }
            }
        }

        static bool Matches(HellTubeComment c, CutFacts f)
        {
            if (string.IsNullOrEmpty(c.required) || c.required == "Generic")
                return true;
            return f.tags.Contains(c.required);
        }

        static ViewerReview ToReview(HellTubeComment c, CutFacts f)
        {
            string a = f.actors.Count > 0 ? f.actors[0] : "кто-то";
            string b = f.actors.Count > 1 ? f.actors[1] : a;
            string body = (c.template ?? "")
                .Replace("{actorA}", a).Replace("{actorB}", b).Replace("{actor}", a)
                .Replace("{event}", f.eventName).Replace("{brand}", f.brand)
                .Replace("{tone}", f.tone).Replace("{situation}", f.situation)
                .Replace("{combo}", f.eventName).Replace("{episode}", "этот выпуск");
            int score = 5;
            if (c.tone == "Drama") score = 7;
            else if (c.tone == "Trash") score = 8;
            else if (c.tone == "Family") score = 7;
            // Оценка зрителя следует за монтажом: каша и повторы — ниже, комбо и хорошая склейка — выше.
            if (c.required == "EditingBad" || c.required == "Repetition" || c.required == "OneActor")
                score = 3;
            else if (c.required == "EditingGood" || (c.required != null && c.required.StartsWith("Combo:")))
                score = Mathf.Max(score, 8);
            if (c.priority >= 80) score = Mathf.Min(10, score + 1);
            if (c.priority <= 20) score = Mathf.Max(1, score - 1);
            string author = (c.persona ?? "зритель").Replace("{actor}", a);
            return new ViewerReview { author = author, score = score, body = body };
        }
    }
}
