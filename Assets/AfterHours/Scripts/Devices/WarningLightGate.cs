using UnityEngine;

/// <summary>
/// 켜졌다 꺼지는 점검등 구역입니다. 불이 켜진 상태에 들어오면 리스폰시키고, 꺼진 상태로 통과하면 미션을 완료합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WarningLightGate : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Light warningLight;
    [SerializeField] private Renderer warningRenderer;
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private CheckpointRespawnManager respawnManager;

    [Header("미션 설정")]
    [SerializeField] private string completionObjectiveId = "pass_warning_lights";

    [Header("점멸 설정")]
    [SerializeField] private float lightOnDuration = 1.4f;
    [SerializeField] private float lightOffDuration = 1.1f;
    [SerializeField] private Color lightOnColor = new Color(1f, 0.1f, 0.05f);
    [SerializeField] private Color lightOffColor = new Color(0.08f, 0.08f, 0.08f);

    private float timer;
    private bool isLightOn = true;
    private bool hasCompleted;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        ApplyLightState();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float duration = isLightOn ? lightOnDuration : lightOffDuration;

        if (timer < duration)
        {
            return;
        }

        timer = 0f;
        isLightOn = !isLightOn;
        ApplyLightState();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (isLightOn)
        {
            if (respawnManager != null)
            {
                respawnManager.RespawnPlayer();
            }

            return;
        }

        if (!hasCompleted && missionManager != null)
        {
            hasCompleted = true;
            missionManager.CompleteMission(completionObjectiveId);
        }
    }

    private void ApplyLightState()
    {
        Color stateColor = isLightOn ? lightOnColor : lightOffColor;

        if (warningLight != null)
        {
            warningLight.enabled = isLightOn;
            warningLight.color = lightOnColor;
        }

        if (warningRenderer != null)
        {
            Material material = Application.isPlaying ? warningRenderer.material : warningRenderer.sharedMaterial;
            if (material == null)
            {
                return;
            }

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", isLightOn ? lightOnColor * 2f : Color.black);

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", stateColor);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", stateColor);
            }
        }
    }
}
