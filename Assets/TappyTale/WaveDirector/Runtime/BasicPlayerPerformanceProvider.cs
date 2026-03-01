using UnityEngine;

namespace TappyTale.WaveDirector.Runtime
{
    public sealed class BasicPlayerPerformanceProvider : MonoBehaviour, IPlayerPerformanceProvider
    {
        [SerializeField] private float damageTakenThisWave;
        [SerializeField] private int deathsThisWave;

        public void AddDamageTaken(float amount) { if (amount > 0f) damageTakenThisWave += amount; }
        public void AddDeath() => deathsThisWave++;
        public void ResetWave() { damageTakenThisWave = 0f; deathsThisWave = 0; }

        public PlayerPerformanceSnapshot GetSnapshot() => new PlayerPerformanceSnapshot(damageTakenThisWave, deathsThisWave);
    }
}
