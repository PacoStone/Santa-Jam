using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(SpriteRenderer))]
public class DirectionalSprite4Way : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private Transform target; // normalmente el Player (o la cámara si prefieres)

    [Header("Sprites (4)")]
    [SerializeField] private Sprite front;
    [SerializeField] private Sprite right;
    [SerializeField] private Sprite back;
    [SerializeField] private Sprite left;

    [Header("Options")]
    [SerializeField] private bool yOnly = true;
    [SerializeField] private bool billboardToCamera = true;
    [SerializeField] private float updateThresholdDegrees = 1.0f; // evita cambios si casi no rota

    private SpriteRenderer sr;
    private float lastAngle;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        Camera cam = brain != null ? brain.OutputCamera : Camera.main;
        if (cam == null) return;

        Transform t = target != null ? target : cam.transform;

        // Vector desde el sprite hacia el target (player/camera)
        Vector3 toTarget = t.position - transform.position;
        if (yOnly) toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f) return;

        // (1) Billboard opcional: que el plano mire a la cámara
        if (billboardToCamera)
        {
            Vector3 toCam = cam.transform.position - transform.position;
            if (yOnly) toCam.y = 0f;

            if (toCam.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(toCam);
        }

        // (2) Selección de sprite por ángulo relativo
        // Queremos el ángulo entre el "forward" del sprite (o su raíz) y la dirección hacia el target.
        float angle = SignedAngleOnY(transform.forward, toTarget);

        // Pequeño umbral para no estar reasignando sprite todo el tiempo
        if (Mathf.Abs(Mathf.DeltaAngle(lastAngle, angle)) < updateThresholdDegrees)
            return;

        lastAngle = angle;
        sr.sprite = PickSprite(angle);
    }

    // Ángulo firmado en el plano XZ (Y)
    private static float SignedAngleOnY(Vector3 from, Vector3 to)
    {
        from.y = 0f;
        to.y = 0f;
        from.Normalize();
        to.Normalize();
        return Vector3.SignedAngle(from, to, Vector3.up);
    }

    // Mapeo por cuadrantes:
    //  -45..45   => front
    //   45..135  => right
    //  -135..-45 => left
    //  resto     => back
    private Sprite PickSprite(float signedAngle)
    {
        if (signedAngle >= -45f && signedAngle < 45f) return front;
        if (signedAngle >= 45f && signedAngle < 135f) return right;
        if (signedAngle >= -135f && signedAngle < -45f) return left;
        return back;
    }
}
