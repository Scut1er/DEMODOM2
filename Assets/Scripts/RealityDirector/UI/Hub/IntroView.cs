using System;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.UI.Hub
{
    public class IntroView : MonoBehaviour
    {
        [SerializeField] Button next;

        public event Action Next;

        void Awake()
        {
            if (next != null)
                next.onClick.AddListener(() => Next?.Invoke());
        }
    }
}
