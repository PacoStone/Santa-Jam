using UnityEngine;

/// <summary>
/// Proyectil simple:
/// - Se mueve con Rigidbody (si existe) o con Translate.
/// - Detecta impacto con Raycast corto (para evitar atravesar colliders).
/// - Al impactar: spawnea decal + partículas y se destruye.
/// </summary>
public class BulletProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float maxDistance;
    private LayerMask environmentMask;

    private GameObject decalPrefab;
    private ParticleSystem impactParticlesPrefab;

    private float decalOffset;
    private Vector2 decalScaleRange;
    private float decalLifeTime;
    private float particlesLifeTime;

    private bool drawDebug;
    private float debugDuration;

    private Rigidbody rb;
    private Vector3 startPos;

    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Init(
        Vector3 dir,
        float bulletSpeed,
        float hitDistance,
        LayerMask envMask,
        GameObject bulletHoleDecalPrefab,
        ParticleSystem particlesPrefab,
        float surfaceOffset,
        Vector2 scaleRange,
        float decalTTL,
        float particlesTTL,
        bool debugDraw,
        float debugDrawDuration
    )
    {
        direction = dir.normalized;
        speed = Mathf.Max(0.01f, bulletSpeed);
        maxDistance = Mathf.Max(1f, hitDistance);
        environmentMask = envMask;

        decalPrefab = bulletHoleDecalPrefab;
        impactParticlesPrefab = particlesPrefab;

        decalOffset = surfaceOffset;
        decalScaleRange = scaleRange;
        decalLifeTime = decalTTL;
        particlesLifeTime = particlesTTL;

        drawDebug = debugDraw;
        debugDuration = debugDrawDuration;

        startPos = transform.position;
        initialized = true;

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (rb == null)
        {
            transform.position += direction * (speed * Time.deltaTime);
        }

        float step = speed * Time.deltaTime;

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, step + 0.05f, environmentMask, QueryTriggerInteraction.Ignore))
        {
            if (drawDebug)
            {
                Debug.DrawLine(transform.position, hit.point, Color.red, debugDuration);
            }

            OnHit(hit);
            return;
        }

        if (Vector3.Distance(startPos, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!initialized)
            return;

        if (((1 << collision.gameObject.layer) & environmentMask.value) == 0)
            return;

        ContactPoint cp = collision.GetContact(0);

        RaycastHit hit = new RaycastHit();
        hit.point = cp.point;
        hit.normal = cp.normal;
        //hit.collider = collision.collider;

        OnHit(hit);
    }

    private void OnHit(RaycastHit hit)
    {
        SpawnDecal(hit);
        SpawnParticles(hit);

        Destroy(gameObject);
    }

    private void SpawnDecal(RaycastHit hit)
    {
        if (decalPrefab == null)
            return;

        Vector3 pos = hit.point + hit.normal * decalOffset;
        Quaternion rot = Quaternion.LookRotation(-hit.normal, Vector3.up);
        rot *= Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        GameObject decal = Instantiate(decalPrefab, pos, rot);

        float s = Random.Range(decalScaleRange.x, decalScaleRange.y);
        decal.transform.localScale = new Vector3(s, s, s);

        Destroy(decal, Mathf.Max(0.5f, decalLifeTime));
    }

    private void SpawnParticles(RaycastHit hit)
    {
        if (impactParticlesPrefab == null)
            return;

        Quaternion rot = Quaternion.LookRotation(hit.normal);
        ParticleSystem fx = Instantiate(impactParticlesPrefab, hit.point, rot);

        Destroy(fx.gameObject, Mathf.Max(0.5f, particlesLifeTime));
    }
}
