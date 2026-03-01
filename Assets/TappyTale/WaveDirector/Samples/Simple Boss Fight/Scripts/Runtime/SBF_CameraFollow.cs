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
        [Header("Vertical Orbit")]
        [SerializeField] private bool allowVerticalOrbit = true;
        [SerializeField] private float verticalOrbitSensitivity = 0.2f;
        [SerializeField] private float minPitch = 30f;
        [SerializeField] private float maxPitch = 75f;

        public Transform Target { get => target; set => target = value; }
        public bool AllowsVerticalOrbit => allowVerticalOrbit;

        private float orbitPitch = 45f;
        private float orbitDistance = 11.3137f;

        private void Awake()
        {
            // Enforce the sample's default 45-degree top-down framing (y ~= -z magnitude).
            if (Mathf.Abs(offset.y - Mathf.Abs(offset.z)) > 0.01f || offset.y < 4f)
            {
                offset = new Vector3(0f, 8f, -8f);
            }

            RebuildOrbitFromOffset();
        }

        public void AddVerticalLook(float lookDeltaY)
        {
            if (!allowVerticalOrbit) return;
            if (Mathf.Abs(lookDeltaY) <= 0.0001f) return;

            orbitPitch = Mathf.Clamp(orbitPitch - lookDeltaY * verticalOrbitSensitivity, minPitch, maxPitch);
            UpdateOffsetFromOrbit();
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

        private void RebuildOrbitFromOffset()
        {
            orbitDistance = Mathf.Max(1f, new Vector2(offset.y, offset.z).magnitude);
            float y01 = Mathf.Clamp(offset.y / orbitDistance, -1f, 1f);
            orbitPitch = Mathf.Clamp(Mathf.Asin(y01) * Mathf.Rad2Deg, minPitch, maxPitch);
            UpdateOffsetFromOrbit();
        }

        private void UpdateOffsetFromOrbit()
        {
            float rad = orbitPitch * Mathf.Deg2Rad;
            float y = Mathf.Sin(rad) * orbitDistance;
            float back = Mathf.Cos(rad) * orbitDistance;
            offset = new Vector3(offset.x, y, -back);
        }
    }
}
