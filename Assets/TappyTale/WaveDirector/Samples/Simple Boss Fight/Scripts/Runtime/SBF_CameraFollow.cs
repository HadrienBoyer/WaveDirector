using UnityEngine;

namespace TappyTale.Samples.SimpleBossFight
{
    public sealed class SBF_CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 8f, -8f);
        [SerializeField] private float positionLerp = 18f;
        [SerializeField] private bool lookAtTarget = true;
        [SerializeField] private float rotationLerp = 12f;

        public Transform Target { get => target; set => target = value; }

        private void Awake()
        {
            // Enforce the sample's default 45-degree top-down framing (y ~= -z magnitude).
            if (Mathf.Abs(offset.y - Mathf.Abs(offset.z)) > 0.01f || offset.y < 4f)
            {
                offset = new Vector3(0f, 8f, -8f);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.TransformPoint(offset);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-positionLerp * Time.deltaTime));

            if (!lookAtTarget) return;
            Vector3 toTarget = target.position - transform.position;
            if (toTarget.sqrMagnitude <= 0.0001f) return;
            var desiredRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, rotationLerp) * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, t);
        }
    }
}
