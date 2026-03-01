using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TappyTale.Samples.SimpleBossFight
{
    public sealed class SBF_Gun : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private Transform muzzleTransform;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileDamage = 25f;
        [SerializeField] private float projectileSpeed = 45f;
        [SerializeField] private float projectileLifetime = 2.5f;
        [SerializeField] private float muzzleDistance = 0.6f;
        [SerializeField] private float fireCooldown = 0.15f;
        [Header("Auto Targeting")]
        [SerializeField] private bool autoTargetByLook = true;
        [SerializeField] private float autoTargetMaxAngle = 14f;
        [SerializeField] private float autoTargetMaxDistance = 64f;
        [SerializeField] private float targetAimHeight = 0.9f;

        private float lastFireTime;
        private SBF_Health currentTarget;

        public GameObject ProjectilePrefab => projectilePrefab;
        public SBF_Health CurrentTarget => currentTarget;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (muzzleTransform == null)
            {
                var body = transform.Find("Body");
                if (body != null) muzzleTransform = body;
            }
        }

        private void Update()
        {
            currentTarget = ResolveTargetByLook();

            if (!IsFirePressed()) return;
            if (Time.time < lastFireTime + fireCooldown) return;

            lastFireTime = Time.time;
            FireProjectile();
        }

        private bool IsFirePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }

        private void FireProjectile()
        {
            if (projectilePrefab == null) return;

            Vector3 forward = muzzleTransform != null ? muzzleTransform.forward : transform.forward;
            if (currentTarget != null)
            {
                var aimPoint = GetAimPoint(currentTarget.transform);
                var muzzleBase = muzzleTransform != null ? muzzleTransform.position : transform.position + Vector3.up * 0.9f;
                var toTarget = aimPoint - muzzleBase;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    forward = toTarget.normalized;
                }
            }

            Vector3 origin = muzzleTransform != null
                ? muzzleTransform.position + forward * muzzleDistance
                : transform.position + Vector3.up * 0.9f + forward * muzzleDistance;
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
            var go = Instantiate(projectilePrefab, origin, rot);
            if (go == null) return;

            var projectile = go.GetComponent<SBF_Projectile>();
            if (projectile != null)
            {
                projectile.Launch(
                    forward,
                    projectileSpeed,
                    projectileDamage,
                    projectileLifetime,
                    SBF_Team.Player);
            }
        }

        public void SetCamera(Camera cameraRef)
        {
            cam = cameraRef;
        }

        public void SetMuzzle(Transform muzzle)
        {
            muzzleTransform = muzzle;
        }

        private SBF_Health ResolveTargetByLook()
        {
            if (!autoTargetByLook) return null;

            var all = SBF_Health.ActiveEntities;
            SBF_Health best = null;
            float bestScore = float.MaxValue;
            var lookPos = muzzleTransform != null ? muzzleTransform.position : transform.position + Vector3.up * 0.9f;
            var lookForward = muzzleTransform != null ? muzzleTransform.forward : transform.forward;
            float maxDistanceSqr = autoTargetMaxDistance * autoTargetMaxDistance;

            for (int i = 0; i < all.Count; i++)
            {
                var h = all[i];
                if (h == null) continue;
                if (!h.isActiveAndEnabled) continue;
                if (h.Team != SBF_Team.Enemy) continue;
                if (h.Hp <= 0f) continue;

                var aimPoint = GetAimPoint(h.transform);
                Vector3 to = aimPoint - lookPos;
                float distSqr = to.sqrMagnitude;
                if (distSqr > maxDistanceSqr || distSqr <= 0.0001f) continue;

                float angle = Vector3.Angle(lookForward, to.normalized);
                if (angle > autoTargetMaxAngle) continue;

                float score = angle * 100f + Mathf.Sqrt(distSqr);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = h;
                }
            }

            return best;
        }

        private Vector3 GetAimPoint(Transform t)
        {
            if (t == null) return Vector3.zero;

            var col = t.GetComponentInChildren<Collider>();
            if (col != null)
            {
                var c = col.bounds.center;
                c.y = Mathf.Max(c.y, t.position.y + targetAimHeight);
                return c;
            }

            return t.position + Vector3.up * targetAimHeight;
        }
    }
}
