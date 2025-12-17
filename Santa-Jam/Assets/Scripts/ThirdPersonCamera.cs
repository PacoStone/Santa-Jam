using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomLerpSpeed = 10f; // smooth
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 15f;

    private Inputs inputs;
    private CinemachineCamera cam;
    private CinemachineThirdPersonFollow thirdPerson;
    private Vector2 scrollDelta;

    private float targetZoom;
    private float currentZoom;

    void Start()
    {
        inputs = new Inputs();
        inputs.Enable();
        inputs.Jugador.Look.performed += HandheldZoomScroll;
        inputs.Jugador.Look.canceled += HandheldZoomScroll;

        Cursor.lockState = CursorLockMode.Locked;

        cam = GetComponent<CinemachineCamera>();
        thirdPerson = GetComponent<CinemachineThirdPersonFollow>();

        if (cam == null || thirdPerson == null)
        {
            Debug.LogError("ThirdPersonCamera: Falta CinemachineCamera o CinemachineThirdPersonFollow.");
            enabled = false;
            return;
        }

        targetZoom = currentZoom = thirdPerson.CameraDistance;
        targetZoom = Mathf.Clamp(targetZoom, minDistance, maxDistance);
        currentZoom = targetZoom;
    }

    private void OnDisable()
    {
        if (inputs != null)
        {
            inputs.Jugador.Look.performed -= HandheldZoomScroll;
            inputs.Jugador.Look.canceled -= HandheldZoomScroll;
            inputs.Disable();
        }
    }

    private void HandheldZoomScroll(InputAction.CallbackContext context)
    {
        scrollDelta = context.ReadValue<Vector2>();
        // Debug.Log("Zoom Input: " + scrollDelta);
    }

    void Update()
    {
        if (thirdPerson == null) return;

        if (scrollDelta.y != 0f)
        {
            // Si el zoom va al revés, cambia "-=" por "+="
            targetZoom -= scrollDelta.y * zoomSpeed * Time.deltaTime;
            targetZoom = Mathf.Clamp(targetZoom, minDistance, maxDistance);
        }

        currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomLerpSpeed);
        thirdPerson.CameraDistance = currentZoom;
    }
}
