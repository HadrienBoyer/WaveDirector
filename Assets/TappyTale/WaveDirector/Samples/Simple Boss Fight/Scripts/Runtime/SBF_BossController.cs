using UnityEngine;

namespace TappyTale.Samples.SimpleBossFight
{
    public sealed class SBF_BossController : MonoBehaviour
    {
        private const float BossProjectileSpawnHeight = 0.5f;

        [SerializeField] private SBF_Health health;
        [SerializeField] private SBF_EnemyNavAI navAI;

        [SerializeField] private string objectiveId = "BossDead";
        [SerializeField] private MonoBehaviour objectiveTrackerBehaviour;

        private object objectiveTracker;

        private void Reset()
        {
            health = GetComponent<SBF_Health>();
            navAI = GetComponent<SBF_EnemyNavAI>();
        }

        private void Awake()
        {
            if (health == null) health = GetComponent<SBF_Health>();
            if (navAI == null) navAI = GetComponent<SBF_EnemyNavAI>();
            if (navAI != null) navAI.SetProjectileSpawnHeight(BossProjectileSpawnHeight);

            objectiveTracker = objectiveTrackerBehaviour;
            if (health != null) health.Died += OnDied;
        }

        public void SetTarget(Transform t)
        {
            if (navAI != null) navAI.SetTarget(t);
        }

        public void SetObjectiveTracker(MonoBehaviour tracker)
        {
            objectiveTrackerBehaviour = tracker;
            objectiveTracker = tracker;
        }

        private void OnDied()
        {
            if (objectiveTracker == null) return;

            var t = objectiveTracker.GetType();
            var m = t.GetMethod("MarkCompleted");
            if (m == null) return;

            try { m.Invoke(objectiveTracker, new object[] { objectiveId }); } catch { }
        }
    }
}
