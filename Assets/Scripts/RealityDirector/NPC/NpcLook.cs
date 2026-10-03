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
        string _outfit;
        Face _face = (Face)(-1);
        BodyPose _pose = (BodyPose)(-1);

        // Подменяет рисованное тело артом, если он есть для этого участника. Иначе ничего не делает.
        public static NpcLook Attach(NPCController npc, Transform visual, SpriteRenderer fallback)
        {
            if (npc == null || visual == null)
                return null;
            string outfit = GameArt.BodyPrefix(npc.Id);
            if (GameArt.Head(npc.Id, Face.Neutral) == null || GameArt.Body(outfit, BodyPose.Neutral) == null)
                return null;

            int order = fallback != null ? fallback.sortingOrder : 10;
            if (fallback != null)
                fallback.enabled = false;

            var look = visual.gameObject.AddComponent<NpcLook>();
            look._npc = npc;
            look._outfit = outfit;
            look._body = SpriteUtil.Show(visual, "artBody", new Vector3(0f, FeetY, 0f), null, order);
            look._head = SpriteUtil.Show(visual, "artHead", Vector3.zero, null, order + 1);
            look.Apply(Face.Neutral, BodyPose.Neutral);
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
                face = Face.Scared;
                pose = BodyPose.Scared;
            }
            else if (emote.Contains("слёз"))
            {
                face = Face.Sad;
                pose = BodyPose.Sad;
            }
            else if (emote.Contains("холод"))
            {
                face = Face.Tired;
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
            else if (_npc.Anger >= NPCController.StrongMood && _npc.Anger >= _npc.Stress && _npc.Anger >= _npc.Sadness)
            {
                // Без реплики лицо всё равно выдаёт, что внутри: злость, стресс, грусть — с 40.
                face = Face.Mad;
                pose = BodyPose.Neutral;
            }
            else if (_npc.Stress >= NPCController.StrongMood && _npc.Stress >= _npc.Sadness)
            {
                face = Face.Scared;
                pose = BodyPose.Neutral;
            }
            else if (_npc.Sadness >= NPCController.StrongMood)
            {
                face = Face.Sad;
                pose = BodyPose.Neutral;
            }
            else
            {
                // Спокоен — нейтральное лицо; у кого его нет (Злой, Добряк), подменится улыбкой, как раньше.
                face = Face.Neutral;
                pose = BodyPose.Neutral;
            }
        }

        void Apply(Face face, BodyPose pose)
        {
            _face = face;
            _pose = pose;
            // Позы нет в наряде — нейтральная того же наряда, и шея по ней.
            BodyPose drawn = GameArt.ExactBody(_outfit, pose) != null ? pose : BodyPose.Neutral;
            var body = GameArt.Body(_outfit, drawn);
            var head = GameArt.Head(_npc.Id, face);
            _body.sprite = body;
            _head.sprite = head;
            if (body == null)
                return;

            Vector2 size = body.bounds.size;
            Vector2 neck = Neck[(int)drawn];
            _head.transform.localPosition = new Vector3((neck.x - 0.5f) * size.x, FeetY + neck.y * size.y - NeckOverlap, 0f);
        }
    }
}
