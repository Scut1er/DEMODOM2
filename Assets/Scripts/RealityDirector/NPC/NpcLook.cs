using RealityDirector.Util;
using UnityEngine;

namespace RealityDirector.NPC
{
    // Голова + тело участника из арта художника. Лицо и поза меняются по состоянию NPCController.
    // Логику NPC не трогает — только читает Action / HasRage / IsFighting / Emote.
    public class NpcLook : MonoBehaviour
    {
        const float FeetY = -0.7f;
        const float NeckOverlap = 0.1f;

        // Где шея на картинке тела (доля ширины/высоты от левого нижнего угла), по BodyPose.
        static readonly Vector2[] Neck =
        {
            new Vector2(0.5f, 1f),     // Neutral
            new Vector2(0.53f, 0.86f), // Happy — руки подняты выше шеи
            new Vector2(0.5f, 1f),     // Mad
            new Vector2(0.5f, 1f),     // Sad (сидит)
            new Vector2(0.53f, 1f)     // Scared
        };

        NPCController _npc;
        SpriteRenderer _head;
        SpriteRenderer _body;
        Face _face = (Face)(-1);
        BodyPose _pose = (BodyPose)(-1);

        // Подменяет рисованное тело артом, если он есть для этого участника. Иначе ничего не делает.
        public static NpcLook Attach(NPCController npc, Transform visual, SpriteRenderer fallback)
        {
            if (npc == null || visual == null || GameArt.Head(npc.Id, Face.Happy) == null || GameArt.Body(BodyPose.Neutral) == null)
                return null;

            int order = fallback != null ? fallback.sortingOrder : 10;
            if (fallback != null)
                fallback.enabled = false;

            var look = visual.gameObject.AddComponent<NpcLook>();
            look._npc = npc;
            look._body = SpriteUtil.Show(visual, "artBody", new Vector3(0f, FeetY, 0f), null, order);
            look._head = SpriteUtil.Show(visual, "artHead", Vector3.zero, null, order + 1);
            look.Apply(Face.Happy, BodyPose.Neutral);
            return look;
        }

        void LateUpdate()
        {
            if (_npc == null)
                return;
            Pick(out Face face, out BodyPose pose);
            if (face != _face || pose != _pose)
                Apply(face, pose);
        }

        void Pick(out Face face, out BodyPose pose)
        {
            string emote = _npc.Emote ?? "";
            if (_npc.IsFighting || _npc.HasRage || _npc.Action == NpcActionId.SeekFight || emote == "злость")
            {
                face = Face.Mad;
                pose = BodyPose.Mad;
            }
            else if (_npc.Action == NpcActionId.Panic)
            {
                face = Face.Sad;
                pose = BodyPose.Scared;
            }
            else if (emote.Contains("слёз"))
            {
                face = Face.Sad;
                pose = BodyPose.Sad;
            }
            else if (emote.Contains("холод"))
            {
                face = Face.Sad;
                pose = BodyPose.Scared;
            }
            else if (emote.Contains("уют"))
            {
                face = Face.Love;
                pose = BodyPose.Happy;
            }
            else if (emote == "ла-ла" || emote == "раз-два" || emote == "пранк")
            {
                face = Face.Happy;
                pose = BodyPose.Happy;
            }
            else
            {
                face = Face.Happy;
                pose = BodyPose.Neutral;
            }
        }

        void Apply(Face face, BodyPose pose)
        {
            _face = face;
            _pose = pose;
            var body = GameArt.Body(pose) ?? GameArt.Body(BodyPose.Neutral);
            var head = GameArt.Head(_npc.Id, face) ?? GameArt.Head(_npc.Id, Face.Happy);
            _body.sprite = body;
            _head.sprite = head;
            if (body == null)
                return;

            Vector2 size = body.bounds.size;
            Vector2 neck = Neck[(int)pose];
            _head.transform.localPosition = new Vector3((neck.x - 0.5f) * size.x, FeetY + neck.y * size.y - NeckOverlap, 0f);
        }
    }
}
