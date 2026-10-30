using UnityEngine;

public class PlayerRagdoll : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody hipsBody;
    [SerializeField] private Rigidbody[] ragdollBodies;
    [SerializeField] private float fallSpeedMultiplier = 0.35f;
    [SerializeField] private float fallUpSpeed = 2.5f;
    [SerializeField] private float getUpDelay = 1.3f;
    [SerializeField] private float getUpBlendTime = 0.3f;

    private Rigidbody rb;
    private Collider[] ragdollColliders;
    private Transform[] bones;
    private Vector3[] savedPositions;
    private Quaternion[] savedRotations;
    private Vector3[] animatedPositions;
    private Quaternion[] animatedRotations;
    private bool isRagdoll = false;
    private bool isDead = false;
    private bool isOwner = false;
    private float getUpTimer = 0f;
    private float blendTimer = -1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ragdollColliders = new Collider[ragdollBodies.Length];
        bones = new Transform[ragdollBodies.Length];
        savedPositions = new Vector3[ragdollBodies.Length];
        savedRotations = new Quaternion[ragdollBodies.Length];
        animatedPositions = new Vector3[ragdollBodies.Length];
        animatedRotations = new Quaternion[ragdollBodies.Length];

        Collider playerCollider = GetComponent<Collider>();
        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            ragdollColliders[i] = ragdollBodies[i].GetComponent<Collider>();
            bones[i] = ragdollBodies[i].transform;
            Physics.IgnoreCollision(playerCollider, ragdollColliders[i]);
        }

        SetRagdoll(false);
    }

    public void Fall(Vector3 force, bool ownsPlayer)
    {
        isOwner = ownsPlayer;
        animator.enabled = false;
        SetRagdoll(true);

        Vector3 fallVelocity = force * fallSpeedMultiplier + Vector3.up * fallUpSpeed;
        foreach (Rigidbody body in ragdollBodies)
        {
            body.linearVelocity = fallVelocity;
        }

        getUpTimer = getUpDelay;
        blendTimer = -1f;
    }

    public void PlayDeath()
    {
        isDead = true;
        if (isRagdoll) return;

        blendTimer = -1f;
        animator.Play("Die", 0, 0f);
    }

    public void PlayRevive()
    {
        if (!isDead) return;

        isDead = false;
        if (isRagdoll)
        {
            getUpTimer = 0f;
            return;
        }

        animator.Play("GetUp", 0, 0f);
    }

    private void Update()
    {
        if (!isRagdoll || isDead) return;

        getUpTimer -= Time.deltaTime;
        if (getUpTimer <= 0f)
        {
            GetUp();
        }
    }

    private void GetUp()
    {
        for (int i = 0; i < bones.Length; i++)
        {
            savedPositions[i] = bones[i].position;
            savedRotations[i] = bones[i].rotation;
        }

        SetRagdoll(false);

        if (isOwner)
        {
            Vector3 landedPosition = hipsBody.worldCenterOfMass;
            landedPosition.y = transform.position.y;
            rb.position = landedPosition;
            transform.position = landedPosition;
        }

        animator.enabled = true;
        animator.Play("GetUp", 0, 0f);
        blendTimer = 0f;
    }

    private void LateUpdate()
    {
        if (blendTimer < 0f) return;

        blendTimer += Time.deltaTime;
        float blend = Mathf.Clamp01(blendTimer / getUpBlendTime);

        for (int i = 0; i < bones.Length; i++)
        {
            animatedPositions[i] = bones[i].position;
            animatedRotations[i] = bones[i].rotation;
        }

        for (int i = 0; i < bones.Length; i++)
        {
            bones[i].position = Vector3.Lerp(savedPositions[i], animatedPositions[i], blend);
            bones[i].rotation = Quaternion.Slerp(savedRotations[i], animatedRotations[i], blend);
        }

        if (blend >= 1f)
        {
            blendTimer = -1f;
        }
    }

    private void SetRagdoll(bool enable)
    {
        isRagdoll = enable;

        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            ragdollBodies[i].isKinematic = !enable;
            ragdollBodies[i].detectCollisions = enable;
            ragdollColliders[i].enabled = enable;
        }
    }
}
