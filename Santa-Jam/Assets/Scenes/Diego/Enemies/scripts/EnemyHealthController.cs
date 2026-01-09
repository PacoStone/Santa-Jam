using UnityEngine;

public class EnemyHealthController : MonoBehaviour
{
    public int maxHealth = 100;

    [SerializeField] private int currentHealth;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

  
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Muerte del enemigo
    void Die()
    {
        if (isDead) return;
        isDead = true;

        //Animaciones, ETC, TODO

        Destroy(gameObject);
    }

    
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}
