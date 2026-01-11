using UnityEngine;

public class EnemyHealthController : MonoBehaviour
{
    public int maxHealth = 100;

    [SerializeField] private int currentHealth;
    private bool isDead = false;

    private EnemyExplosive explosiveEnemy;

    void Start()
    {
        currentHealth = maxHealth;
        explosiveEnemy = GetComponent<EnemyExplosive>();
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (explosiveEnemy != null)
            explosiveEnemy.OnDamaged();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (explosiveEnemy != null)
            explosiveEnemy.OnDeath();

        if (explosiveEnemy == null)
            Destroy(gameObject);
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}
