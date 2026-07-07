using System.Collections;
using UnityEngine;

/// <summary>
/// 잠긴 문 오브젝트를 부드럽게 이동시켜 열고 닫는 보안문입니다.
/// </summary>
public class SecurityDoor : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private AudioSource openAudio;

    [Header("문 설정")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f);
    [SerializeField] private float openDuration = 1.5f;
    [SerializeField] private float closeDuration = 0.35f;
    [SerializeField] private bool startOpened;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen;
    private Coroutine moveRoutine;

    private void Awake()
    {
        if (doorTransform == null)
        {
            doorTransform = transform;
        }

        closedPosition = doorTransform.position;
        openPosition = closedPosition + openOffset;

        if (startOpened)
        {
            doorTransform.position = openPosition;
            isOpen = true;
        }
    }

    public void OpenDoor()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;

        if (openAudio != null)
        {
            openAudio.Play();
        }

        StartMoveRoutine(openPosition, openDuration);
    }

    public void CloseDoor()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        StartMoveRoutine(closedPosition, closeDuration);
    }

    private void StartMoveRoutine(Vector3 targetPosition, float duration)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveDoorRoutine(targetPosition, duration));
    }

    private IEnumerator MoveDoorRoutine(Vector3 targetPosition, float duration)
    {
        // 현재 위치에서 목표 위치까지 보간해 열림/닫힘을 모두 처리합니다.
        Vector3 startPosition = doorTransform.position;
        float elapsedTime = 0f;
        float safeDuration = Mathf.Max(duration, 0.01f);

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / safeDuration);
            doorTransform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }

        doorTransform.position = targetPosition;
        moveRoutine = null;
    }
}
