using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 현재 미션 목표를 순서대로 보여주고 완료되면 다음 목표로 넘깁니다.
/// </summary>
public class MissionManager : MonoBehaviour
{
    [Serializable]
    private class MissionStep
    {
        [SerializeField] private string objectiveId;
        [SerializeField] private string title;
        [TextArea]
        [SerializeField] private string description;
        [TextArea]
        [SerializeField] private string completionMessage;

        public string ObjectiveId => objectiveId;
        public string Title => title;
        public string Description => description;
        public string CompletionMessage => completionMessage;
    }

    [Header("참조")]
    [SerializeField] private UIManager uiManager;

    [Header("미션 목록")]
    [SerializeField] private MissionStep[] missionSteps;
    [SerializeField] private float nextMissionDelay = 1.4f;

    private int currentMissionIndex;
    private Coroutine advanceRoutine;

    private void Start()
    {
        ShowCurrentMission();
    }

    public void CompleteMission(string objectiveId)
    {
        if (missionSteps == null || currentMissionIndex >= missionSteps.Length)
        {
            return;
        }

        MissionStep currentMission = missionSteps[currentMissionIndex];
        if (currentMission == null || currentMission.ObjectiveId != objectiveId)
        {
            return;
        }

        if (advanceRoutine != null)
        {
            return;
        }

        advanceRoutine = StartCoroutine(AdvanceMissionRoutine(currentMission));
    }

    private IEnumerator AdvanceMissionRoutine(MissionStep completedMission)
    {
        if (uiManager != null)
        {
            uiManager.ShowCompleted(completedMission.CompletionMessage);
        }

        yield return new WaitForSeconds(nextMissionDelay);

        currentMissionIndex++;
        advanceRoutine = null;
        ShowCurrentMission();
    }

    private void ShowCurrentMission()
    {
        if (uiManager == null)
        {
            Debug.LogWarning($"{name}에 UIManager가 연결되지 않았습니다.");
            return;
        }

        if (missionSteps == null || currentMissionIndex >= missionSteps.Length)
        {
            uiManager.ShowObjective("탈출 준비 완료", "모든 목표를 완료했습니다. Airlock을 지나 탈출하세요.");
            return;
        }

        MissionStep currentMission = missionSteps[currentMissionIndex];
        if (currentMission == null)
        {
            return;
        }

        uiManager.ShowObjective(currentMission.Title, currentMission.Description);
    }
}
