using RealityDirector.Meta;
using RealityDirector.Util;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    [CustomEditor(typeof(ActorDefinition))]
    public class ActorDefinitionEditor : UnityEditor.Editor
    {
        static readonly Face[] Faces = { Face.Happy, Face.Love, Face.Mad, Face.Sad };

        public override void OnInspectorGUI()
        {
            var actor = (ActorDefinition)target;
            DrawPreview(actor);
            DrawDefaultInspector();

            if (actor.portrait == null && GameArt.HeadByPrefix(actor.artPrefix, Face.Happy) == null && GameArt.Head(actor.Id, Face.Happy) == null)
                EditorGUILayout.HelpBox("Нет портрета: задайте portrait или artPrefix, для которого есть файлы <prefix>_happy / _mad / _sad / _love в Resources/Art/Characters.", MessageType.Warning);
            if (actor.Id != "npc_zloi" && actor.Id != "npc_dobryak")
                EditorGUILayout.HelpBox("Участник появится в хабе. В съёмке (квартира) пока живут только npc_zloi и npc_dobryak — " +
                                        "новые участники попадут туда после шага 8 вместе с кором.", MessageType.Info);
            if (!DesignerData.InFolder(actor, DesignerData.CharactersRoot))
                EditorGUILayout.HelpBox("Ассет лежит не в " + DesignerData.CharactersRoot + " — в игру не попадёт.", MessageType.Warning);
        }

        static void DrawPreview(ActorDefinition actor)
        {
            var rect = GUILayoutUtility.GetRect(10, 74, GUILayout.ExpandWidth(true));
            float x = rect.x;
            if (actor.portrait != null)
            {
                Draw(new Rect(x, rect.y, 70, 70), actor.portrait);
                x += 76;
            }

            foreach (var face in Faces)
            {
                var sprite = GameArt.HeadByPrefix(actor.artPrefix, face) ?? GameArt.Head(actor.Id, face);
                if (sprite == null)
                    continue;
                Draw(new Rect(x, rect.y, 70, 70), sprite);
                GUI.Label(new Rect(x, rect.y + 56, 70, 16), face.ToString(), EditorStyles.centeredGreyMiniLabel);
                x += 76;
            }

            if (x == rect.x)
                GUI.Label(rect, "(нет арта)", EditorStyles.centeredGreyMiniLabel);
        }

        static void Draw(Rect r, Sprite sprite)
        {
            var tex = sprite.texture;
            var t = sprite.textureRect;
            var uv = new Rect(t.x / tex.width, t.y / tex.height, t.width / tex.width, t.height / tex.height);
            GUI.DrawTextureWithTexCoords(r, tex, uv, true);
        }
    }
}
