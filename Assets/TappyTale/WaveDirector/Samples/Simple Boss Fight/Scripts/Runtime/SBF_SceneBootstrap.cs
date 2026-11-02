using UnityEngine;

namespace TappyTale.Samples.SimpleBossFight
{
    // Scene runtime wiring helper: set enemy targets + hook boss objective tracker if present.
    [DisallowMultipleComponent]
    public sealed class SBF_SceneBootstrap : MonoBehaviour
    {
        private void Start()
        {
            var player = GameObject.FindWithTag("Player")?.transform;

            MonoBehaviour objectiveTracker = null;
            var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i].GetType().Name == "WaveObjectiveTracker")
                {
                    objectiveTracker = all[i];
                    break;
                }
            }

            var enemies = FindObjectsByType<SBF_EnemyNavAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].SetTarget(player);
            }

            var bosses = FindObjectsByType<SBF_BossController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < bosses.Length; i++)
            {
                bosses[i].SetTarget(player);
                if (objectiveTracker != null) bosses[i].SetObjectiveTracker(objectiveTracker);
            }
        }
    }
}
