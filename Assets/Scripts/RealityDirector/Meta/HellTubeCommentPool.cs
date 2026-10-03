using System.Collections.Generic;
using UnityEngine;

namespace RealityDirector.Meta
{
    // Пул комментариев HellTube в ассете (Resources/Content/HellTubeComments): дизайнер правит тексты, приоритеты и условия.
    // Нет ассета — встроенный пул из кода.
    [CreateAssetMenu(menuName = "RealityDirector/HellTube Comments", fileName = "HellTubeComments")]
    public class HellTubeCommentPool : ScriptableObject
    {
        public List<HellTubeComment> comments = new List<HellTubeComment>();
    }
}
