using System;
using System.Collections.Generic;
using UnityEngine;

namespace TappyTale.Samples.SimpleBossFight
{
    public enum SBF_Team
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2
    }

    public sealed class SBF_Health : MonoBehaviour
    {
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private SBF_Team team = SBF_Team.Neutral;
        [SerializeField] private bool destroyOnDeath = true;

        private static readonly List<SBF_Health> Active = new List<SBF_Health>(64);

        public float Hp { get; private set; }
        public float MaxHp => Mathf.Max(1f, maxHp);
        public float Normalized => Mathf.Clamp01(Hp / MaxHp);
        public SBF_Team Team => team;
        public static IReadOnlyList<SBF_Health> ActiveEntities => Active;

        public event Action Died;
        public event Action<float, float> HealthChanged;

        private void Awake()
        {
            Hp = MaxHp;
        }

        private void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
            HealthChanged?.Invoke(Hp, MaxHp);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        public void Damage(float amount)
        {
            ApplyDamage(amount, SBF_Team.Neutral);
        }

        public bool ApplyDamage(float amount, SBF_Team sourceTeam)
        {
            if (Hp <= 0f) return false;
            if (amount <= 0f) return false;
            if (sourceTeam != SBF_Team.Neutral && sourceTeam == team) return false;

            Hp = Mathf.Max(0f, Hp - amount);
            HealthChanged?.Invoke(Hp, MaxHp);
            if (Hp <= 0f)
            {
                Died?.Invoke();
                if (destroyOnDeath) Destroy(gameObject);
            }
            return true;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f) return;
            Hp = Mathf.Clamp(Hp + amount, 0f, MaxHp);
            HealthChanged?.Invoke(Hp, MaxHp);
        }
    }
}
