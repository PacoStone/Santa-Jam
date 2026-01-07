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
    public float turnResistance = 0.1f; // 0 = no gira nada, 1 = gira normal

    private NavMeshAgent agent;
    private bool isCharging = false;

    private Vector3 chargeDirection;
    private float chargeTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (isCharging)
        {
            HandleCharge();
            return;
        }

        // --- MODO PERSECUCIÓN ---
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

        // Paramos el NavMesh
        agent.isStopped = true;
        agent.updateRotation = false;
    }

    void HandleCharge()
    {
        // Reducimos el tiempo de carga
        chargeTimer -= Time.deltaTime;

        // Movimiento hacia delante
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

    void OnCollisionEnter(Collision col)
    {
        // si colisiona con eljugador peus no hacemos nada, o ahcemos que empuje al jugador hacia los lados ol algo //TODO
        if (col.transform.GetComponent<PlayerHealthController>())
        {
            return;
        }
        if (isCharging)
        {
            EndCharge();
        }
    }

}
