using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현재 체크포인트를 저장하고, 낙하 시 암전 후 플레이어를 체크포인트로 되돌립니다.
/// </summary>
public class CheckpointRespawnManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private CharacterController playerController;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Image fadeImage;
    [SerializeField] private Text wakeUpText;

    [Header("리스폰 설정")]
    [SerializeField] private Transform startingSpawnPoint;
    [SerializeField] private float fadeOutDuration = 0.45f;
    [SerializeField] private float sleepDuration = 0.65f;
    [SerializeField] private float fadeInDuration = 1.15f;
    [SerializeField] private string wakeUpMessage = "눈을 뜨는 중...";

    private Transform currentSpawnPoint;
    private bool isRespawning;

    private void Start()
    {
        if (startingSpawnPoint != null)
        {
            SetCheckpoint(startingSpawnPoint);
        }

        SetFadeAlpha(0f);
        SetWakeUpTextVisible(false);
    }

    public void SetCheckpoint(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            return;
        }

        currentSpawnPoint = spawnPoint;
    }

    public void RespawnPlayer()
    {
        if (isRespawning || currentSpawnPoint == null || playerTransform == null)
        {
            return;
        }

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        yield return FadeRoutine(0f, 1f, fadeOutDuration);
        SetWakeUpTextVisible(true);

        yield return new WaitForSeconds(sleepDuration);

        TeleportPlayerToCheckpoint();

        yield return FadeRoutine(1f, 0f, fadeInDuration);
        SetWakeUpTextVisible(false);

        isRespawning = false;
    }

    private void TeleportPlayerToCheckpoint()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        playerTransform.SetPositionAndRotation(currentSpawnPoint.position, currentSpawnPoint.rotation);

        if (playerMovement != null)
        {
            playerMovement.ResetVerticalVelocity();
        }

        if (playerController != null)
        {
            playerController.enabled = true;
        }
    }

    private IEnumerator FadeRoutine(float startAlpha, float targetAlpha, float duration)
    {
        if (fadeImage == null)
        {
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / Mathf.Max(duration, 0.01f));
            SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, progress));
            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
        fadeImage.raycastTarget = alpha > 0.01f;
    }

    private void SetWakeUpTextVisible(bool isVisible)
    {
        if (wakeUpText == null)
        {
            return;
        }

        wakeUpText.text = wakeUpMessage;
        wakeUpText.enabled = isVisible;
    }
}
