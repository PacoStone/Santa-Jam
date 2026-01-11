using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyHealthController))]
public class EnemyExplosive : MonoBehaviour
{
    public Transform player;

    [Header("Movimiento")]
    public float baseSpeed = 6f;
    public float slowMultiplierPerHit = 0.15f;

    [Header("Crecimiento")]
    public float scaleIncreasePerHit = 0.15f;
    public float maxScale = 2.5f;

    [Header("Explosión")]
    public float explosionRadius = 5f;
    public int explosionDamage = 30;
    public float explosionForce = 600f;
    public float explosionUpwards = 1.5f;
    public float controllerPushForce = 12f;
    public float controllerPushTime = 0.25f;
    public GameObject explosionEffect;
    public float explodeDistanceToPlayer = 2f;

    private NavMeshAgent agent;
    private EnemyHealthController health;
    private int hitsTaken = 0;
    private bool hasExploded = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealthController>();

        if (agent != null)
            agent.speed = baseSpeed;
        CharacterController pc = FindAnyObjectByType<CharacterController>();
        if (pc != null)
            player = pc.transform;
    }

    void Update()
    {
        if (player == null || hasExploded) return;

        // Persigue al jugador
        if (agent != null)
            agent.SetDestination(player.position);

        // Explota si llega demasiado cerca
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= explodeDistanceToPlayer)
        {
            ExplodeAndDie();
        }
    }

    // Este método debe llamarse desde EnemyHealthController al recibir daño
    public void OnDamaged()
    {
        if (hasExploded) return;

        hitsTaken++;

        // Crece y se vuelve más lento
        ApplyGrowth();
        ApplySlow();
    }

    // Este método debe llamarse desde EnemyHealthController al morir
    public void OnDeath()
    {
        ExplodeAndDie();
    }

    // Aumenta el tamaño del enemigo
    void ApplyGrowth()
    {
        float newScale = 1f + hitsTaken * scaleIncreasePerHit;
        newScale = Mathf.Clamp(newScale, 1f, maxScale);
        transform.localScale = Vector3.one * newScale;
    }

    // Reduce la velocidad del enemigo
    void ApplySlow()
    {
        if (agent == null) return;

        float slowFactor = 1f - hitsTaken * slowMultiplierPerHit;
        slowFactor = Mathf.Clamp(slowFactor, 0.2f, 1f);
        agent.speed = baseSpeed * slowFactor;
    }

    // Llama a la explosión y destruye el enemigo
    void ExplodeAndDie()
    {
        if (hasExploded) return;

        hasExploded = true;
        Explode();
        Destroy(gameObject);
    }

    // Daño y empuje en área
    void Explode()
    {
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider c in hits)
        {
            Vector3 dir = (c.transform.position - transform.position).normalized;

            // Empuje para Rigidbody
            Rigidbody rb = c.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(
                    explosionForce,
                    transform.position,
                    explosionRadius,
                    explosionUpwards,
                    ForceMode.Impulse
                );
            }

            // Empuje para CharacterController
            CharacterController cc = c.GetComponent<CharacterController>();
            if (cc != null)
            {
                StartCoroutine(PushCharacterController(cc, dir));
            }

            // Daño al jugador
            PlayerHealthController playerHp = c.GetComponent<PlayerHealthController>();
            if (playerHp != null)
                playerHp.TakeDamage(explosionDamage);

            // Daño a otros enemigos
            EnemyHealthController enemyHp = c.GetComponent<EnemyHealthController>();
            if (enemyHp != null && enemyHp != health)
                enemyHp.TakeDamage(explosionDamage);
        }
    }

    // Empuje manual para CharacterController
    System.Collections.IEnumerator PushCharacterController(CharacterController cc, Vector3 dir)
    {
        float timer = 0f;

        while (timer < controllerPushTime)
        {
            cc.Move(dir * controllerPushForce * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    // Dibuja los radios en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explodeDistanceToPlayer);
    }
}
