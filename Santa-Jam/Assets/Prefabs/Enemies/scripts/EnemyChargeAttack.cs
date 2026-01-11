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

    [Header("Cooldown entre cargas")]
    public float chargeCooldown = 3f; // Tiempo antes de poder volver a cargar

    private NavMeshAgent agent;

    private bool isCharging = false;
    private bool isStunned = false;
    private bool canCharge = true;   

    private Vector3 chargeDirection;
    private float chargeTimer;

    void Start()
    {
        CharacterController pc = FindAnyObjectByType<CharacterController>();
        if (pc != null)
            player = pc.transform;

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
        if (canCharge && dist <= chargeTriggerDistance && !isCharging)
        {
            StartCharge();
        }
    }

    void StartCharge()
    {
        isCharging = true;
        canCharge = false; // ⬅️ bloquea nuevas cargas hasta que pase el cooldown

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

        //empieza el cooldown para poder volver a cargar
        StartCoroutine(ChargeCooldown());
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

            // Tras golpear al jugador también entra en aturdimiento
            StartCoroutine(Stun());
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

    // COOLDOWN ENTRE CARGAS
    System.Collections.IEnumerator ChargeCooldown()
    {
        yield return new WaitForSeconds(chargeCooldown);
        canCharge = true;
    }
}
