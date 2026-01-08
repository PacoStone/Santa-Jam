using UnityEngine;
using UnityEngine.AI;

public class EnemyShooterAI : MonoBehaviour
{
    public Transform player;

    [Header("Movimiento")]
    public float approachDistance = 12f;
    public float stopDistance = 8f;
    public float repositionRadius = 3f;

    [Header("Separación al recolocarse")]
    public float avoidRadius = 3f;
    public int repositionTries = 10;

    [Header("Separación dinámica")]
    public float separationRadius = 2.5f;
    public float separationStrength = 3f;

    [Header("Disparo")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 1f;
    public float bulletSpeed = 15f;

    [Header("Disparos antes de moverse")]
    public int minShotsBeforeMove = 2;
    public int maxShotsBeforeMove = 6;

    [Header("Daño")]
    public int damage = 10;

    private NavMeshAgent agent;
    private float nextFireTime = 0f;

    private bool isHoldingPosition = false;
    private bool isRepositioning = false;

    private int shotsFired = 0;
    private int currentShotsLimit = 0;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        PickNewShotLimit();
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        HandleMovement(dist);
        HandleAiming();
        HandleShooting();
    }

    // Controla el movimiento general
    void HandleMovement(float dist)
    {
        // Si se está recolocando
        if (isRepositioning)
        {
            ApplySeparation();

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isRepositioning = false;
                isHoldingPosition = true;
                agent.isStopped = true;

                shotsFired = 0;
                PickNewShotLimit();
            }
            return;
        }

        // Si está quieto disparando
        if (isHoldingPosition)
        {
            if (dist > approachDistance)
            {
                isHoldingPosition = false;
                agent.isStopped = false;
            }
            else
            {
                agent.isStopped = true;
                return;
            }
        }

        // Movimiento normal hacia el jugador
        if (!isHoldingPosition)
        {
            if (dist > stopDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
                ApplySeparation();
            }
            else
            {
                isHoldingPosition = true;
                agent.isStopped = true;
                shotsFired = 0;
                PickNewShotLimit();
            }
        }
    }

    // Hace que mire siempre al jugador
    void HandleAiming()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;

        if (dir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 6f);
        }
    }

    // Controla cuándo puede disparar
    void HandleShooting()
    {
        if (!isHoldingPosition || isRepositioning) return;
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + 1f / fireRate;
        Shoot();
    }

    // Disparo real
    void Shoot()
    {
        shotsFired++;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = firePoint.forward * bulletSpeed;

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.damage = damage;

        // Si ha disparado suficiente, se recoloca
        if (shotsFired >= currentShotsLimit)
        {
            Reposition();
        }
    }

    // Busca una nueva posición sin juntarse con otros
    void Reposition()
    {
        isHoldingPosition = false;
        isRepositioning = true;
        agent.isStopped = false;

        bool found = false;

        for (int i = 0; i < repositionTries; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * repositionRadius;
            randomDir.y = 0f;

            Vector3 candidate = transform.position + randomDir;

            NavMeshHit hit;
            if (!NavMesh.SamplePosition(candidate, out hit, repositionRadius, NavMesh.AllAreas))
                continue;

            if (IsPositionSafe(hit.position))
            {
                agent.SetDestination(hit.position);
                found = true;
                break;
            }
        }

        // Si no encuentra sitio válido, se queda donde está
        if (!found)
        {
            isRepositioning = false;
            isHoldingPosition = true;
            agent.isStopped = true;
        }
    }

    // Comprueba que no esté demasiado cerca de otros enemigos
    bool IsPositionSafe(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, avoidRadius);

        foreach (Collider c in hits)
        {
            if (c.gameObject == gameObject) continue;

            if (c.GetComponent<EnemyShooterAI>())
                return false;
        }

        return true;
    }

    // Aplica una fuerza para separarse de otros enemigos
    void ApplySeparation()
    {
        Vector3 force = CalculateSeparationForce();
        if (force == Vector3.zero) return;

        agent.velocity += force * Time.deltaTime;
    }

    // Calcula la dirección de separación
    Vector3 CalculateSeparationForce()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, separationRadius);

        Vector3 force = Vector3.zero;
        int count = 0;

        foreach (Collider c in hits)
        {
            if (c.gameObject == gameObject) continue;

            EnemyShooterAI other = c.GetComponent<EnemyShooterAI>();
            if (!other) continue;

            Vector3 diff = transform.position - c.transform.position;
            float dist = diff.magnitude;

            if (dist > 0.01f)
            {
                force += diff.normalized / dist;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;

        return force.normalized * separationStrength;
    }

    // Elige cuántos disparos hará antes de moverse
    void PickNewShotLimit()
    {
        currentShotsLimit = Random.Range(minShotsBeforeMove, maxShotsBeforeMove + 1);
    }

    // Dibuja radios de depuración en escena
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, avoidRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}
