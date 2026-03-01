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

        private float lastFireTime;
        public GameObject ProjectilePrefab => projectilePrefab;

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

            Vector3 forward = cam != null ? cam.transform.forward : transform.forward;
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
    }
}
