using UnityEngine;

public class AirboneEnemy : MonoBehaviour
{
    public Transform player;

    [Header("Órbita")]
    public float orbitRadius = 8f;
    public float orbitSpeed = 40f;   // grados por segundo
    public float moveSpeed = 5f;

    [Header("Altura de vuelo")]
    public float minFlyHeight = 4f;
    public float maxFlyHeight = 8f;

    [Header("Aleteo")]
    public float flapAmplitude = 0.7f;   // cuánto sube y baja
    public float flapSpeed = 2f;         // velocidad del aleteo

    [Header("Separación entre enemigos")]
    public float separationRadius = 2.5f;
    public float separationStrength = 6f;

    [Header("Ataque en picado")]
    public float diveSpeed = 12f;
    public float returnSpeed = 6f;

    [Header("Daño")]
    public int damage = 15;

    [Header("Decisión de ataque")]
    [Range(0f, 1f)]
    public float attackChancePerSecond = 0.2f;
    public float minAttackInterval = 2f;

    private enum State { Orbiting, Diving, Returning }
    private State currentState = State.Orbiting;

    private float orbitAngle;
    private float baseFlyHeight;     // altura base individual
    private float flapOffset;        // offset del aleteo
    private float angleOffset;       // evita pasar justo por encima

    private int orbitDirection;      // 1 = horario, -1 = antihorario

    private Vector3 diveTarget;
    private float nextAttackTime = 0f;

    void Start()
    {
        CharacterController pc = FindAnyObjectByType<CharacterController>();
        if (pc != null)
            player = pc.transform;

        if (player != null)
        {
            Vector3 dir = transform.position - player.position;
            orbitAngle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        }

        // Altura base aleatoria para este enemigo
        baseFlyHeight = Random.Range(minFlyHeight, maxFlyHeight);

        // Offset angular para que no pasen por el centro
        angleOffset = Random.Range(20f, 70f) * (Random.value > 0.5f ? 1 : -1);

        // Dirección inicial aleatoria
        orbitDirection = Random.value > 0.5f ? 1 : -1;

        nextAttackTime = Time.time + Random.Range(0.5f, minAttackInterval);
    }

    void Update()
    {
        if (player == null) return;

        // Aleteo continuo
        flapOffset = Mathf.Sin(Time.time * flapSpeed) * flapAmplitude;

        switch (currentState)
        {
            case State.Orbiting:
                HandleOrbiting();
                break;

            case State.Diving:
                HandleDiving();
                break;

            case State.Returning:
                HandleReturning();
                break;
        }
    }

    // ESTADO: ORBITANDO
    void HandleOrbiting()
    {
        orbitAngle += orbitSpeed * orbitDirection * Time.deltaTime;

        float angle = orbitAngle + angleOffset;
        float currentHeight = baseFlyHeight + flapOffset;

        Vector3 orbitPos = player.position +
            new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * orbitRadius,
                currentHeight,
                Mathf.Sin(angle * Mathf.Deg2Rad) * orbitRadius
            );

        Vector3 moveDir = (orbitPos - transform.position);

        Vector3 separation = CalculateSeparation();

        Vector3 finalDir = (moveDir + separation).normalized;

        transform.position += finalDir * moveSpeed * Time.deltaTime;

        LookInMoveDirection(finalDir);

        // DECISIÓN ALEATORIA DE ATAQUE QUE ASEGURA EL ENEMIGO ESTA EN FRENTE DEL JUGADOR
        if (Time.time >= nextAttackTime)
        {
            if (IsInFrontOfPlayer() && Random.value < attackChancePerSecond)
            {
                StartDive();
            }

            nextAttackTime = Time.time + minAttackInterval;
        }

    }

    // INICIAR PICADO
    void StartDive()
    {
        currentState = State.Diving;
        diveTarget = player.position;
    }

    // ESTADO: PICADO
    void HandleDiving()
    {
        MoveTowards(diveTarget, diveSpeed);

        if (Vector3.Distance(transform.position, diveTarget) < 0.5f)
        {
            currentState = State.Returning;
        }
    }

    // ESTADO: VOLVER A VOLAR
    void HandleReturning()
    {
        float currentHeight = baseFlyHeight + flapOffset;

        Vector3 returnPos = player.position + Vector3.up * currentHeight;
        MoveTowards(returnPos, returnSpeed);

        if (Vector3.Distance(transform.position, returnPos) < 0.5f)
        {
            //Puede cambiar de dirección después de cada ataque
            if (Random.value < 0.5f)
                orbitDirection *= -1;

            currentState = State.Orbiting;
        }
    }

    bool IsInFrontOfPlayer()
    {
        Vector3 toEnemy = (transform.position - player.position).normalized;
        float dot = Vector3.Dot(player.forward, toEnemy);

        return dot > 0f;   // solo semicírculo frontal (180°)
    }


    // MOVIMIENTO
    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 dir = (target - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        LookInMoveDirection(dir);
    }

    void LookInMoveDirection(Vector3 dir)
    {
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 6f
            );
    }

    // SEPARACIÓN USANDO COMPONENTE
    Vector3 CalculateSeparation()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, separationRadius);

        Vector3 force = Vector3.zero;
        int count = 0;

        foreach (Collider c in hits)
        {
            if (c.gameObject == gameObject) continue;

            if (c.GetComponent<AirboneEnemy>())
            {
                Vector3 diff = transform.position - c.transform.position;
                float dist = diff.magnitude;

                if (dist > 0.01f)
                {
                    force += diff.normalized / dist;
                    count++;
                }
            }
        }

        if (count == 0) return Vector3.zero;

        return force.normalized * separationStrength;
    }

    // DAÑO AL COLISIONAR
    void OnCollisionEnter(Collision col)
    {
        if (currentState != State.Diving) return;

        if (col.transform.GetComponent<PlayerHealthController>())
        {
            PlayerHealthController health = col.transform.GetComponent<PlayerHealthController>();
            if (health != null)
                health.TakeDamage(damage);
        }

        currentState = State.Returning;
    }

    // DEBUG
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, orbitRadius);
        }
    }
}
