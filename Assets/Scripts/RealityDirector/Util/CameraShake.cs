using UnityEngine;

namespace RealityDirector.Util
{
    public class CameraShake : MonoBehaviour
    {
        public Vector3 Base;
        float _until;
        float _start;
        float _duration;
        float _magnitude;
        Vector2 _kick;
        float _roll;

        public void Punch(float magnitude, float duration)
        {
            if (duration <= 0f)
                return;
            _magnitude = magnitude;
            _duration = duration;
            _start = Time.time;
            _until = _start + duration;
            Vector2 dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.01f)
                dir = Vector2.down;
            _kick = dir.normalized * magnitude;
            _roll = Random.Range(-1f, 1f) * Mathf.Clamp(magnitude / 0.12f, 0.35f, 1.4f);
        }

        void LateUpdate()
        {
            if (_duration <= 0f || Time.time >= _until)
            {
                transform.position = Base;
                transform.rotation = Quaternion.identity;
                return;
            }

            float t = Mathf.Clamp01((Time.time - _start) / _duration);
            float fall = (1f - t) * (1f - t);
            Vector2 noise = Random.insideUnitCircle * (_magnitude * 0.4f * fall);
            transform.position = Base + (Vector3)(_kick * fall + noise);
            transform.rotation = Quaternion.Euler(0f, 0f, _roll * fall);
        }
    }
}
