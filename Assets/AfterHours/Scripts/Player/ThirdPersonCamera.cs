using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// target의 머리 위치를 따라가며 마우스 입력으로 시야 회전을 처리합니다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform target;

    [Header("카메라 설정")]
    [SerializeField] private float sensitivity = 0.1f;
    [SerializeField] private float distance = 0f;
    [SerializeField] private float height = 1.65f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        LockCursor();

        Vector3 currentAngles = transform.eulerAngles;
        yaw = currentAngles.y;
        pitch = NormalizeAngle(currentAngles.x);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        HandleCursorLock();

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            RotateCamera();
        }

        FollowTarget();
    }

    private void HandleCursorLock()
    {
        if (WasEscapePressed())
        {
            UnlockCursor();
        }
    }

    private void RotateCamera()
    {
        Vector2 mouseDelta = GetMouseDelta();

        // 1인칭 시점처럼 마우스가 움직인 방향으로 시야가 향하도록 적용합니다.
        yaw += mouseDelta.x * sensitivity;
        pitch -= mouseDelta.y * sensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void FollowTarget()
    {
        // target의 머리 위치를 기준으로 카메라를 배치합니다.
        // distance가 0이면 플레이어 머리에 붙은 1인칭 카메라처럼 동작합니다.
        Vector3 headPosition = target.position + Vector3.up * height;
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 cameraOffset = cameraRotation * new Vector3(0f, 0f, -distance);

        transform.SetPositionAndRotation(headPosition + cameraOffset, cameraRotation);
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return Vector2.zero;
        }

        return mouse.delta.ReadValue();
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    private bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
