using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    public bool IsDead() => currentHealth <= 0f;
}
