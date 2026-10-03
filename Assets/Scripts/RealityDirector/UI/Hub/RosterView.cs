using System;
using RealityDirector.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    // Колонка «Участники»: занятые места, свободные и закрытые до апгрейда кастинга.
    public class RosterView : MonoBehaviour
    {
        [SerializeField] Text header;
        [SerializeField] RectTransform list;
        [SerializeField] CastRowView rowPrefab;
        [SerializeField] Button invite;

        public event Action Invite;

        void Awake()
        {
            if (invite != null)
                invite.onClick.AddListener(() => Invite?.Invoke());
        }

        public void Show(CastMember[] members, int castLevel)
        {
            int seats = CastRoster.Seats(castLevel);
            int filled = Mathf.Min(members.Length, seats);
            if (header != null)
                header.text = "УЧАСТНИКИ (" + filled + "/" + seats + ")";
            if (list == null || rowPrefab == null)
                return;

            for (int i = list.childCount - 1; i >= 0; i--)
            {
                var old = list.GetChild(i).gameObject;
                old.SetActive(false);
                Destroy(old);
            }

            for (int i = 0; i < CastRoster.MaxSeats; i++)
            {
                var row = Instantiate(rowPrefab, list);
                if (i < filled)
                {
                    var m = members[i];
                    row.name = rowPrefab.name + "_" + m.id;
                    string secret = castLevel >= 3 ? m.secretKnown : m.secretHidden;
                    string about = string.Join("\n", m.traits);
                    if (!string.IsNullOrEmpty(secret))
                        about += "\n" + secret;
                    row.Show(m.name, about, m.portrait, false);
                }
                else if (i < seats)
                {
                    row.name = rowPrefab.name + "_free" + i;
                    row.Show("Свободное место", "персонаж в разработке", null, false);
                }
                else
                {
                    row.name = rowPrefab.name + "_locked" + i;
                    row.Show("Закрыто", "Кастинг ур. " + (i), null, true);
                }
            }
        }
    }
}
