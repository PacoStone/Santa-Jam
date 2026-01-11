using UnityEngine;

public class MagneticLoot : MonoBehaviour
{
    [Header("Magnetismo")]
    public float attractRadius = 6f;
    public float attractSpeed = 10f;

    [Header("Salto inicial")]
    public float jumpHeight = 2f;
    public float jumpDuration = 0.4f;

    [Header("Puntos")]
    public int centsGiven = 1;

    private Transform player;
    private bool isAttracting = false;
    private bool didJump = false;

    private Vector3 jumpStartPos;
    private Vector3 jumpTargetPos;
    private float jumpTimer = 0f;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attractRadius && !isAttracting)
        {
            isAttracting = true;
            StartJump();
        }

        if (!isAttracting) return;

        if (!didJump)
        {
            DoParabolicJump();
        }
        else
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                attractSpeed * Time.deltaTime
            );
        }
    }

    void StartJump()
    {
        jumpStartPos = transform.position;
        jumpTargetPos = player.position;
        jumpTimer = 0f;
        didJump = false;
    }

    void DoParabolicJump()
    {
        jumpTimer += Time.deltaTime;
        float t = jumpTimer / jumpDuration;

        if (t >= 1f)
        {
            didJump = true;
            return;
        }

        Vector3 linearPos = Vector3.Lerp(jumpStartPos, jumpTargetPos, t);

        float height = 4f * jumpHeight * t * (1f - t);
        Vector3 offset = Vector3.up * height;

        transform.position = linearPos + offset;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<PlayerHealthController>()) return;

        ScoreManager.Instance.AddCents(centsGiven);
        Destroy(gameObject);
    }
}
