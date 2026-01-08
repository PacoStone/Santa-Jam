using UnityEngine;
using UnityEngine.AI;

public class EnemyShooterAI : MonoBehaviour
{
    public Transform player;

    [Header("Movimiento")]
    public float approachDistance = 12f;
    public float stopDistance = 8f;
    public float repositionRadius = 3f;

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

    // MOVIMIENTO PRINCIPAL
    void HandleMovement(float dist)
    {
        //RECOLOCÁNDOSE
        if (isRepositioning)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                // Ha llegado a la nueva posición
                isRepositioning = false;
                isHoldingPosition = true;
                agent.isStopped = true;

                shotsFired = 0;
                PickNewShotLimit();
            }
            return;
        }

        //DISPARANDO
        if (isHoldingPosition)
        {
            // Si el jugador se ha ido lejos, volver a avanzar
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

        //NORMAL
        if (!isHoldingPosition)
        {
            if (dist > stopDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
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

    // APUNTAR
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

    // DISPARO
    void HandleShooting()
    {
        if (!isHoldingPosition || isRepositioning) return;

        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + 1f / fireRate;

        Shoot();
    }

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

   
        if (shotsFired >= currentShotsLimit)
        {
            Reposition();
        }
    }

    // RECOLOCARSE
    void Reposition()
    {
        isHoldingPosition = false;
        isRepositioning = true;

        agent.isStopped = false;

        Vector3 randomDir = Random.insideUnitSphere * repositionRadius;
        randomDir.y = 0f;

        Vector3 targetPos = transform.position + randomDir;

        // Asegura que esté en NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, repositionRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // fallback: si no encuentra punto válido, cancela recolocación
            isRepositioning = false;
            isHoldingPosition = true;
            agent.isStopped = true;
        }
    }

    // UTILIDAD
    void PickNewShotLimit()
    {
        currentShotsLimit = Random.Range(minShotsBeforeMove, maxShotsBeforeMove + 1);
    }
}
