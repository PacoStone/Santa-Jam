using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(SpriteRenderer))]
public class DirectionalSprite4Way : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private Transform target; // Player o Camera

    [Header("Sprites (4)")]
    [SerializeField] private Sprite front;
    [SerializeField] private Sprite right;
    [SerializeField] private Sprite back;
    [SerializeField] private Sprite left;

    [Header("Options")]
    [SerializeField] private bool yOnly = true;
    [SerializeField] private float updateThresholdDegrees = 1.0f;

    private SpriteRenderer sr;
    private float lastAngle;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        Transform t = ResolveTarget();
        if (t == null) return;

        Vector3 toTarget = t.position - transform.position;
        if (yOnly)
            toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        float angle = SignedAngleOnY(transform.forward, toTarget);

        if (Mathf.Abs(Mathf.DeltaAngle(lastAngle, angle)) < updateThresholdDegrees)
            return;

        lastAngle = angle;
        sr.sprite = PickSprite(angle);
    }

    private Transform ResolveTarget()
    {
        if (target != null)
            return target;

        Camera cam = brain != null ? brain.OutputCamera : Camera.main;
        return cam != null ? cam.transform : null;
    }

    private static float SignedAngleOnY(Vector3 from, Vector3 to)
    {
        from.y = 0f;
        to.y = 0f;
        from.Normalize();
        to.Normalize();
        return Vector3.SignedAngle(from, to, Vector3.up);
    }

    // Cuadrantes clásicos PS1
    private Sprite PickSprite(float signedAngle)
    {
        if (signedAngle >= -45f && signedAngle < 45f)
            return front;

        if (signedAngle >= 45f && signedAngle < 135f)
            return right;

        if (signedAngle >= -135f && signedAngle < -45f)
            return left;

        return back;
    }
}
