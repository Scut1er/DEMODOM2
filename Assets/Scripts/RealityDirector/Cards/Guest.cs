using System.Collections;
using RealityDirector.Meta;
using RealityDirector.NPC;
using RealityDirector.Util;
using UnityEngine;

namespace RealityDirector.Cards
{
    // Гость по карте («бывший», «третий»): входит из прихожей, идёт к цели, говорит, уходит.
    // Это не участник шоу — только фигура с репликами, но цель и её пара реагируют по-настоящему.
    public class Guest : MonoBehaviour
    {
        const float FeetY = -0.7f;

        CardStage _stage;
        NPCController _target;
        bool _third;
        string _line = "";
        float _lineUntil;
        Transform _visual;

        public string Line => Time.time < _lineUntil ? _line : "";
        public int Love = 3;
        public int Rage = 3;

        public static Guest Spawn(CardStage stage, string role, NPCController target, bool third)
        {
            string id = PickActor(stage);
            var go = new GameObject("Guest_" + role);
            go.transform.position = HouseMap.FrontDoor;
            var guest = go.AddComponent<Guest>();
            guest._stage = stage;
            guest._target = target;
            guest._third = third;
            guest.Build(id);
            stage.Fx.Label(go.transform, role.ToUpperInvariant(), new Color(1f, 0.65f, 0.8f), new Vector2(0f, -95f));
            guest.StartCoroutine(guest.Walk());
            return guest;
        }

        static string PickActor(CardStage stage)
        {
            var roster = CastRoster.All();
            for (int i = 0; i < roster.Length; i++)
            {
                bool inCast = false;
                foreach (var npc in stage.Alive())
                {
                    if (npc.Id == roster[i].id)
                        inCast = true;
                }

                if (!inCast && GameArt.Head(roster[i].id, Face.Neutral) != null)
                    return roster[i].id;
            }

            return "npc_max";
        }

        void Build(string id)
        {
            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);
            string outfit = GameArt.BodyPrefix(id);
            var bodySprite = GameArt.Body(outfit, BodyPose.Neutral);
            var body = SpriteUtil.Show(_visual, "body", new Vector3(0f, FeetY, 0f), bodySprite, 10);
            var head = SpriteUtil.Show(_visual, "head", Vector3.zero, GameArt.Head(id, Face.Neutral), 11);
            // Гость чуть темнее участников — видно, что он «чужой» на площадке.
            body.color = new Color(0.85f, 0.8f, 0.9f, 1f);
            head.color = new Color(0.9f, 0.85f, 0.95f, 1f);
            if (bodySprite != null)
                head.transform.localPosition = new Vector3(0f, FeetY + bodySprite.bounds.size.y - 0.1f, 0f);
            var glow = SpriteUtil.Show(_visual, "ring", new Vector3(0f, -0.15f, 0f), IllustratedArt.Glow, 8);
            SpriteUtil.Fit(glow, new Vector2(1.2f, 0.6f));
            glow.color = new Color(1f, 0.5f, 0.75f, 0.6f);
        }

        public void Say(string text, float seconds)
        {
            _line = text;
            _lineUntil = Time.time + seconds;
            _stage.Fx.FloatText(() => transform.position + Vector3.up * 1.7f, "«" + text + "»", new Color(1f, 0.85f, 0.9f), 22, seconds);
        }

        IEnumerator Walk()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                yield break;
            }

            Sfx.Play(Cue.Bell, 0.45f, 1.3f);
            float t = 0f;
            while (_target != null && t < 9f)
            {
                t += Time.deltaTime;
                Vector2 goal = (Vector2)_target.transform.position + new Vector2(-0.95f, 0f);
                if (MoveTo(goal, 2.2f))
                    break;
                yield return null;
            }

            _stage.GuestArrived(this, _target, _third);
            float stay = 0f;
            while (stay < 9f && _target != null)
            {
                stay += Time.deltaTime;
                Vector2 goal = (Vector2)_target.transform.position + new Vector2(-1f, 0f);
                MoveTo(goal, 1.6f);
                Bob();
                yield return null;
            }

            Say(_third ? "ну, я пошёл" : "я ещё вернусь", 2f);
            t = 0f;
            while (t < 8f)
            {
                t += Time.deltaTime;
                if (MoveTo(HouseMap.FrontDoor, 2.4f))
                    break;
                yield return null;
            }

            _stage.ForgetGuest(this);
            Destroy(gameObject);
        }

        bool MoveTo(Vector2 goal, float speed)
        {
            Vector2 p = transform.position;
            Vector2 next = Vector2.MoveTowards(p, goal, speed * Time.deltaTime);
            float dx = next.x - p.x;
            if (Mathf.Abs(dx) > 0.001f && _visual != null)
            {
                var s = _visual.localScale;
                s.x = Mathf.Abs(s.x) * (dx >= 0f ? 1f : -1f);
                _visual.localScale = s;
            }

            transform.position = next;
            if (_visual != null)
                _visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.04f, 0f);
            return Vector2.Distance(next, goal) < 0.15f;
        }

        void Bob()
        {
            if (_visual != null)
                _visual.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2.4f) * 0.03f, 0f);
        }
    }
}
