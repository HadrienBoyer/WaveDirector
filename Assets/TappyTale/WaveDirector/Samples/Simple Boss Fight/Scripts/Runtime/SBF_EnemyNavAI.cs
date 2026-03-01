using UnityEngine;
using UnityEngine.AI;

namespace TappyTale.Samples.SimpleBossFight
{
    [DisallowMultipleComponent]
    public sealed class SBF_EnemyNavAI : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Transform target;
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackDamage = 8f;
        [SerializeField] private float attackCooldown = 0.8f;

        private float lastAttackTime;

        private void Reset() => agent = GetComponent<NavMeshAgent>();

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
        }

        public void SetTarget(Transform t) => target = t;

        private void Update()
        {
            if (target == null) return;

            agent.SetDestination(target.position);

            float d = Vector3.Distance(transform.position, target.position);
            if (d <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                var hp = target.GetComponentInParent<SBF_Health>();
                if (hp != null) hp.Damage(attackDamage);
            }
        }
    }
}
