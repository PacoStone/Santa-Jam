using UnityEngine;

public class Billboard : MonoBehaviour
{
    public enum FacingMode
    {
        LookAtCameraPosition,
        MatchCameraRotation
    }

    [Header("Settings")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private FacingMode facingMode = FacingMode.MatchCameraRotation;

    [SerializeField] private bool yawOnly = true;
    [SerializeField] private bool invertForward = false;

    [SerializeField] private bool updateEveryFrame = true;
    [Min(0.01f)]
    [SerializeField] private float updateThreshold = 0.1f;

    private Transform myTransform;
    private Transform camTransform;

    private Vector3 lastCamPos;
    private Quaternion lastCamRot;

    private float thresholdSqr;

    private void Awake()
    {
        myTransform = transform;
        thresholdSqr = updateThreshold * updateThreshold;
    }

    private void OnEnable()
    {
        InitializeCamera();
        FaceCamera(true);
    }

    private void LateUpdate()
    {
        if (camTransform == null)
        {
            InitializeCamera();
            if (camTransform == null) return;
        }

        if (updateEveryFrame)
        {
            FaceCamera(false);
            return;
        }

        bool needsUpdate;

        if (facingMode == FacingMode.LookAtCameraPosition)
        {
            needsUpdate = (camTransform.position - lastCamPos).sqrMagnitude >= thresholdSqr;
        }
        else
        {
            // rotación: muy útil para UI World Space (se ve más estable)
            float angle = Quaternion.Angle(camTransform.rotation, lastCamRot);
            needsUpdate = angle >= updateThreshold;
        }

        if (needsUpdate)
            FaceCamera(true);
    }

    private void InitializeCamera()
    {
        if (targetCamera != null)
        {
            camTransform = targetCamera.transform;
            return;
        }

        // Cinemachine: normalmente la cámara con CinemachineBrain es la Main Camera
        var main = Camera.main;
        if (main != null)
        {
            targetCamera = main;
            camTransform = main.transform;
            return;
        }

        // Fallback: cualquier cámara activa
        var anyCam = FindFirstObjectByType<Camera>();
        if (anyCam != null)
        {
            targetCamera = anyCam;
            camTransform = anyCam.transform;
        }
    }

    private void FaceCamera(bool storeState)
    {
        if (camTransform == null) return;

        Quaternion rotation;

        if (facingMode == FacingMode.MatchCameraRotation)
        {
            // "billboard" por rotación (ideal para UI World Space)
            rotation = camTransform.rotation;

            if (yawOnly)
            {
                Vector3 e = rotation.eulerAngles;
                rotation = Quaternion.Euler(0f, e.y, 0f);
            }

            if (invertForward)
                rotation *= Quaternion.Euler(0f, 180f, 0f);

            myTransform.rotation = rotation;
        }
        else
        {
            // "billboard" por posición (clásico)
            Vector3 dir = camTransform.position - myTransform.position;

            if (yawOnly) dir.y = 0f;
            if (dir.sqrMagnitude < 0.000001f) return;

            rotation = Quaternion.LookRotation(dir);

            if (invertForward)
                rotation *= Quaternion.Euler(0f, 180f, 0f);

            myTransform.rotation = rotation;
        }

        if (storeState)
        {
            lastCamPos = camTransform.position;
            lastCamRot = camTransform.rotation;
        }
    }

    public void UpdateBillboard()
    {
        InitializeCamera();
        if (camTransform == null) return;

        FaceCamera(true);
    }

    public void SetTargetCamera(Camera newCamera)
    {
        targetCamera = newCamera;
        camTransform = (newCamera != null) ? newCamera.transform : null;
        UpdateBillboard();
    }
}
