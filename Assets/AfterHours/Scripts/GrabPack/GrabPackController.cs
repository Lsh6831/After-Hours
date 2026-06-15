using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 카메라 중앙 Raycast로 GrabTarget을 감지하고, 좌클릭 중 대상 오브젝트를 끌어당깁니다.
/// </summary>
public class GrabPackController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform muzzleTransform;
    [SerializeField] private Transform grabHoldPoint;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("잡기 설정")]
    [SerializeField] private float grabRange = 8f;
    [SerializeField] private float pullForce = 35f;
    [SerializeField] private float breakDistance = 12f;
    [SerializeField] private float cooldown = 0.25f;

    private GrabTarget currentTarget;
    private Rigidbody currentRigidbody;
    private float nextGrabTime;

    private void Awake()
    {
        DisableLine();
    }

    private void Update()
    {
        if (cameraTransform == null || muzzleTransform == null || grabHoldPoint == null)
        {
            ReleaseCurrentTarget();
            return;
        }

        if (IsGrabPressed())
        {
            TryStartGrab();
        }

        if (WasGrabReleased())
        {
            ReleaseCurrentTarget();
        }

        UpdateLine();
        CheckBreakDistance();
    }

    private void FixedUpdate()
    {
        if (currentTarget == null || currentRigidbody == null || grabHoldPoint == null)
        {
            return;
        }

        // HoldPoint 방향으로 힘을 더해 자연스럽게 끌려오도록 합니다.
        Vector3 pullDirection = grabHoldPoint.position - currentRigidbody.position;
        currentRigidbody.AddForce(pullDirection.normalized * pullForce, ForceMode.Acceleration);
    }

    private void TryStartGrab()
    {
        if (currentTarget != null || Time.time < nextGrabTime)
        {
            return;
        }

        if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hitInfo, grabRange))
        {
            return;
        }

        GrabTarget target = hitInfo.collider.GetComponentInParent<GrabTarget>();
        if (target == null || target.CurrentState == GrabTarget.TargetState.Captured)
        {
            return;
        }

        if (!target.TryGetComponent(out Rigidbody targetRigidbody))
        {
            return;
        }

        currentTarget = target;
        currentRigidbody = targetRigidbody;

        currentTarget.StartGrab();
        EnableLine();
        UpdateLine();
    }

    private void ReleaseCurrentTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.StopGrab();
            nextGrabTime = Time.time + cooldown;
        }

        currentTarget = null;
        currentRigidbody = null;
        DisableLine();
    }

    private void CheckBreakDistance()
    {
        if (currentTarget == null || muzzleTransform == null)
        {
            return;
        }

        float distanceToTarget = Vector3.Distance(muzzleTransform.position, currentTarget.transform.position);
        if (distanceToTarget > breakDistance)
        {
            ReleaseCurrentTarget();
        }
    }

    private void EnableLine()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
    }

    private void DisableLine()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }

    private void UpdateLine()
    {
        if (lineRenderer == null || currentTarget == null || muzzleTransform == null)
        {
            return;
        }

        lineRenderer.SetPosition(0, muzzleTransform.position);
        lineRenderer.SetPosition(1, currentTarget.transform.position);
    }

    private bool IsGrabPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    private bool WasGrabReleased()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(0);
#endif
    }
}
