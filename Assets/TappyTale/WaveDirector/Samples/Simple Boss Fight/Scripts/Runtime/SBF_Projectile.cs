using UnityEngine;

namespace TappyTale.Samples.SimpleBossFight
{
    [DisallowMultipleComponent]
    public sealed class SBF_Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 45f;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float lifeTime = 2.5f;
        [SerializeField] private SBF_Team sourceTeam = SBF_Team.Player;
        [SerializeField] private bool destroyOnHit = true;
        [SerializeField] private bool ensureSphereVisual = true;
        [SerializeField] private float sphereVisualRadius = 0.12f;
        [SerializeField] private Material fallbackVisualMaterial;

        private float expireAt;
        private Rigidbody rb;
        private static Mesh cachedSphereMesh;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            EnsureSphereVisualMesh();
        }

        private void OnEnable()
        {
            expireAt = Time.time + Mathf.Max(0.05f, lifeTime);
        }

        private void Update()
        {
            if (Time.time >= expireAt)
            {
                Destroy(gameObject);
            }
        }

        private void FixedUpdate()
        {
            if (rb == null) return;
            rb.linearVelocity = transform.forward * Mathf.Max(0f, speed);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHit(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null) return;
            TryHit(collision.collider);
        }

        public void Launch(Vector3 direction, float newSpeed, float newDamage, float newLifeTime, SBF_Team newSourceTeam)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            speed = Mathf.Max(0f, newSpeed);
            damage = Mathf.Max(0f, newDamage);
            lifeTime = Mathf.Max(0.05f, newLifeTime);
            sourceTeam = newSourceTeam;
            expireAt = Time.time + lifeTime;
        }

        private void TryHit(Collider other)
        {
            if (other == null) return;

            var hp = other.GetComponentInParent<SBF_Health>();
            if (hp != null)
            {
                bool applied = hp.ApplyDamage(damage, sourceTeam);
                if (applied && destroyOnHit)
                {
                    Destroy(gameObject);
                }
                return;
            }

            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }

        private void EnsureSphereVisualMesh()
        {
            if (!ensureSphereVisual) return;
            if (HasVisibleMesh()) return;

            var visual = new GameObject("ProjectileVisual");
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            float diameter = Mathf.Max(0.02f, sphereVisualRadius * 2f);
            visual.transform.localScale = new Vector3(diameter, diameter, diameter);

            var filter = visual.AddComponent<MeshFilter>();
            var renderer = visual.AddComponent<MeshRenderer>();
            filter.sharedMesh = GetBuiltinSphereMesh();
            if (fallbackVisualMaterial != null)
            {
                renderer.sharedMaterial = fallbackVisualMaterial;
            }
        }

        private bool HasVisibleMesh()
        {
            var filters = GetComponentsInChildren<MeshFilter>(true);
            var renderers = GetComponentsInChildren<MeshRenderer>(true);
            return filters != null && filters.Length > 0 && renderers != null && renderers.Length > 0;
        }

        private static Mesh GetBuiltinSphereMesh()
        {
            if (cachedSphereMesh != null) return cachedSphereMesh;

            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                var mf = temp.GetComponent<MeshFilter>();
                cachedSphereMesh = mf != null ? mf.sharedMesh : null;
            }
            finally
            {
                Destroy(temp);
            }

            return cachedSphereMesh;
        }
    }
}
