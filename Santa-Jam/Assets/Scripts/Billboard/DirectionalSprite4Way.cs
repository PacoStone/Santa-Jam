using UnityEngine;
using Unity.Cinemachine;

public class DirectionalSprite4WayBillboard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private Transform target;          // si está vacío, usa la cámara
    [SerializeField] private Transform visual;          // hijo que rota (billboard)
    [SerializeField] private SpriteRenderer sr;         // renderer del hijo (visual)

    [Header("Sprites (4)")]
    [SerializeField] private Sprite front;
    [SerializeField] private Sprite right;
    [SerializeField] private Sprite back;
    [SerializeField] private Sprite left;

    [Header("Options")]
    [SerializeField] private bool yOnly = true;
    [SerializeField] private float updateThresholdDegrees = 1.0f;

    private float lastAngle;

    void Reset()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) visual = sr.transform;
    }

    void Awake()
    {
        if (sr == null && visual != null)
            sr = visual.GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        Camera cam = brain != null ? brain.OutputCamera : Camera.main;
        if (cam == null) return;

        Transform t = target != null ? target : cam.transform;
        if (t == null || visual == null || sr == null) return;

        // --- 1) Calcula el ángulo usando el ROOT (este objeto), que NO debe estar billboard ---
        Vector3 toTarget = t.position - transform.position;
        if (yOnly) toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        // Convertimos la dirección a espacio local del ROOT:
        // z+ = "front" del NPC, x+ = "right" del NPC.
        Vector3 localDir = transform.InverseTransformDirection(toTarget.normalized);

        // Ángulo en el plano XZ relativo al frente del NPC
        float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg; // [-180..180]

        if (Mathf.Abs(Mathf.DeltaAngle(lastAngle, angle)) >= updateThresholdDegrees)
        {
            lastAngle = angle;
            sr.sprite = PickSprite(angle);
        }

        // --- 2) Billboard SOLO del visual (hijo) hacia la cámara ---
        Vector3 toCam = cam.transform.position - visual.position;
        if (yOnly) toCam.y = 0f;

        if (toCam.sqrMagnitude > 0.0001f)
            visual.rotation = Quaternion.LookRotation(toCam);
    }

    private Sprite PickSprite(float angle)
    {
        // mismos cuadrantes que antes:
        if (angle >= -45f && angle < 45f) return front;
        if (angle >= 45f && angle < 135f) return right;
        if (angle >= -135f && angle < -45f) return left;
        return back;
    }
}
