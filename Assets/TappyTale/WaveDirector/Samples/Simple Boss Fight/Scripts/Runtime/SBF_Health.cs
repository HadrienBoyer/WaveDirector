using System;
using UnityEngine;

namespace TappyTale.Samples.SimpleBossFight
{
    public sealed class SBF_Health : MonoBehaviour
    {
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private bool destroyOnDeath = true;

        public float Hp { get; private set; }
        public event Action Died;

        private void Awake() => Hp = Mathf.Max(1f, maxHp);

        public void Damage(float amount)
        {
            if (Hp <= 0f) return;

            Hp = Mathf.Max(0f, Hp - Mathf.Max(0f, amount));
            if (Hp <= 0f)
            {
                Died?.Invoke();
                if (destroyOnDeath) Destroy(gameObject);
            }
        }
    }
}
