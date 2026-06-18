using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 좌/우 Grab Pack 팔을 독립적으로 발사해 GrabTarget을 끌어당깁니다.
/// </summary>
public class GrabPackController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CharacterController playerController;
    [SerializeField] private Transform leftMuzzleTransform;
    [SerializeField] private Transform rightMuzzleTransform;
    [SerializeField] private Transform leftGrabHoldPoint;
    [SerializeField] private Transform rightGrabHoldPoint;
    [SerializeField] private LineRenderer leftLineRenderer;
    [SerializeField] private LineRenderer rightLineRenderer;
    [SerializeField] private Transform leftArmVisual;
    [SerializeField] private Transform rightArmVisual;
    [SerializeField] private Transform leftHandVisual;
    [SerializeField] private Transform rightHandVisual;

    [Header("잡기 설정")]
    [SerializeField] private float grabRange = 8f;
    [SerializeField] private float pullForce = 35f;
    [SerializeField] private float breakDistance = 12f;
    [SerializeField] private float cooldown = 0.25f;

    [Header("팔 발사 비주얼")]
    [SerializeField] private float armExtendSpeed = 22f;
    [SerializeField] private float armRetractSpeed = 26f;
    [SerializeField] private float armRadius = 0.06f;
    [SerializeField] private float handVisualSize = 0.22f;

    [Header("플레이어 끌림 설정")]
    [SerializeField] private bool enablePlayerPull = true;
    [SerializeField] private float playerPullDelay = 2f;
    [SerializeField] private float playerPullSpeed = 8f;
    [SerializeField] private float playerPullStopDistance = 1.5f;

    private readonly GrabArm leftArm = new GrabArm();
    private readonly GrabArm rightArm = new GrabArm();

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponent<CharacterController>();
        }

        leftArm.Initialize(leftMuzzleTransform, leftGrabHoldPoint, leftLineRenderer, leftArmVisual, leftHandVisual);
        rightArm.Initialize(rightMuzzleTransform, rightGrabHoldPoint, rightLineRenderer, rightArmVisual, rightHandVisual);

        leftArm.HideVisuals();
        rightArm.HideVisuals();
    }

    private void Update()
    {
        if (cameraTransform == null)
        {
            ReleaseArm(leftArm);
            ReleaseArm(rightArm);
            return;
        }

        UpdateArm(leftArm, rightArm, IsLeftGrabPressed(), WasLeftGrabPressed(), WasLeftGrabReleased());
        UpdateArm(rightArm, leftArm, IsRightGrabPressed(), WasRightGrabPressed(), WasRightGrabReleased());
    }

    private void FixedUpdate()
    {
        PullArm(leftArm);
        PullArm(rightArm);
        PullPlayerToGrabbedTarget();
    }

    private void UpdateArm(GrabArm arm, GrabArm otherArm, bool isPressed, bool wasPressed, bool wasReleased)
    {
        if (!arm.HasRequiredReferences)
        {
            ReleaseArm(arm);
            return;
        }

        if (wasPressed)
        {
            TryFireArm(arm, otherArm);
        }

        if (wasReleased)
        {
            ReleaseArm(arm);
        }

        UpdateArmFlight(arm, isPressed);
        CheckBreakDistance(arm);
        arm.UpdateVisuals(armRadius, handVisualSize);
    }

    private void TryFireArm(GrabArm arm, GrabArm otherArm)
    {
        if (arm.IsBusy || Time.time < arm.NextGrabTime)
        {
            return;
        }

        Vector3 targetPoint = arm.MuzzleTransform.position + cameraTransform.forward * grabRange;
        GrabTarget target = null;
        Rigidbody targetRigidbody = null;

        if (Physics.Raycast(arm.MuzzleTransform.position, cameraTransform.forward, out RaycastHit hitInfo, grabRange))
        {
            targetPoint = hitInfo.point;
            target = hitInfo.collider.GetComponentInParent<GrabTarget>();

            if (target == null || target.CurrentState == GrabTarget.TargetState.Captured || target.CurrentState == GrabTarget.TargetState.Grabbed || otherArm.CurrentTarget == target || otherArm.PendingTarget == target || !target.TryGetComponent(out targetRigidbody))
            {
                target = null;
                targetRigidbody = null;
            }
        }

        arm.BeginFire(targetPoint, target, targetRigidbody);
    }

    private void UpdateArmFlight(GrabArm arm, bool isPressed)
    {
        if (!arm.IsBusy)
        {
            return;
        }

        if (arm.CurrentState == GrabArm.ArmState.Extending)
        {
            Vector3 destination = arm.PendingTarget != null ? arm.PendingTarget.transform.position : arm.TargetPoint;
            arm.HandPosition = Vector3.MoveTowards(arm.HandPosition, destination, armExtendSpeed * Time.deltaTime);

            if (Vector3.Distance(arm.HandPosition, destination) <= 0.05f)
            {
                if (arm.PendingTarget != null && isPressed)
                {
                    arm.AttachPendingTarget();
                    return;
                }

                arm.StartRetract();
            }
        }

        if (arm.CurrentState == GrabArm.ArmState.Attached)
        {
            if (arm.CurrentTarget != null)
            {
                arm.HandPosition = arm.CurrentTarget.transform.position;
            }

            return;
        }

        if (arm.CurrentState == GrabArm.ArmState.Retracting)
        {
            arm.HandPosition = Vector3.MoveTowards(arm.HandPosition, arm.MuzzleTransform.position, armRetractSpeed * Time.deltaTime);

            if (Vector3.Distance(arm.HandPosition, arm.MuzzleTransform.position) <= 0.05f)
            {
                arm.FinishRetract();
            }
        }
    }

    private void ReleaseArm(GrabArm arm)
    {
        if (arm.CurrentTarget != null)
        {
            arm.CurrentTarget.StopGrab();
            arm.NextGrabTime = Time.time + cooldown;
        }

        arm.CurrentTarget = null;
        arm.CurrentRigidbody = null;
        arm.PendingTarget = null;
        arm.PendingRigidbody = null;
        arm.GrabStartTime = 0f;
        arm.StartRetract();
    }

    private void PullArm(GrabArm arm)
    {
        if (arm.CurrentTarget == null || arm.CurrentRigidbody == null || arm.GrabHoldPoint == null)
        {
            return;
        }

        if (!arm.CurrentTarget.CanBePulled)
        {
            return;
        }

        // 각 손의 HoldPoint 방향으로 독립적인 힘을 더합니다.
        Vector3 pullDirection = arm.GrabHoldPoint.position - arm.CurrentRigidbody.position;
        arm.CurrentRigidbody.AddForce(pullDirection.normalized * pullForce, ForceMode.Acceleration);
    }

    private void PullPlayerToGrabbedTarget()
    {
        if (!enablePlayerPull || playerController == null)
        {
            return;
        }

        Vector3 combinedDirection = Vector3.zero;
        int pullCount = 0;

        AddPlayerPullDirection(leftArm, ref combinedDirection, ref pullCount);
        AddPlayerPullDirection(rightArm, ref combinedDirection, ref pullCount);

        if (pullCount == 0 || combinedDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        // 두 손이 동시에 플레이어를 끌면 방향을 평균내서 급격한 흔들림을 줄입니다.
        Vector3 moveDirection = combinedDirection.normalized;
        playerController.Move(moveDirection * playerPullSpeed * Time.fixedDeltaTime);
    }

    private void AddPlayerPullDirection(GrabArm arm, ref Vector3 combinedDirection, ref int pullCount)
    {
        if (arm.CurrentTarget == null)
        {
            return;
        }

        if (!arm.CurrentTarget.CanPullPlayer)
        {
            return;
        }

        if (Time.time - arm.GrabStartTime < playerPullDelay)
        {
            return;
        }

        Vector3 targetPosition = arm.CurrentTarget.transform.position;
        Vector3 playerPosition = playerController.transform.position;
        Vector3 directionToTarget = targetPosition - playerPosition;

        if (directionToTarget.magnitude <= playerPullStopDistance)
        {
            return;
        }

        // 플레이어가 타겟 쪽으로 끌려가도록 방향을 누적합니다.
        combinedDirection += directionToTarget.normalized;
        pullCount++;
    }

    private void CheckBreakDistance(GrabArm arm)
    {
        if (arm.CurrentTarget == null || arm.MuzzleTransform == null)
        {
            return;
        }

        float distanceToTarget = Vector3.Distance(arm.MuzzleTransform.position, arm.CurrentTarget.transform.position);
        if (distanceToTarget > breakDistance)
        {
            ReleaseArm(arm);
        }
    }

    private bool IsLeftGrabPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    private bool WasLeftGrabPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private bool WasLeftGrabReleased()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(0);
#endif
    }

    private bool IsRightGrabPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.rightButton.isPressed;
#else
        return Input.GetMouseButton(1);
#endif
    }

    private bool WasRightGrabPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.rightButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(1);
#endif
    }

    private bool WasRightGrabReleased()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.rightButton.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(1);
#endif
    }

    private class GrabArm
    {
        public enum ArmState
        {
            Idle,
            Extending,
            Attached,
            Retracting
        }

        public Transform MuzzleTransform { get; private set; }
        public Transform GrabHoldPoint { get; private set; }
        public GrabTarget CurrentTarget { get; set; }
        public Rigidbody CurrentRigidbody { get; set; }
        public GrabTarget PendingTarget { get; set; }
        public Rigidbody PendingRigidbody { get; set; }
        public Vector3 TargetPoint { get; private set; }
        public Vector3 HandPosition { get; set; }
        public ArmState CurrentState { get; private set; }
        public float NextGrabTime { get; set; }
        public float GrabStartTime { get; set; }

        private LineRenderer lineRenderer;
        private Transform armVisual;
        private Transform handVisual;

        public bool HasRequiredReferences => MuzzleTransform != null && GrabHoldPoint != null && armVisual != null && handVisual != null;
        public bool IsBusy => CurrentState != ArmState.Idle || CurrentTarget != null || PendingTarget != null;

        public void Initialize(Transform muzzleTransform, Transform grabHoldPoint, LineRenderer grabLineRenderer, Transform armVisualTransform, Transform handVisualTransform)
        {
            MuzzleTransform = muzzleTransform;
            GrabHoldPoint = grabHoldPoint;
            lineRenderer = grabLineRenderer;
            armVisual = armVisualTransform;
            handVisual = handVisualTransform;
            HandPosition = MuzzleTransform != null ? MuzzleTransform.position : Vector3.zero;
        }

        public void BeginFire(Vector3 targetPoint, GrabTarget pendingTarget, Rigidbody pendingRigidbody)
        {
            TargetPoint = targetPoint;
            PendingTarget = pendingTarget;
            PendingRigidbody = pendingRigidbody;
            CurrentState = ArmState.Extending;
            HandPosition = MuzzleTransform.position;
            ShowVisuals();
        }

        public void AttachPendingTarget()
        {
            CurrentTarget = PendingTarget;
            CurrentRigidbody = PendingRigidbody;
            PendingTarget = null;
            PendingRigidbody = null;
            GrabStartTime = Time.time;
            CurrentState = ArmState.Attached;

            CurrentTarget.StartGrab();
        }

        public void StartRetract()
        {
            if (CurrentState == ArmState.Idle)
            {
                return;
            }

            CurrentState = ArmState.Retracting;
        }

        public void FinishRetract()
        {
            CurrentState = ArmState.Idle;
            PendingTarget = null;
            PendingRigidbody = null;
            HideVisuals();
        }

        public void ShowVisuals()
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
                lineRenderer.positionCount = 0;
            }

            if (armVisual != null)
            {
                armVisual.gameObject.SetActive(true);
            }

            if (handVisual != null)
            {
                handVisual.gameObject.SetActive(true);
            }
        }

        public void HideVisuals()
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
                lineRenderer.positionCount = 0;
            }

            if (armVisual != null)
            {
                armVisual.gameObject.SetActive(false);
            }

            if (handVisual != null)
            {
                handVisual.gameObject.SetActive(false);
            }
        }

        public void UpdateVisuals(float armRadius, float handSize)
        {
            if (armVisual == null || handVisual == null || MuzzleTransform == null || CurrentState == ArmState.Idle)
            {
                return;
            }

            Vector3 start = MuzzleTransform.position;
            Vector3 end = HandPosition;
            Vector3 difference = end - start;
            float length = difference.magnitude;

            if (length > 0.001f)
            {
                armVisual.position = start + difference * 0.5f;
                armVisual.rotation = Quaternion.FromToRotation(Vector3.up, difference.normalized);
                armVisual.localScale = new Vector3(armRadius, length * 0.5f, armRadius);
            }

            handVisual.position = end;
            handVisual.localScale = Vector3.one * handSize;
        }
    }
}
