using UnityEngine;
using Unity.Cinemachine;

public class SpriteBillboard : MonoBehaviour
{
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private bool yOnly = true;

    void LateUpdate()
    {
        Camera cam = brain != null ? brain.OutputCamera : Camera.main;
        if (cam == null) return;

        Vector3 direction = cam.transform.position - transform.position;

        if (yOnly)
            direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction);
    }
}
