using UnityEngine;

/// <summary>
/// GrabPack으로 잡거나 고정할 수 있는 물리 오브젝트의 상태와 Rigidbody 제어를 담당합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GrabTarget : MonoBehaviour
{
    public enum TargetState
    {
        Idle,
        Grabbed,
        Captured,
        Released,
        Launched
    }

    [Header("참조")]
    [SerializeField] private Rigidbody targetRigidbody;

    [Header("잡기 설정")]
    [SerializeField] private bool canBePulled = true;
    [SerializeField] private bool canPullPlayer = false;

    public TargetState CurrentState { get; private set; } = TargetState.Idle;
    public bool CanBePulled => canBePulled;
    public bool CanPullPlayer => canPullPlayer;

    private Transform currentCapturePoint;

    private void Reset()
    {
        targetRigidbody = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponent<Rigidbody>();
        }
    }

    private void FixedUpdate()
    {
        if (CurrentState != TargetState.Captured || currentCapturePoint == null)
        {
            return;
        }

        // Captured 상태에서는 지정된 고정 위치를 계속 따라갑니다.
        transform.SetPositionAndRotation(currentCapturePoint.position, currentCapturePoint.rotation);
    }

    public void StartGrab()
    {
        if (targetRigidbody == null || CurrentState == TargetState.Captured)
        {
            return;
        }

        CurrentState = TargetState.Grabbed;
        currentCapturePoint = null;

        // 앵커 기둥처럼 고정된 타겟은 잡히더라도 Rigidbody 상태를 움직이지 않게 유지합니다.
        targetRigidbody.isKinematic = !canBePulled;
        targetRigidbody.useGravity = false;
        targetRigidbody.linearVelocity = Vector3.zero;
        targetRigidbody.angularVelocity = Vector3.zero;
    }

    public void StopGrab()
    {
        if (targetRigidbody == null || CurrentState == TargetState.Captured)
        {
            return;
        }

        CurrentState = TargetState.Released;
        currentCapturePoint = null;

        targetRigidbody.isKinematic = !canBePulled;
        targetRigidbody.useGravity = canBePulled;
        targetRigidbody.linearVelocity = Vector3.zero;
        targetRigidbody.angularVelocity = Vector3.zero;
    }

    public void Capture(Transform capturePoint)
    {
        if (targetRigidbody == null || capturePoint == null)
        {
            return;
        }

        CurrentState = TargetState.Captured;
        currentCapturePoint = capturePoint;

        targetRigidbody.linearVelocity = Vector3.zero;
        targetRigidbody.angularVelocity = Vector3.zero;
        targetRigidbody.useGravity = false;
        targetRigidbody.isKinematic = true;

        transform.SetPositionAndRotation(capturePoint.position, capturePoint.rotation);
    }

    public void Release()
    {
        if (targetRigidbody == null)
        {
            return;
        }

        CurrentState = TargetState.Released;
        currentCapturePoint = null;

        targetRigidbody.isKinematic = !canBePulled;
        targetRigidbody.useGravity = canBePulled;
        targetRigidbody.linearVelocity = Vector3.zero;
        targetRigidbody.angularVelocity = Vector3.zero;
    }

    public void Launch(Vector3 force)
    {
        if (targetRigidbody == null)
        {
            return;
        }

        CurrentState = TargetState.Launched;
        currentCapturePoint = null;

        if (!canBePulled)
        {
            targetRigidbody.isKinematic = true;
            targetRigidbody.useGravity = false;
            return;
        }

        targetRigidbody.isKinematic = false;
        targetRigidbody.useGravity = true;
        targetRigidbody.linearVelocity = Vector3.zero;
        targetRigidbody.angularVelocity = Vector3.zero;
        targetRigidbody.AddForce(force, ForceMode.Impulse);
    }
}
