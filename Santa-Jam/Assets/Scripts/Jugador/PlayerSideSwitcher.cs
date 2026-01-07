using Unity.Cinemachine;
using UnityEngine;

public class PlayerSideSwitcher : MonoBehaviour
{
    public enum Side { Left, Right }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform playerRoot;

    [Header("Weapon")]
    [SerializeField] private Transform weaponRoot; // Tu objeto "arma" real (el que se ve)
    [SerializeField] private Transform rightAnchor; // WeaponAnchor_Right
    [SerializeField] private Transform leftAnchor; // WeaponAnchor_Left

    [Header("Muzzle (optional helpers)")]
    [SerializeField] private Transform rightMuzzle; // MuzzlePoint (Right)
    [SerializeField] private Transform leftMuzzle; // MuzzlePoint (Left)

    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera rightVcam; // CinemachineCamera_Right
    [SerializeField] private CinemachineCamera leftVcam; // CinemachineCamera_Left
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 10;

    [Header("Behaviour")]
    [Tooltip("Si true, el lado se decide por el ray del centro de cámara.")]
    [SerializeField] private bool sideFromCameraAimRay = true;

    [Tooltip("Si tienes un input de aim, así solo conmuta mientras apuntas.")]
    [SerializeField] private bool onlyWhileAiming = true;

    [SerializeField] private InputManager input;

    /*
    [Header("Weapon Follow Smoothing")]
    [SerializeField] private float weaponFollowPosSpeed = 18f;
    [SerializeField] private float weaponFollowRotSpeed = 18f;
    */

    [Header("Debug")]
    [SerializeField] private bool drawAimRay = false;
    [SerializeField] private float aimRayLength = 25f;

    public Side CurrentSide { get; private set; } = Side.Right;

    private void Awake()
    {
        playerRoot = transform;
        mainCamera = Camera.main;
        input = GetComponent<InputManager>();

        // Seguridad: que no crashee si faltan refs
        if (weaponRoot == null)
            Debug.LogWarning("[PlayerSideSwitcher] weaponRoot no asignado.");

        if (rightAnchor == null || leftAnchor == null)
            Debug.LogWarning("[PlayerSideSwitcher] anchors no asignados.");

        if (rightVcam == null || leftVcam == null)
            Debug.LogWarning("[PlayerSideSwitcher] vcams no asignadas.");
    }

    private void Update()
    {
        if (onlyWhileAiming && (input == null || !input.aimHeld))
            return;

        Side desired = DetermineSide();
        if (desired != CurrentSide)
        {
            SetSide(desired);
        }
    }

    private void LateUpdate()
    {
        // Mover el arma al anchor en LateUpdate para evitar peleas con otros scripts/animaciones.
        if (weaponRoot == null) return;

        Transform target = (CurrentSide == Side.Right) ? rightAnchor : leftAnchor;
        if (target == null) return;

        //weaponRoot.position = Vector3.Lerp(weaponRoot.position,target.position,Time.deltaTime * weaponFollowPosSpeed);
        weaponRoot.position = Vector3.Lerp(weaponRoot.position, target.position, Time.deltaTime);

        //weaponRoot.rotation = Quaternion.Slerp(weaponRoot.rotation, target.rotation, Time.deltaTime * weaponFollowRotSpeed);
        weaponRoot.rotation = Quaternion.Slerp(weaponRoot.rotation, target.rotation, Time.deltaTime);
    }

    private Side DetermineSide()
    {
        if (sideFromCameraAimRay && mainCamera != null)
        {
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 dir = ray.direction;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f)
                return Side.Right;

            dir.Normalize();

            // side > 0 => a la derecha del jugador
            float side = Vector3.Dot(playerRoot.right, dir);

            if (drawAimRay)
                Debug.DrawRay(ray.origin, ray.direction * aimRayLength, Color.cyan);

            return side >= 0f ? Side.Right : Side.Left;
        }

        // Fallback: usa el mouse/look X (menos robusto)
        if (input != null)
            return input.look.x >= 0f ? Side.Right : Side.Left;

        return Side.Right;
    }

    public void SetSide(Side side)
    {
        CurrentSide = side;

        // 1) Cinemachine por prioridad
        if (rightVcam != null && leftVcam != null)
        {
            if (side == Side.Right)
            {
                rightVcam.Priority = activePriority;
                leftVcam.Priority = inactivePriority;
            }
            else
            {
                leftVcam.Priority = activePriority;
                rightVcam.Priority = inactivePriority;
            }
        }
    }

    public Transform GetActiveMuzzle()
    {
        if (CurrentSide == Side.Right)
            return rightMuzzle != null ? rightMuzzle : null;

        return leftMuzzle != null ? leftMuzzle : null;
    }
}
