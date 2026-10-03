using System.Collections.Generic;
using RealityDirector.Events;
using RealityDirector.Meta;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Комната маркетинга: как игрок увидит предложения (что получит, срок, выплата, требование) и проверки данных.
    [CustomEditor(typeof(MarketingRoomDefinition))]
    public class MarketingRoomEditor : UnityEditor.Editor
    {
        static bool _preview = true;

        public override void OnInspectorGUI()
        {
            var room = (MarketingRoomDefinition)target;
            EditorGUILayout.HelpBox("Маркетинг на карте выпуска: слева ПОКУПКИ (нал выпуска → карта или бонус), справа КОНТРАКТЫ "
                                    + "(карта бренда; платят, если кадр с брендом попадёт в эфир). «Показывать» — сколько предложений "
                                    + "выпадет в этой комнате (для узла карты выбор постоянный).", MessageType.None);
            DrawDefaultInspector();

            var cards = new Dictionary<string, EventDefinition>();
            foreach (var c in DesignerData.LoadAll<EventDefinition>())
            {
                if (c != null && !string.IsNullOrEmpty(c.id))
                    cards[c.id] = c;
            }

            EditorGUILayout.Space(8);
            _preview = EditorGUILayout.Foldout(_preview, "Как увидит игрок (репутация 25, нал 90, контракт 1)", true);
            if (_preview)
            {
                var season = new SeasonState();
                var ep = new EpisodeState { cash = 90 };
                ep.EnsureLists();
                foreach (var o in room.offers)
                {
                    if (o == null)
                        continue;
                    var v = MarketingDesk.Describe(o, season, ep, 1, id => cards.TryGetValue(id ?? "", out var d) ? d : null);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField((v.contract ? "КОНТРАКТ · " + (string.IsNullOrEmpty(v.brand) ? "" : v.brand.ToUpperInvariant() + " · ") : "ПОКУПКА · ") + v.title + "   " + v.price, EditorStyles.boldLabel);
                        if (v.contract)
                        {
                            EditorGUILayout.LabelField("Задача: " + v.task, EditorStyles.wordWrappedMiniLabel);
                            EditorGUILayout.LabelField("Карта: «" + v.card + "» · " + v.lifetime, EditorStyles.wordWrappedMiniLabel);
                            EditorGUILayout.LabelField("Успех: " + v.success + " · Провал: " + v.fail + (string.IsNullOrEmpty(v.requirement) ? "" : " · Требование: " + v.requirement), EditorStyles.wordWrappedMiniLabel);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Получите: " + v.gets, EditorStyles.wordWrappedMiniLabel);
                            if (!string.IsNullOrEmpty(v.lifetime))
                                EditorGUILayout.LabelField("Действует: " + v.lifetime, EditorStyles.wordWrappedMiniLabel);
                        }

                        if (!v.available && !string.IsNullOrEmpty(v.reason))
                            EditorGUILayout.LabelField("Сейчас нельзя: " + v.reason, EditorStyles.miniBoldLabel);
                    }
                }
            }

            foreach (var w in Warnings(room, cards))
                EditorGUILayout.HelpBox(w, MessageType.Warning);
        }

        static List<string> Warnings(MarketingRoomDefinition room, Dictionary<string, EventDefinition> cards)
        {
            var list = new List<string>();
            var ids = new HashSet<string>();
            int buys = 0;
            int deals = 0;
            foreach (var o in room.offers)
            {
                if (o == null)
                    continue;
                if (o.kind == OfferKind.Contract)
                    deals++;
                else
                    buys++;
                string name = string.IsNullOrEmpty(o.title) ? o.id : o.title;
                if (string.IsNullOrEmpty(o.id))
                    list.Add("«" + name + "»: нет id — «куплено» запомнится по названию.");
                else if (!ids.Add(o.id))
                    list.Add("Повтор id «" + o.id + "».");
                if (!string.IsNullOrEmpty(o.cardId) && !cards.ContainsKey(o.cardId))
                    list.Add("«" + name + "»: карты «" + o.cardId + "» нет.");
                if (o.kind == OfferKind.Contract && string.IsNullOrEmpty(o.cardId))
                    list.Add("«" + name + "»: контракт без карты бренда — его нечем выполнить.");
                if (o.kind == OfferKind.Contract && o.payout <= 0)
                    list.Add("«" + name + "»: контракт без выплаты.");
                if (o.kind == OfferKind.Purchase && o.price <= 0)
                    list.Add("«" + name + "»: покупка бесплатная — так задумано?");
                if (o.kind == OfferKind.Purchase && string.IsNullOrEmpty(o.cardId) && string.IsNullOrEmpty(o.flag))
                    list.Add("«" + name + "»: покупка ничего не даёт (нет ни карты, ни флага).");
                if (!MarketingDesk.Supports(o.flag))
                    list.Add("«" + name + "»: флаг «" + o.flag + "» игра не исполняет — в комнате будет «пока недоступно».");
                if (!string.IsNullOrEmpty(o.cardId) && cards.TryGetValue(o.cardId, out var card) && !RealityDirector.Cards.CardRuntime.Playable(card))
                    list.Add("«" + name + "»: карта «" + card.displayName + "» NOT RUNTIME SUPPORTED.");
            }

            if (room.purchasesShown > buys)
                list.Add("Показывать покупок " + room.purchasesShown + ", а их всего " + buys + ".");
            if (room.contractsShown > deals)
                list.Add("Показывать контрактов " + room.contractsShown + ", а их всего " + deals + ".");
            return list;
        }
    }
}
