using UnityEngine;

[DefaultExecutionOrder(-50)]
public class PlayerHoldPose : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform rightArm;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Vector3 leftHandTip = new Vector3(-0.26f, 0f, 0f);
    [SerializeField] private Vector3 rightHandTip = new Vector3(0.26f, 0f, 0f);
    [SerializeField] private float holdHeight = 0.55f;
    [SerializeField] private float bodyFront = 0.3f;
    [SerializeField] private float minHandSpread = 0.1f;
    [SerializeField] private float maxHandSpread = 0.42f;
    [SerializeField] private float blendSpeed = 8f;
    [SerializeField] private float swingBlendSpeed = 14f;

    private PlayerInteraction playerInteraction;
    private float holdWeight = 0f;
    private float rightArmWeight = 1f;

    private void Awake()
    {
        playerInteraction = GetComponent<PlayerInteraction>();
    }

    private void LateUpdate()
    {
        HoldableItem heldItem = playerInteraction.GetHeldItem();
        bool isHolding = heldItem != null && animator.enabled;

        holdWeight = Mathf.MoveTowards(holdWeight, isHolding ? 1f : 0f, blendSpeed * Time.deltaTime);
        rightArmWeight = Mathf.MoveTowards(rightArmWeight, IsSwinging() ? 0f : 1f, swingBlendSpeed * Time.deltaTime);

        if (!isHolding || holdWeight <= 0f) return;

        Bounds itemBounds = heldItem.GetLocalBounds();
        Vector3 itemCenter = transform.position + Vector3.up * holdHeight + transform.forward * (bodyFront + itemBounds.extents.z);
        float handSpread = Mathf.Clamp(itemBounds.extents.x, minHandSpread, maxHandSpread);
        Vector3 gripPoint = itemCenter - transform.forward * itemBounds.extents.z * 0.5f;

        Quaternion animatedRightArm = rightArm.rotation;
        Quaternion leftHoldRotation = GetArmRotation(leftArm, leftHandTip, gripPoint - transform.right * handSpread);
        Quaternion rightHoldRotation = GetArmRotation(rightArm, rightHandTip, gripPoint + transform.right * handSpread);

        Vector3 holdPosition = itemCenter - transform.rotation * itemBounds.center;
        Vector3 itemLocalPosition = Quaternion.Inverse(rightHoldRotation) * (holdPosition - rightArm.position);
        Quaternion itemLocalRotation = Quaternion.Inverse(rightHoldRotation) * transform.rotation;

        leftArm.rotation = Quaternion.Slerp(leftArm.rotation, leftHoldRotation, holdWeight);
        rightArm.rotation = Quaternion.Slerp(animatedRightArm, rightHoldRotation, holdWeight * rightArmWeight);

        holdPoint.SetPositionAndRotation(rightArm.position + rightArm.rotation * itemLocalPosition, rightArm.rotation * itemLocalRotation);
    }

    private Quaternion GetArmRotation(Transform arm, Vector3 handTip, Vector3 target)
    {
        Vector3 currentDirection = arm.TransformPoint(handTip) - arm.position;
        Vector3 targetDirection = target - arm.position;

        return Quaternion.FromToRotation(currentDirection, targetDirection) * arm.rotation;
    }

    private bool IsSwinging()
    {
        if (!animator.enabled) return false;

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Slap")) return true;

        return animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Slap");
    }
}
