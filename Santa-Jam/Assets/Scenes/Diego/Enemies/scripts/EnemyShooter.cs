using UnityEngine;
using UnityEngine.AI;

public class EnemyShooterAI : MonoBehaviour
{
    public Transform player;

    [Header("Movimiento")]
    public float approachDistance = 12f;   // hasta aquí se acerca
    public float stopDistance = 8f;         // aquí se detiene

    [Header("Disparo")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 1f;
    public float bulletSpeed = 15f;

    [Header("Daño")]
    public int damage = 10;

    private NavMeshAgent agent;
    private float nextFireTime = 0f;

    private bool isHoldingPosition = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        HandleMovement(dist);
        HandleAiming();
        HandleShooting(dist);
    }

    // -------------------------
    // MOVIMIENTO
    // -------------------------
    void HandleMovement(float dist)
    {
        // Si está quieto esperando
        if (isHoldingPosition)
        {
            // Si el jugador se ha ido lejos → vuelve a avanzar
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

        // Si no está quieto
        if (!isHoldingPosition)
        {
            if (dist > stopDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            else
            {
                // Ha llegado a la distancia ideal
                isHoldingPosition = true;
                agent.isStopped = true;
            }
        }
    }

    // -------------------------
    // APUNTAR
    // -------------------------
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

    // -------------------------
    // DISPARO
    // -------------------------
    void HandleShooting(float dist)
    {
        // Solo dispara si está parado a distancia correcta
        if (!isHoldingPosition) return;

        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + 1f / fireRate;

        Shoot();
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = firePoint.forward * bulletSpeed;
        }

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = damage;
        }
    }
}
