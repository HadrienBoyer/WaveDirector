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
        [SerializeField] private float fallbackMoveSpeed = 3.5f;

        private float lastAttackTime;

        private void Reset() => agent = GetComponent<NavMeshAgent>();

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
        }

        public void SetTarget(Transform t) => target = t;

        public void OnSpawned()
        {
            // We'll attempt to re-enable the agent only when/where a valid NavMesh is present.
            lastAttackTime = Time.time;
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
            if (target == null) return;

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

            float d = Vector3.Distance(transform.position, target.position);
            if (d <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                var hp = target.GetComponentInParent<SBF_Health>();
                if (hp != null) hp.Damage(attackDamage);
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
    }
}
