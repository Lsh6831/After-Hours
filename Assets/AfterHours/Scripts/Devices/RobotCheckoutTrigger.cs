using UnityEngine;

/// <summary>
/// 퇴근하지 않은 로봇을 창문 밖 처리 구역으로 보내면 미션을 완료합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RobotCheckoutTrigger : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private MissionManager missionManager;

    [Header("미션 설정")]
    [SerializeField] private string robotObjectName = "Overtime_Robot";
    [SerializeField] private string completionObjectiveId = "checkout_robot";

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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasCompleted || missionManager == null)
        {
            return;
        }

        Transform root = other.transform.root;
        if (!root.name.Contains(robotObjectName))
        {
            return;
        }

        hasCompleted = true;
        missionManager.CompleteMission(completionObjectiveId);
    }
}
