using System;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    public class SeasonEndView : MonoBehaviour
    {
        [SerializeField] Text body;
        [SerializeField] Button newSeason;
        [SerializeField] Button menu;

        public event Action NewSeason;
        public event Action Menu;

        void Awake()
        {
            if (newSeason != null)
                newSeason.onClick.AddListener(() => NewSeason?.Invoke());
            if (menu != null)
                menu.onClick.AddListener(() => Menu?.Invoke());
        }

        public void Show(string text)
        {
            if (body != null)
                body.text = text;
        }
    }
}
