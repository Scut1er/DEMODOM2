using RealityDirector.Meta;
using RealityDirector.Util;
using UnityEditor;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    [CustomEditor(typeof(ActorDefinition))]
    public class ActorDefinitionEditor : UnityEditor.Editor
    {
        static readonly Face[] Faces = { Face.Neutral, Face.Happy, Face.Love, Face.Mad, Face.Sad, Face.Scared, Face.Tired };
        static readonly BodyPose[] Poses = { BodyPose.Neutral, BodyPose.Happy, BodyPose.Mad, BodyPose.Sad, BodyPose.Scared };

        public override void OnInspectorGUI()
        {
            var actor = (ActorDefinition)target;
            DrawPreview(actor);
            DrawDefaultInspector();

            if (actor.portrait == null && GameArt.HeadByPrefix(actor.artPrefix, Face.Neutral) == null && GameArt.Head(actor.Id, Face.Neutral) == null)
                EditorGUILayout.HelpBox("Нет портрета: задайте portrait или artPrefix, для которого есть файлы <prefix>_neutral / _happy / _love / _mad / _sad / _scared / _tired " +
                                        "в Resources/Art/Characters. Лица можно рисовать не все — недостающее подменится ближайшим.", MessageType.Warning);
            string body = string.IsNullOrEmpty(actor.bodyPrefix) ? GameArt.DefaultBody : actor.bodyPrefix;
            if (GameArt.ExactBody(body, BodyPose.Neutral) == null)
                EditorGUILayout.HelpBox("Нет файла " + body + "_neutral в Resources/Art/Characters — в квартире будет общее тело (body).", MessageType.Warning);
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

            string prefix = string.IsNullOrEmpty(actor.artPrefix) ? null : actor.artPrefix;
            foreach (var face in Faces)
            {
                var sprite = prefix != null ? GameArt.ExactHead(prefix, face) : GameArt.Head(actor.Id, face);
                if (sprite == null)
                    continue;
                Draw(new Rect(x, rect.y, 70, 70), sprite);
                GUI.Label(new Rect(x, rect.y + 56, 70, 16), face.ToString(), EditorStyles.centeredGreyMiniLabel);
                x += 76;
            }

            if (x == rect.x)
                GUI.Label(rect, "(нет арта)", EditorStyles.centeredGreyMiniLabel);

            // Наряд: позы, которые нарисованы (остальные в игре — нейтральная этого наряда).
            string body = string.IsNullOrEmpty(actor.bodyPrefix) ? GameArt.DefaultBody : actor.bodyPrefix;
            var row = GUILayoutUtility.GetRect(10, 74, GUILayout.ExpandWidth(true));
            float bx = row.x;
            foreach (var pose in Poses)
            {
                var sprite = GameArt.ExactBody(body, pose);
                if (sprite == null)
                    continue;
                Draw(new Rect(bx, row.y, 70, 70), sprite);
                GUI.Label(new Rect(bx, row.y + 56, 70, 16), pose.ToString(), EditorStyles.centeredGreyMiniLabel);
                bx += 76;
            }

            if (bx == row.x)
                GUI.Label(row, "(нет наряда " + body + ")", EditorStyles.centeredGreyMiniLabel);
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
