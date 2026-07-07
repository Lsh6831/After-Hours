using UnityEngine;

/// <summary>
/// 플레이어가 목표 지점에 들어오면 MissionManager에 목표 완료를 알립니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MissionObjectiveTrigger : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private MissionManager missionManager;

    [Header("목표 설정")]
    [SerializeField] private string objectiveId;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        hasTriggered = true;

        if (missionManager != null)
        {
            missionManager.CompleteMission(objectiveId);
        }
    }
}
