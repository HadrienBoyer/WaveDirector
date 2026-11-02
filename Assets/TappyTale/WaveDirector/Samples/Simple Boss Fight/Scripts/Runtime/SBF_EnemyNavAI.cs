using UnityEngine;
using UnityEngine.AI;

namespace TappyTale.Samples.SimpleBossFight
{
    [DisallowMultipleComponent]
    public sealed class SBF_EnemyNavAI : MonoBehaviour, TappyTale.IPoolable
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Transform target;
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackDamage = 8f;
        [SerializeField] private float attackCooldown = 0.8f;
        [SerializeField] private bool useProjectileAttack = true;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 22f;
        [SerializeField] private float projectileLifetime = 2.8f;
        [SerializeField] private float projectileMuzzleDistance = 0.8f;
        [SerializeField] private float projectileSpawnHeight = 1.0f;
        [SerializeField] private float fallbackMoveSpeed = 3.5f;
        [SerializeField] private bool autoFindPlayerTarget = true;
        [SerializeField] private bool keepFixedY = true;
        [SerializeField] private float fixedY = 1f;

        private float lastAttackTime;
        private float nextTargetSearchTime;
        private GameObject cachedProjectilePrefab;

        private void Reset() => agent = GetComponent<NavMeshAgent>();

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            var health = GetComponent<SBF_Health>();
            if (health != null) health.SetTeam(SBF_Team.Enemy);
            TryResolveTarget(true);
            TryResolveProjectilePrefab();
            if (keepFixedY) SnapToFixedY();
        }

        public void SetTarget(Transform t) => target = t;

        public void SetProjectileSpawnHeight(float y)
        {
            projectileSpawnHeight = y;
        }

        public void OnSpawned()
        {
            // We'll attempt to re-enable the agent only when/where a valid NavMesh is present.
            lastAttackTime = Time.time;
            TryResolveTarget(true);
            TryResolveProjectilePrefab();
            if (keepFixedY) SnapToFixedY();
        }

        public void OnDespawned()
        {
            if (agent != null && agent.enabled)
            {
                agent.enabled = false;
            }
        }

        private void Update()
        {
            if (target == null)
            {
                TryResolveTarget(false);
                if (target == null) return;
            }

            if (TryUseNavMesh())
            {
                agent.SetDestination(target.position);
            }
            else
            {
                Vector3 delta = target.position - transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > 0.001f)
                {
                    var dir = delta.normalized;
                    transform.position += dir * Mathf.Max(0f, fallbackMoveSpeed) * Time.deltaTime;
                    transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                }
            }

            if (keepFixedY) SnapToFixedY();

            float d = Vector3.Distance(transform.position, target.position);
            if (d <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                if (!TryFireProjectileAtTarget())
                {
                    var hp = target.GetComponentInParent<SBF_Health>();
                    if (hp != null) hp.ApplyDamage(attackDamage, SBF_Team.Enemy);
                }
            }
        }

        private bool TryFireProjectileAtTarget()
        {
            if (!useProjectileAttack) return false;
            var prefab = TryResolveProjectilePrefab();
            if (prefab == null) return false;
            if (target == null) return false;

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.0001f) return false;

            Vector3 forward = toTarget.normalized;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            Vector3 origin = transform.position + Vector3.up * projectileSpawnHeight + forward * projectileMuzzleDistance;
            var projectileGo = Instantiate(prefab, origin, Quaternion.LookRotation(forward, Vector3.up));
            if (projectileGo == null) return false;

            IgnoreShooterCollisions(projectileGo);

            var projectile = projectileGo.GetComponent<SBF_Projectile>();
            if (projectile != null)
            {
                projectile.Launch(forward, projectileSpeed, attackDamage, projectileLifetime, SBF_Team.Enemy);
            }

            return true;
        }

        private GameObject TryResolveProjectilePrefab()
        {
            if (projectilePrefab != null) return projectilePrefab;
            if (cachedProjectilePrefab != null) return cachedProjectilePrefab;

            var player = GameObject.FindWithTag("Player");
            var gun = player != null ? player.GetComponent<SBF_Gun>() : null;
            if (gun == null) return null;

            cachedProjectilePrefab = gun.ProjectilePrefab;
            return cachedProjectilePrefab;
        }

        private void IgnoreShooterCollisions(GameObject projectileGo)
        {
            var projectileCollider = projectileGo.GetComponent<Collider>();
            if (projectileCollider == null) return;

            var selfColliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < selfColliders.Length; i++)
            {
                var c = selfColliders[i];
                if (c == null) continue;
                Physics.IgnoreCollision(projectileCollider, c, true);
            }
        }

        private bool TryUseNavMesh()
        {
            if (agent == null) return false;

            if (!agent.enabled)
            {
                if (!NavMesh.SamplePosition(transform.position, out var hit, 4f, NavMesh.AllAreas))
                    return false;

                agent.enabled = true;
                if (agent.enabled)
                {
                    agent.Warp(hit.position);
                }
            }

            if (!agent.enabled) return false;
            if (agent.isOnNavMesh) return true;

            if (NavMesh.SamplePosition(transform.position, out var nearest, 4f, NavMesh.AllAreas))
            {
                agent.Warp(nearest.position);
                return agent.isOnNavMesh;
            }

            agent.enabled = false;
            return false;
        }

        private void TryResolveTarget(bool immediate)
        {
            if (!autoFindPlayerTarget) return;
            if (target != null) return;
            if (!immediate && Time.time < nextTargetSearchTime) return;

            nextTargetSearchTime = Time.time + 0.5f;
            var player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }

        private void SnapToFixedY()
        {
            var p = transform.position;
            if (Mathf.Abs(p.y - fixedY) <= 0.0001f) return;
            p.y = fixedY;
            transform.position = p;
        }
    }
}
