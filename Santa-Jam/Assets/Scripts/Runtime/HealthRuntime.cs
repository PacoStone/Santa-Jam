using UnityEngine;

public class HealthRuntime : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }

    public bool Heal(int amount)
    {
        if (amount <= 0)
            return false;

        int before = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);

        return 
            currentHealth > before;
    }

    public void TakeDamage(int damage)
    {
        damage = Mathf.Max(0, damage);
        currentHealth = Mathf.Max(0, currentHealth - damage);
    }
}
