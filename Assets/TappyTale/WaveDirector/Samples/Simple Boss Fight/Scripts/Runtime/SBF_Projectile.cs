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

        private float expireAt;
        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
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
            rb.velocity = transform.forward * Mathf.Max(0f, speed);
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
    }
}
