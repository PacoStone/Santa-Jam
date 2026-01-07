using UnityEngine;
using UnityEngine.AI;

public class EnemyChargeAttack : MonoBehaviour
{
    public Transform player;

    [Header("Distancias")]
    public float chaseDistance = 15f;
    public float chargeTriggerDistance = 10f;

    [Header("Carga")]
    public float chargeSpeed = 12f;
    public float chargeDuration = 1.5f;
    public float turnResistance = 0.1f; // 0 = no gira nada

    [Header("Daño")]
    public int damage = 20;

    [Header("Aturdimiento")]
    public float stunTime = 2f; // Tiempo que queda aturdido tras chocar

    private NavMeshAgent agent;

    private bool isCharging = false;
    private bool isStunned = false;

    private Vector3 chargeDirection;
    private float chargeTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (player == null) return;

        // Si está aturdido, no hace nada
        if (isStunned)
            return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (isCharging)
        {
            HandleCharge();
            return;
        }

        // --- PERSECUCIÓN NORMAL ---
        if (dist <= chaseDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        // --- INICIAR CARGA ---
        if (dist <= chargeTriggerDistance && !isCharging)
        {
            StartCharge();
        }
    }

    void StartCharge()
    {
        isCharging = true;

        // Guardamos la dirección INICIAL hacia el jugador
        chargeDirection = (player.position - transform.position).normalized;
        chargeTimer = chargeDuration;

        agent.isStopped = true;
        agent.updateRotation = false;
    }

    void HandleCharge()
    {
        chargeTimer -= Time.deltaTime;

        transform.position += chargeDirection * chargeSpeed * Time.deltaTime;

        // Rotación lenta (simula inercia)
        Quaternion targetRot = Quaternion.LookRotation(chargeDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnResistance);

        if (chargeTimer <= 0f)
        {
            EndCharge();
        }
    }

    void EndCharge()
    {
        isCharging = false;

        agent.updateRotation = true;
        agent.isStopped = false;
    }

    // COLISIÓN Y DAÑO
    void OnCollisionEnter(Collision col)
    {
        if (!isCharging) return;

        if (col.transform.GetComponent<PlayerHealthController>())
        {
            PlayerHealthController health = col.transform.GetComponent<PlayerHealthController>();
            if (health != null)
                health.TakeDamage(damage);
            return;
        }

        // Siempre que choque durante la carga → se aturde
        StartCoroutine(Stun());
    }

    // ESTADO DE ATURDIDO
    System.Collections.IEnumerator Stun()
    {
        isCharging = false;
        isStunned = true;

        agent.isStopped = true;

        Debug.Log("Enemigo aturdido");

        yield return new WaitForSeconds(stunTime);

        Debug.Log("Enemigo recuperado");

        isStunned = false;
        agent.isStopped = false;
        agent.updateRotation = true;
    }
}
