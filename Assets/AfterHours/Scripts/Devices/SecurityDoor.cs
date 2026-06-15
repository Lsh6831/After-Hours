using System.Collections;
using UnityEngine;

/// <summary>
/// 잠긴 문 오브젝트를 부드럽게 이동시켜 여는 보안문입니다.
/// </summary>
public class SecurityDoor : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private AudioSource openAudio;

    [Header("문 설정")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f);
    [SerializeField] private float openDuration = 1.5f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen;
    private Coroutine openRoutine;

    private void Awake()
    {
        if (doorTransform == null)
        {
            doorTransform = transform;
        }

        closedPosition = doorTransform.position;
        openPosition = closedPosition + openOffset;
    }

    public void OpenDoor()
    {
        if (isOpen || openRoutine != null)
        {
            return;
        }

        isOpen = true;

        if (openAudio != null)
        {
            openAudio.Play();
        }

        openRoutine = StartCoroutine(OpenDoorRoutine());
    }

    private IEnumerator OpenDoorRoutine()
    {
        // 설정된 시간 동안 닫힌 위치에서 열린 위치까지 보간합니다.
        float elapsedTime = 0f;

        while (elapsedTime < openDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / openDuration);
            doorTransform.position = Vector3.Lerp(closedPosition, openPosition, progress);
            yield return null;
        }

        doorTransform.position = openPosition;
        openRoutine = null;
    }
}
