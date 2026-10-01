using UnityEngine;

namespace RealityDirector.Util
{
    public class CameraShake : MonoBehaviour
    {
        public Vector3 Base;
        float _until;
        float _magnitude;

        public void Punch(float magnitude, float duration)
        {
            _magnitude = magnitude;
            _until = Time.time + duration;
        }

        void LateUpdate()
        {
            if (Time.time < _until)
            {
                transform.position = Base + (Vector3)(Random.insideUnitCircle * _magnitude);
                return;
            }

            transform.position = Base;
        }
    }
}
