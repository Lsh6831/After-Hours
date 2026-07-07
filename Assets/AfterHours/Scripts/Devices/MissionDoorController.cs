using UnityEngine;

/// <summary>
/// 지정한 미션이 완료되면 연결된 문을 엽니다.
/// </summary>
public class MissionDoorController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private SecurityDoor securityDoor;

    [Header("미션 설정")]
    [SerializeField] private string openObjectiveId;

    private void OnEnable()
    {
        if (missionManager != null)
        {
            missionManager.MissionCompleted += HandleMissionCompleted;
        }
    }

    private void OnDisable()
    {
        if (missionManager != null)
        {
            missionManager.MissionCompleted -= HandleMissionCompleted;
        }
    }

    private void HandleMissionCompleted(string objectiveId)
    {
        if (securityDoor == null || objectiveId != openObjectiveId)
        {
            return;
        }

        securityDoor.OpenDoor();
    }
}
