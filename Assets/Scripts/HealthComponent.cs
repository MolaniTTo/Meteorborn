using UnityEngine;
using System;

public class HealthComponent : MonoBehaviour
{
    [Header("Configuració")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool targetableByEnemy = false;
    [SerializeField] private bool targetableByMinion = false;

    [SerializeField] public float currentHealth { get; private set; }

    public Action<float> OnDamageTaken;
    public Action OnDeath;

    public bool IsTargetableByEnemy => targetableByEnemy;
    public bool IsTargetableByMinion => targetableByMinion;
    public float HealthPercent => currentHealth / maxHealth;

    private bool isDead = false;

    void Awake() => currentHealth = maxHealth;

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnDamageTaken?.Invoke(amount);
        if (currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public bool IsDead() => isDead;

    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke();
        Debug.Log($"{gameObject.name} ha mort.");
    }
}