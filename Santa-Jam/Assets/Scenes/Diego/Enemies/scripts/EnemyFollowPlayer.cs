using UnityEngine;
using UnityEngine.AI;

public class EnemyFollowAndAttack : MonoBehaviour
{
    public Transform player;
    public float stopDistance = 2f;      // Distancia para detenerse
    public float attackRange = 3f;       // Distancia del raycast
    public float attackDelay = 0.5f;     // Tiempo antes de atacar
    public int damage = 10;

    private NavMeshAgent agent;
    private bool isAttacking = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > stopDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;

            if (!isAttacking)
            {
                Debug.Log("enemigo golpeando");
                StartCoroutine(Attack());
            }
        }
    }

    System.Collections.IEnumerator Attack()
    {
        isAttacking = true;

        // Espera antes de atacar
        yield return new WaitForSeconds(attackDelay);

        // Dirección hacia el jugador
        Vector3 dir = (player.position - transform.position).normalized;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, dir, out hit, attackRange))
        {
            if (hit.transform.GetComponent<PlayerHealthController>())
            {
                Debug.Log("Jugador golpeado");

                // Llamada a sistema de vida
                PlayerHealthController health = hit.transform.GetComponent<PlayerHealthController>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                }
            }
        }

        // Pequeño cooldown para no atacar cada frame
        yield return new WaitForSeconds(1f);

        isAttacking = false;
    }

    // Para ver el raycast en la escena
    void OnDrawGizmos()
    {
        if (player == null) return;

        Gizmos.color = Color.red;
        Vector3 dir = (player.position - transform.position).normalized;
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + dir * attackRange);
    }
}
